using Autodesk.Revit.DB;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SplitFloor
{
    public class DistinctCurve : IEqualityComparer<Curve>
    {
        public bool Equals(Curve x, Curve y)
        {
            return x.GetHashCode() == y.GetHashCode();
        }

        public int GetHashCode(Curve obj)
        {
             return 1;
        }
    }
}
