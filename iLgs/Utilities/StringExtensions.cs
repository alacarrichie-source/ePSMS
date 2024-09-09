using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Utilities
{
    public static class StringExtensions
    {
        public static bool IsNullOrWhiteSpaceX(this string value)
        {
            // Check if the string is null, empty, or consists only of whitespace
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            if (value.Equals("-"))
            {
                return true;
            }

            // Check if the string consists only of digits
            //if (int.TryParse(value, out _))
            //{
            //    return true;
            //}

            return false;
        }
    }
}