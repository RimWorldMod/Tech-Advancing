using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TechAdvancing
{
    /// <summary>
    /// Class storing all the detours and the detour call.
    /// </summary>
    class HarmonyDetours
    {
        /// <summary>
        /// Method for performing all the detours via Harmony.
        /// </summary>
        public static void Setup()
        {
            var harmony = new Harmony("com.ghxx.rimworld.techadvancing");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    /// <summary>
    /// Prefix for adding the button below the progressbar of the research window. The button is used for opening the config screen.
    /// </summary>
    [HarmonyPatch(typeof(RimWorld.MainTabWindow_Research))]
    [HarmonyPatch("DrawLeftRect")]
    [HarmonyPatch(new Type[] { typeof(Rect) })]
    class TA_Research_Menu_Patch
    {
        [SuppressMessage("Codequality", "IDE0051:Remove unused private member", Justification = "Referenced at runtime by harmony")]
        static void Prefix(Rect leftOutRect)
        {
            // code for adding the techadvancing config button to the (vanilla) research screen
            Rect TA_Cfgrect = new Rect(0f, 0f, 30, 30f);
            TA_Cfgrect.x = leftOutRect.width - TA_Cfgrect.width - 5;
            TA_Cfgrect.y = 0;

            if (Widgets.ButtonImage(TA_Cfgrect, TechAdvancingStartupClass.ConfigButtonTexture, Color.white, Color.cyan, true))
            {
                SoundDef.Named("ResearchStart").PlayOneShotOnCamera();
                Find.WindowStack.Add((Window)new TechAdvancing_Config_Tab());
            }
        }
    }

    /// <summary>
    /// Replace research cost calc method to be able to remove cost cap, like in A18
    /// </summary>
    [HarmonyPatch(typeof(Verse.ResearchProjectDef))]
    [HarmonyPatch("CostFactor")]
    [HarmonyPatch(new Type[] { typeof(TechLevel) })]
    class TA_ReplaceResearchProjectDef
    {
        [SuppressMessage("Codequality", "IDE0051:Remove unused private member", Justification = "Referenced at runtime by harmony")]
        static void Postfix(Verse.ResearchProjectDef __instance, ref float __result, TechLevel researcherTechLevel)
        {
            if (researcherTechLevel >= __instance.techLevel)
            {
                __result = 1f;
            }
            else
            {
                int num = __instance.techLevel - researcherTechLevel;
                __result = 1f + num * 0.5f;

                if (TechAdvancing_Config_Tab.ConfigCheckboxDisableCostMultiplicatorCap == 0)
                {
                    __result = Mathf.Min(__result, 2);
                }

                if (TechAdvancing_Config_Tab.ConfigCheckboxMakeHigherResearchesSuperExpensive == 1)
                {
                    __result *= (float)(TechAdvancing_Config_Tab.ConfigCheckboxMakeHigherResearchesSuperExpensiveFac * Math.Pow(2, num));
                }
            }

            __result *= TechAdvancing_Config_Tab.ConfigChangeResearchCostFacAsFloat();

            __result = (float)Math.Round(__result, 2);
        }
    }

    /// <summary>
    /// Patch for having a method called when a pawn dies.
    /// </summary>
    [HarmonyPatch(typeof(Verse.Pawn))]
    [HarmonyPatch("Kill")]
    [HarmonyPatch(new Type[] { typeof(DamageInfo?), typeof(Hediff) })]
    class TA_OnKill_Event
    {
        [SuppressMessage("Codequality", "IDE0051:Remove unused private member", Justification = "Referenced at runtime by harmony")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameters", Justification = "Referenced at runtime by harmony")]
        static void Postfix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit = null)
        {
            TechAdvancing.Event.OnKill(__instance);
        }
    }

    /// <summary>
    /// Patch for getting notified about faction changes. E.g.: when a pawn joins the colony.
    /// </summary>
    [HarmonyPatch(typeof(Verse.Pawn))]
    [HarmonyPatch("SetFaction")]
    [HarmonyPatch(new Type[] { typeof(Faction), typeof(Pawn) })]
    class TA_OnNewPawn_Event
    {
        [SuppressMessage("Codequality", "IDE0051:Remove unused private member", Justification = "Referenced at runtime by harmony")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameters", Justification = "Referenced at runtime by harmony")]
        static void Prefix(Pawn __instance, Faction newFaction, Pawn recruiter = null)
        {
            TechAdvancing.Event.OnNewPawn(__instance);
        }
    }

    /// <summary>
    /// Postfix Patch for getting to know the new faction.
    /// </summary>
    [HarmonyPatch(typeof(Verse.Pawn))]
    [HarmonyPatch("SetFaction")]
    [HarmonyPatch(new Type[] { typeof(Faction), typeof(Pawn) })]
    class TA_PostOnNewPawn_Event
    {
        [SuppressMessage("Codequality", "IDE0051:Remove unused private member", Justification = "Referenced at runtime by harmony")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameters", Justification = "Referenced at runtime by harmony")]
        static void Postfix(Faction newFaction, Pawn recruiter = null)
        {
            TechAdvancing.Event.PostOnNewPawn();
        }
    }

    /// <summary>
    /// Postfix Patch for hooking the LoadGame method to load configs
    /// </summary>
    [HarmonyPatch(typeof(Verse.Game))]
    [HarmonyPatch("LoadGame")]
    [HarmonyPatch(new Type[] { })]
    class TA_PostOnMapLoad_Event
    {
        [SuppressMessage("Codequality", "IDE0051:Remove unused private member", Justification = "Referenced at runtime by harmony")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameters", Justification = "Referenced at runtime by harmony")]
        static void Postfix()
        {
            LogOutput.WriteLogMessage(Errorlevel.Information, "Loading config...");
            var TA_currentWorld = Find.World;

            if (TA_currentWorld.components.Any(x => x is WorldCompSaveHandler wcshObj && wcshObj.isInitialized))
                LogOutput.WriteLogMessage(Errorlevel.Warning, "Found an already existing and initialized worldcomponent!!!");


            var wcsh = TA_currentWorld.GetComponent<WorldCompSaveHandler>();
            var wcshHasData = wcsh?.GetConfigValueNames?.Any() ?? false;

            if (wcsh?.isInitialized != true)
            {
                if (wcsh == null)
                {
                    wcsh = new WorldCompSaveHandler(TA_currentWorld);

                    TA_currentWorld.components.Add(wcsh);
                    LogOutput.WriteLogMessage(Errorlevel.Information, "Adding a WorldComponent to store some information.");
                }

                bool needUpgrade = wcsh.GetConfigValueNames.Count == 0
                    && Find.Maps.Any(x => x.GetComponent<MapCompSaveHandler>() is MapCompSaveHandler && MapCompSaveHandler.GetConfigValueNames.Count > 0); // TODO cleanup. This was added on 28.Feb.2020


                if (needUpgrade)
                {
                    LogOutput.WriteLogMessage(Errorlevel.Warning, "Detected legacy save system! Upgrading...");
                    wcsh.LoadValuesForUpgrade(MapCompSaveHandler.Configvalues, MapCompSaveHandler.ColonyPeople);

                    int removalCount = 0;
                    foreach (var map in Find.Maps)
                    {
                        while (true)
                        {
                            var comp = map.GetComponent<MapCompSaveHandler>();
                            if (comp == null)
                                break;
                            else
                            {
                                if (map.components.Remove(comp))
                                    removalCount++;
                                else
                                    LogOutput.WriteLogMessage(Errorlevel.Error, "Cleaning up 1 element failed.");
                            }
                        }
                    }

                    LogOutput.WriteLogMessage(Errorlevel.Warning, $"Removed {removalCount} old dataset(s).");
                }

                wcsh.isInitialized = true;
            }
            TechAdvancing_Config_Tab.worldCompSaveHandler = wcsh;
            wcsh.ExposeData();
            LoadCfgValues();

        }
        internal static void LoadCfgValues()
        {
            if (TechAdvancing_Config_Tab.worldCompSaveHandler.world != Find.World)
            {
                LogOutput.WriteLogMessage(Errorlevel.Warning, "wcsh not referencing the current world!!!");
            }

            TechAdvancing_Config_Tab.ExposeData(TA_Expose_Mode.Load);

            if (TechAdvancing_Config_Tab.BaseTechlvlCfg != 1)
            {
                TechAdvancing_Config_Tab.baseFactionTechLevel = (TechAdvancing_Config_Tab.BaseTechlvlCfg == 0) ? TechLevel.Neolithic : TechLevel.Industrial;
            }
        }
    }

    /// <summary>
    /// Postfix Patch for the research manager to do the techlevel calculation
    /// </summary>
    [HarmonyPatch(typeof(RimWorld.ResearchManager))]
    [HarmonyPatch("ReapplyAllMods")]
    static class TA_ResearchManager
    {
        public static TechLevel factionDefault = TechLevel.Undefined;
        public static bool isTribe = true;
        private static bool firstNotificationHidden = false;

        public static DateTime startedAt = DateTime.Now;
        public static string facName = "";
        public static bool firstpass = true;

        [SuppressMessage("Codequality", "IDE0051:Remove unused private member", Justification = "Referenced at runtime by harmony")]
        static void Postfix()
        {
            if (Faction.OfPlayerSilentFail?.def?.techLevel == null || Faction.OfPlayer.def.techLevel == TechLevel.Undefined) // abort if our techlevel is undefined for some reason
                return;


            if (firstpass || facName != Faction.OfPlayer.def.defName)
            {
                startedAt = DateTime.Now;
                facName = Faction.OfPlayer.def.defName;
                try
                {
                    GetAndReloadTL();        //store the default value for the techlevel because we will modify it later and we need the one from right now

                    isTribe = factionDefault == TechLevel.Neolithic;
                    //LoadCfgValues();
                    firstpass = false;

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
                    {
                        researchProjectStoreFinished[researchProjectDef.techLevel]++;  //finished projects for techlevel
                        researchProjectDef.ReapplyAllMods();
                    }
                }
                else
                {
                    LogOutput.WriteLogMessage(Errorlevel.Debug, "Found ta-ignore tag in:" + researchProjectDef.defName);
                }

                if (researchProjectDef.IsFinished)
                    researchProjectDef.ReapplyAllMods();
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

        internal static TechLevel GetAndReloadTL()
        {
            if (Faction.OfPlayer.def.techLevel > TechLevel.Undefined && TA_ResearchManager.factionDefault == TechLevel.Undefined)
            {
                TA_ResearchManager.factionDefault = Faction.OfPlayer.def.techLevel;
                TechAdvancing_Config_Tab.baseFactionTechLevel = Faction.OfPlayer.def.techLevel;
            }
            if (Faction.OfPlayer.def.techLevel == TechLevel.Undefined)
            {
                LogOutput.WriteLogMessage(Errorlevel.Warning, "Called without valid TL");
            }
            return Faction.OfPlayer.def.techLevel;
        }

        internal static void RecalculateTechlevel(bool showIncreaseMsg = true)
        {
            if (Faction.OfPlayerSilentFail?.def?.techLevel == null || Faction.OfPlayer.def.techLevel == TechLevel.Undefined)   // if some mod does something funky again....
                return;

            GetAndReloadTL();
            TechLevel baseNewTL = Rules.GetNewTechLevel();
            if (TechAdvancing_Config_Tab.ConfigCheckboxNeedTechColonists == 1 && !Util.ColonyHasHiTechPeople())
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
}
