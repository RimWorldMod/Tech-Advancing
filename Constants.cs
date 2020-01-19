using RimWorld;

namespace TechAdvancing
{
    class Constants
    {
        public const string TAResearchProjDefPrefixName = "TechAdvancing_PrereqBlocker_";
        public static string TAResearchProjDefNameFromTechLvl(TechLevel t) => TAResearchProjDefPrefixName + (int)t;

    }
}
