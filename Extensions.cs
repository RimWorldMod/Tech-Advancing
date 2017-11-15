using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using TechAdvancing;
using Harmony;
using Verse.Sound;
using System.Reflection;

namespace TechAdvancing
{
    public static class Extensions
    {
        public static string TranslateOrDefault(this string x,string fallback = null, string Prefix = null)
        {
            if ((Prefix + x).TryTranslate(out string retvar))
            {
                return retvar;
            }
            return fallback??(Prefix+x);
        }
    }
}
