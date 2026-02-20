using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Utilities
{
    class ItemNoSequenceUtil : IComparer<int[]>
    {
        public int Compare(int[] x, int[] y)
        {
            int length = Math.Max(x.Length, y.Length);

            for (int i = 0; i < length; i++)
            {
                int xi = i < x.Length ? x[i] : 0;
                int yi = i < y.Length ? y[i] : 0;

                int result = xi.CompareTo(yi);
                if (result != 0)
                    return result;
            }

            return 0;
        }
    }

}