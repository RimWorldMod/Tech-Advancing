using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TechAdvancing
{
    class Util
    {
        /// <summary>
        /// Helper method for clamping an int value.
        /// </summary>
        /// <param name="min">Lower limit.</param>
        /// <param name="val">The value.</param>
        /// <param name="max">Upper limit.</param>
        /// <returns>The value or the border that was exceeded.</returns>
        internal static int Clamp(int min, int val, int max) //helper method
        {
            if (val < min)
            {
                return min;
            }
            else if (max < val)
            {
                return max;
            }
            else
            {
                return val;
            }
        }

        /// <summary>
        /// Helper method for clamping a Techlevel.
        /// </summary>
        /// <param name="min">Lower limit.</param>
        /// <param name="val">The value.</param>
        /// <param name="max">Upper limit.</param>
        /// <returns>The value or the border that was exceeded.</returns>
        internal static TechLevel Clamp(TechLevel min, TechLevel val, TechLevel max) //helper method
        {
            if (val < min)
            {
                return min;
            }
            else if (max < val)
            {
                return max;
            }
            else
            {
                return val;
            }
        }


        internal static bool ColonyHasHiTechPeople()
        {
            FactionDef[] hitechfactions = new FactionDef[] { FactionDefOf.Mechanoid, FactionDefOf.Ancients, FactionDefOf.PlayerColony };
            string[] hightechkinds = new string[] { "colonist" };

            //Debug
            //   foreach (var pawn in RimWorld.PawnsFinder.AllMaps_FreeColonists)
            //   {
            //       string techlvl = null;
            //       if (MapComponent_TA_Expose.TA_Expose_People?.ContainsKey(pawn)==true)
            //       {
            //           techlvl = ((int?)(MapComponent_TA_Expose.TA_Expose_People[pawn])?.def?.techLevel ?? -1).ToString();
            //       }
            //       LogOutput.writeLogMessage(Errorlevel.Warning, "Pawn: " + pawn?.Name + " |Faction: " + pawn?.Faction?.Name + " |DefName: " + pawn?.kindDef?.defaultFactionType?.defName  + "|Tech lvl: "+ techlvl + " |High Tech (whitelist): " + (hitechfactions.Contains(pawn?.Faction?.def) ? "yes" : "no"));    
            //}
            //   LogOutput.writeLogMessage(Errorlevel.Warning,"done");

            return MapCompSaveHandler.ColonyPeople.Any(x => x.Value?.def?.techLevel >= TechLevel.Industrial) || RimWorld.PawnsFinder.AllMaps_FreeColonists.Any(x => hightechkinds.Contains(x.kindDef.defName.ToLowerInvariant()));
        }

        internal static TechLevel GetHighestTechlevel(params TechLevel[] t)
        {
            var max = t.Select(x => (int)x).Max();
            return (max > (int)TechLevel.Archotech) ? TechLevel.Archotech : (TechLevel)max;
        }
    }
}
