using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;

namespace SplitFloor
{
    public class FloorFilter : ISelectionFilter
    {
        private static readonly BuiltInParameter Slop = (BuiltInParameter)(-1006016);
        public Document Doc { get; set; }
        public FloorFilter(Document document)
        {
            Doc = document;
        }
        public bool AllowElement(Element elem)
        {
           return (elem is Floor)&&(NoneSlope(elem as Floor))&&(OneCurveArrayArray(elem as Floor))&&(LineOrArcEdgeArray(elem as Floor));
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
           return true;
        }
       private bool NoneSlope(Floor floor)
        {

            return GetPlanarFaceTop(floor) != null;
        }
        private bool LineOrArcEdgeArray(Floor floor)
        {
            EdgeArrayArray edgeArrayArray = GetPlanarFaceTop(floor).EdgeLoops;
            List < Curve > curvesFloor = GetCurveFloor(edgeArrayArray);
            foreach (var item in curvesFloor)
            {
                Line line = item as Line;
                Arc arc = item as Arc;
                if ((line==null)&&(arc==null))
                {
                    return false;
                }
            }
            return true;
        }
        private bool OneCurveArrayArray(Floor floor)
        {
            PlanarFace top = GetPlanarFaceTop(floor);
            return top.EdgeLoops.Size==1;
        }
        public  PlanarFace GetPlanarFaceTop(Floor floor)
        {
            PlanarFace top = null;
            Solid solidFloor = GetSolidFloor(floor);
            FaceArray faceArray = solidFloor.Faces;
            foreach (var item in faceArray)
            {
                PlanarFace planar = item as PlanarFace;

                if (planar != null)
                {
                    if (PointModel.AreEqual((planar.FaceNormal.AngleTo(XYZ.BasisZ)), 0)) 
                    {
                        top = planar;
                    }
                }
            }
            return top;
        }
        private  Solid GetSolidFloor(Floor floor)
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
        private List<Curve> GetCurveFloor(EdgeArrayArray edgeArrayArray)
        {
            List<Curve> curvesFloor = new List<Curve>();
            EdgeArray edgeArray = edgeArrayArray.get_Item(0);
            foreach (var item in edgeArray)
            {
                Edge edge = item as Edge;
                if (edge != null)
                {
                    curvesFloor.Add(edge.AsCurve());
                }
            }
            return curvesFloor;
        }
        public  List<Curve> GetAllCurveFloor(Floor floor)
        {
            EdgeArrayArray edgeArrayArray = GetPlanarFaceTop(floor).EdgeLoops;
            List<Curve> curvesFloor = new List<Curve>();
            EdgeArray edgeArray = edgeArrayArray.get_Item(0);
            foreach (var item in edgeArray)
            {
                Edge edge = item as Edge;
                if (edge != null)
                {
                    curvesFloor.Add(edge.AsCurve());
                }
            }
            return curvesFloor;
        }
    }
}
