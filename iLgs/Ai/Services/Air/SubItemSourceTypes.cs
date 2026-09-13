using System;

namespace iLgs.Ai.Services.Air
{
    public static class SubItemSourceTypes
    {
        public const string Ordered = "ORDERED";
        public const string Freebie = "FREEBIE";
        public const string InspectionAdded = "INSPECTION_ADDED";

        public static bool IsValid(string sourceType)
        {
            return sourceType == Ordered || sourceType == Freebie || sourceType == InspectionAdded;
        }

        public static string Normalize(string sourceType, bool hasOrderSubItem = false)
        {
            if (string.IsNullOrWhiteSpace(sourceType))
            {
                return hasOrderSubItem ? Ordered : InspectionAdded;
            }

            string upper = sourceType.Trim().ToUpperInvariant();
            if (upper == Freebie) return Freebie;
            if (upper == InspectionAdded) return InspectionAdded;
            return Ordered;
        }

        public static bool ResolveBundleRequired(string sourceType, bool? currentSetting = null, bool hasOrderSubItem = false)
        {
            string normalized = Normalize(sourceType, hasOrderSubItem);
            if (normalized == Ordered) return true;
            return currentSetting ?? false;
        }
    }
}
