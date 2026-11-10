using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TechAdvancing
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ConfigTabValueSavedAttribute : Attribute
    {
        public string SaveName { get; }
        public object DefaultValue { get => attributeDefaultValues[this.SaveName]; }
        private bool IsDefaultValueSet => allAttributeSavedNames.Contains(this.SaveName);

        internal static readonly Dictionary<string, object> attributeDefaultValues = new Dictionary<string, object>();
        private static readonly List<string> allAttributeSavedNames = new List<string>();
        private static readonly List<ConfigTabValueSavedAttribute> allAttributes = new List<ConfigTabValueSavedAttribute>();

        public ConfigTabValueSavedAttribute(string saveName)
        {
            this.SaveName = saveName;

            if (!allAttributes.Any(x => x.SaveName == saveName))
            {
                allAttributes.Add(this);
            }
        }


        private static bool defaultValueCacheBuilt = false;

        public static void BuildDefaultValueCache()
        {
            if (!defaultValueCacheBuilt)
            {
                defaultValueCacheBuilt = true;
                RebuildDefaultValueCache();
            }
        }

        public static void RebuildDefaultValueCache()
        {
            var types = new[] { typeof(TechAdvancing_Config_Tab) };

            foreach (var t in types)
            {
                var props = t.GetProperties().Where(x => x.GetCustomAttribute<ConfigTabValueSavedAttribute>() != null);

                foreach (var p in props)
                {
                    var attrib = p.GetCustomAttribute<ConfigTabValueSavedAttribute>();

                    attributeDefaultValues.Add(attrib.SaveName, p.GetValue(null, null));
                    allAttributeSavedNames.Add(attrib.SaveName);
                }
            }

            var notFoundAttribs = allAttributes.Where(x => !x.IsDefaultValueSet).ToList();

            if (notFoundAttribs.Any())
            {
                LogOutput.WriteLogMessage(Errorlevel.Error, "Unable to find linked property of one or more attributes. Default config values cannot be determined. Full list:\n"
                                                            + string.Join(", ", notFoundAttribs.Select(x => x.SaveName)));
            }
        }

    }
}