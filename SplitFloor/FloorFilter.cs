using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace SplitFloor
{
    public class FloorFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
           return elem is Floor;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
           return true;
        }
    }
}
