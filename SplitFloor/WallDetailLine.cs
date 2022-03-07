
#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using Application = Autodesk.Revit.ApplicationServices.Application;
#endregion

namespace SplitFloor
{
    [Transaction(TransactionMode.Manual)]
    public class WallDetailLine : IExternalCommand
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
                Reference WallRef = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);
                Wall wall = doc.GetElement(WallRef) as Wall;
                Line line = (wall.Location as LocationCurve).Curve as Line;
                //Lấy 2 mặt phẳng vuông góc vs line, gọi là Nouth(hướng bắc, Sounth hướng nam tương ứng với cevtor face normal vuông góc vs line
                PlanarFace Nouth = SolidFace.GetNouth(wall);
                PlanarFace South = SolidFace.GetSouth(wall);
                WallType wallType = wall.WallType;
                CompoundStructure compound = wallType.GetCompoundStructure();

                IList<CompoundStructureLayer> compoundstructuralLayer = compound.GetLayers();
                //vì tường có nhiều lớp nên luôn lớp trên cùng tính từ hướng mặt Nouth=>sounth
                double a = 0;
                // duyệt các layer, đến khi nào gặp layer có kiểu enum là  MaterialFunctionAssignment.Structure thì dừng, đồng thời tính tổng cộng dồn và 0.5 bề dày lớp structural
                for (int i = 0; i < compoundstructuralLayer.Count; i++)
                {

                    if (compoundstructuralLayer[i].Function == MaterialFunctionAssignment.Structure)
                    {
                        a += compoundstructuralLayer[i].Width / 2;
                        break;
                    }
                    else
                    {
                        a += compoundstructuralLayer[i].Width;
                    }
                }
                //lấy hình chiếu endpoint 0 của line leen mặt phẳng Nouth
                XYZ w0 = PointModel.ProjectToPlane(line.GetEndPoint(0), Nouth);
                //offset điểm này theo phương sounth.Facenormal
                XYZ w0A = w0 + South.FaceNormal * a;
                // tương tự nhử điểm endPoint của 0 
                XYZ w1 = PointModel.ProjectToPlane(line.GetEndPoint(1), Nouth);
                XYZ w1A = w1 + South.FaceNormal * a; ;
                Line DetailLine = Line.CreateBound(w0A, w1A);
                //tạo 1 line mới từ 2 điểm đó.
                using (Transaction tran = new Transaction(doc))
                {
                    tran.Start("aaa");
                    DetailCurve detailCurve = doc.Create.NewDetailCurve(doc.ActiveView, DetailLine);
                    tran.Commit();
                }

                return Result.Succeeded;
            }
            catch (Exception e)
            {

                System.Windows.Forms.MessageBox.Show(e.Message);
                return Result.Cancelled;
            }
        }


    }

}
