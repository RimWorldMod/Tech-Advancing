
using Multiplayer.API;
using RimWorld;
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
            GameObject initializer = new GameObject("TechAdvancingStartupObject");
            ConfigButtonTexture = ContentFinder<Texture2D>.Get("TechAdvancingSettingsLogo", true);
            
            initializer.AddComponent<MapComponentInjector>();
            UnityEngine.Object.DontDestroyOnLoad(initializer);

            HarmonyDetours.Setup();

            if (MP.enabled)
            {
                MP.RegisterAll();
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

        public static void OnNewPawn(Pawn oldPawn)  //event for new pawn in the colony
        {
            if (((int?)oldPawn?.Faction?.def?.techLevel ?? -1) >= (int)TechLevel.Industrial)
            {
                if (!TechAdvancing.MapCompSaveHandler.ColonyPeople.ContainsKey(oldPawn))
                {
                    TechAdvancing.MapCompSaveHandler.ColonyPeople.Add(oldPawn, oldPawn.Faction);
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
                    TechAdvancing.MapCompSaveHandler.ColonyPeople[oldPawn] = oldPawn.Faction;
                }
            }
        }
        public static void PostOnNewPawn()  //post version of onNewPawn (after the pawn joined)
        {
            TechAdvancing.TA_ResearchManager.RecalculateTechlevel(false);
        }
    }
}