using Multiplayer.API;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace TechAdvancing
{
    class TechAdvancing_Config_Tab : Window
    {
        public static int Conditionvalue_A = 1;

        public static int Conditionvalue_B = 0;

        public static int Conditionvalue_B_s = 50;  // Default slider val

        public const int Conditionvalue_A_Default = 1;
        public const int Conditionvalue_B_Default = 0;
        public const int Conditionvalue_B_s_Default = 50;
        internal static int last_Conditionvalue_A = Conditionvalue_A_Default;
        internal static int last_Conditionvalue_B = Conditionvalue_B_Default;
        internal static int last_Conditionvalue_B_s = Conditionvalue_B_s_Default;
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

        public static int baseTechlvlCfg = 1; //0=neolithic ; 1= auto ; 2=colony
        public static int last_baseTechlvlCfg = 1; //default for auto
        public static TechLevel baseFactionTechLevel = TechLevel.Undefined;
        public const TechLevel maxTechLevelForTribals = TechLevel.Medieval;

        public static int configCheckboxNeedTechColonists = 0; //bool for selecting if we need colonists instead of tribals if we want to advance past medival tech
        public static int last_configCheckboxNeedTechColonists = 0;
        public static bool b_configCheckboxNeedTechColonists = configCheckboxNeedTechColonists == 1;

        public static int configCheckboxDisableCostMultiplicatorCap = 0;
        public static int last_configCheckboxDisableCostMultiplicatorCap = 0;
        public static bool b_configCheckboxDisableCostMultiplicatorCap = configCheckboxDisableCostMultiplicatorCap == 1;

        public override void DoWindowContents(Rect canvas)
        {
            // zooming in seems to cause Text.Font to start at Tiny, make sure it's set to Small for our panels.
            Text.Font = GameFont.Small;
            float drawpos = 0;

            DrawText(canvas, this.description, ref drawpos);
            AddSpace(ref drawpos, 10f);
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
            DrawText(canvas, this.descriptionA2 + " (" + "configWordDefault".Translate() + Conditionvalue_A_Default + ")", ref drawpos);
            string bufferA = null;
            string bufferB = null;
            Widgets.TextFieldNumeric(new Rect(canvas.x + Verse.Text.CalcSize(this.descriptionA2_calc + " (" + "configWordDefault".Translate() + Conditionvalue_A_Default + ")").x - 25f, canvas.y + drawpos - 22f, 50f, Verse.Text.CalcSize("Text").y), ref Conditionvalue_A, ref bufferA, -100, 100);
            AddSpace(ref drawpos, 10f);
            DrawText(canvas, "configExpectedTechLvl".Translate() + " " + ((TechLevel)Math.Min((int)TechLevel.Archotech, (int)this.previewTechLevels[0])).ToString().TranslateOrDefault(null, "TA_TL_"), ref drawpos);

            AddSpace(ref drawpos, 20f);
            DrawText(canvas, this.descriptionB2.Replace("50", Conditionvalue_B_s.ToString()) + " (" + "configWordDefault".Translate() + Conditionvalue_B_Default + ")", ref drawpos);
            Widgets.TextFieldNumeric(new Rect(canvas.x + Verse.Text.CalcSize(this.descriptionB2_calc + " (" + "configWordDefault".Translate() + Conditionvalue_B_Default + ")").x - 25f, canvas.y + drawpos - 22f, 50f, Verse.Text.CalcSize("Text").y), ref Conditionvalue_B, ref bufferB, -100, 100);

            AddSpace(ref drawpos, 10f);
            DrawText(canvas, "configExpectedTechLvl".Translate() + " " + ((TechLevel)Math.Min((int)TechLevel.Archotech, (int)this.previewTechLevels[1])).ToString().TranslateOrDefault(null, "TA_TL_"), ref drawpos);
            AddSpace(ref drawpos, 20f);


            DrawText(canvas, this.descriptionB2_s + " (" + "configWordDefault".Translate() + Conditionvalue_B_s_Default + ")", ref drawpos);
            AddSpace(ref drawpos, 10f);
            Conditionvalue_B_s = (int)Widgets.HorizontalSlider(new Rect(canvas.x, canvas.y + drawpos, 500, 15), Conditionvalue_B_s, 1, 100, true, "50%", "1%", "100%", 1);
            DrawText(new Rect(canvas.x + 530, canvas.y - 5, canvas.width, canvas.height), $"{Conditionvalue_B_s}%", ref drawpos);

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

            b_configCheckboxDisableCostMultiplicatorCap = configCheckboxDisableCostMultiplicatorCap == 1;

            Widgets.CheckboxLabeled(new Rect(canvas.x, drawpos, Verse.Text.CalcSize("configCheckboxDisableCostMultiplicatorCap".Translate(maxTechLevelForTribals.ToString().TranslateOrDefault(null, "TA_TL_"))).x + 40f, 40f), "configCheckboxDisableCostMultiplicatorCap".Translate(), ref b_configCheckboxDisableCostMultiplicatorCap, false);
            configCheckboxDisableCostMultiplicatorCap = (b_configCheckboxDisableCostMultiplicatorCap) ? 1 : 0;

            AddSpace(ref drawpos, 50f);
            DrawText(canvas, "availableTechLvls".Translate(), ref drawpos);

            AddSpace(ref drawpos, 10f);
            string[] techLevels = Enum.GetNames(typeof(TechLevel));
            for (int i = 0; i < techLevels.Length; i++)
            {
                DrawText(canvas, techLevels[i].ToString().TranslateOrDefault(null, "TA_TL_") + " = " + i, ref drawpos);
            }
        }

        private void AddSpace(ref float drawpos, float amount = 0f)
        {
            drawpos += (amount != 0f) ? amount : 10f;
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            if ((last_Conditionvalue_A != Conditionvalue_A) || (last_Conditionvalue_B != Conditionvalue_B) || (last_baseTechlvlCfg != baseTechlvlCfg) || (last_configCheckboxNeedTechColonists != configCheckboxNeedTechColonists)
                || last_Conditionvalue_B_s != Conditionvalue_B_s || last_configCheckboxDisableCostMultiplicatorCap != configCheckboxDisableCostMultiplicatorCap
            )
            {
                last_Conditionvalue_A = Conditionvalue_A;
                last_Conditionvalue_B = Conditionvalue_B;
                last_Conditionvalue_B_s = Conditionvalue_B_s;
                last_baseTechlvlCfg = baseTechlvlCfg;
                last_configCheckboxNeedTechColonists = configCheckboxNeedTechColonists;
                last_configCheckboxDisableCostMultiplicatorCap = configCheckboxDisableCostMultiplicatorCap;
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
        public static void CloseVariableSync(int conditionvalue_A_param, int conditionvalue_B_param, int conditionvalue_B_s_param, int baseTechlvlCfg_param, int configCheckboxNeedTechColonists_param, int configCheckboxDisableCostMultiplicatorCap_param)
        {
            // run anyway since it doesnt matter if MP is enabled or not.

            Conditionvalue_A = conditionvalue_A_param;
            Conditionvalue_B = conditionvalue_B_param;
            Conditionvalue_B_s = conditionvalue_B_s_param;
            baseTechlvlCfg = baseTechlvlCfg_param;
            configCheckboxNeedTechColonists = configCheckboxNeedTechColonists_param;
            configCheckboxDisableCostMultiplicatorCap = configCheckboxDisableCostMultiplicatorCap_param;

            TechAdvancing._ResearchManager.RecalculateTechlevel();
            LogOutput.WriteLogMessage(Errorlevel.Information, "Saving data.");
            ExposeData(TA_Expose_Mode.Save);
        }

        public override void PostClose()
        {
            base.PostClose();
            if (this.settingsChanged)
            {
                CloseVariableSync(Conditionvalue_A, Conditionvalue_B, Conditionvalue_B_s, baseTechlvlCfg, configCheckboxNeedTechColonists, configCheckboxDisableCostMultiplicatorCap);
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
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(950f, 775f);
            }
        }

        public TechAdvancing_Config_Tab()
        {
            this.forcePause = true;
            this.doCloseX = true;
            //this.closeOnEscapeKey = true;
            this.doCloseButton = true;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
        }

        public static void ExposeData(TA_Expose_Mode mode)
        {
            MapCompSaveHandler.TA_ExposeData("Conditionvalue_A", ref Conditionvalue_A, mode);
            MapCompSaveHandler.TA_ExposeData("Conditionvalue_B", ref Conditionvalue_B, mode);
            MapCompSaveHandler.TA_ExposeData("Conditionvalue_B_s", ref Conditionvalue_B_s, mode);
            MapCompSaveHandler.TA_ExposeData("baseTechlvlCfg", ref baseTechlvlCfg, mode);
            MapCompSaveHandler.TA_ExposeData("configCheckboxNeedTechColonists", ref configCheckboxNeedTechColonists, mode);
            MapCompSaveHandler.TA_ExposeData("configCheckboxDisableCostMultiplicatorCap", ref configCheckboxDisableCostMultiplicatorCap, mode);

            if (!MapCompSaveHandler.IsValueSaved("Conditionvalue_A"))
            {
                MapCompSaveHandler.TA_ExposeData("Conditionvalue_A", ref Conditionvalue_A, TA_Expose_Mode.Save);
                LogOutput.WriteLogMessage(Errorlevel.Information, "Value 'Conditionvalue_A' was added to the save file. This message shouldn't appear more than once per value and world.");
            }
            if (!MapCompSaveHandler.IsValueSaved("Conditionvalue_B"))
            {
                MapCompSaveHandler.TA_ExposeData("Conditionvalue_B", ref Conditionvalue_B, TA_Expose_Mode.Save);
                LogOutput.WriteLogMessage(Errorlevel.Information, "Value 'Conditionvalue_B' was added to the save file. This message shouldn't appear more than once per value and world.");
            }
            if (!MapCompSaveHandler.IsValueSaved("Conditionvalue_B_s"))
            {
                MapCompSaveHandler.TA_ExposeData("Conditionvalue_B_s", ref Conditionvalue_B_s, TA_Expose_Mode.Save);
                LogOutput.WriteLogMessage(Errorlevel.Information, "Value 'Conditionvalue_B_s' was added to the save file. This message shouldn't appear more than once per value and world.");
            }
            if (!MapCompSaveHandler.IsValueSaved("baseTechlvlCfg"))
            {
                MapCompSaveHandler.TA_ExposeData("baseTechlvlCfg", ref baseTechlvlCfg, TA_Expose_Mode.Save);
                LogOutput.WriteLogMessage(Errorlevel.Information, "Value 'baseTechlvlCfg' was added to the save file. This message shouldn't appear more than once per value and world.");
            }
            if (!MapCompSaveHandler.IsValueSaved("configCheckboxNeedTechColonists"))
            {
                MapCompSaveHandler.TA_ExposeData("configCheckboxNeedTechColonists", ref configCheckboxNeedTechColonists, TA_Expose_Mode.Save);
                LogOutput.WriteLogMessage(Errorlevel.Information, "Value 'configCheckboxNeedTechColonists' was added to the save file. This message shouldn't appear more than once per value and world.");
            }
            if (!MapCompSaveHandler.IsValueSaved("configCheckboxDisableCostMultiplicatorCap"))
            {
                MapCompSaveHandler.TA_ExposeData("configCheckboxDisableCostMultiplicatorCap", ref configCheckboxNeedTechColonists, TA_Expose_Mode.Save);
                LogOutput.WriteLogMessage(Errorlevel.Information, "Value 'configCheckboxDisableCostMultiplicatorCap' was added to the save file. This message shouldn't appear more than once per value and world.");
            }

            last_Conditionvalue_A = Conditionvalue_A;
            last_Conditionvalue_B = Conditionvalue_B;
            last_Conditionvalue_B_s = Conditionvalue_B_s;
            last_baseTechlvlCfg = baseTechlvlCfg;
            last_configCheckboxNeedTechColonists = configCheckboxNeedTechColonists;
            last_configCheckboxDisableCostMultiplicatorCap = configCheckboxDisableCostMultiplicatorCap;

        }

    }
}