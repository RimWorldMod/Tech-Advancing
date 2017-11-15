
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

namespace TechAdvancing
{

    [StaticConstructorOnStartup]
    public class Injector_GHXXTechAdvancing
    {
        static Injector_GHXXTechAdvancing()     //Detour the method that gets run when a research gets finished. Old school detour. Could be replaced with harmony
        {
            MethodInfo source = typeof(ResearchManager).GetMethod("ReapplyAllMods", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo destination = typeof(_ResearchManager).GetMethod("_ReapplyAllMods", BindingFlags.Static | BindingFlags.NonPublic);
            //Log.Message("Source method = " + source.Name + "Target = " + destination.Name);

            Detour.DoDetour(source, destination);
            GameObject initializer = new GameObject("GHXXTAMapComponentInjector");

            initializer.AddComponent<MapComponentInjector>();
            UnityEngine.Object.DontDestroyOnLoad(initializer);

            HarmonyDetours.Setup();
        }

    }

    /// <summary>
    /// Class storing the new ReapplyAllMods method to perform the old-school detour with.
    /// </summary>
    internal static class _ResearchManager  // TODO use Harmony for this.
    {
        private static TechLevel highestProjTechsOverHalf = TechLevel.Undefined;
        private static TechLevel lowestProjectLvlNotResearched = TechLevel.Undefined;

        public static TechLevel factionDefault = TechLevel.Undefined;
        public static bool isTribe = true;
        private static bool firstNotificationHidden = false;
        private static TechLevel suggestedTechLevel = TechLevel.Undefined;
        private static int[][] researchProjectsArray = new int[][] { new int[2], new int[2], new int[2],
            new int[2], new int[2], new int[2], new int[2], new int[2]}; // Techlevel --> Researched | Total
                                                                         //   .. ....  . . .. . . . .. .. . 


        public static string facName = "";
        public static bool firstpass = true;
        private static bool isInceased = false;
        internal static void _ReapplyAllMods(this ResearchManager _this)    //new ReaookyAllMods Method
        {
            if (firstpass || facName != Faction.OfPlayer.def.defName)
            {
                facName = Faction.OfPlayer.def.defName;
                try
                {
                    factionDefault = Faction.OfPlayer.def.techLevel;        //store the default value for the techlevel because we will modify it later and we need the one from right now
                    TechAdvancing_Config_Tab.baseFactionTechLevel = factionDefault;
                    isTribe = factionDefault == TechLevel.Neolithic;
                    LoadCfgValues();
                    firstpass = false;

                    //Debug
                    LogOutput.WriteLogMessage(Errorlevel.Debug, "Con A val= " + TechAdvancing_Config_Tab.Conditionvalue_A + "|||Con B Val= " + TechAdvancing_Config_Tab.Conditionvalue_B);

                }
                catch (Exception ex)
                {
                    LogOutput.WriteLogMessage(Errorlevel.Error, "Caught error in Reapply All Mods: " + ex.ToString());
                }

            }

            var researchProjectStoreTotal = new Dictionary<TechLevel, int>();
            var researchProjectStoreFinished = new Dictionary<TechLevel, int>();

            for (int i = 0; i < Enum.GetValues(typeof(TechLevel)).Length; i++)
            {
                researchProjectStoreTotal.Add((TechLevel)i, 0);
                researchProjectStoreFinished.Add((TechLevel)i, 0);
            }

            lowestProjectLvlNotResearched = TechLevel.Transcendent; //set it to something high.

            foreach (var researchProjectDef in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                if (researchProjectDef.tags?.Contains("ta-ignore") == true)
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
                researchProjectStoreTotal[researchProjectDef.techLevel]++;  //total projects for techlevel  
                if (researchProjectDef.IsFinished)
                {   // TODO filter out undefined later
                    researchProjectStoreFinished[researchProjectDef.techLevel]++;  //finished projects for techlevel
                    researchProjectDef.ReapplyAllMods();    // TODO always run it?
                }

                if (!researchProjectDef.IsFinished && researchProjectDef.techLevel != TechLevel.Undefined)
                {
                    if (lowestProjectLvlNotResearched > researchProjectDef.techLevel)   // TODO merge?
                        lowestProjectLvlNotResearched = researchProjectDef.techLevel;
                }
            }

            TechAdvancing.Rules.researchProjectStoreTotal = researchProjectStoreTotal;
            TechAdvancing.Rules.researchProjectStoreFinished = researchProjectStoreFinished;

            // player researched all techs of techlevel X and below. the techlevel rises to X+1
            // techlevelRuleA = (TechLevel)Math.Max((int)lowestProjectLvlNotResearched + TechAdvancing_Config_Tab.Conditionvalue_A - 1, (int)TechAdvancing_Config_Tab.baseFactionTechLevel);

            // techlevelRuleA = (TechLevel)Util.Clamp(0, (int)techlevelRuleA, (int)TechLevel.Transcendent);

            //player researched more than 50% of the techlevel Y then the techlevel rises to Y
            //int highestProjTechsOverHalf = 0;
            //for (int i = 0; i < researchProjectStoreTotal.Count; i++)
            //{
            //    if (researchProjectStoreTotal[(TechLevel)i] != 0)
            //    {
            //        if ((float)researchProjectStoreFinished[(TechLevel)i] / (float)researchProjectStoreFinished[(TechLevel)i] > 0.5f)
            //            highestProjTechsOverHalf = i;

            //    }
            //}

            //techlevelRuleB = (TechLevel)Util.Clamp((int)TechAdvancing.TechAdvancing_Config_Tab.baseFactionTechLevel, highestProjTechsOverHalf + TechAdvancing_Config_Tab.Conditionvalue_B, (int)TechLevel.Transcendent);
            //techlevelResult = (TechLevel)(Math.Max((int)techlevelRuleA, (int)techlevelRuleB));

            TechLevel newLevel = TechAdvancing.Rules.GetNewTechLevel();

            if (newLevel != TechLevel.Undefined)
            {
                if (firstNotificationHidden) //hiding the notification on world start
                {
                    if (Faction.OfPlayer.def.techLevel < newLevel)
                        Find.LetterStack.ReceiveLetter("newTechLevelLetterTitle".Translate(), "newTechLevelLetterContents".Translate(isTribe ? "configTribe".Translate() : "configColony".Translate()) + " " + newLevel.ToString() + ".", LetterDefOf.Good);
                }
                else
                {
                    firstNotificationHidden = true;
                }

                Faction.OfPlayer.def.techLevel = newLevel;
            }

            /***
            how techlevel increases:
            player researched all techs of techlevel X and below. the techlevel rises to X+1

            player researched more than 50% of the techlevel Y then the techlevel rises to Y
            **/
            RecalculateTechlevel(false);
        }

        private static void LoadCfgValues() //could be improved using just vanilla loading  // TODO obsolete?
        {
            Scribe_Deep.Look(ref TechAdvancing_Config_Tab.Conditionvalue_A, "Conditionvalue_A");
            Scribe_Deep.Look(ref TechAdvancing_Config_Tab.Conditionvalue_B, "Conditionvalue_B");
            Scribe_Deep.Look(ref TechAdvancing_Config_Tab.baseTechlvlCfg, "baseTechlvlCfg");
            Scribe_Deep.Look(ref TechAdvancing_Config_Tab.configCheckboxNeedTechColonists, "configCheckboxNeedTechColonists");
            if (TechAdvancing_Config_Tab.baseTechlvlCfg != 1)
            {
                TechAdvancing_Config_Tab.baseFactionTechLevel = (TechAdvancing_Config_Tab.baseTechlvlCfg == 0) ? TechLevel.Neolithic : TechLevel.Industrial;
            }
        }

        internal static void RecalculateTechlevel(bool showIncreaseMsg = true)
        {
            //try
            //{
            //TechLevel suggestedTechLevel1 = (TechLevel)Util.Clamp((int)TechLevel.Undefined, (int)lowestProjectLvlNotResearched + (int)TechAdvancing_Config_Tab.Conditionvalue_A - 1, (int)TechLevel.Transcendent);
            //TechLevel suggestedTechLevel2 = (TechLevel)Util.Clamp((int)TechLevel.Undefined, (int)highestProjTechsOverHalf + (int)TechAdvancing_Config_Tab.Conditionvalue_B, (int)TechLevel.Transcendent);

            //if (!returnyes)
            //{
            //  Log.Message("GHXX TECHLEVEL ADVANCER - DEBUG : TECHLEVEL INCREASED TO " + suggestedTechLevel.ToString());
            //Log.Error("2 Setting techlevel to "+(TechLevel)(((int)suggestedTechLevel2 >(int)TechLevel.Transcendent) ? TechLevel.Transcendent : suggestedTechLevel2));
            //TechLevel unclampedTL = (TechLevel)Util.Clamp((int)TechAdvancing.TechAdvancing_Config_Tab.baseFactionTechLevel, (int)Math.Max((int)suggestedTechLevel1, (int)suggestedTechLevel2), (int)TechLevel.Transcendent);
            
            TechLevel baseNewTL = Rules.GetNewTechLevel();
            if (TechAdvancing_Config_Tab.configCheckboxNeedTechColonists == 1 && !Util.ColonyHasHiTechPeople())
            {
                Faction.OfPlayer.def.techLevel = (TechLevel)Util.Clamp((int)TechLevel.Undefined, (int)baseNewTL, (int)TechAdvancing_Config_Tab.maxTechLevelForTribals);
            }
            else
            {
                Faction.OfPlayer.def.techLevel = baseNewTL;
            }

            // ((int)suggestedTechLevel2 > (int)TechLevel.Transcendent) ? TechLevel.Transcendent : suggestedTechLevel2;
            if (showIncreaseMsg) //used to supress the first update message| Treat as always false
            {
                Messages.Message("ConfigEditTechlevelChange".Translate() + " " + (TechLevel)Faction.OfPlayer.def.techLevel + ".", MessageSound.Benefit);
            }
            //TRANSLATION: OLD:"Due to editing how Tech Advancing affects your game, your technology level has been changed to"
            //  Log.Message("GHXX TECHLEVEL ADVANCER - DEBUG : SUCCESS.  New tech lvl: " + Faction.OfPlayer.def.techLevel.ToString());
            //}
            //else
            //{
            //    return new TechLevel[] { suggestedTechLevel1, suggestedTechLevel2, (Util.ColonyHasHiTechPeople()) ? TechLevel.Transcendent : TechAdvancing_Config_Tab.maxTechLevelForTribals };
            //}
            //}
            //catch (Exception)
            //{

            //}
            //return null;
        }
    }

    public static class Event
    {
        public static void OnKill(Pawn oldPawn) //event for when a pawn dies
        {
            //namespace prefix is required
            if (TechAdvancing.MapCompSaveHandler.ColonyPeople.ContainsKey(oldPawn))
            {
                TechAdvancing.MapCompSaveHandler.ColonyPeople.Remove(oldPawn);
                if (TechAdvancing.MapCompSaveHandler.ColonyPeople.Count == 0 &&   // that means there was something in there before -> now the techlvl is locked
                    TechAdvancing_Config_Tab.configCheckboxNeedTechColonists == 1         // and the limit is enabled
                    )
                {
                    Find.LetterStack.ReceiveLetter("newTechLevelMedievalCapRemLetterTitleRev".Translate(), "newTechLevelMedievalCapRemLetterContentsRev".Translate(), LetterDefOf.BadNonUrgent);
                }
            }
            TechAdvancing._ResearchManager.RecalculateTechlevel(false);
        }
        public static void OnNewPawn(Pawn oldPawn)  //event for new pawn in the colony
        {
            if (((int?)oldPawn?.Faction?.def?.techLevel ?? -1) >= (int)TechLevel.Industrial)
            {
                if (!TechAdvancing.MapCompSaveHandler.ColonyPeople.ContainsKey(oldPawn))
                {
                    TechAdvancing.MapCompSaveHandler.ColonyPeople.Add(oldPawn, oldPawn.Faction);
                    if (TechAdvancing.MapCompSaveHandler.ColonyPeople.Count == 1 &&   // that means there was nothing in there before -> now the techlvl is unlocked
                        TechAdvancing_Config_Tab.configCheckboxNeedTechColonists == 1         // and the limit is enabled
                        )
                    {
                        Find.LetterStack.ReceiveLetter("newTechLevelMedievalCapRemLetterTitle".Translate(), "newTechLevelMedievalCapRemLetterContents".Translate(_ResearchManager.isTribe ? "configTribe".Translate() : "configColony".Translate()), LetterDefOf.Good);
                    }
                }
                else
                {
                    TechAdvancing.MapCompSaveHandler.ColonyPeople[oldPawn] = oldPawn.Faction;
                }
            }
        }
        public static void PostOnNewPawn()  //post version of onNewPawn (after the pawn joined)
        {
            TechAdvancing._ResearchManager.RecalculateTechlevel(false);
        }
    }
}