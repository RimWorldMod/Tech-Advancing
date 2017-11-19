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
        private static Type TA_Storage = typeof(MapCompSaveHandler);

        public void FixedUpdate()
        {
            if (Current.ProgramState != ProgramState.Playing)            
                return;

            foreach (var map in Find.Maps.Where(m => m.GetComponent<MapCompSaveHandler>() == null))
            {
                map.components.Add(new MapCompSaveHandler(map));    //for saving data associated with a map
                LogOutput.WriteLogMessage(Errorlevel.Information, "Added a MapComponent to store some information.");
            }

            Destroy(this);
        }

    }
}