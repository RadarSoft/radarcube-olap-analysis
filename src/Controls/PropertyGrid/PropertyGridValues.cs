using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Controls.PropertyGrid
{
    public class PropertyGridValues
    {
        internal object RootObject { get; set; }

        public virtual void Read()
        {
            SetData(true);
        }

        public virtual void Write()
        {
            SetData(false);
        }

        private void SetData(bool inRead)
        {
            var pFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            var metaFields = GetType().GetFields(pFlags);
            var nodeName = "";
            PropertyInfo parentProp = null;
            object parentObject = null;
            foreach (var mfield in metaFields)
            {
                var path = mfield.Name.Split('_');
                var level = 0;
                var o = RootObject;
                foreach (var pName in path)
                {
                    var currentPropSource = o.GetType().GetProperties(pFlags).FirstOrDefault(x => x.Name == pName);
                    if (currentPropSource == null)
                    {
                        level++;
                        continue;
                    }

                    if (level == path.Length - 1)
                    {
                        if (inRead)
                            SetPropertyData(mfield, currentPropSource, o);
                        else
                            SetObjectData(mfield, currentPropSource, parentProp, o, parentObject);
                        break;
                    }

                    parentObject = o;
                    parentProp = currentPropSource;
                    o = currentPropSource.GetValue(o, null);
                    level++;
                }
            }
        }

        private void SetPropertyData(FieldInfo field, PropertyInfo propSource, object propObject)
        {
            var pType = propSource.PropertyType;
            var pFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

            //if (pType == typeof(Unit))
            //{
            //    field.SetValue(this, ((Unit)propSource.GetValue(propObject, null)).ToString());
            //    return;
            //}

            if (pType.GetTypeInfo().IsEnum)
            {
                field.SetValue(this, (int) propSource.GetValue(propObject, null));
                return;
            }

            if (pType == typeof(Color))
            {
                var c = (Color) propSource.GetValue(propObject, null);
                field.SetValue(this, RadarUtils.GetHexStringFromColor(c));
                return;
            }

            if (pType == typeof(float))
            {
                field.SetValue(this, Convert.ToInt32(propSource.GetValue(propObject, null)));
                return;
            }


            field.SetValue(this, propSource.GetValue(propObject, null));
        }


        private void SetObjectData(FieldInfo field, PropertyInfo propSource, PropertyInfo parentProp, object propObject,
            object parentObject)
        {
            var pType = propSource.PropertyType;
            var pFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

            //if (pType == typeof(Unit))
            //{
            //    var v = new Unit((string)field.GetValue(this));
            //    propSource.SetValue(propObject, v, null);
            //    return;
            //}

            if (pType == typeof(Color))
            {
                var c = RadarUtils.GetColorFromHexString((string) field.GetValue(this));
                propSource.SetValue(propObject, c, null);
                return;
            }

            if (propObject is Font)
            {
                var oldFont = ((Font) propObject).Clone() as Font;
                FontStyle oldStyle;
                var pPath = field.Name.Split('_');
                var shortname = pPath[pPath.Length - 1];
                switch (shortname)
                {
                    case "Name":
                        propObject = new Font((string) field.GetValue(this), oldFont.Size, oldFont.Style);
                        break;
                    case "Size":
                        propObject = new Font(oldFont.Name, (int) field.GetValue(this), oldFont.Style);
                        break;
                    case "Bold":
                        oldStyle = oldFont.Style;
                        if ((bool) field.GetValue(this))
                        {
                            if (!oldStyle.HasFlag(FontStyle.Bold))
                                oldStyle |= FontStyle.Bold;
                        }
                        else if (oldStyle.HasFlag(FontStyle.Bold))
                        {
                            oldStyle ^= FontStyle.Bold;
                        }
                        propObject = new Font(oldFont.Name, oldFont.Size, oldStyle);
                        break;
                    case "Italic":
                        oldStyle = oldFont.Style;
                        if ((bool) field.GetValue(this))
                        {
                            if (!oldStyle.HasFlag(FontStyle.Italic))
                                oldStyle |= FontStyle.Italic;
                        }
                        else if (oldStyle.HasFlag(FontStyle.Italic))
                        {
                            oldStyle ^= FontStyle.Italic;
                        }
                        propObject = new Font(oldFont.Name, oldFont.Size, oldStyle);
                        break;
                    case "Underline":
                        oldStyle = oldFont.Style;
                        if ((bool) field.GetValue(this))
                        {
                            if (!oldStyle.HasFlag(FontStyle.Underline))
                                oldStyle |= FontStyle.Underline;
                        }
                        else if (oldStyle.HasFlag(FontStyle.Underline))
                        {
                            oldStyle ^= FontStyle.Underline;
                        }
                        propObject = new Font(oldFont.Name, oldFont.Size, oldStyle);
                        break;
                    case "Strikeout":
                        oldStyle = oldFont.Style;
                        if ((bool) field.GetValue(this))
                        {
                            if (!oldStyle.HasFlag(FontStyle.Strikeout))
                                oldStyle |= FontStyle.Strikeout;
                        }
                        else if (oldStyle.HasFlag(FontStyle.Strikeout))
                        {
                            oldStyle ^= FontStyle.Strikeout;
                        }
                        propObject = new Font(oldFont.Name, oldFont.Size, oldStyle);
                        break;
                }
                parentProp.SetValue(parentObject, propObject, null);
                return;
            }

            propSource.SetValue(propObject, field.GetValue(this), null);
        }
    }
}