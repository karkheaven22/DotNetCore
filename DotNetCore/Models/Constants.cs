using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace DotNetCore.Models
{
    public static class Constants
    {
        public static DateTime MinDateTime => new DateTime(1900, 1, 1);

        public static string GetEnumDescription(this Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes != null && attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }

        public enum EnumSerialCode
        {
            [Description("CUST")]
            Customer,

            [Description("S.ORD")]
            SalesOrder
        }

        public static Dictionary<EnumSerialCode, string> SerialCodeDic = new() {
            { EnumSerialCode.Customer, "CUST" },
            { EnumSerialCode.SalesOrder, "S.ORD" }
        };

        public static string GetSerialCode(EnumSerialCode serialCode)
        {
            if (SerialCodeDic.TryGetValue(serialCode, out string description))
                return description;
            return "Unknown";
        }
    }
}