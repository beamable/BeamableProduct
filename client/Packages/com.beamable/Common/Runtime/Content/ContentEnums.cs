using System;

namespace Beamable.Common.Content
{
    public static class EnumConversionHelper
    {
        private const string VALUE_CONVERTED_MARKER = "-VALUE CONVERTED-";
        
        public static T ParseEnumType<T>(string value, T defaultValue = default) where T : Enum
        {
            foreach (var name in Enum.GetNames(typeof(T)))
            {
                if (value == name.ToLower())
                {
                    return (T)Enum.Parse(typeof(T), name);
                }
            }

            return defaultValue;
        }

        public static void ConvertIfNotDoneAlready<T>(ref T conversionTarget, ref string value, T defaultValue = default) where T : Enum
        {
            if (value != VALUE_CONVERTED_MARKER)
            {
                conversionTarget = ParseEnumType<T>(value, defaultValue);
                value = VALUE_CONVERTED_MARKER;
            }
        }
    }
    
    public enum AccessType
    {
        Private, Public
    }

    public enum ComparatorType
    {
        Eq, Ne, Gt, Ge, Lt, Le
    }
}