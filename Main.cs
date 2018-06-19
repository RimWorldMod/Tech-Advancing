
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
        public static TechLevel factionDefault = TechLevel.Undefined;
        public static bool isTribe = true;
        private static bool firstNotificationHidden = false;
        private static int[][] researchProjectsArray = new int[][] { new int[2], new int[2], new int[2],
            new int[2], new int[2], new int[2], new int[2], new int[2]}; // Techlevel --> Researched | Total
                                                                         //   .. ....  . . .. . . . .. .. . 

        public static DateTime startedAt = DateTime.Now;
        public static string facName = "";
        public static bool firstpass = true;
        internal static void _ReapplyAllMods(this ResearchManager _this)    //new ReaookyAllMods Method
        {
            if (Faction.OfPlayerSilentFail?.def?.techLevel == null || Faction.OfPlayer.def.techLevel == TechLevel.Undefined)       // if some mod does something funky again....
                return;


            if (firstpass || facName != Faction.OfPlayer.def.defName)
            {
                startedAt = DateTime.Now;
                facName = Faction.OfPlayer.def.defName;
                try
                {
                    GetAndReloadTL();        //store the default value for the techlevel because we will modify it later and we need the one from right now

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

            foreach (var researchProjectDef in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                //skip the research if it contains the disabled-tag:
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

                if (researchProjectDef.tags?.Any(x => x.defName == "ta-ignore") != true)
                {
                    researchProjectStoreTotal[researchProjectDef.techLevel]++;  //total projects for techlevel  
                    if (researchProjectDef.IsFinished)
                    {   // TODO filter out undefined later
                        researchProjectStoreFinished[researchProjectDef.techLevel]++;  //finished projects for techlevel
                        researchProjectDef.ReapplyAllMods();    // TODO always run it?
                    }
                }
                else
                {
                    LogOutput.WriteLogMessage(Errorlevel.Debug, "Found ta-ignore tag in:" + researchProjectDef.defName);
                }
            }

            TechAdvancing.Rules.researchProjectStoreTotal = researchProjectStoreTotal;
            TechAdvancing.Rules.researchProjectStoreFinished = researchProjectStoreFinished;

            TechLevel newLevel = TechAdvancing.Rules.GetNewTechLevel();

            if (newLevel != TechLevel.Undefined)
            {
                if (firstNotificationHidden && DateTime.Now.Subtract(TimeSpan.FromSeconds(5)) > startedAt) //hiding the notification on world start
                {
                    if (Faction.OfPlayer.def.techLevel < newLevel)
                        Find.LetterStack.ReceiveLetter("newTechLevelLetterTitle".Translate(), "newTechLevelLetterContents".Translate(isTribe ? "configTribe".Translate() : "configColony".Translate()) + " " + newLevel.ToString() + ".", LetterDefOf.PositiveEvent);
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

        internal static TechLevel GetAndReloadTL()
        {
            if (Faction.OfPlayer.def.techLevel > TechLevel.Undefined && _ResearchManager.factionDefault == TechLevel.Undefined)
            {
                _ResearchManager.factionDefault = Faction.OfPlayer.def.techLevel;
                TechAdvancing_Config_Tab.baseFactionTechLevel = Faction.OfPlayer.def.techLevel;
            }
            if (Faction.OfPlayer.def.techLevel == TechLevel.Undefined)
            {
                LogOutput.WriteLogMessage(Errorlevel.Warning, "Called without valid TL");
#if DEBUG
                throw new InvalidOperationException("If you see this message please report it immediately. Thanks! (0x1)");
#endif
            }
            return Faction.OfPlayer.def.techLevel;
        }

        internal static void RecalculateTechlevel(bool showIncreaseMsg = true)
        {
            if (Faction.OfPlayerSilentFail?.def?.techLevel == null || Faction.OfPlayer.def.techLevel == TechLevel.Undefined)   // if some mod does something funky again....
                return;

            GetAndReloadTL();
            TechLevel baseNewTL = Rules.GetNewTechLevel();
            if (TechAdvancing_Config_Tab.configCheckboxNeedTechColonists == 1 && !Util.ColonyHasHiTechPeople())
            {
                Faction.OfPlayer.def.techLevel = (TechLevel)Util.Clamp((int)TechLevel.Undefined, (int)baseNewTL, (int)TechAdvancing_Config_Tab.maxTechLevelForTribals);
            }
            else
            {
                Faction.OfPlayer.def.techLevel = baseNewTL;
            }

            if (showIncreaseMsg) //used to supress the first update message| Treat as always false
            {
                Messages.Message("ConfigEditTechlevelChange".Translate() + " " + (TechLevel)Faction.OfPlayer.def.techLevel + ".", MessageTypeDefOf.PositiveEvent);
            }
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
                    Find.LetterStack.ReceiveLetter("newTechLevelMedievalCapRemLetterTitleRev".Translate(), "newTechLevelMedievalCapRemLetterContentsRev".Translate(), LetterDefOf.NegativeEvent);
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
                        Find.LetterStack.ReceiveLetter("newTechLevelMedievalCapRemLetterTitle".Translate(), "newTechLevelMedievalCapRemLetterContents".Translate(_ResearchManager.isTribe ? "configTribe".Translate() : "configColony".Translate()), LetterDefOf.PositiveEvent);
                        TechAdvancing._ResearchManager.RecalculateTechlevel(false);
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