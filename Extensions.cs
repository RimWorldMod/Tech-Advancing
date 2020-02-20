using Verse;

namespace TechAdvancing
{
    public static class Extensions
    {
        public static string TranslateOrDefault(this string x, string fallback = null, string Prefix = null)
        {
            if ((Prefix + x).TryTranslate(out TaggedString retvar))
            {
                return retvar;
            }
            return fallback ?? (Prefix + x);
        }
    }
}
