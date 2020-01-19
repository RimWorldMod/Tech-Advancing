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
    class TechAdvancing_Config_Tab : Window
    {
        public const int conditionvalue_A_Default = 1;
        public const int conditionvalue_B_Default = 0;
        public const int conditionvalue_B_s_Default = 50;  // Default slider val

        [ConfigTabValueSaved("Conditionvalue_A")]
        public static int conditionvalue_A = conditionvalue_A_Default;

        [ConfigTabValueSaved("Conditionvalue_B")]
        public static int conditionvalue_B = conditionvalue_B_Default;

        [ConfigTabValueSaved("Conditionvalue_B_s")]
        public static int conditionvalue_B_s = conditionvalue_B_s_Default;

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
        public static int baseTechlvlCfg = 1; //0=neolithic ; 1= auto ; 2=colony
        public static TechLevel baseFactionTechLevel = TechLevel.Undefined;
        public const TechLevel maxTechLevelForTribals = TechLevel.Medieval;

        [ConfigTabValueSaved("configCheckboxNeedTechColonists")]
        public static int configCheckboxNeedTechColonists = 0; //bool for selecting if we need colonists instead of tribals if we want to advance past medival tech
        public static bool b_configCheckboxNeedTechColonists = configCheckboxNeedTechColonists == 1;

        [ConfigTabValueSaved("configCheckboxDisableCostMultiplicatorCap")]
        public static int configCheckboxDisableCostMultiplicatorCap = 0;
        public static bool b_configCheckboxDisableCostMultiplicatorCap = configCheckboxDisableCostMultiplicatorCap == 1;

        [ConfigTabValueSaved("configBlockMoreAdvancedResearches")]
        public static int configBlockMoreAdvancedResearches = 0;
        public static bool B_configBlockMoreAdvancedResearches { get => configBlockMoreAdvancedResearches == 1; set => configBlockMoreAdvancedResearches = value ? 1 : 0; }

        private static readonly Dictionary<string, object> oldCfgValues = new Dictionary<string, object>();

        private static object GetOldCfgValue(string name) => oldCfgValues.TryGetValue(name, out object val) ? val : null;
        private static void SetOldCfgValue(string name, object value)
        {
            if (oldCfgValues.ContainsKey(name))
                oldCfgValues[name] = value;
            else
                oldCfgValues.Add(name, value);
        }

        private static List<FieldInfo> GetSaveableFields()
        {
            return typeof(TechAdvancing_Config_Tab).GetFields().Where(x => x.GetCustomAttributes(typeof(ConfigTabValueSavedAttribute), false).Length > 0).ToList();
        }

        string menuButtonSelected = null;
        public override void DoWindowContents(Rect canvas)
        {
            // zooming in seems to cause Text.Font to start at Tiny, make sure it's set to Small for our panels.
            Text.Font = GameFont.Small;
            float drawpos = 0;

            // --- start of menu selection code --- //
            string[] buttonTexts = new[] { "config_menu_button_main", "config_menu_button_research_project_settings" };
            if (this.menuButtonSelected == null)
            {
                this.menuButtonSelected = buttonTexts[0];
            }
            Log.Message($"Currently selected: {this.menuButtonSelected}");
            float buttonDrawposX = 0;
            for (int i = 0; i < buttonTexts.Length; i++)
            {
                var name = buttonTexts[i];
                var buttoNText = name.Translate();
                var isActive = this.menuButtonSelected != name;
                var clicked = Widgets.ButtonText(GetButtonRect(ref buttonDrawposX, drawpos, 20, buttoNText), buttoNText, isActive, isActive, isActive ? Color.white : Color.grey, active: isActive);
                if (clicked)
                {
                    this.menuButtonSelected = name;
                    Log.Message($"Clicked: {buttoNText}");
                }
            }

            AddSpace(ref drawpos, 30f);

            // --- end of menu selection code --- //

            var pageIndex = Array.IndexOf(buttonTexts, this.menuButtonSelected);

            switch (pageIndex)
            {
                case 0:
                    {
                        DrawText(canvas, this.description, ref drawpos);
                        AddSpace(ref drawpos, 5f);
                        if (Widgets.RadioButtonLabeled(new Rect(canvas.x + 20, drawpos, 100f, 60f), "configRadioBtnNeolithic".Translate(), baseTechlvlCfg == 0)) //translation default: Neolithic / Tribal
                        {
                            baseTechlvlCfg = 0;
                        }
                        if (Widgets.RadioButtonLabeled(new Rect(canvas.x + 200f, drawpos, 100f, 60f), "configRadioBtnAutoDetect".Translate(), baseTechlvlCfg == 1)) //translation default: Auto-Detect (default)
                        {
                            baseTechlvlCfg = 1;
                        }

                        if (Widgets.RadioButtonLabeled(new Rect(canvas.x + 400f, drawpos, 100f, 60f), "configRadioBtnIndustrial".Translate(), baseTechlvlCfg == 2)) //translation default: Industrial / Colony
                        {
                            baseTechlvlCfg = 2;
                        }
                        AddSpace(ref drawpos, 70f);
                        DrawText(canvas, "configBaseTechLvl".Translate() + " (" + ((baseTechlvlCfg == 1) ? ((_ResearchManager.isTribe) ? "configTribe".Translate() : "configColony".Translate()) : ((baseTechlvlCfg == 0) ? "configSetToTribe".Translate() : "configSetToColony".Translate())) + "): " + ((baseTechlvlCfg == 1) ? _ResearchManager.factionDefault.ToString().TranslateOrDefault(null, "TA_TL_") : ((baseTechlvlCfg == 0) ? "configNeolithic".Translate() : "configIndustrial".Translate())), ref drawpos);
                        AddSpace(ref drawpos, 20f);
                        baseFactionTechLevel = _ResearchManager.factionDefault;
                        if (baseTechlvlCfg != 1)
                        {
                            baseFactionTechLevel = (baseTechlvlCfg == 0) ? TechLevel.Neolithic : TechLevel.Industrial;
                        }
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
                        conditionvalue_B_s = (int)Widgets.HorizontalSlider(new Rect(canvas.x, canvas.y + drawpos, 500, 15), conditionvalue_B_s, 1, 100, true, "50%", "1%", "100%", 1);
                        DrawText(new Rect(canvas.x + 530, canvas.y - 5, canvas.width, canvas.height), $"{conditionvalue_B_s}%", ref drawpos);

                        AddSpace(ref drawpos, 20f);

                        //if (b_configCheckboxNeedTechColonists != (configCheckboxNeedTechColonists == 1))
                        //{
                        //    previewTechLevels[2] = (Util.ColonyHasHiTechPeople()) ? TechLevel.Archotech : TechAdvancing_Config_Tab.maxTechLevelForTribals;
                        //}

                        b_configCheckboxNeedTechColonists = configCheckboxNeedTechColonists == 1;

                        Widgets.CheckboxLabeled(new Rect(canvas.x, drawpos, Verse.Text.CalcSize("configCheckboxNeedTechColonists".Translate(maxTechLevelForTribals.ToString().TranslateOrDefault(null, "TA_TL_"))).x + 40f, 40f), "configCheckboxNeedTechColonists".Translate(maxTechLevelForTribals.ToString().TranslateOrDefault(null, "TA_TL_")), ref b_configCheckboxNeedTechColonists, false);
                        configCheckboxNeedTechColonists = (b_configCheckboxNeedTechColonists) ? 1 : 0;
                        AddSpace(ref drawpos, 32f);

                        if (this.previewTechLevels[2] == maxTechLevelForTribals && b_configCheckboxNeedTechColonists)
                        {
                            DrawText(canvas, "configCheckboxNeedTechColonists_CappedAt".Translate(maxTechLevelForTribals.ToString().TranslateOrDefault(null, "TA_TL_")), ref drawpos, false, Color.red);
                        }


                        AddSpace(ref drawpos, 30f);
                        DrawText(canvas, "configResultingTechLvl".Translate() + " " + Rules.GetNewTechLevel().ToString().TranslateOrDefault(null, "TA_TL_"), ref drawpos);

                    }
                    break;

                case 1:
                    {
                        b_configCheckboxDisableCostMultiplicatorCap = configCheckboxDisableCostMultiplicatorCap == 1;

                        Widgets.CheckboxLabeled(new Rect(canvas.x, drawpos, Verse.Text.CalcSize("configCheckboxDisableCostMultiplicatorCap".Translate(maxTechLevelForTribals.ToString().TranslateOrDefault(null, "TA_TL_"))).x + 40f, 40f), "configCheckboxDisableCostMultiplicatorCap".Translate(), ref b_configCheckboxDisableCostMultiplicatorCap, false);
                        configCheckboxDisableCostMultiplicatorCap = (b_configCheckboxDisableCostMultiplicatorCap) ? 1 : 0;
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
            var fields = GetSaveableFields();
            var dirty = fields.Any(x => !x.GetValue(null).Equals(GetOldCfgValue(((ConfigTabValueSavedAttribute)x.GetCustomAttributes(typeof(ConfigTabValueSavedAttribute), false)[0]).SaveName)));
            if (dirty)
            {
                foreach (var field in fields)
                {
                    var val = field.GetValue(null); // get current value
                    var varSaveName = field.TryGetAttribute<ConfigTabValueSavedAttribute>().SaveName; // get save name                                       
                    var lastVal = GetOldCfgValue(varSaveName); // get last value

                    if (!Equals(val, lastVal))
                    {
                        LogOutput.WriteLogMessage(Errorlevel.Debug, field.Name + $" differs: newVal: {val} ; lastval: {lastVal}");
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

        private void DrawText(Rect canvas, string Text, ref float drawpos, bool increaseDrawpos = true, Color? color = null)
        {
            Color defaultcolor = GUI.contentColor;
            if (color != null)
            {
                GUI.contentColor = (Color)color;
            }

            var descHeight = Verse.Text.CalcSize(Text).y;
            Rect drawCanvas = new Rect(canvas.x, canvas.y + drawpos, canvas.width, descHeight);
            GUI.Label(drawCanvas, Text);
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

            var fields = GetSaveableFields();
            if (fields.Count != newFields.Length)
            {
                throw new InvalidOperationException("Error while syncing variables. There was a mismatch in the amount of variables that got passed. Make sure you are running the latest version of tech advancing!");
            }

            for (int i = 0; i < fields.Count; i++)
            {
                fields[i].SetValue(null, newFields[i]);
            }

            TechAdvancing._ResearchManager.RecalculateTechlevel();
            LogOutput.WriteLogMessage(Errorlevel.Information, "Saving data.");
            ExposeData(TA_Expose_Mode.Save);
        }

        public override void PostClose()
        {
            base.PostClose();
            if (this.settingsChanged)
            {
                var fields = GetSaveableFields();

                CloseVariableSync(fields.Select(x => (int)x.GetValue(null)).ToArray());
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
                return new Vector2(950f, 800f);
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
            foreach (var value in GetSaveableFields())
            {
                var attribute = (ConfigTabValueSavedAttribute)value.GetCustomAttributes(typeof(ConfigTabValueSavedAttribute), false)[0];

                var refVal = (int)value.GetValue(null);
                var oldRefVal = refVal;
                MapCompSaveHandler.TA_ExposeData(attribute.SaveName, ref refVal, mode);
                if (oldRefVal != refVal) // if the value was changed by the method
                    value.SetValue(null, refVal);

                if (!MapCompSaveHandler.IsValueSaved(attribute.SaveName))
                {
                    MapCompSaveHandler.TA_ExposeData(attribute.SaveName, ref refVal, TA_Expose_Mode.Save);
                    LogOutput.WriteLogMessage(Errorlevel.Information, $"Added new value called '{attribute.SaveName}' was added to the save file. This message shouldn't appear more than once per value and world.");
                }

                SetOldCfgValue(attribute.SaveName, refVal);
            }
        }
    }
}