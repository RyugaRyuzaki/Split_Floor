
#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Application = Autodesk.Revit.ApplicationServices.Application;
#endregion

namespace SplitFloor
{
    [Transaction(TransactionMode.Manual)]
    public class RevitCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData,
            ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // chon Floor
            try
            {
                Reference refFloor = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new FloorFilter()," Choose Floor" );
                // Floor Elelment
                Floor floor = doc.GetElement(refFloor) as Floor;
                // FloorType
                FloorType floorType = GetFloorType(doc, floor);
                // Level
                Level level = GetFloorLevel(doc, floor);
                // bool structural
                bool structural = floor.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL).AsInteger() == 1;
                
                try
                {
                    List<Reference> refModlCurve = uidoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element, new ModelCurveFilter(), "Choose ModelCurves").ToList();
                    // các floor phải ko slope
                    //các modelcurve phải nằm trên mp của top floor

                    PlanarFace top = GetPlanarFaceTop(floor);
                    List<Curve> allModelCurve = GetAllModelCurve(doc,refModlCurve,top);
                    if (allModelCurve.Count==0)
                    {
                        System.Windows.Forms.MessageBox.Show("Please choose ModelCurve are same Plan of Floor");
                        return Result.Cancelled;
                    }
                    if (top != null)
                    {
                        double a = DateTime.Now.Second;
                        EdgeArrayArray edgeArrayArray = top.EdgeLoops;
                        if (edgeArrayArray.Size > 1)
                        {
                            System.Windows.Forms.MessageBox.Show("Please choose floor with only one EdgeArrayArray");
                            return Result.Cancelled;
                        }
                        List<Curve> curvesFloor = GetCurveFloor(edgeArrayArray);
                       
                        if (allModelCurve.Count == 1)
                        {
                            CreateSplitFloorOneModelCurve(doc, floorType, structural, level,  floor,curvesFloor, allModelCurve);
                        }
                        else
                        {
                            //allModelCurve = allModelCurve.Where(x => GetNumberPointIntersectOneCModelCurve(curvesFloor, x)).ToList();

                            CreateSplitFloorMultipleModelCurve(doc, floorType, structural, level, floor, GetAllListListCurvesMultipleModelCurveNoneIntersect(curvesFloor, allModelCurve));
                           
                        }
                        double b = DateTime.Now.Second;
                        System.Windows.Forms.MessageBox.Show("Test"+(b )+" "+a);
                        return Result.Succeeded;
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show("Please Choose the floor with none-Slope");
                        return Result.Cancelled;
                    }


                }
                catch (Exception e)
                {

                    System.Windows.Forms.MessageBox.Show(e.Message + "1");
                    return Result.Cancelled;
                }

            }
            catch (Exception e)
            {

                System.Windows.Forms.MessageBox.Show(e.Message);
                return Result.Cancelled;
            }
        }

        #region     OneModelCurves
        private void CreateSplitFloorOneModelCurve(Document doc, FloorType floorType, bool structural, Level level, Floor floor,List<Curve> curvesFloor,  List<Curve> allModelCurve)
        {
            List<XYZ> AllInterSectPoint;
            List<Curve> CurveIntersect0;
            List<Curve> CurveNoneIntersect;
            GetNumberPointIntersectofListCurve(curvesFloor, allModelCurve[0], out AllInterSectPoint, out CurveIntersect0, out CurveNoneIntersect);
            List<Curve> CurveIntersect = GetListCurveIntersecWithPoint(AllInterSectPoint, CurveIntersect0);
            List<Curve> Left;
            List<Curve> Right;
            SplitCurveLeftOrRight(CurveIntersect, CurveNoneIntersect, allModelCurve[0], out Left, out Right, AllInterSectPoint);
            CurveArray curveArray = GetCurveArrayFromCurve(Left);
            //foreach (var item in Right)
            //{
            //    System.Windows.Forms.MessageBox.Show(item.GetEndPoint(0).ToString() + "\n" + item.GetEndPoint(1).ToString());
            //}

            CurveArray curveArray1 = GetCurveArrayFromCurve(Right);

            using (Transaction tran = new Transaction(doc))
            {
                tran.Start("aaa");
                ICollection<ElementId> deletes = doc.Delete(floor.Id);
                try
                {
                    Floor floor0 = doc.Create.NewFloor(curveArray, floorType, level, structural, XYZ.BasisZ);
                    Floor floor1 = doc.Create.NewFloor(curveArray1, floorType, level, structural, XYZ.BasisZ);
                }
                catch (Exception e)
                {

                    System.Windows.Forms.MessageBox.Show(e.Message);
                }


                tran.Commit();
            }
        }
        // chu y doan nay
        #endregion





        private void GetNumberPointIntersectofListCurve(List<Curve> curvesFloor, Curve modelCurve, out List<XYZ> AllInterSectPoint, out List<Curve> curveIntersect, out List<Curve> curveNoneIntersect)
        {
            AllInterSectPoint = new List<XYZ>();
            curveIntersect = new List<Curve>();
            curveNoneIntersect = new List<Curve>();
            List<IntersectionResultArray> intersectionResultArray = new List< IntersectionResultArray>();
            
            foreach (var item in curvesFloor)
            {
                IntersectionResultArray inter = new IntersectionResultArray();
                
                if (item.Intersect(modelCurve,out inter) ==SetComparisonResult.Overlap)
                {
                    if (inter.Size != 0) { intersectionResultArray.Add(inter); curveIntersect.Add(item); }
                    
                }
                else
                {
                    curveNoneIntersect.Add(item);

                }
               
            }
            foreach (var item in intersectionResultArray)
            {
                foreach (var item1 in item)
                {
                    IntersectionResult result = item1 as IntersectionResult;
                    if (result != null)
                    {
                        AllInterSectPoint.Add(result.XYZPoint);
                    }
                }
            }
            AllInterSectPoint= AllInterSectPoint.Distinct(new DistinctXYZ()).ToList();
        }
       
        private List<Curve> GetListCurveIntersecWithPoint( List<XYZ> AllInterSectPoint,  List<Curve> curveIntersect)
        {
            List<Curve> curves = new List<Curve>();
            for (int i = 0; i < AllInterSectPoint.Count; i++)
            {
                for (int j = 0; j < curveIntersect.Count; j++)
                {
                    XYZ pro = PointModel.ProjectToLine(AllInterSectPoint[i], (curveIntersect[j] as Line));
                    if(AreEqual(pro.DistanceTo(AllInterSectPoint[i]),0))
                    {
                        if (IsLapXYZ(AllInterSectPoint[i],curveIntersect[j].GetEndPoint(0))|| IsLapXYZ(AllInterSectPoint[i], curveIntersect[j].GetEndPoint(1)))
                        {
                            curves.Add(curveIntersect[j]);
                        }
                        else
                        {
                            curves.Add(Line.CreateBound(curveIntersect[j].GetEndPoint(0), AllInterSectPoint[i]));
                            curves.Add(Line.CreateBound(curveIntersect[j].GetEndPoint(1),AllInterSectPoint[i] ));
                        }
                    }
                }
            }
            return curves;
        }

       

        private void SplitCurveLeftOrRight(List<Curve> curveIntersect, List<Curve> curveNoneIntersect, Curve modelCurve, out List<Curve> Left, out List<Curve> Right, List<XYZ> AllInterSectPoint)
        {
           
            Left = new List<Curve>();
            Right = new List<Curve>();
            for (int i = 0; i < curveIntersect.Count; i++)
            {
                if (IsLeftOrRight(curveIntersect[i],modelCurve))
                {
                    Left.Add(curveIntersect[i]);

                }
                else
                {
                    Right.Add(curveIntersect[i]);
                }
            }
            for (int i = 0; i < curveNoneIntersect.Count; i++)
            {
                if (IsLeftOrRight(curveNoneIntersect[i], modelCurve))
                {
                    Left.Add(curveNoneIntersect[i]);
                }
                else
                {
                    Right.Add(curveNoneIntersect[i]);
                }
            }
          
            Left.Add(Line.CreateBound(AllInterSectPoint[0], AllInterSectPoint[AllInterSectPoint.Count - 1]));
            Right.Add(Line.CreateBound(AllInterSectPoint[0], AllInterSectPoint[AllInterSectPoint.Count - 1]));
        }
        private List<Curve> GetCurveFloor(EdgeArrayArray edgeArrayArray)
        {
            List<Curve> curvesFloor = new List<Curve>();
            EdgeArray edgeArray = edgeArrayArray.get_Item(0);
            foreach (var item in edgeArray)
            {
                Edge edge = item as Edge;
                if (edge != null)
                {
                    curvesFloor.Add( edge.AsCurve());
                }
            }
            return curvesFloor;
        }







        #region MultipleModelCurves

        private void DeleteFloor(Document doc,Floor floor)
        {
            using (Transaction tran = new Transaction(doc))
            {
                tran.Start("aaa");
                ICollection<ElementId> deletes = doc.Delete(floor.Id);
               
                tran.Commit();
            }
        }
        private List<XYZ> GetPointIntersectofOneCurveToFloor(List<Curve> curvesFloor, Curve modelCurve)
        {
            List<XYZ>  AllInterSectPoint = new List<XYZ>();
            List<IntersectionResultArray> intersectionResultArray = new List<IntersectionResultArray>();

            foreach (var item in curvesFloor)
            {
                IntersectionResultArray inter = new IntersectionResultArray();

                if (item.Intersect(modelCurve, out inter) == SetComparisonResult.Overlap)
                {
                    if (inter.Size != 0) { intersectionResultArray.Add(inter); }

                }
            }
            foreach (var item in intersectionResultArray)
            {
                foreach (var item1 in item)
                {
                    IntersectionResult result = item1 as IntersectionResult;
                    if (result != null)
                    {
                        AllInterSectPoint.Add(result.XYZPoint);
                    }
                }
            }
            AllInterSectPoint = AllInterSectPoint.Distinct(new DistinctXYZ()).ToList();
            return AllInterSectPoint;
        }
        private void CreateSplitFloorMultipleModelCurve(Document doc, FloorType floorType, bool structural, Level level, Floor floor, List<List<Curve>> MulticurvesFloor)
        {
           
            using (Transaction tran = new Transaction(doc))
            {
                tran.Start("aaa");
                ICollection<ElementId> deletes = doc.Delete(floor.Id);
                for (int i = 0; i < MulticurvesFloor.Count; i++)
                {
                    CurveArray curveArray = GetCurveArrayFromCurve(MulticurvesFloor[i]);
                    if (curveArray.Size!=0)
                    {
                        try
                        {
                            Floor floor0 = doc.Create.NewFloor(curveArray, floorType, level, structural, XYZ.BasisZ);

                        }
                        catch (Exception e)
                        {

                            System.Windows.Forms.MessageBox.Show(e.Message+": "+i);
                        }
                    }
                    

                }
                tran.Commit();
            }
        }
        private List<Floor> GetSplitFloorMultipleModelCurve(Document doc, FloorType floorType, bool structural, Level level, List<Curve> curvesFloor, List<Curve> allModelCurve)
        {
            List<Floor> floors = new List<Floor>();
            List<List<Curve>> list = new List<List<Curve>>();
            list.Add(curvesFloor);
            int numberList = list.Count;
            for (int i = 0; i < allModelCurve.Count; i++)
            {
              
                for (int j = 0; j < numberList; j++)
                {
                    if (GetNumberPointIntersectOneCModelCurve(list[j], allModelCurve[i]))
                    {
                        List<XYZ> AllInterSectPoint;
                        List<Curve> CurveIntersect0;
                        List<Curve> CurveNoneIntersect;
                        GetNumberPointIntersectofListCurve(list[j], allModelCurve[i], out AllInterSectPoint, out CurveIntersect0, out CurveNoneIntersect);
                        List<Curve> CurveIntersect = GetListCurveIntersecWithPoint(AllInterSectPoint, CurveIntersect0);
                        List<Curve> Left;
                        List<Curve> Right;
                        SplitCurveLeftOrRight(CurveIntersect, CurveNoneIntersect, allModelCurve[i], out Left, out Right, AllInterSectPoint);
                        List<Curve> Left1 = ChangeCurvesToIsClockOverwise(Left);
                        List<Curve> Right1 = ChangeCurvesToIsClockOverwise(Right);
                        list.RemoveAt(j);
                        list.Add(Left1);
                        list.Add(Right1);
                        if (i != 0) DeleteFloor(doc, floors[j]);
                        CurveArray curveArrayLeft = GetCurveArrayFromCurve(Left1);
                        CurveArray curveArrayRight = GetCurveArrayFromCurve(Right1);
                        using (Transaction tran = new Transaction(doc))
                        {
                            tran.Start("aaa");
                            try
                            {
                                Floor floorLeft = doc.Create.NewFloor(curveArrayLeft, floorType, level, structural, XYZ.BasisZ);
                                Floor floorRight = doc.Create.NewFloor(curveArrayRight, floorType, level, structural, XYZ.BasisZ);
                                floors.Add(floorLeft);
                                floors.Add(floorRight);
                            }
                            catch (Exception e)
                            {

                                System.Windows.Forms.MessageBox.Show(e.Message);
                                
                            }

                            tran.Commit();
                        }
                      
                    }
                }
                numberList = list.Count;
            }
            return floors;
        }
        private List<List<Curve>> GetAllListListCurvesMultipleModelCurveNoneIntersect(List<Curve> curvesFloor, List<Curve> allModelCurve)
        {
            List<List<Curve>> list = new List<List<Curve>>();
            list.Add(curvesFloor);
           
            for (int i = 0; i < allModelCurve.Count; i++)
            {

                List<List<Curve>> list1 = new List<List<Curve>>(list);
                for (int j = 0; j < list1.Count; j++)
                {
                    List<Curve> Left1=new List<Curve>();
                    List<Curve> Right1= new List<Curve>();
                    
                    if (GetNumberPointIntersectOneCModelCurve(list1[j], allModelCurve[i]))
                    {
                       
                        GetNumberPointIntersectofListCurve(list1[j], allModelCurve[i], out List<XYZ> AllInterSectPoint, out List<Curve> CurveIntersect0, out List<Curve> CurveNoneIntersect);
                       
                        List<Curve> CurveIntersect = GetListCurveIntersecWithPoint(AllInterSectPoint, CurveIntersect0);
                        SplitCurveLeftOrRight(CurveIntersect, CurveNoneIntersect, allModelCurve[i], out List<Curve> Left, out List<Curve> Right, AllInterSectPoint);
                         Left1 = ChangeCurvesToIsClockOverwise(Left);
                         Right1 = ChangeCurvesToIsClockOverwise(Right);
                        list.Add(Left1);
                     list.Add(Right1);
                        list.Remove(list1[j]);
                       
                        //break;
                    }
                    
                }
               
            }
            return list;
        }
        private bool NoneOverlapModelCurves(List<Curve> curves)
        {
            for (int i = 0; i < curves.Count; i++)
            {
               
                for (int j= i+1; j < curves.Count; j++)
                {
                    if (curves[i].Intersect(curves[j])==SetComparisonResult.Overlap)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        private bool NoneOverlapModelCurvesOrOutSidePoint(List<Curve> curves, List<Curve> curvesFloor)
        {
            
            for (int i = 0; i < curves.Count; i++)
            {

                for (int j = i + 1; j < curves.Count; j++)
                {
                    if ((curves[i].Intersect(curves[j]) == SetComparisonResult.Overlap)&&!IsOutsideIntersectPointTwoModelCurve(curvesFloor, curves[i], curves[j]))
                    {
                        return false;
                    }   
                }
            }
            return true;
        }
        //private void GetCurvesOverlapModelCurvesOrOutSidePoint(List<Curve> curves, List<Curve> curvesFloor, out List<Curve> OutsideCurves, out List<Curve> InsideCurves)
        //{
        //    OutsideCurves = new List<Curve>();
        //    InsideCurves = new List<Curve>();
        //    for (int i = 0; i < curves.Count; i++)
        //    {

        //        for (int j = i + 1; j < curves.Count; j++)
        //        {
        //            if ((curves[i].Intersect(curves[j]) == SetComparisonResult.Overlap) && !IsOutsideIntersectPointTwoModelCurve(curvesFloor, curves[i], curves[j]))
        //            {
        //                InsideCurves.Add(curves[i]);
        //                InsideCurves.Add(curves[j]);
        //            }
        //            else
        //            {

        //            }
        //        }
        //    }
        //}
        private bool IsOutsideIntersectPointTwoModelCurve(List<Curve> curvesFloor,Curve a, Curve b)
        {
            List<XYZ> Intersecta = GetPointIntersectofOneCurveToFloor(curvesFloor, a);
            IntersectionResultArray inter = new IntersectionResultArray();
            XYZ point = null;
            if (a.Intersect(b, out inter) == SetComparisonResult.Overlap)
            {
                foreach (var item1 in inter)
                {
                    IntersectionResult result = item1 as IntersectionResult;
                    if (result != null)
                    {
                        point=(result.XYZPoint);
                    }
                }
            }
            double delta = Intersecta[0].DistanceTo(Intersecta[Intersecta.Count - 1]);
            double delta1 = Intersecta[0].DistanceTo(point);
            double delta2 = Intersecta[Intersecta.Count - 1].DistanceTo(point);
            return delta1 + delta2 > delta;

        }
        #endregion


        #region Item
        private bool GetNumberPointIntersectOneCModelCurve(List<Curve> curvesFloor, Curve modelCurve)
        {
           List<XYZ> AllInterSectPoint = new List<XYZ>();
            
            List<IntersectionResultArray> intersectionResultArray = new List<IntersectionResultArray>();
            foreach (var item in curvesFloor)
            {
                IntersectionResultArray inter = new IntersectionResultArray();
            
                if (item.Intersect(modelCurve, out inter) == SetComparisonResult.Overlap)
                {
                    if (inter.Size != 0) { intersectionResultArray.Add(inter); }

                }
                

            }
            foreach (var item in intersectionResultArray)
            {
                foreach (var item1 in item)
                {
                    IntersectionResult result = item1 as IntersectionResult;
                    if (result != null)
                    {
                        AllInterSectPoint.Add(result.XYZPoint);
                    }
                }
            }
            AllInterSectPoint = AllInterSectPoint.Distinct(new DistinctXYZ()).ToList();
            //for (int i = 0; i < AllInterSectPoint.Count; i++)
            //{
            //    System.Windows.Forms.MessageBox.Show(AllInterSectPoint[i].ToString());
            //}
            return AllInterSectPoint.Count == 2;
        }
        private bool IsLeftOrRight(Curve a, Curve model)
        {
            XYZ a0pro = PointModel.ProjectToLine(a.GetEndPoint(0), model as Line);
            XYZ a1pro = PointModel.ProjectToLine(a.GetEndPoint(1), model as Line);
            if ((IsLapXYZ(a0pro, a.GetEndPoint(0))))
            {
                XYZ vec = a.GetEndPoint(1) - a1pro;
                XYZ vh = vec.CrossProduct((model as Line).Direction);
                return AreEqual(vh.AngleTo(XYZ.BasisZ), 0);
            }
            else
            {
                XYZ vec = a.GetEndPoint(0) - a0pro;
                XYZ vh = vec.CrossProduct((model as Line).Direction);
                return AreEqual(vh.AngleTo(XYZ.BasisZ), 0);
            }

        }
        private List<XYZ> GetAllXYZFromListCurves(List<Curve> curves)
        {
            List<XYZ> list = new List<XYZ>();
            foreach (var item in curves)
            {
                list.Add(item.GetEndPoint(0));
                list.Add(item.GetEndPoint(1));
            }
            list = list.Distinct(new DistinctXYZ()).ToList();
            return list;
        }
        private XYZ GetCenterOfLoop(List<Curve> curves)
        {
            List<XYZ> list = GetAllXYZFromListCurves(curves);
            List<double> x = new List<double>();
            List<double> y = new List<double>();
            List<double> z = new List<double>();
            foreach (var item in list)
            {
                x.Add(item.X);
                y.Add(item.Y);
                z.Add(item.Z);
            }

            return new XYZ((x.Average()), (y.Average()), (z.Average()));
        }
       
         private CurveArray GetCurveArrayFromCurve(List<Curve> curves)
        {
            CurveArray curveArray = new CurveArray();
            List<XYZ> list = new List<XYZ>();
            foreach (var item in curves)
            {
                list.Add(item.GetEndPoint(0));
                list.Add(item.GetEndPoint(1));

            }
            list = list.Distinct(new DistinctXYZ()).ToList();
            XYZ center = GetCenterOfLoop(curves);
            //cach xap xep theo chieu nguoc chieu kim dong ho
            list = list.OrderBy(x => Angle(center, x)).ToList();
            for (int i = 0; i < list.Count; i++)
            {
                if (i == list.Count - 1)
                {
                    curveArray.Append(Line.CreateBound(list[i], list[0]));
                }
                else
                {
                    curveArray.Append(Line.CreateBound(list[i], list[i + 1]));
                }

            }
            return curveArray;
        }
        private List<Curve> ChangeCurvesToIsClockOverwise(List<Curve> curves)
        {
            List<Curve> listCurve = new List<Curve>();
            List<XYZ> list = new List<XYZ>();
            foreach (var item in curves)
            {
                list.Add(item.GetEndPoint(0));
                list.Add(item.GetEndPoint(1));

            }
            list = list.Distinct(new DistinctXYZ()).ToList();
            XYZ center = GetCenterOfLoop(curves);
            //cach xap xep theo chieu nguoc chieu kim dong ho
            list = list.OrderBy(x => Angle(center, x)).ToList();
            for (int i = 0; i < list.Count; i++)
            {
                if (i == list.Count - 1)
                {
                    listCurve.Add(Line.CreateBound(list[i], list[0]));
                }
                else
                {
                    listCurve.Add(Line.CreateBound(list[i], list[i + 1]));
                }

            }
            return listCurve;
        }
        private double Angle(XYZ center, XYZ a)
        {
            XYZ vector = a - center;
            return (vector.Y >= 0) ? (vector.AngleTo(XYZ.BasisX)) : (Math.PI + vector.AngleTo(-XYZ.BasisX));
        }
        #endregion
        #region MyRegion
        private Line GetLine(XYZ l1, XYZ l2)
        {
            return Line.CreateBound(l1, l2);
        }
        private Arc GetArc(Arc arc, XYZ inter, int i)
        {

            Line startLine = Line.CreateBound(arc.GetEndPoint((i % 2 == 0) ? (0) : (1)), arc.Center);
            double starAngle = startLine.Direction.AngleTo(XYZ.BasisX);
            Line endLine = Line.CreateBound(inter, arc.Center);
            double endAngle = endLine.Direction.AngleTo(XYZ.BasisX);
            return Arc.Create(arc.Center, arc.Radius, starAngle, endAngle, arc.XDirection, arc.YDirection);
        }
        private PlanarFace GetPlanarFaceTop(Floor floor)
        {
            PlanarFace top = null;
            Solid solidFloor = GetSolidFloor(floor);
            FaceArray faceArray = solidFloor.Faces;
            foreach (var item in faceArray)
            {
                PlanarFace planar = item as PlanarFace;

                if (planar != null)
                {
                    if (AreEqual((planar.FaceNormal.AngleTo(XYZ.BasisZ)), 0))
                    {
                        top = planar;
                    }
                }
            }
            return top;
        }

        private Solid GetSolidFloor(Floor floor)
        {
            List<Solid> a = new List<Solid>();
            List<Solid> b = new List<Solid>();
            Solid c = null;
            Options options = new Options();
            options.ComputeReferences = true;
            GeometryElement geometryElement = floor.get_Geometry(options) as GeometryElement;
            foreach (GeometryObject geometryObject in geometryElement)
            {
                Solid solid = geometryObject as Solid;
                if (solid != null)
                {
                    a.Add(solid);
                }
                else
                {
                    GeometryInstance geometryInstance = geometryObject as GeometryInstance;
                    GeometryElement geometryElement1 = geometryInstance.GetInstanceGeometry();
                    foreach (GeometryObject geometryObject1 in geometryElement1)
                    {
                        Solid solid1 = geometryObject1 as Solid;
                        if (solid1 != null)
                        {
                            a.Add(solid1);
                        }
                    }
                }
            }
            foreach (Solid item in a)
            {
                if (item.Volume != 0) { b.Add(item); }
            }
            if (b.Count == 1) { c = b[0]; } else { c = null; }
            return c;
        }

        private List<Curve> GetAllModelCurve(Document doc, List<Reference> refModlCurve, PlanarFace top)
        {
            List<Curve> curves = new List<Curve>();
            foreach (var item in refModlCurve)
            {
                Curve curve = (doc.GetElement(item).Location as LocationCurve).Curve;
                if (curve is Line)
                {
                    Line line = curve as Line;
                    if (AreEqual((line.Direction.AngleTo(XYZ.BasisZ)), Math.PI * 0.5))
                    {
                        curves.Add(curve);
                    }

                }
                if (curve is Arc)
                {
                    Arc arc = curve as Arc;
                    if (AreEqual((arc.Normal.AngleTo(XYZ.BasisZ)), 0) || AreEqual((arc.Normal.AngleTo(XYZ.BasisZ)), Math.PI))
                    {
                        if (AreEqual((arc.Center.Z), (top.Origin.Z)))
                        {
                            curves.Add(curve);
                        }
                    }
                }

            }

            return curves;
        }

        private Level GetFloorLevel(Document doc, Floor floor)
        {
            return doc.GetElement(floor.get_Parameter(BuiltInParameter.LEVEL_PARAM).AsElementId()) as Level;
        }

        private FloorType GetFloorType(Document doc, Floor floor)
        {
            return doc.GetElement(floor.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsElementId()) as FloorType;
        }
        public static bool AreEqual(double firstValue, double secondValue, double tolerance = 1.0e-9)
        {
            return (secondValue - tolerance < firstValue && firstValue < secondValue + tolerance);
        }
        private bool IsLapXYZ(XYZ a, XYZ b)
        {
            return ((AreEqual(a.X, b.X)) && (AreEqual(a.Y, b.Y)) && (AreEqual(a.Z, b.Z)));
        }

        #endregion
    }

}
