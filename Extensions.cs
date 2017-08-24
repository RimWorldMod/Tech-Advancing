using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using TechAdvancing;
using GHXXTechAdvancing;
using Harmony;
using Verse.Sound;
using System.Reflection;

namespace TechAdvancing
{
    public static class Extensions
    {
        public static string TranslateOrDefault(this string x,string fallback = null, string Prefix = null)
        {
            string retvar = "";
            if ((Prefix+x).TryTranslate(out retvar))
            {
                return retvar;  
            }
            return fallback??(Prefix+x);
        }
    }
}
