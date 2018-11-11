using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using TechAdvancing;

namespace TechAdvancing
{
    public class MapCompSaveHandler : MapComponent
    {

        public MapCompSaveHandler(Map map) : base(map)
        {

        }

        private static Dictionary<int, int> Configvalues = new Dictionary<int, int>();
        /// <summary>
        /// Stores all the pawns that joined along with their old Faction
        /// </summary>
        public static Dictionary<Pawn, Faction> ColonyPeople = new Dictionary<Pawn, Faction>(); //pawn , ORIGINAL faction

        public static bool IsValueSaved(string key) { return Configvalues.ContainsKey(GetInt(key)); }

        public static void TA_ExposeData(string key, ref int value, TA_Expose_Mode mode = TA_Expose_Mode.Load)
        {
            bool accessWasValid = false;
            if (mode == TA_Expose_Mode.Save)
            {
                LogOutput.WriteLogMessage(Errorlevel.Debug, "Adding " + key + " : " + value + "to save dictionary");
                if (Configvalues.ContainsKey(GetInt(key)))
                {
                    Configvalues.Remove(GetInt(key));
                }
                Configvalues.Add(GetInt(key), value);
            }
            else if (mode == TA_Expose_Mode.Load)
            {
                accessWasValid = Configvalues.TryGetValue(GetInt(key), out int tempval);
                if (accessWasValid)
                {
                    value = tempval;
                }
                else
                {
                    //TA_Expose_Numbers.Add(getInt(key),)
                    LogOutput.WriteLogMessage(Errorlevel.Information, "Value " + GetInt(key) + " could not be loaded. This usually happens when updating to the new config-system. Try saving and reloading the map.");
                }

                LogOutput.WriteLogMessage(Errorlevel.Debug, "Loaded " + key + " : " + value + "from save dictionary. Success: " + accessWasValid);
            }
        }

        private static int GetInt(string key)
        {
            return (int)Enum.Parse(typeof(TA_Expose_Name), key);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref Configvalues, "TA_Expose_Numbers", LookMode.Value, LookMode.Value);
            int isPplDictSaved = 1;
            //LogOutput.WriteLogMessage(Errorlevel.Information, "val:" + isPplDictSaved.ToString());
            Scribe_Values.Look(ref isPplDictSaved, "TA_Expose_People_isSaved", -1, true);
            //LogOutput.WriteLogMessage(Errorlevel.Information, "val:" + isPplDictSaved.ToString());
            if (ColonyPeople != null)
            {
                ColonyPeople.RemoveAll(x => x.Key == null);
            }
            if (isPplDictSaved == 1)
            {
                Scribe_Collections.Look(ref ColonyPeople, "TA_Expose_People", LookMode.Reference, LookMode.Reference);
                //LogOutput.WriteLogMessage(Errorlevel.Information, "Read TA_ExposePeople");
            }
            TechAdvancing_Config_Tab.ExposeData(TA_Expose_Mode.Load);
            if (ColonyPeople == null)
            {
                ColonyPeople = new Dictionary<Pawn, Faction>();
            }
            LogOutput.WriteLogMessage(Errorlevel.Information, "Loading finished.");
        }
    }

    public enum TA_Expose_Mode
    {
        Save,
        Load
    }

    public enum TA_Expose_Name
    {
        Conditionvalue_A,
        Conditionvalue_B,
        Conditionvalue_B_s,
        baseTechlvlCfg,
        configCheckboxNeedTechColonists,
    }
}
