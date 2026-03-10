using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class FilterCondition
    {
        public string Field { get; set; }
        public string Operator { get; set; }
        public object Value { get; set; }
    }

    public class FilterGroup
    {
        public string Logic { get; set; } = "and";   // and / or
        public List<FilterCondition> Filters { get; set; } = new List<FilterCondition>();
    }
}