using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TechAdvancing
{
    class Rules
    {
        public static Dictionary<TechLevel, int> researchProjectStoreTotal = new Dictionary<TechLevel, int>();
        public static Dictionary<TechLevel, int> researchProjectStoreFinished = new Dictionary<TechLevel, int>();
        public static TechLevel baseFactionLevel = TechLevel.Undefined;

        internal static TechLevel GetNewTechLevel()
        {
            if (TechAdvancing_Config_Tab.b_configCheckboxNeedTechColonists)
            {
                return (TechLevel)(Math.Min((int)GetRuleTechlevel(), (int)TechAdvancing_Config_Tab.maxTechLevelForTribals));
            }
            return GetRuleTechlevel();
        }

        internal static TechLevel GetRuleTechlevel()
        {
            LogOutput.WriteLogMessage(Errorlevel.Debug, $"A: {RuleA().ToString()} | B:{RuleB().ToString()}");
            return Util.GetHighestTechlevel(TechAdvancing_Config_Tab.baseFactionTechLevel, RuleA(), RuleB());
        }

        internal static TechLevel GetLowTechTL()
        {
            return TechAdvancing_Config_Tab.b_configCheckboxNeedTechColonists ? TechAdvancing_Config_Tab.maxTechLevelForTribals : TechLevel.Transcendent;
        }

        internal static TechLevel RuleA()
        {
            var notResearched = researchProjectStoreTotal.Except(researchProjectStoreFinished);
            int min = notResearched.Where(x => x.Value > 0).Min(x => (int)x.Key);
            return (TechLevel)(TechLevel)Util.Clamp(0, min - 1 + TechAdvancing_Config_Tab.Conditionvalue_A, (int)TechLevel.Transcendent);
        }

        internal static TechLevel RuleB()
        {
            int result = 0; //tl undef

            foreach (var tl in researchProjectStoreTotal.Where(x => x.Value > 0))
            {
                if ((float)researchProjectStoreFinished[tl.Key] / (float)tl.Value > 0.5f)   // TODO allow configuring?
                {
                    result = (int)tl.Key;
                }
            }
            return (TechLevel)Util.Clamp(0, result + (int)TechAdvancing_Config_Tab.Conditionvalue_B, (int)TechLevel.Transcendent);
        }
    }
}
