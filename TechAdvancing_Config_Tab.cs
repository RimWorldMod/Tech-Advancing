using Multiplayer.API;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace TechAdvancing
{
    public class TechAdvancing_Config_Tab : Window
    {
        public const int conditionvalue_A_Default = 1;
        public const int conditionvalue_B_Default = 0;
        public const int conditionvalue_B_s_Default = 50;  // Default slider val

        [ConfigTabValueSaved("Conditionvalue_A")]
        public static int Conditionvalue_A { get => conditionvalue_A; set => conditionvalue_A = value; }
        public static int conditionvalue_A = conditionvalue_A_Default;

        [ConfigTabValueSaved("Conditionvalue_B")]
        public static int Conditionvalue_B { get => conditionvalue_B; set => conditionvalue_B = value; }
        public static int conditionvalue_B = conditionvalue_B_Default;

        [ConfigTabValueSaved("Conditionvalue_B_s")]
        public static int Conditionvalue_B_s { get => conditionvalue_B_s; set => conditionvalue_B_s = value; }
        public static int conditionvalue_B_s = conditionvalue_B_s_Default;

        internal static WorldCompSaveHandler worldCompSaveHandler = null;

        TechLevel[] previewTechLevels = { TechLevel.Undefined, TechLevel.Undefined, TechLevel.Undefined };
        public readonly string description = "configHeader".Translate();           //translation default:You can edit the rules here:
        private readonly string descriptionA2 = "configRuleAdesc".Translate();     //Rule A: \nIf the Player researched all techs of the techlevel X and below, the techlevel rises to X +
        private readonly string descriptionB2 = "configRuleBdesc".Translate();     //Rule B: \nIf the Player researched more than 50% of the techs of the techlevel Y and below, the techlevel rises to Y +
        private readonly string descriptionB2_s = "configRuleBSliderdesc".Translate();     //Description for the Slider thats used to change the threshold of the rule B
        private readonly string descriptionA2_calc = "configRuleAdesc".Translate();
        private readonly string descriptionB2_calc = "configRuleBdesc".Translate();
        private bool settingsChanged = false;
        // private static float _iconSize = 30f;
        // private static float _margin = 6f;

        [ConfigTabValueSaved("baseTechlvlCfg")]
        public static int BaseTechlvlCfg { get; set; } = 1; //0=neolithic ; 1= auto ; 2=colony
        public static TechLevel GetBaseTechlevel()
        {
            if (BaseTechlvlCfg == 1) // automatic detection (no override)
            {
                return TA_ResearchManager.factionDefault;
            }
            else // override techlevel
            {
                return BaseTechlvlCfg == 0 ? TechLevel.Neolithic : TechLevel.Industrial;
            }
        }
        public const TechLevel maxTechLevelForTribals = TechLevel.Medieval;

        [ConfigTabValueSaved("configCheckboxNeedTechColonists")]
        public static int ConfigCheckboxNeedTechColonists { get => b_configCheckboxNeedTechColonists ? 1 : 0; set => b_configCheckboxNeedTechColonists = value == 1; }
        public static bool b_configCheckboxNeedTechColonists = false;  //bool for selecting if we need colonists instead of tribals if we want to advance past medival tech

        [ConfigTabValueSaved("configCheckboxDisableCostMultiplicatorCap")]
        public static int ConfigCheckboxDisableCostMultiplicatorCap { get => b_configCheckboxDisableCostMultiplicatorCap ? 1 : 0; set => b_configCheckboxDisableCostMultiplicatorCap = value == 1; }
        public static bool b_configCheckboxDisableCostMultiplicatorCap = false;

        [ConfigTabValueSaved("configCheckboxMakeHigherResearchesSuperExpensive")]
        public static int ConfigCheckboxMakeHigherResearchesSuperExpensive { get => b_configCheckboxMakeHigherResearchesSuperExpensive ? 1 : 0; set => b_configCheckboxMakeHigherResearchesSuperExpensive = value == 1; }
        public static bool b_configCheckboxMakeHigherResearchesSuperExpensive = false;

        [ConfigTabValueSaved("configCheckboxMakeHigherResearchesSuperExpensiveFac")]
        public static int ConfigCheckboxMakeHigherResearchesSuperExpensiveFac { get; set; } = 10;

        [ConfigTabValueSaved("configChangeResearchCostFac")]
        public static int ConfigChangeResearchCostFac { get; set; } = 100; // 100 equals to a factor of x1

        [ConfigTabValueSaved("configSimpleResearchDiscountFac")]
        public static int ConfigDiscountPctForLowerTechs { get; set; } = 20; // 0 equals no discount

        [ConfigTabValueSaved("configCheckboxIgnoreNonMainTreeTechs")]
        public static int ConfigCheckboxIgnoreNonMainTreeTechs { get => b_configCheckboxIgnoreNonMainTreeTechs ? 1 : 0; set => b_configCheckboxIgnoreNonMainTreeTechs = value == 1; }
        public static bool b_configCheckboxIgnoreNonMainTreeTechs = false;


        internal const int spaceBetweenSettings = 50;

        public static float ConfigChangeResearchCostFacAsFloat()
        {
            var scaled = ConfigChangeResearchCostFac * 0.01f;

            if (scaled < 1) // e.g. 0.99
            {
                return (float)Math.Round(scaled, 3);
            }
            else if (scaled < 10) // e.g. 9.99
            {
                return (float)Math.Round(scaled, 2);
            }
            else if (scaled < 100) // e.g. 99.9
            {
                return (float)Math.Round(scaled, 1);
            }
            else // e.g. 100 and above
            {
                return (float)Math.Round(scaled, 0);
            }
        }

        private static readonly Dictionary<string, object> oldCfgValues = new Dictionary<string, object>();

        private static object GetOldCfgValue(string name) => oldCfgValues.TryGetValue(name, out object val) ? val : null;
        private static void SetOldCfgValue(string name, object value)
        {
            if (oldCfgValues.ContainsKey(name))
                oldCfgValues[name] = value;
            else
                oldCfgValues.Add(name, value);
        }

        private static List<PropertyInfo> GetSaveableProperties()
        {
            return typeof(TechAdvancing_Config_Tab).GetProperties().Where(x => x.GetCustomAttributes(typeof(ConfigTabValueSavedAttribute), false).Length > 0).ToList();
        }

        string menuButtonSelected = null;

        public override void DoWindowContents(Rect canvas)
        {
            // zooming in seems to cause Text.Font to start at Tiny, make sure it's set to Small for our panels.
            Text.Font = GameFont.Small;
            float drawpos = 0;

            Vector2 CalcSizeOnCanvas(string text, float paddingRight = 50f)
            {
                var size = Text.CalcSize(text);
                float maxWidth = canvas.width - paddingRight;
                if (size.x < maxWidth)
                {
                    return size;
                }
                return new Vector2(maxWidth, Text.CalcHeight(text, maxWidth));
            }

            void AddCheckboxSetting(ref bool val, string name, int prefixSpace = spaceBetweenSettings)
            {
                AddSpace(ref drawpos, prefixSpace);
                var size = CalcSizeOnCanvas(name);
                Widgets.CheckboxLabeled(new Rect(canvas.x, drawpos, size.x + 40, size.y), name, ref val, false);
            }

            float AddSliderSetting(float val, string name, float leftValue, float rightValue, float roundTo = 1f, int prefixSpace = spaceBetweenSettings)
            {
                AddSpace(ref drawpos, prefixSpace);
                var size = CalcSizeOnCanvas(name);

                return Widgets.HorizontalSlider(new Rect(canvas.x, drawpos, this.windowRect.width - 40f, size.y), ConfigDiscountPctForLowerTechs, leftValue, rightValue,
                        label: name, roundTo: roundTo);
            }


            void AddSliderSettingRef(ref float val, string name, float leftValue, float rightValue, float roundTo = 1f, int prefixSpace = spaceBetweenSettings)
            {
                val = AddSliderSetting(val, name, roundTo, prefixSpace);
            }

            // --- start of menu selection code --- //
            string[] buttonTexts = new[] { "config_menu_button_main", "config_menu_button_research_project_settings", "config_menu_button_research_project_info" };
            if (this.menuButtonSelected == null)
            {
                this.menuButtonSelected = buttonTexts[0];
            }
            //  Log.Message($"Currently selected: {this.menuButtonSelected}");
            float buttonDrawposX = 0;
            for (int i = 0; i < buttonTexts.Length; i++)
            {
                var name = buttonTexts[i];
                var buttonText = name.Translate();
                var isActive = this.menuButtonSelected != name;
                var clicked = Widgets.ButtonText(GetButtonRect(ref buttonDrawposX, drawpos, 20, buttonText), buttonText, isActive, isActive, isActive ? Color.white : Color.grey, active: isActive);
                if (clicked)
                {
                    this.menuButtonSelected = name;
                    LogOutput.WriteLogMessage(Errorlevel.Debug, $"Clicked: {buttonText}");
                }
            }

            AddSpace(ref drawpos, 30f);

            // --- end of menu selection code --- //

            var pageIndex = Array.IndexOf(buttonTexts, this.menuButtonSelected);

            switch (pageIndex) // Main Settings tab
            {
                case 0:
                    {
                        DrawText(canvas, this.description, ref drawpos);
                        AddSpace(ref drawpos, 5f);
                        if (Widgets.RadioButtonLabeled(new Rect(canvas.x + 20, drawpos, 100f, 60f), "configRadioBtnNeolithic".Translate(), BaseTechlvlCfg == 0)) //translation default: Neolithic / Tribal
                        {
                            BaseTechlvlCfg = 0;
                        }
                        if (Widgets.RadioButtonLabeled(new Rect(canvas.x + 200f, drawpos, 100f, 60f), "configRadioBtnAutoDetect".Translate(), BaseTechlvlCfg == 1)) //translation default: Auto-Detect (default)
                        {
                            BaseTechlvlCfg = 1;
                        }

                        if (Widgets.RadioButtonLabeled(new Rect(canvas.x + 400f, drawpos, 100f, 60f), "configRadioBtnIndustrial".Translate(), BaseTechlvlCfg == 2)) //translation default: Industrial / Colony
                        {
                            BaseTechlvlCfg = 2;
                        }
                        AddSpace(ref drawpos, 70f);
                        DrawText(canvas, "configBaseTechLvl".Translate() + " (" + ((BaseTechlvlCfg == 1) ? ((TA_ResearchManager.isTribe) ? "configTribe".Translate() : "configColony".Translate()) : ((BaseTechlvlCfg == 0) ? "configSetToTribe".Translate() : "configSetToColony".Translate())) + "): " + ((BaseTechlvlCfg == 1) ? TA_ResearchManager.factionDefault.ToString().TranslateOrDefault(null, "TA_TL_") : ((BaseTechlvlCfg == 0) ? "configNeolithic".Translate().ToString() : "configIndustrial".Translate().ToString())), ref drawpos);
                        AddSpace(ref drawpos, 20f);

                        DrawText(canvas, this.descriptionA2 + " (" + "configWordDefault".Translate() + conditionvalue_A_Default + ")", ref drawpos);
                        string bufferA = null;
                        string bufferB = null;
                        Widgets.TextFieldNumeric(new Rect(canvas.x + Verse.Text.CalcSize(this.descriptionA2_calc + " (" + "configWordDefault".Translate() + conditionvalue_A_Default + ")").x - 25f, canvas.y + drawpos - 22f, 50f, Verse.Text.CalcSize("Text").y), ref conditionvalue_A, ref bufferA, -100, 100);
                        AddSpace(ref drawpos, 10f);
                        DrawText(canvas, "configExpectedTechLvl".Translate() + " " + ((TechLevel)Math.Min((int)TechLevel.Archotech, (int)this.previewTechLevels[0])).ToString().TranslateOrDefault(null, "TA_TL_"), ref drawpos);

                        AddSpace(ref drawpos, 20f);
                        DrawText(canvas, this.descriptionB2.Replace("50", conditionvalue_B_s.ToString()) + " (" + "configWordDefault".Translate() + conditionvalue_B_Default + ")", ref drawpos);
                        Widgets.TextFieldNumeric(new Rect(canvas.x + Verse.Text.CalcSize(this.descriptionB2_calc + " (" + "configWordDefault".Translate() + conditionvalue_B_Default + ")").x - 25f, canvas.y + drawpos - 22f, 50f, Verse.Text.CalcSize("Text").y), ref conditionvalue_B, ref bufferB, -100, 100);

                        AddSpace(ref drawpos, 10f);
                        DrawText(canvas, "configExpectedTechLvl".Translate() + " " + ((TechLevel)Math.Min((int)TechLevel.Archotech, (int)this.previewTechLevels[1])).ToString().TranslateOrDefault(null, "TA_TL_"), ref drawpos);
                        AddSpace(ref drawpos, 20f);


                        DrawText(canvas, this.descriptionB2_s + " (" + "configWordDefault".Translate() + conditionvalue_B_s_Default + ")", ref drawpos);
                        AddSpace(ref drawpos, 10f);
                        conditionvalue_B_s = (int)Widgets.HorizontalSlider(new Rect(canvas.x, canvas.y + drawpos, 500, 15), conditionvalue_B_s, 1, 99, true, "50%", "1%", "99%", 1);
                        DrawText(new Rect(canvas.x + 530, canvas.y - 5, canvas.width, canvas.height), $"{conditionvalue_B_s}%", ref drawpos);

                        AddSpace(ref drawpos, 20f);

                        //if (b_configCheckboxNeedTechColonists != (configCheckboxNeedTechColonists == 1))
                        //{
                        //    previewTechLevels[2] = (Util.ColonyHasHiTechPeople()) ? TechLevel.Archotech : TechAdvancing_Config_Tab.maxTechLevelForTribals;
                        //}

                        b_configCheckboxNeedTechColonists = ConfigCheckboxNeedTechColonists == 1;

                        var cfgCbNTCTranslated = "configCheckboxNeedTechColonists".Translate(maxTechLevelForTribals.ToString().TranslateOrDefault(null, "TA_TL_"));
                        Widgets.CheckboxLabeled(new Rect(canvas.x, drawpos, Verse.Text.CalcSize("configCheckboxNeedTechColonists".Translate(maxTechLevelForTribals.ToString().TranslateOrDefault(null, "TA_TL_"))).x + 40f, 40f), cfgCbNTCTranslated, ref b_configCheckboxNeedTechColonists, false);
                        ConfigCheckboxNeedTechColonists = (b_configCheckboxNeedTechColonists) ? 1 : 0;
                        AddSpace(ref drawpos, 32f);

                        if (this.previewTechLevels[2] == maxTechLevelForTribals && b_configCheckboxNeedTechColonists)
                        {
                            DrawText(canvas, "configCheckboxNeedTechColonists_CappedAt".Translate(maxTechLevelForTribals.ToString().TranslateOrDefault(null, "TA_TL_")), ref drawpos, false, Color.red);
                        }


                        AddSpace(ref drawpos, 30f);
                        DrawText(canvas, "configResultingTechLvl".Translate() + " " + Rules.GetNewTechLevel().ToString().TranslateOrDefault(null, "TA_TL_"), ref drawpos);

                    }
                    break;

                case 1: // Project Settings tab
                    {
                        AddCheckboxSetting(ref b_configCheckboxDisableCostMultiplicatorCap, "configCheckboxDisableCostMultiplicatorCap".Translate(), 0);
                        AddCheckboxSetting(ref b_configCheckboxMakeHigherResearchesSuperExpensive, "configCheckboxMakeHigherResearchesSuperExpensive".Translate());

                        AddSpace(ref drawpos, 40f);
                        if (b_configCheckboxMakeHigherResearchesSuperExpensive)
                        {
                            int slider1ToCfg(float x) => x < 10 ? (int)x : (int)(x * x / 10);
                            float cfgToSlider1(float x) => x < 10 ? x : (float)Math.Sqrt(10 * x);
                            var translatedLabel = "configCheckboxMakeHigherResearchesSuperExpensiveFac".Translate(ConfigCheckboxMakeHigherResearchesSuperExpensiveFac);


                            ConfigCheckboxMakeHigherResearchesSuperExpensiveFac = slider1ToCfg(Widgets.HorizontalSlider(new Rect(canvas.x, drawpos, this.windowRect.width - 40f, Text.CalcSize(translatedLabel).y),
                                cfgToSlider1(ConfigCheckboxMakeHigherResearchesSuperExpensiveFac), 1, 100f, label: translatedLabel));
                        }

                        AddSpace(ref drawpos, 40f);
                        int slider2ToCfg(float x) => x < 50 ? (int)(x / 2) : (int)(x * x / 100);
                        float cfgToSlider2(float x) => x < 25 ? x * 2 : (float)(Math.Sqrt(x) * 10);

                        float val = ConfigChangeResearchCostFacAsFloat();
                        string s;
                        if (val < 100)
                        {
                            var s2 = val.ToString();
                            s = (s2 + (s2.Contains('.') ? "" : ".")).PadRight(4, '0');
                        }
                        else
                        {
                            s = val.ToString();
                        }

                        ConfigChangeResearchCostFac = slider2ToCfg(Widgets.HorizontalSlider(new Rect(canvas.x, drawpos, this.windowRect.width - 40f,
                            Text.CalcSize("configChangeResearchCostFac".Translate(s)).y), (int)cfgToSlider2(ConfigChangeResearchCostFac), 2, 1000f,
                            label: "configChangeResearchCostFac".Translate(s), roundTo: 1f));

                        ConfigDiscountPctForLowerTechs = (int)AddSliderSetting(ConfigDiscountPctForLowerTechs, "configDiscountPctForLowerTechs".Translate(ConfigDiscountPctForLowerTechs), 0, 99, roundTo: 1f);

                        var lastState = b_configCheckboxIgnoreNonMainTreeTechs;
                        AddCheckboxSetting(ref b_configCheckboxIgnoreNonMainTreeTechs, "configCheckboxIgnoreNonMainTreeTechs".Translate(), 70);
                        if (lastState != b_configCheckboxIgnoreNonMainTreeTechs)
                            TA_ResearchManager.UpdateFinishedProjectCounts();


                    }
                    break;

                case 2: // project info
                    {
                        var techs = (TechLevel[])Enum.GetValues(typeof(TechLevel));
                        var allProjects = Rules.nonIgnoredTechs.GroupBy(x => x.techLevel).ToDictionary(x => x.Key, x => x.ToList());
                        for (int i = 0; i < techs.Length; i++)
                        {
                            var tl = techs[i];
                            if (!allProjects.ContainsKey(tl))
                                continue;

                            var unfinishedTechs = allProjects[tl].Where(x => !x.IsFinished).ToList();
                            var tlNameTranslated = $"TA_TL_{tl}".Translate();

                            if (unfinishedTechs.Count != Rules.researchProjectStoreTotal[tl] - Rules.researchProjectStoreFinished[tl])
                            {
                                LogOutput.WriteLogMessage(Errorlevel.Error, "Count mismatch.");
                            }

                            string percentageResearched = (100 - unfinishedTechs.Count * 100f / allProjects[tl].Count).ToString("N0");

                            var translationToUse = unfinishedTechs.Count == 0 ? "configResearchProjectInfo_TechsRemainingNone" : (unfinishedTechs.Count == 1 ? "configResearchProjectInfo_TechsRemainingSingular" : "configResearchProjectInfo_TechsRemainingPlural");

                            var contentTranslated = translationToUse.Translate(unfinishedTechs.Count, percentageResearched, string.Join(", ", unfinishedTechs.Select(x => x.label.CapitalizeFirst())));
                            DrawText(canvas, tlNameTranslated + ":\n" + contentTranslated, ref drawpos);

                            AddSpace(ref drawpos, 10f);
                        }
                    }
                    break;

                default:
                    {
                        DrawText(canvas, "ERROR: Bad Tab Page. Please report this.", ref drawpos, color: Color.red);
                    }
                    break;

            }
            // -- Footer Area -- //
            drawpos = 575;
            DrawText(canvas, "availableTechLvls".Translate(), ref drawpos);

            AddSpace(ref drawpos, 10f);
            string[] techLevels = Enum.GetNames(typeof(TechLevel));
            for (int i = 0; i < techLevels.Length; i++)
            {
                DrawText(canvas, techLevels[i].ToString().TranslateOrDefault(null, "TA_TL_") + " = " + i, ref drawpos);
            }

            // ----------------- //
        }

        private void AddSpace(ref float drawpos, float amount = 0f)
        {
            drawpos += (amount != 0f) ? amount : 10f;
        }

        private Rect GetButtonRect(ref float drawPosX, float drawPosY, float height, string text)
        {
            int btnSpacer = 2;
            int btnTextMargin = 4;

            var btnWidth = Text.CalcSize(text).x + btnTextMargin * 2;
            var rect = new Rect(drawPosX, drawPosY, btnWidth, height);
            drawPosX += btnWidth + btnSpacer;
            return rect;
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            var fields = GetSaveableProperties();
            var dirty = fields.Any(x => !x.GetValue(null, null).Equals(GetOldCfgValue(((ConfigTabValueSavedAttribute)x.GetCustomAttributes(typeof(ConfigTabValueSavedAttribute), false)[0]).SaveName)));
            if (dirty)
            {
                foreach (var field in fields)
                {
                    var val = field.GetValue(null, null); // get current value
                    var varSaveName = field.TryGetAttribute<ConfigTabValueSavedAttribute>().SaveName; // get save name                                       
                    var lastVal = GetOldCfgValue(varSaveName); // get last value

                    if (!Equals(val, lastVal))
                    {
                        LogOutput.WriteLogMessage(Errorlevel.Debug, field.Name + $" differs: newVal: {val}; lastval: {lastVal}");
                    }

                    SetOldCfgValue(varSaveName, val);
                }

                this.settingsChanged = true;
                LogOutput.WriteLogMessage(Errorlevel.Information, "Settings changed.");
                this.previewTechLevels = GetTechlevelPreview();
            }
        }

        private TechLevel[] GetTechlevelPreview()
        {
            return new[] { Rules.RuleA(), Rules.RuleB(), Rules.GetLowTechTL() };
        }

        private void DrawText(Rect canvas, string text, ref float drawpos, bool increaseDrawpos = true, Color? color = null)
        {
            Color defaultcolor = GUI.contentColor;
            if (color != null)
            {
                GUI.contentColor = (Color)color;
            }

            var descHeight = Verse.Text.CalcHeight(text, canvas.width);
            Rect drawCanvas = new Rect(canvas.x, canvas.y + drawpos, canvas.width, descHeight);
            GUI.Label(drawCanvas, text);
            if (increaseDrawpos)
            {
                drawpos += drawCanvas.height;
            }

            if (color != null)
            {
                GUI.contentColor = defaultcolor;
            }
        }

        [SyncMethod]
        public static void CloseVariableSync(int[] newFields)
        {
            // run anyway since it doesnt matter if MP is enabled or not.

            var fields = GetSaveableProperties();
            if (fields.Count != newFields.Length)
            {
                throw new InvalidOperationException("Error while syncing variables. There was a mismatch in the amount of variables that got passed. Make sure you are running the latest version of tech advancing!");
            }

            for (int i = 0; i < fields.Count; i++)
            {
                fields[i].SetValue(null, newFields[i], null);
            }

            TechAdvancing.TA_ResearchManager.RecalculateTechlevel();
            LogOutput.WriteLogMessage(Errorlevel.Information, "Saving data.");
            ExposeData(TA_Expose_Mode.Save);
        }

        public override void PostClose()
        {
            base.PostClose();
            if (this.settingsChanged)
            {
                var fields = GetSaveableProperties();

                CloseVariableSync(fields.Select(x => (int)x.GetValue(null, null)).ToArray());
            }
            this.forcePause = false;
        }

        public override void PostOpen()
        {
            base.PostOpen();
        }

        public override void PreClose()
        {
            base.PreClose();
        }

        public override void PreOpen()
        {
            base.PreOpen();
            ExposeData(TA_Expose_Mode.Load);
            this.forcePause = true;
            this.settingsChanged = false;
            this.previewTechLevels = GetTechlevelPreview();
            this.menuButtonSelected = null; // reset the last clicked button for selecting pages. This will show the normal view.
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(950f, 820f);
            }
        }

        public TechAdvancing_Config_Tab()
        {
            this.forcePause = true;
            this.doCloseX = true;
            this.doCloseButton = true;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
        }

        public static void ExposeData(TA_Expose_Mode mode)
        {
            var savedNames = new List<string>();
            foreach (var value in GetSaveableProperties())
            {
                var attribute = (ConfigTabValueSavedAttribute)value.GetCustomAttributes(typeof(ConfigTabValueSavedAttribute), false)[0];

                var refVal = (int)value.GetValue(null, null);
                var oldRefVal = refVal;
                worldCompSaveHandler.TA_ExposeData(attribute, ref refVal, mode);
                if (oldRefVal != refVal) // if the value was changed by the method
                    value.SetValue(null, refVal, null);

                if (!worldCompSaveHandler.IsValueSaved(attribute.SaveName))
                {
                    worldCompSaveHandler.TA_ExposeData(attribute, ref refVal, TA_Expose_Mode.Save);
                    LogOutput.WriteLogMessage(Errorlevel.Information, $"Added new value called '{attribute.SaveName}' was added to the save file. This message shouldn't appear more than once per value and world.");
                }

                SetOldCfgValue(attribute.SaveName, refVal);
                savedNames.Add(attribute.SaveName);
            }

            foreach (var name in worldCompSaveHandler.GetConfigValueNames)
            {
                if (!savedNames.Contains(name))
                {
                    LogOutput.WriteLogMessage(Errorlevel.Information, $"Removed value {name} as it is no longer referenced.");
                    worldCompSaveHandler.RemoveConfigValue(name);

                    if (name == "configBlockMoreAdvancedResearches") // TODO remove fallback 23.01.2020
                    {
                        LogOutput.WriteLogMessage(Errorlevel.Warning, "Please disregard the errors above, if any. Just save the world and reload and they will be gone :)");
                    }
                }
            }
        }
    }
}