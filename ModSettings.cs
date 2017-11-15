using System.Reflection;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;


namespace TechAdvancing
{
    public class Settings : ModSettings
    {

        private static void DrawText(Rect canvas, string Text, ref float drawpos, bool increaseDrawpos = true)
        {
            var descHeight = Verse.Text.CalcSize(Text).y;  //Verse.Text.CalcHeight(descTR, Listing.ColumnSpacing);
            Rect drawCanvas = new Rect(canvas.x+200, canvas.y + drawpos, canvas.width, descHeight);
            GUI.Label(drawCanvas, Text);
            
            if (increaseDrawpos)
            {
                drawpos += drawCanvas.height;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }

        public static void DoSettingsWindowContents(Rect rect)
        {
            Rect TA_Cfgrect = new Rect(0f, 0f, 180f, 20f);
            TA_Cfgrect.x = (rect.width - TA_Cfgrect.width) / 4f;
            TA_Cfgrect.y = 40f;
            float drawpos = 30f;
            Color defaultGuiColor = GUI.contentColor;

            if (Current.ProgramState == ProgramState.Playing)
            {
                if (Widgets.ButtonText(TA_Cfgrect, "TAcfgmenulabel".Translate(), true, false, true))
                {
                    Find.WindowStack.Add((Window)new TechAdvancing_Config_Tab());
                }
            }
            else
            {
                GUI.contentColor = Color.red;
                DrawText(rect,"TAcfgunavailable".Translate(),ref drawpos,true);
                GUI.contentColor = defaultGuiColor;
            }
        }
    }

    public class SettingController : Mod
    {
     
        public SettingController(ModContentPack content)
            : base(content)
        {
            GetSettings<Settings>();
        }

        public override string SettingsCategory() { return "TAcfgmenulabel".Translate(); }
        public override void DoSettingsWindowContents(Rect inRect) { Settings.DoSettingsWindowContents(inRect); }

  
    }
    


}