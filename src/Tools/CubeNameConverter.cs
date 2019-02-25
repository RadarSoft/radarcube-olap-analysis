using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace RadarSoft.RadarCube.Tools
{
    public class CubeNameConverter : TypeConverter
    {
        private readonly ArrayList values;

        public CubeNameConverter()
        {
            // Initializes the standard values list with defaults.
            values = new ArrayList(new string[] { });
        }

        // Indicates this converter provides a list of standard values.
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        // Returns a StandardValuesCollection of standard value objects.
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            string cs = null;
            var svc = new StandardValuesCollection(values);
            if (context.Instance != null)
                cs = RadarUtils.GetPropertyValue(context.Instance, "ConnectionString") as string;

            //if (cs.IsFill())
            //    svc = new StandardValuesCollection(MDConnectionEditor.GetCubeNames(cs).ToList());

            return svc;
        }

        // Returns true for a sourceType of string to indicate that 
        // conversions from string to integer are supported. (The 
        // GetStandardValues method requires a string to native type 
        // conversion because the items in the drop-down list are 
        // translated to string.)
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }

        // If the type of the value to convert is string, parses the string 
        // and returns the integer to set the value of the property to. 
        // This example first extends the integer array that supplies the 
        // standard values collection if the user-entered value is not 
        // already in the array.
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value == null)
                return null;

            if (value.GetType() == typeof(string))
            {
                var newVal = (string) value;
                return newVal;
            }
            return base.ConvertFrom(context, culture, value);
        }
    }
}