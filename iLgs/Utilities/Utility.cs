using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace iLgs.Utilities
{
    public class Utility
    {
        public static string ToProperCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Use InvariantCulture to ensure consistent results regardless of the current culture
            TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;

            // Convert the input to lowercase and then to title case
            return textInfo.ToTitleCase(input.ToLower());
        }
    }
}