using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace SplitFloor
{
    public class ModelCurveFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem.Category.Name.Equals("Lines")&&((IsLine(elem))||(IsArc(elem)));
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
        private bool IsLine(Element elem)
        {
            return ((elem.Location as LocationCurve).Curve as Line)!=null;
        }
        private bool IsArc(Element elem)
        {
            return ((elem.Location as LocationCurve).Curve as Arc) != null;
        }
    }
}
