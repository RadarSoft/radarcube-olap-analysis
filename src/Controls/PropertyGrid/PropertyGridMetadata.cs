using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Controls.PropertyGrid
{
    public class PropertyGridMetadata
    {
        protected virtual Type RootType => null;

        public void Initialize()
        {
            var pFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            var metaFields = GetType().GetFields(pFlags);
            var nodeName = "";
            string[] path;
            var treeNodeIndexes = new List<string>();
            foreach (var mfield in metaFields)
            {
                path = mfield.Name.Split('_');
                var level = 0;
                var t = RootType;
                PropertyInfo parentProp = null;
                var propertyPrefix = "";
                var parentUniqueName = "";
                foreach (var pName in path)
                {
                    var uniqueName = propertyPrefix + pName;
                    var currentProp = t.GetProperties(pFlags).FirstOrDefault(x => x.Name == pName);
//                    if (currentProp == null)
//                        currentProp = t.BaseType.GetProperties(pFlags).FirstOrDefault(x => x.Name == pName);
//                    if (currentProp == null)
//                        currentProp = t.BaseType.BaseType.GetProperties(pFlags).FirstOrDefault(x => x.Name == pName);
                    if (level == path.Length - 1)
                    {
                        var groupName = "";
                        var propClasses = "";
                        var groupClasses = "";

                        if (!treeNodeIndexes.Contains(parentUniqueName))
                            treeNodeIndexes.Add(parentUniqueName);

                        treeNodeIndexes.Add(uniqueName);

                        var groupIndex = treeNodeIndexes.IndexOf(parentUniqueName) + 1;
                        var propIndex = treeNodeIndexes.IndexOf(uniqueName) + 1;
                        groupClasses = "treegrid-" + groupIndex;
                        var parentPath = parentUniqueName.Split('_');
                        var grandParentIndex = -1;
                        if (parentPath.Length > 1)
                        {
                            var grandParentUniqueName = parentUniqueName.Substring(0,
                                parentUniqueName.Length - parentPath[parentPath.Length - 1].Length - 1);
                            grandParentIndex = treeNodeIndexes.IndexOf(grandParentUniqueName) + 1;
                            groupName = parentPath[parentPath.Length - 2] + ".";
                        }
                        groupName += parentProp != null ? parentProp.Name : parentUniqueName;

                        propClasses = "treegrid-" + propIndex + " treegrid-parent-" + groupIndex;
                        if (grandParentIndex >= 0)
                            groupClasses += " grand-parent treegrid-parent-" + grandParentIndex;

                        var pm = SetPropertyMetaData(currentProp, groupClasses, groupName, propClasses);
                        mfield.SetValue(this, pm);
                    }
                    if (currentProp != null)
                    {
                        t = currentProp.PropertyType;
                        parentProp = currentProp;
                    }
                    parentUniqueName = propertyPrefix + pName;
                    propertyPrefix += pName + "_";
                    level++;
                }
            }
        }

        private PropertyMetadata SetPropertyMetaData(PropertyInfo prop, string groupClasses, string groupName,
            string propClasses)
        {
            var res = new PropertyMetadata();
            res.group = groupName;
            res.groupclass = groupClasses;
            res.name = prop.Name;
            res.classes = propClasses;
            var pType = prop.PropertyType;

            if (pType.GetTypeInfo().IsEnum)
            {
                var collection = new List<object>();
                foreach (var fi in pType.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    var obj = new {text = fi.Name, value = fi.GetRawConstantValue()};
                    collection.Add(obj);
                }
                res.options = new List<object>();
                res.options.AddRange(collection);
                res.type = "options";
            }

            if (pType == typeof(bool))
                res.type = "boolean";

            if (pType == typeof(Color))
            {
                res.type = "color";
                res.options = new {preferredFormat = "hex", showInput = true, showInitial = true};
            }
            if (prop.Name == "ButtonID")
                res.type = "label";

            var descript = "";
            if (prop.GetCustomAttributes(typeof(DescriptionAttribute), true).Count() > 0)
            {
                var attribute = prop.GetCustomAttributes(typeof(DescriptionAttribute), true).First();
                descript = ((DescriptionAttribute) attribute).Description;
            }
            if (descript.IsFill())
            {
                res.showHelp = true;
                res.description = descript;
            }

            return res;
        }
    }
}