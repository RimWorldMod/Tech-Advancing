using System;

namespace TechAdvancing
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ConfigTabValueSavedAttribute : Attribute
    {
        public string SaveName { get; }

        public ConfigTabValueSavedAttribute(string saveName)
        {
            this.SaveName = saveName;
        }

    }
}