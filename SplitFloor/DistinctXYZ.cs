using Autodesk.Revit.DB;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SplitFloor
{
    public class DistinctXYZ : IEqualityComparer<XYZ>
    {
        public bool Equals(XYZ x, XYZ y)
        {
            return (AreEqual(x.X, y.X) && AreEqual(x.Y, y.Y) && AreEqual(x.Z, y.Z));
        }

        public int GetHashCode(XYZ obj)
        {
            return 1;
        }
        public static bool AreEqual(double firstValue, double secondValue, double tolerance = 1.0e-9)
        {
            return (secondValue - tolerance < firstValue && firstValue < secondValue + tolerance);
        }
    }
}
