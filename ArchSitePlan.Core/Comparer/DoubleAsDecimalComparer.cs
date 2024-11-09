using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.Core.Comparer
{
    public class DoubleAsDecimalComparer : IEqualityComparer<double>
    {
        private readonly int _decimalPlaces;

        public DoubleAsDecimalComparer(int decimalPlaces = 3)
        {
            _decimalPlaces = decimalPlaces;
        }

        public bool Equals(double x, double y)
        {
            decimal decX = (decimal)Math.Round(x, _decimalPlaces);
            decimal decY = (decimal)Math.Round(y, _decimalPlaces);
            return decX == decY;
        }

        public int GetHashCode(double obj)
        {
            // Use rounded value for hash code
            return Math.Round((decimal)obj, _decimalPlaces).GetHashCode();
        }
    }
}
