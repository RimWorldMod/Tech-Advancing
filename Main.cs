
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TechAdvancing;
using UnityEngine;
using Verse;
using RimWorld;
using System.Reflection;
using System.IO;
using GHXXTechAdvancing;

namespace GHXXTechAdvancing
{

    [StaticConstructorOnStartup]
    public class Injector_GHXXTechAdvancing
    {
        static Injector_GHXXTechAdvancing()     //Detour the method that gets run when a research gets finished. Old school detour. Could be replaced with harmony
        {
            MethodInfo source = typeof(ResearchManager).GetMethod("ReapplyAllMods", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo destination = typeof(_ResearchManager).GetMethod("_ReapplyAllMods", BindingFlags.Static | BindingFlags.NonPublic);
            //Log.Message("Source method = " + source.Name + "Target = " + destination.Name);
            
            Detour.detour(source, destination);
            GameObject initializer = new GameObject("GHXXTAMapComponentInjector");

            initializer.AddComponent<MapComponentInjector>();
            UnityEngine.Object.DontDestroyOnLoad(initializer);

            SetupHarmony.Setup();
        }

    }

    internal static class _ResearchManager
    {
        private static TechLevel lowestProjectTechLevel = TechLevel.Transcendent;
        public static TechLevel factionDefault = TechLevel.Undefined;
        public static bool isTribe = true;
        private static bool skippedWorldStart = false;
        private static TechLevel suggestedTechLevel = TechLevel.Undefined;
        private static int[][] researchProjectsArray = new int[][] { new int[2], new int[2], new int[2],
            new int[2], new int[2], new int[2], new int[2], new int[2]}; // Techlevel --> Researched | Total
                                                                         //   .. ....  . . .. . . . .. .. . 
        public static string facName = "";
        public static bool firstpass = true;
        private static bool isInceased = false;
        private static TechLevel highestResearchCategoryOverHalf = TechLevel.Undefined;
        internal static void _ReapplyAllMods(this ResearchManager _this)    //new ReaookyAllMods Method
        {
            if (firstpass || facName != Faction.OfPlayer.def.defName)
            {
                facName = Faction.OfPlayer.def.defName;
                try
                {
                    factionDefault = Faction.OfPlayer.def.techLevel;        //store the default value for the techlevel because we will modify it later and we need the one from right now
                    TechAdvancing_Config_Tab.tempOverridableLevel = factionDefault;
                    isTribe = factionDefault == TechLevel.Neolithic;
                    loadCfgValues();
                    firstpass = false;

                    //Debug
                    LogOutput.writeLogMessage(Errorlevel.Debug,"Con A val= " + TechAdvancing_Config_Tab.Conditionvalue_A + "|||Con B Val= " + TechAdvancing_Config_Tab.Conditionvalue_B);

                }
                catch (Exception ex)
                {
                    LogOutput.writeLogMessage(Errorlevel.Error, "Caught error in Reapply All Mods: " + ex.ToString());
                }
                
            }
            for (int i = 0; i < researchProjectsArray.GetLength(0); i++)        //Reset Start
            {
                researchProjectsArray[i][0] = 0;                    //Reset value so that it can be recalculated.
                researchProjectsArray[i][1] = 0;                    

            }
            lowestProjectTechLevel = TechLevel.Transcendent;        //Reset End

            foreach (var researchProjectDef in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                if (researchProjectDef?.tags?.Contains("ta-ignore") == true)
                {
                    break;  //skip the research if it contains the disabled tag:
                    #region tagDesc                    
                    /*
                    	<ResearchProjectDef>
	                        <defName>Firefoam</defName>
	                        <label>firefoam</label>
	                        <description>Allows the construction of firefoam poppers; fire-safety buildings which spread fire-retardant foam in response to encroaching flames.</description>
	                        <baseCost>800</baseCost>
	                        <techLevel>Industrial</techLevel>
	                        <prerequisites>
		                        <li>MicroelectronicsBasics</li>
	                        </prerequisites>
	                   !    <tags>
		    Important  !        <li>ta-ignore</li>
	                   !    </tags>
	                        <requiredResearchBuilding>HiTechResearchBench</requiredResearchBuilding>
	                        <researchViewX>7</researchViewX>
	                        <researchViewY>4</researchViewY>
                        </ResearchProjectDef>

                    */
                    #endregion
                }
                researchProjectsArray[(int)researchProjectDef.techLevel][1]++;  //total projects for techlevel  
                if (researchProjectDef.IsFinished)
                {
                    if (researchProjectDef.techLevel != TechLevel.Undefined)
                    { 
                        researchProjectsArray[(int)researchProjectDef.techLevel][0]++;  //finished projects for techlevel
                    }
                    
                    researchProjectDef.ReapplyAllMods();
                }

                if (!researchProjectDef.IsFinished&&researchProjectDef.techLevel!=TechLevel.Undefined)
                {
                    lowestProjectTechLevel=(TechLevel)Math.Min((int)researchProjectDef.techLevel,(int)lowestProjectTechLevel);
                }
               

            }


            // player researched all techs of techlevel X and below. the techlevel rises to X+1
            suggestedTechLevel = (TechLevel)Math.Max((int)((int)lowestProjectTechLevel+(int)TechAdvancing_Config_Tab.Conditionvalue_A-1), (int)TechAdvancing_Config_Tab.tempOverridableLevel);



            //player researched more than 50% of the techlevel Y then the techlevel rises to Y
            for (int i = 0; i < researchProjectsArray.Length; i++)
            {
                if (researchProjectsArray[i][1] != 0)
                {
                   // Log.Message("Debug GHXX: Division Check for techlevel: "+((TechLevel) i)+ " Researchech projects: "+researchProjectsArray[i][0]+" | Projects Total: "+ researchProjectsArray[i][1]);
                   // Log.Message("Debug GHXX: Division a:" + (float)researchProjectsArray[i][0] / (double)researchProjectsArray[i][1]);
                    
                    if ((float)researchProjectsArray[i][0]/(double)researchProjectsArray[i][1]>0.5)
                    {
                        highestResearchCategoryOverHalf = (TechLevel) i;
                    }
                }
            }
            
            //TechAdvancing.LogOutput.writeLogMessage(Errorlevel.Error, "50% techlvl:" + ((TechLevel)highestResearchCategoryOverHalf));
            
            suggestedTechLevel = (TechLevel)Math.Max((int)suggestedTechLevel, (int)((int)highestResearchCategoryOverHalf + (int)TechAdvancing_Config_Tab.Conditionvalue_B));
            if (suggestedTechLevel != TechLevel.Undefined)
            {
                isInceased = Faction.OfPlayer.def.techLevel < (((int)suggestedTechLevel > (int)TechLevel.Transcendent) ? TechLevel.Transcendent : suggestedTechLevel) ;
                Faction.OfPlayer.def.techLevel = ((int)suggestedTechLevel>(int)TechLevel.Transcendent)?TechLevel.Transcendent:suggestedTechLevel;
                
                //Log.Error("1 Setting techlevel to " +(TechLevel) (((int)suggestedTechLevel > (int)TechLevel.Transcendent) ? TechLevel.Transcendent : suggestedTechLevel));
                if (skippedWorldStart) //hiding the notification on world start
                {
                    if (isInceased)
                    {
                        Find.LetterStack.ReceiveLetter("newTechLevelLetterTitle".Translate(), "newTechLevelLetterContents".Translate(isTribe ? "configTribe".Translate() : "configColony".Translate()) + " " + Faction.OfPlayer.def.techLevel + ".",LetterDefOf.Good);
                        
                        isInceased = false;
                    }
                }
                else
                {
                    skippedWorldStart = true;
                }
            }
            
            /***
            how techlevel increases:
            player researched all techs of techlevel X and below. the techlevel rises to X+1

            player researched more than 50% of the techlevel Y then the techlevel rises to Y
            **/
            RecalculateTechlevel(false,false);
        }

        private static void loadCfgValues() //could be improved using just vanilla loading
        {
            Scribe_Deep.Look(ref TechAdvancing_Config_Tab.Conditionvalue_A, "Conditionvalue_A");
            Scribe_Deep.Look(ref TechAdvancing_Config_Tab.Conditionvalue_B, "Conditionvalue_B");
            Scribe_Deep.Look(ref TechAdvancing_Config_Tab.baseTechlvlCfg, "baseTechlvlCfg");
            Scribe_Deep.Look(ref TechAdvancing_Config_Tab.configCheckboxNeedTechColonists, "configCheckboxNeedTechColonists");
            if (TechAdvancing_Config_Tab.baseTechlvlCfg != 1)
            {
                TechAdvancing_Config_Tab.tempOverridableLevel = (TechAdvancing_Config_Tab.baseTechlvlCfg == 0) ? TechLevel.Neolithic : TechLevel.Industrial;
            }
        }

        internal static TechLevel[] RecalculateTechlevel(bool returnyes=false,bool showIncreaseMsg = true)
        {
            try
            {
                TechLevel suggestedTechLevel1 = (TechLevel)Clamp((int)TechLevel.Undefined,(int)lowestProjectTechLevel + (int)TechAdvancing_Config_Tab.Conditionvalue_A - 1,(int)TechLevel.Transcendent);
                TechLevel suggestedTechLevel2 = (TechLevel)Clamp((int)TechLevel.Undefined, (int)highestResearchCategoryOverHalf + (int)TechAdvancing_Config_Tab.Conditionvalue_B, (int)TechLevel.Transcendent);
                
                if (!returnyes)
                {
                    //  Log.Message("GHXX TECHLEVEL ADVANCER - DEBUG : TECHLEVEL INCREASED TO " + suggestedTechLevel.ToString());
                    //Log.Error("2 Setting techlevel to "+(TechLevel)(((int)suggestedTechLevel2 >(int)TechLevel.Transcendent) ? TechLevel.Transcendent : suggestedTechLevel2));
                    TechLevel unclampedTL = (TechLevel)Clamp((int)TechAdvancing.TechAdvancing_Config_Tab.tempOverridableLevel, (int)Math.Max((int)suggestedTechLevel1, (int)suggestedTechLevel2), (int)TechLevel.Transcendent);

                    if (TechAdvancing_Config_Tab.configCheckboxNeedTechColonists == 1 && !ColonyHasHiTechPeople())
	                {
		                Faction.OfPlayer.def.techLevel = (TechLevel)Clamp((int)TechLevel.Undefined,(int)unclampedTL,(int)TechAdvancing_Config_Tab.maxTechLevelForTribals);
                    }
                    else
	                {
                        Faction.OfPlayer.def.techLevel = unclampedTL;
                    }

                    // ((int)suggestedTechLevel2 > (int)TechLevel.Transcendent) ? TechLevel.Transcendent : suggestedTechLevel2;
                    if (showIncreaseMsg) //used to supress the first update message| Treat as always false
                    {
                        Messages.Message("ConfigEditTechlevelChange".Translate() + " " + (TechLevel)Faction.OfPlayer.def.techLevel + ".", MessageSound.Benefit);
                    }
                    //TRANSLATION: OLD:"Due to editing how Tech Advancing affects your game, your technology level has been changed to"
                    //  Log.Message("GHXX TECHLEVEL ADVANCER - DEBUG : SUCCESS.  New tech lvl: " + Faction.OfPlayer.def.techLevel.ToString());
                }
                else {
                    return new TechLevel[] { suggestedTechLevel1, suggestedTechLevel2, (ColonyHasHiTechPeople()) ? TechLevel.Transcendent : TechAdvancing_Config_Tab.maxTechLevelForTribals };
                }
            }
            catch (Exception)
            {

            }
            return null;
        }

        private static int Clamp(int min, int val, int max) //helper method
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

        public static bool ColonyHasHiTechPeople()  
        {
            FactionDef[] hitechfactions = new FactionDef[] { FactionDefOf.Mechanoid, FactionDefOf.Outlander, FactionDefOf.Spacer, FactionDefOf.PlayerColony };
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
            
            return RimWorld.PawnsFinder.AllMaps_FreeColonists.Any(x => hightechkinds.Contains(x.kindDef.defName.ToLowerInvariant())||((int?)((MapComponent_TA_Expose.TA_Expose_People?.ContainsKey(x)==true)?MapComponent_TA_Expose.TA_Expose_People[x]:null)?.def?.techLevel??-1)>=(int)TechLevel.Industrial); 
        }
    }

    public static class Event
    {
        public static void onKill(Pawn oldPawn) //event for when a pawn dies
        {
            //namespace prefix is required
            if (TechAdvancing.MapComponent_TA_Expose.TA_Expose_People.ContainsKey(oldPawn))
            {
                TechAdvancing.MapComponent_TA_Expose.TA_Expose_People.Remove(oldPawn);
                if (TechAdvancing.MapComponent_TA_Expose.TA_Expose_People.Count == 0 &&   // that means there was something in there before -> now the techlvl is locked
                    TechAdvancing_Config_Tab.configCheckboxNeedTechColonists == 1         // and the limit is enabled
                    )
                {
                    Find.LetterStack.ReceiveLetter("newTechLevelMedievalCapRemLetterTitleRev".Translate(), "newTechLevelMedievalCapRemLetterContentsRev".Translate(), LetterDefOf.BadNonUrgent);
                }
            }
            GHXXTechAdvancing._ResearchManager.RecalculateTechlevel(false,false);
        }
        public static void onNewPawn(Pawn oldPawn)  //event for new pawn in the colony
        {
            if (((int?)oldPawn?.Faction?.def?.techLevel??-1) >= (int)TechLevel.Industrial)
            {
                if (!TechAdvancing.MapComponent_TA_Expose.TA_Expose_People.ContainsKey(oldPawn))
                {
                    TechAdvancing.MapComponent_TA_Expose.TA_Expose_People.Add(oldPawn,oldPawn.Faction);
                    if (TechAdvancing.MapComponent_TA_Expose.TA_Expose_People.Count==1 &&   // that means there was nothing in there before -> now the techlvl is unlocked
                        TechAdvancing_Config_Tab.configCheckboxNeedTechColonists==1         // and the limit is enabled
                        )
                    {
                        Find.LetterStack.ReceiveLetter("newTechLevelMedievalCapRemLetterTitle".Translate(), "newTechLevelMedievalCapRemLetterContents".Translate(_ResearchManager.isTribe ? "configTribe".Translate() : "configColony".Translate()), LetterDefOf.Good);
                    }
                }
                else
                {
                    TechAdvancing.MapComponent_TA_Expose.TA_Expose_People[oldPawn] = oldPawn.Faction;
                }
            }
        }
        public static void postOnNewPawn()  //post version of onNewPawn (after the pawn joined)
        {
            GHXXTechAdvancing._ResearchManager.RecalculateTechlevel(false, false);
        }
    }
}