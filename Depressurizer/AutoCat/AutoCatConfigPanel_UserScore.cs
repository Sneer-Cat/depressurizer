﻿/*
    This file is part of Depressurizer.
    Original work Copyright 2011, 2012, 2013 Steve Labbe.
    Modified work Copyright 2017 Martijn Vegter.

    Depressurizer is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Depressurizer is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Depressurizer.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using Depressurizer.Model;

namespace Depressurizer.AutoCat
{
    public partial class AutoCatConfigPanel_UserScore : AutoCatConfigPanel
    {
        private readonly BindingSource binding = new BindingSource();

        private readonly Dictionary<string, UserScorePresetDelegate> presetMap =
            new Dictionary<string, UserScorePresetDelegate>();

        private readonly BindingList<UserScoreRule> ruleList = new BindingList<UserScoreRule>();

        public AutoCatConfigPanel_UserScore()
        {
            InitializeComponent();

            // Set up help tooltips
            ttHelp.Ext_SetToolTip(helpPrefix, GlobalStrings.DlgAutoCat_Help_Prefix);
            ttHelp.Ext_SetToolTip(helpUseWilsonScore, GlobalStrings.DlgAutoCat_Help_UseWilsonScore);
            ttHelp.Ext_SetToolTip(helpRules, GlobalStrings.AutoCatUserScore_Help_Rules);

            // Set up bindings.
            // None of these strings should be localized.
            binding.DataSource = ruleList;

            lstRules.DisplayMember = "Name";
            lstRules.DataSource = binding;

            txtRuleName.DataBindings.Add("Text", binding, "Name");
            numRuleMinScore.DataBindings.Add("Value", binding, "MinScore");
            numRuleMaxScore.DataBindings.Add("Value", binding, "MaxScore");
            numRuleMinReviews.DataBindings.Add("Value", binding, "MinReviews");
            numRuleMaxReviews.DataBindings.Add("Value", binding, "MaxReviews");

            // Set up preset list
            presetMap.Add(GlobalStrings.AutoCatUserScore_Preset_Name_SteamLabels, GenerateSteamRules);

            foreach (string s in presetMap.Keys)
            {
                cmbPresets.Items.Add(s);
            }

            cmbPresets.SelectedIndex = 0;

            UpdateEnabledSettings();
        }

        public delegate void UserScorePresetDelegate(ICollection<UserScoreRule> rules);

        public override void SaveToAutoCat(AutoCat ac)
        {
            AutoCatUserScore acScore = ac as AutoCatUserScore;
            if (ac == null)
            {
                return;
            }

            acScore.Prefix = txtPrefix.Text;
            acScore.UseWilsonScore = chkUseWilsonScore.Checked;
            acScore.Rules = new List<UserScoreRule>(ruleList);
        }

        public override void LoadFromAutoCat(AutoCat ac)
        {
            AutoCatUserScore acScore = ac as AutoCatUserScore;
            if (ac == null)
            {
                return;
            }

            txtPrefix.Text = acScore.Prefix;
            chkUseWilsonScore.Checked = acScore.UseWilsonScore;

            ruleList.Clear();
            foreach (UserScoreRule rule in acScore.Rules)
            {
                ruleList.Add(new UserScoreRule(rule));
            }

            UpdateEnabledSettings();
        }

        /// <summary>
        ///     Updates enabled states of all form elements that depend on the rule selection.
        /// </summary>
        private void UpdateEnabledSettings()
        {
            bool ruleSelected = lstRules.SelectedIndex >= 0;

            txtRuleName.Enabled = numRuleMaxScore.Enabled = numRuleMinScore.Enabled = numRuleMinReviews.Enabled =
                numRuleMaxReviews.Enabled = cmdRuleRemove.Enabled = ruleSelected;
            cmdRuleUp.Enabled = ruleSelected && (lstRules.SelectedIndex != 0);
            cmdRuleDown.Enabled = ruleSelected = ruleSelected && (lstRules.SelectedIndex != (lstRules.Items.Count - 1));
        }

        /// <summary>
        ///     Moves the specified rule a certain number of spots up or down in the list. Does nothing if the spot would be off
        ///     the list.
        /// </summary>
        /// <param name="mainIndex">Index of the rule to move.</param>
        /// <param name="offset">Number of spots to move the rule. Negative moves up, positive moves down.</param>
        /// <param name="selectMoved">If true, select the moved element afterwards</param>
        private void MoveItem(int mainIndex, int offset, bool selectMoved)
        {
            int alterIndex = mainIndex + offset;
            if ((mainIndex < 0) || (mainIndex >= lstRules.Items.Count) || (alterIndex < 0) ||
                (alterIndex >= lstRules.Items.Count))
            {
                return;
            }

            UserScoreRule mainItem = ruleList[mainIndex];
            ruleList[mainIndex] = ruleList[alterIndex];
            ruleList[alterIndex] = mainItem;
            if (selectMoved)
            {
                lstRules.SelectedIndex = alterIndex;
            }
        }

        /// <summary>
        ///     Replaces the current rule list with the named preset. Asks for user confirmation if the current rule list is not
        ///     empty.
        /// </summary>
        /// <param name="name">Name of the preset to apply.</param>
        private void ApplyPreset(string name)
        {
            if ((name != null) && presetMap.ContainsKey(name))
            {
                if ((ruleList.Count == 0) ||
                    (MessageBox.Show(GlobalStrings.AutoCatUserScore_Dialog_ConfirmPreset, GlobalStrings.Gen_Warning,
                         MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes))
                {
                    UserScorePresetDelegate dlgt = presetMap[name];
                    ruleList.Clear();
                    dlgt(ruleList);
                    UpdateEnabledSettings();
                }
            }
        }

        /// <summary>
        ///     Adds a new rule to the end of the list and selects it.
        /// </summary>
        private void AddRule()
        {
            UserScoreRule newRule = new UserScoreRule(GlobalStrings.AutoCatUserScore_NewRuleName, 0, 100, 0, 0);
            ruleList.Add(newRule);
            lstRules.SelectedIndex = lstRules.Items.Count - 1;
        }

        /// <summary>
        ///     Removes the rule at the given index
        /// </summary>
        /// <param name="index">Index of the rule to remove</param>
        private void RemoveRule(int index)
        {
            if (index >= 0)
            {
                ruleList.RemoveAt(index);
            }
        }

        #region Preset generators

        /// <summary>
        ///     Generates rules that match the Steam Store rating labels
        /// </summary>
        /// <param name="rules">List of UserScoreRule objects to populate with the new ones. Should generally be empty.</param>
        public void GenerateSteamRules(ICollection<UserScoreRule> rules)
        {
            rules.Add(new UserScoreRule(GlobalStrings.AutoCatUserScore_Preset_Steam_Positive4, 95, 100, 500, 0));
            rules.Add(new UserScoreRule(GlobalStrings.AutoCatUserScore_Preset_Steam_Positive3, 85, 100, 50, 0));
            rules.Add(new UserScoreRule(GlobalStrings.AutoCatUserScore_Preset_Steam_Positive2, 80, 100, 1, 0));
            rules.Add(new UserScoreRule(GlobalStrings.AutoCatUserScore_Preset_Steam_Positive1, 70, 79, 1, 0));
            rules.Add(new UserScoreRule(GlobalStrings.AutoCatUserScore_Preset_Steam_Mixed, 40, 69, 1, 0));
            rules.Add(new UserScoreRule(GlobalStrings.AutoCatUserScore_Preset_Steam_Negative1, 20, 39, 1, 0));
            rules.Add(new UserScoreRule(GlobalStrings.AutoCatUserScore_Preset_Steam_Negative4, 0, 19, 500, 0));
            rules.Add(new UserScoreRule(GlobalStrings.AutoCatUserScore_Preset_Steam_Negative3, 0, 19, 50, 0));
            rules.Add(new UserScoreRule(GlobalStrings.AutoCatUserScore_Preset_Steam_Negative2, 0, 19, 1, 0));
        }

        #endregion

        #region Event Handlers

        private void lstRules_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateEnabledSettings();
        }

        private void cmdRuleAdd_Click(object sender, EventArgs e)
        {
            AddRule();
        }

        private void cmdRuleRemove_Click(object sender, EventArgs e)
        {
            RemoveRule(lstRules.SelectedIndex);
        }

        private void cmdRuleUp_Click(object sender, EventArgs e)
        {
            MoveItem(lstRules.SelectedIndex, -1, true);
        }

        private void cmdRuleDown_Click(object sender, EventArgs e)
        {
            MoveItem(lstRules.SelectedIndex, 1, true);
        }

        private void cmdApplyPreset_Click(object sender, EventArgs e)
        {
            ApplyPreset(cmbPresets.SelectedItem as string);
        }

        #endregion
    }
}