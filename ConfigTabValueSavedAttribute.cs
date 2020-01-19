using System;

namespace TechAdvancing
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class ConfigTabValueSavedAttribute : Attribute
    {
        public string SaveName { get; }

        public ConfigTabValueSavedAttribute(string saveName)
        {
            this.SaveName = saveName;
        }

    }
}