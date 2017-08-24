using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using TechAdvancing;

namespace TechAdvancing
{
    public class MapComponent_TA_Expose : MapComponent
    {

        public MapComponent_TA_Expose(Map map):base(map)
        {

        }

        private static Dictionary<int, int> TA_Expose_Numbers = new Dictionary<int, int>();
        public static Dictionary<Pawn, Faction> TA_Expose_People = new Dictionary<Pawn, Faction>(); //pawn , ORIGINAL faction

        public static bool isValueSaved(string key) {  return TA_Expose_Numbers.ContainsKey(getInt(key)); }

        public static void TA_ExposeData(string key, ref int value, TA_Expose_Mode mode = TA_Expose_Mode.Load)
        {
            bool accessWasValid = false;
            if(mode == TA_Expose_Mode.Save)
            {
                LogOutput.writeLogMessage(Errorlevel.Debug, "Adding " + key + " : " + value +"to save dictionary");
                if (TA_Expose_Numbers.ContainsKey(getInt(key)))
                {
                    TA_Expose_Numbers.Remove(getInt(key));
                }
                TA_Expose_Numbers.Add(getInt(key), value);
            }
            else if(mode == TA_Expose_Mode.Load)
            {
                int tempval = 0;
                accessWasValid=TA_Expose_Numbers.TryGetValue(getInt(key), out tempval);
                if(accessWasValid)
                {
                    value = tempval;
                }
                else
                {
                    //TA_Expose_Numbers.Add(getInt(key),)
                    LogOutput.writeLogMessage(Errorlevel.Warning, "Value " + getInt(key) + " could not be loaded. This usually happens when updating to the new config-system. Try saving and reloading the map.");
                }

                LogOutput.writeLogMessage(Errorlevel.Debug,"Loaded " + key + " : " + value + "from save dictionary. Valid? "+ accessWasValid);
            }
         }

        private static int getInt(string key)
        {
            return (int)Enum.Parse(typeof(TA_Expose_Name),key);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref TA_Expose_Numbers, "TA_Expose_Numbers", LookMode.Value, LookMode.Value);
            int isPplDictSaved = 1;
            LogOutput.writeLogMessage(Errorlevel.Warning, "val:"+ isPplDictSaved.ToString());
            Scribe_Values.Look(ref isPplDictSaved, "TA_Expose_People_isSaved", -1,true);
            LogOutput.writeLogMessage(Errorlevel.Warning, "val:"+ isPplDictSaved.ToString());
            if (isPplDictSaved == 1)
            {
                Scribe_Collections.Look(ref TA_Expose_People, "TA_Expose_People", LookMode.Reference, LookMode.Reference);
                LogOutput.writeLogMessage(Errorlevel.Warning, "Read TA_ExposePeople");
            }
            LogOutput.writeLogMessage(Errorlevel.Warning, "Loading");
            TechAdvancing_Config_Tab.ExposeData(TA_Expose_Mode.Load);
            if (TA_Expose_People==null)
            {
                TA_Expose_People = new Dictionary<Pawn, Faction>();
            }
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
        baseTechlvlCfg,
        configCheckboxNeedTechColonists,
    }
}
