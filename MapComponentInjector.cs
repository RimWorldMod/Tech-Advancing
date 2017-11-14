using System;
using UnityEngine;
using Verse;
using System.Linq;
using Object = UnityEngine.Object;
using RimWorld;

namespace TechAdvancing
{
    class MapComponentInjector : MonoBehaviour
    {
        private static Type TA_Storage = typeof(MapComponent_TA_Expose);

        

        public void FixedUpdate()
        {
            if (Current.ProgramState != ProgramState.Playing)
            {
                return;
            }

            foreach (var map in Find.Maps.Where(m => m.GetComponent<MapComponent_TA_Expose>() == null))
            {
                map.components.Add(new MapComponent_TA_Expose(map));    //for saving data associated with a map
                LogOutput.WriteLogMessage(Errorlevel.Information, "Added a MapComponent to Store some information.");
            }

            Destroy(this);
        }
         
    }
}