using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;


namespace TechAdvancing
{
    class LogOutput
    {
#if DEBUG 
        public static readonly bool DebugMode_TA_enabled = true;
#else
        public static readonly bool DebugMode_TA_enabled = false;
#endif

        public static void WriteLogMessage(Errorlevel level, string message)
        {
            if((level == Errorlevel.Debug && DebugMode_TA_enabled)|| level == Errorlevel.Information)
            {
                Log.Message("[Tech Advancing] [" + level.ToString() + "] " + message);
            }
            else if (level == Errorlevel.Warning || level == Errorlevel.Potential_Error)
            {
                Log.Warning("[Tech Advancing] [" + level.ToString() + "] " + message);
            }
            else if (level == Errorlevel.Error || level == Errorlevel.Critical)
            {
                Log.Error("[Tech Advancing] [" + level.ToString() + "] " + message);
            }
        }


    }

    public enum Errorlevel
    {
        Debug,
        Information,
        Warning,
        Potential_Error,
        Error,
        Critical
    }
}
