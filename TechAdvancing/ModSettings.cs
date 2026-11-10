using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TechAdvancing
{
    public class Settings : ModSettings
    {
        private static void DrawText(Rect canvas, string Text, ref float drawpos, bool increaseDrawpos = true)
        {
            var descHeight = Verse.Text.CalcSize(Text).y;  //Verse.Text.CalcHeight(descTR, Listing.ColumnSpacing);
            Rect drawCanvas = new Rect(canvas.x + 200, canvas.y + drawpos, canvas.width, descHeight);
            GUI.Label(drawCanvas, Text);

            if (increaseDrawpos)
            {
                drawpos += drawCanvas.height;
            }
        }

        Dictionary<string, object> tempdict = new Dictionary<string, object>();
        public override void ExposeData()
        {
            if (Current.ProgramState == ProgramState.Entry)
            {
                if (Scribe.mode == LoadSaveMode.LoadingVars)
                {
                    ConfigTabValueSavedAttribute.BuildDefaultValueCache();
                    LogOutput.WriteLogMessage(Errorlevel.Debug, "Loading default vars");
                    var dict = new Dictionary<string, int>();
                    Scribe_Collections.Look(ref dict, "TA_Expose_Default_Numbers", LookMode.Value, LookMode.Value);

                    //LogOutput.WriteLogMessage(Errorlevel.Debug, $"DefaultDict: {string.Join(";", ConfigTabValueSavedAttribute.attributeDefaultValues.Select(x => $"{x.Key}={x.Value}"))}");
                    if (dict != null)
                    {
                        //LogOutput.WriteLogMessage(Errorlevel.Debug, $"SavedDict: {string.Join(";", dict.Select(x => $"{x.Key}={x.Value}"))}");
                        foreach (var kv in dict)
                            if (ConfigTabValueSavedAttribute.attributeDefaultValues.ContainsKey(kv.Key)) // load existing keys
                                ConfigTabValueSavedAttribute.attributeDefaultValues[kv.Key] = kv.Value;
                            else
                                LogOutput.WriteLogMessage(Errorlevel.Debug, $"Wiped old value with key {kv.Key}");
                    }
                }
                else if (Scribe.mode == LoadSaveMode.Saving)
                {
                    LogOutput.WriteLogMessage(Errorlevel.Debug, "Saving default vars");
                    var dict = ConfigTabValueSavedAttribute.attributeDefaultValues.ToDictionary(x => x.Key, x => x.Value);
                    Scribe_Collections.Look(ref dict, "TA_Expose_Default_Numbers", LookMode.Value, LookMode.Value);
                    settingsText = null;
                }
            }
            else
            {
                this.tempdict = ConfigTabValueSavedAttribute.attributeDefaultValues.ToDictionary(x => x.Key, x => x.Value);
                Scribe_Collections.Look(ref this.tempdict, "TA_Expose_Default_Numbers", LookMode.Value, LookMode.Value); // because rimworld thinks its a great idea to clear configs otherwise...
            }
            base.ExposeData();
        }


        private static string settingsText = null;
        public static void DoSettingsWindowContents(Rect rect)
        {
            Rect TA_Cfgrect = new Rect(0f, 0f, 180f, 20f);
            TA_Cfgrect.x = (rect.width - TA_Cfgrect.width) / 4f;
            TA_Cfgrect.y = 40f;
            float drawpos = 10f + rect.position.y;
            Color defaultGuiColor = GUI.contentColor;

            if (Current.ProgramState == ProgramState.Playing)
            {
                if (Widgets.ButtonText(TA_Cfgrect, "TAcfgmenulabel".Translate(), true, false, true))
                {
                    Find.WindowStack.Add(new TechAdvancing_Config_Tab());
                }
            }
            else
            {
                if (settingsText == null)
                    settingsText = string.Join("\n", ConfigTabValueSavedAttribute.attributeDefaultValues.Select(x => $"{x.Key}:{x.Value}"));

                GUI.Label(new Rect(rect.position.x, drawpos, rect.width, 20), "TAcfgDefaultsHeader".Translate());
                AddSpace(ref drawpos, 10 + 20);

                var r = new Rect(rect.position.x, drawpos, rect.width, rect.height - drawpos - 50);
                settingsText = GUI.TextArea(r, settingsText);

                AddSpace(ref drawpos, r.height + 10);

                var lbl = "TAcfgSetDefaults".Translate();
                if (GUI.Button(GetButtonRectVert(0, ref drawpos, 20, lbl), lbl))
                {
                    var newDict = settingsText.Replace("\r\n", "\n").Replace("\r", "\n")
                        .Split('\n')
                        .Select(x => x.Split(':').Select(y => y.Trim()).ToArray())
                        .ToDictionary(x => x[0], x => int.Parse(x[1]));

                    foreach (var kv in newDict)
                        if (ConfigTabValueSavedAttribute.attributeDefaultValues.ContainsKey(kv.Key)) // overwrite default attribute values if they exist
                            ConfigTabValueSavedAttribute.attributeDefaultValues[kv.Key] = kv.Value;
                        else
                            LogOutput.WriteLogMessage(Errorlevel.Warning, $"Not saving value with key '{kv.Key}' as that key does not exist.");

                    SoundDef.Named("Click").PlayOneShot(new SoundInfo() { pitchFactor = 1, volumeFactor = 1 });
                }

                GUI.contentColor = defaultGuiColor;
            }
        }
        private static void AddSpace(ref float drawpos, float amount = 0f)
        {
            drawpos += (amount != 0f) ? amount : 10f;
        }

        private static Rect GetButtonRectVert(float drawPosX, ref float drawPosY, float height, string text)
        {
            int btnSpacer = 2;
            int btnTextMargin = 4;

            var btnWidth = Text.CalcSize(text).x + btnTextMargin * 2;
            var rect = new Rect(drawPosX, drawPosY, btnWidth, height);
            drawPosY += height + btnSpacer;
            return rect;
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