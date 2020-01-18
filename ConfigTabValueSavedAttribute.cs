using System;

namespace TechAdvancing
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    internal class ConfigTabValueSavedAttribute : Attribute
    {
        public object LastValue { get; set; }
        public string SaveName { get; }

        public ConfigTabValueSavedAttribute(string saveName)
        {
            this.SaveName = saveName;
        }

    }
}