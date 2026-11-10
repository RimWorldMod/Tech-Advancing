
using Multiplayer.API;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace TechAdvancing
{

    [StaticConstructorOnStartup]
    public class TechAdvancingStartupClass
    {
        public static Texture2D ConfigButtonTexture { get; }

        static TechAdvancingStartupClass()     // Initialize the mod
        {
            ConfigTabValueSavedAttribute.BuildDefaultValueCache();

            ConfigButtonTexture = ContentFinder<Texture2D>.Get("TechAdvancingSettingsLogo", true);

            HarmonyDetours.Setup();

            try
            {
                if (MP.enabled)
                {
                    MP.RegisterAll();
                }
            }
            catch (MissingMethodException) // somehow this throws an exception for some people all of the sudden
            {
                LogOutput.WriteLogMessage(Errorlevel.Information, "Oddly checking the availability of the multiplayer mod threw an error. " +
                    "If you are not using that mod you can freely ignore this information. If you are using the mod, try moving this mod further up in the load order.");
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
                    TechAdvancing_Config_Tab.ConfigCheckboxNeedTechColonists == 1         // and the limit is enabled
                    )
                {
                    Find.LetterStack.ReceiveLetter("newTechLevelMedievalCapRemLetterTitleRev".Translate(), "newTechLevelMedievalCapRemLetterContentsRev".Translate(), LetterDefOf.NegativeEvent);
                }
            }
            TechAdvancing.TA_ResearchManager.RecalculateTechlevel(false);
        }

        public static void OnNewPawn(Pawn newPawn, Faction newFaction)  //event for new pawn in the colony
        {
            if (newPawn == null || newFaction?.IsPlayer != true) // skip pawns on the map that wont belong to the player and pawns that are null or dont have a faction
            {
                return;
            }

            if (((int?)newPawn?.Faction?.def?.techLevel ?? -1) >= (int)TechLevel.Industrial) 
            {
                if (!TechAdvancing.MapCompSaveHandler.ColonyPeople.ContainsKey(newPawn))
                {
                    TechAdvancing.MapCompSaveHandler.ColonyPeople.Add(newPawn, newPawn.Faction);
                    if (TechAdvancing.MapCompSaveHandler.ColonyPeople.Count == 1 &&   // that means there was nothing in there before -> now the techlvl is unlocked
                        TechAdvancing_Config_Tab.ConfigCheckboxNeedTechColonists == 1         // and the limit is enabled
                        )
                    {
                        Find.LetterStack.ReceiveLetter("newTechLevelMedievalCapRemLetterTitle".Translate(), "newTechLevelMedievalCapRemLetterContents".Translate(TA_ResearchManager.isTribe ? "configTribe".Translate() : "configColony".Translate()), LetterDefOf.PositiveEvent);
                        TechAdvancing.TA_ResearchManager.RecalculateTechlevel(false);
                    }
                }
                else
                {
                    TechAdvancing.MapCompSaveHandler.ColonyPeople[newPawn] = newPawn.Faction;
                }
            }
        }
        public static void PostOnNewPawn()  //post version of onNewPawn (after the pawn joined)
        {
            TechAdvancing.TA_ResearchManager.RecalculateTechlevel(false);
        }
    }
}