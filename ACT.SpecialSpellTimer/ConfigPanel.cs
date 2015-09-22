﻿namespace ACT.SpecialSpellTimer
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Reflection;
    using System.Windows.Forms;

    using ACT.SpecialSpellTimer.Image;
    using ACT.SpecialSpellTimer.Properties;
    using ACT.SpecialSpellTimer.Sound;
    using ACT.SpecialSpellTimer.Utility;

    /// <summary>
    /// 設定Panel
    /// </summary>
    public partial class ConfigPanel : UserControl
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ConfigPanel()
        {
            this.InitializeComponent();

            Translate.TranslateControls(this);

            this.LanguageComboBox.Items.AddRange(Utility.Language.GetLanguageList());

            this.ToolTip.SetToolTip(this.KeywordTextBox, Utility.Translate.Get("MatchingKeywordExplanationTooltip"));
            this.ToolTip.SetToolTip(this.RegexEnabledCheckBox, Utility.Translate.Get("RegularExpressionExplanationTooltip"));
            this.ToolTip.SetToolTip(this.TelopRegexEnabledCheckBox, Utility.Translate.Get("RegularExpressionExplanationTooltip"));
            this.ToolTip.SetToolTip(this.TelopKeywordTextBox, Utility.Translate.Get("MatchingKeywordExplanationTooltip"));
            this.ToolTip.SetToolTip(this.TelopMessageTextBox, Utility.Translate.Get("TelopMessageExplanationTooltip"));
            this.ToolTip.SetToolTip(this.label46, Utility.Translate.Get("TelopMessageExplanationTooltip"));

            // 戦闘分析用のListViewのダブルバッファリングを有効にする
            typeof(ListView)
                .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(this.CombatLogListView, true, null);

            // パネルの詳細用グループボックスの場所を決める
            this.DetailPanelGroupBox.Location = this.DetailGroupBox.Location;
            this.DetailPanelGroupBox.Size = this.DetailGroupBox.Size;
            this.DetailPanelGroupBox.Anchor = this.DetailGroupBox.Anchor;

            this.Load += this.ConfigPanel_Load;
        }

        /// <summary>
        /// Load
        /// </summary>
        /// <param name="sender">イベント発生元</param>
        /// <param name="e">イベント引数</param>
        private void ConfigPanel_Load(object sender, EventArgs e)
        {
            this.LoadSpellTimerTable();

            this.DetailGroupBox.Visible = false;
            this.DetailPanelGroupBox.Visible = false;

            // コンボボックスにアイテムを装填する
            this.MatchSoundComboBox.DataSource = SoundController.Default.EnumlateWave();
            this.MatchSoundComboBox.ValueMember = "FullPath";
            this.MatchSoundComboBox.DisplayMember = "Name";

            this.OverSoundComboBox.DataSource = SoundController.Default.EnumlateWave();
            this.OverSoundComboBox.ValueMember = "FullPath";
            this.OverSoundComboBox.DisplayMember = "Name";

            this.BeforeSoundComboBox.DataSource = SoundController.Default.EnumlateWave();
            this.BeforeSoundComboBox.ValueMember = "FullPath";
            this.BeforeSoundComboBox.DisplayMember = "Name";

            this.TimeupSoundComboBox.DataSource = SoundController.Default.EnumlateWave();
            this.TimeupSoundComboBox.ValueMember = "FullPath";
            this.TimeupSoundComboBox.DisplayMember = "Name";

            this.SpellIconComboBox.DataSource = IconController.Default.EnumlateIcon()
                .OrderBy(x => x.RelativePath)
                .ToArray();
            this.SpellIconComboBox.ValueMember = "RelativePath";
            this.SpellIconComboBox.DisplayMember = "RelativePath";

            // イベントを設定する
            this.SpellTimerTreeView.AfterSelect += this.SpellTimerTreeView_AfterSelect;
            this.AddButton.Click += this.AddButton_Click;
            this.UpdateButton.Click += this.UpdateButton_Click;
            this.DeleteButton.Click += this.DeleteButton_Click;

            this.Play1Button.Click += (s1, e1) =>
            {
                SoundController.Default.Play((string)this.MatchSoundComboBox.SelectedValue ?? string.Empty);
            };

            this.Play2Button.Click += (s1, e1) =>
            {
                SoundController.Default.Play((string)this.OverSoundComboBox.SelectedValue ?? string.Empty);
            };

            this.Play3Button.Click += (s1, e1) =>
            {
                SoundController.Default.Play((string)this.TimeupSoundComboBox.SelectedValue ?? string.Empty);
            };

            this.Play4Button.Click += (s1, e1) =>
            {
                SoundController.Default.Play((string)this.BeforeSoundComboBox.SelectedValue ?? string.Empty);
            };

            this.Speak1Button.Click += (s1, e1) =>
            {
                SoundController.Default.Play(this.MatchTextToSpeakTextBox.Text);
            };

            this.Speak2Button.Click += (s1, e1) =>
            {
                SoundController.Default.Play(this.OverTextToSpeakTextBox.Text);
            };

            this.Speak3Button.Click += (s1, e1) =>
            {
                SoundController.Default.Play(this.TimeupTextToSpeakTextBox.Text);
            };

            this.Speak4Button.Click += (s1, e1) =>
            {
                SoundController.Default.Play(this.BeforeTextToSpeakTextBox.Text);
            };

            this.SpellTimerTreeView.AfterCheck += (s1, e1) =>
            {
                var source = e1.Node.Tag as SpellTimer;
                if (source != null)
                {
                    source.Enabled = e1.Node.Checked;
                    source.UpdateDone = false;
                }
                else
                {
                    foreach (TreeNode node in e1.Node.Nodes)
                    {
                        var sourceChild = node.Tag as SpellTimer;
                        if (sourceChild != null)
                        {
                            sourceChild.Enabled = e1.Node.Checked;
                        }

                        node.Checked = e1.Node.Checked;
                    }
                }

                // キャッシュを無効にする
                SpellTimerTable.ClearReplacedKeywords();

                // スペルの有効・無効が変化した際に、標準のスペルタイマーに反映する
                SpellTimerCore.Default.applyToNormalSpellTimer();
            };

            this.SelectJobButton.Click += (s1, e1) =>
            {
                var src = this.DetailGroupBox.Tag as SpellTimer;
                if (src != null)
                {
                    using (var f = new SelectJobForm())
                    {
                        f.JobFilter = src.JobFilter;
                        if (f.ShowDialog(this) == DialogResult.OK)
                        {
                            src.JobFilter = f.JobFilter;
                        }
                    }
                }
            };

            this.SelectZoneButton.Click += (s1, e1) =>
            {
                var src = this.DetailGroupBox.Tag as SpellTimer;
                if (src != null)
                {
                    using (var f = new SelectZoneForm())
                    {
                        f.ZoneFilter = src.ZoneFilter;
                        if (f.ShowDialog(this) == DialogResult.OK)
                        {
                            src.ZoneFilter = f.ZoneFilter;
                        }
                    }
                }
            };

            this.SpellIconComboBox.SelectionChangeCommitted += (s1, e1) =>
            {
                this.SpellVisualSetting.SpellIcon = (string)this.SpellIconComboBox.SelectedValue;
                this.SpellVisualSetting.RefreshSampleImage();
            };

            this.SpellIconSizeUpDown.ValueChanged += (s1, e1) =>
            {
                this.SpellVisualSetting.SpellIconSize = (int)this.SpellIconSizeUpDown.Value;
                this.SpellVisualSetting.RefreshSampleImage();
            };

            this.HideSpellNameCheckBox.CheckedChanged += (s1, e1) =>
            {
                this.SpellVisualSetting.HideSpellName = this.HideSpellNameCheckBox.Checked;
                this.SpellVisualSetting.RefreshSampleImage();
            };

            this.OverlapRecastTimeCheckBox.CheckedChanged += (s1, e1) =>
            {
                this.SpellVisualSetting.OverlapRecastTime = this.OverlapRecastTimeCheckBox.Checked;
                this.SpellVisualSetting.RefreshSampleImage();
            };

            // オプションのロードメソッドを呼ぶ
            this.LoadOption();

            // ワンポイントテロップのロードメソッドを呼ぶ
            this.LoadOnePointTelop();

            // 戦闘アナライザのロードメソッドを呼ぶ
            this.LoadCombatAnalyzer();
        }

        /// <summary>
        /// 追加 Click
        /// </summary>
        /// <param name="sender">イベント発生元</param>
        /// <param name="e">イベント引数</param>
        private void AddButton_Click(object sender, EventArgs e)
        {
            lock (SpellTimerTable.Table)
            {
                var nr = new SpellTimer();

                nr.ID = SpellTimerTable.Table.Any() ?
                    SpellTimerTable.Table.Max(x => x.ID) + 1 :
                    1;
                nr.Panel = "General";
                nr.SpellTitle = "New Spell";
                nr.SpellIcon = string.Empty;
                nr.SpellIconSize = 24;
                nr.ProgressBarVisible = true;
                nr.FontColor = Settings.Default.FontColor.ToHTML();
                nr.FontOutlineColor = Settings.Default.FontOutlineColor.ToHTML();
                nr.BarColor = Settings.Default.ProgressBarColor.ToHTML();
                nr.BarOutlineColor = Settings.Default.ProgressBarOutlineColor.ToHTML();
                nr.FontFamily = Settings.Default.Font.Name;
                nr.FontSize = Settings.Default.Font.Size;
                nr.FontStyle = (int)Settings.Default.Font.Style;
                nr.BarWidth = Settings.Default.ProgressBarSize.Width;
                nr.BarHeight = Settings.Default.ProgressBarSize.Height;
                nr.BackgroundColor = Settings.Default.BackgroundColor.ToHTML();
                nr.JobFilter = string.Empty;
                nr.ZoneFilter = string.Empty;

                // 現在選択しているノードの情報を一部コピーする
                if (this.SpellTimerTreeView.SelectedNode != null)
                {
                    var baseRow = this.SpellTimerTreeView.SelectedNode.Tag != null ?
                        this.SpellTimerTreeView.SelectedNode.Tag as SpellTimer :
                        this.SpellTimerTreeView.SelectedNode.Nodes[0].Tag as SpellTimer;

                    if (baseRow != null)
                    {
                        nr.Panel = baseRow.Panel;
                        nr.SpellTitle = baseRow.SpellTitle + " New";
                        nr.SpellIcon = baseRow.SpellIcon;
                        nr.SpellIconSize = baseRow.SpellIconSize;
                        nr.Keyword = baseRow.Keyword;
                        nr.RegexEnabled = baseRow.RegexEnabled;
                        nr.RecastTime = baseRow.RecastTime;
                        nr.KeywordForExtend1 = baseRow.KeywordForExtend1;
                        nr.RecastTimeExtending1 = baseRow.RecastTimeExtending1;
                        nr.KeywordForExtend2 = baseRow.KeywordForExtend2;
                        nr.RecastTimeExtending2 = baseRow.RecastTimeExtending2;
                        nr.ExtendBeyondOriginalRecastTime = baseRow.ExtendBeyondOriginalRecastTime;
                        nr.UpperLimitOfExtension = baseRow.UpperLimitOfExtension;
                        nr.RepeatEnabled = baseRow.RepeatEnabled;
                        nr.ProgressBarVisible = baseRow.ProgressBarVisible;
                        nr.IsReverse = baseRow.IsReverse;
                        nr.FontColor = baseRow.FontColor;
                        nr.FontOutlineColor = baseRow.FontOutlineColor;
                        nr.BarColor = baseRow.BarColor;
                        nr.BarOutlineColor = baseRow.BarOutlineColor;
                        nr.DontHide = baseRow.DontHide;
                        nr.HideSpellName = baseRow.HideSpellName;
                        nr.OverlapRecastTime = baseRow.OverlapRecastTime;
                        nr.ReduceIconBrightness = baseRow.ReduceIconBrightness;
                        nr.FontFamily = baseRow.FontFamily;
                        nr.FontSize = baseRow.FontSize;
                        nr.FontStyle = baseRow.FontStyle;
                        nr.Font = baseRow.Font;
                        nr.BarWidth = baseRow.BarWidth;
                        nr.BarHeight = baseRow.BarHeight;
                        nr.BackgroundColor = baseRow.BackgroundColor;
                        nr.BackgroundAlpha = baseRow.BackgroundAlpha;
                        nr.JobFilter = baseRow.JobFilter;
                        nr.ZoneFilter = baseRow.ZoneFilter;
                    }
                }

                nr.MatchDateTime = DateTime.MinValue;
                nr.UpdateDone = false;
                nr.Enabled = true;
                nr.DisplayNo = SpellTimerTable.Table.Any() ?
                    SpellTimerTable.Table.Max(x => x.DisplayNo) + 1 :
                    50;
                nr.Regex = null;
                nr.RegexPattern = string.Empty;
                SpellTimerTable.Table.Add(nr);

                SpellTimerTable.ClearReplacedKeywords();
                SpellTimerTable.Save();

                // 新しいノードを生成する
                var node = new TreeNode(nr.SpellTitle)
                {
                    Tag = nr,
                    ToolTipText = nr.Keyword,
                    Checked = nr.Enabled
                };

                // 親ノードがあれば追加する
                foreach (TreeNode item in this.SpellTimerTreeView.Nodes)
                {
                    if (item.Text == nr.Panel)
                    {
                        item.Nodes.Add(node);
                        this.SpellTimerTreeView.SelectedNode = node;
                        break;
                    }
                }

                // 親ノードがなかった
                if (this.SpellTimerTreeView.SelectedNode != node)
                {
                    var parentNode = new TreeNode(nr.Panel, new TreeNode[] { node })
                    {
                        Checked = true
                    };

                    this.SpellTimerTreeView.Nodes.Add(parentNode);
                    this.SpellTimerTreeView.SelectedNode = node;
                }
            }

            // 標準のスペルタイマーへ変更を反映する
            SpellTimerCore.Default.applyToNormalSpellTimer();
        }

        /// <summary>
        /// 更新 Click
        /// </summary>
        /// <param name="sender">イベント発生元</param>
        /// <param name="e">イベント引数</param>
        private void UpdateButton_Click(object sender, EventArgs e)
        {
            lock (SpellTimerTable.Table)
            {
                if (string.IsNullOrWhiteSpace(this.PanelNameTextBox.Text))
                {
                    MessageBox.Show(
                        this,
                        Translate.Get("UpdateSpellPanelTitle"),
                        "ACT.SpecialSpellTimer",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation);
                    return;
                }

                if (string.IsNullOrWhiteSpace(this.SpellTitleTextBox.Text))
                {
                    MessageBox.Show(
                        this,
                        Translate.Get("UpdateSpellNameTitle"),
                        "ACT.SpecialSpellTimer",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation);
                    return;
                }

                var src = this.DetailGroupBox.Tag as SpellTimer;
                if (src != null)
                {
                    src.Panel = this.PanelNameTextBox.Text;
                    src.SpellTitle = this.SpellTitleTextBox.Text;
                    src.SpellIcon = (string)this.SpellIconComboBox.SelectedValue ?? string.Empty;
                    src.SpellIconSize = (int)this.SpellIconSizeUpDown.Value;
                    src.DisplayNo = (int)this.DisplayNoNumericUpDown.Value;
                    src.Keyword = this.KeywordTextBox.Text;
                    src.RegexEnabled = this.RegexEnabledCheckBox.Checked;
                    src.RecastTime = (long)this.RecastTimeNumericUpDown.Value;
                    src.RepeatEnabled = this.RepeatCheckBox.Checked;
                    src.ProgressBarVisible = this.ShowProgressBarCheckBox.Checked;

                    src.KeywordForExtend1 = this.KeywordToExpand1TextBox.Text;
                    src.RecastTimeExtending1 = (long)this.ExpandSecounds1NumericUpDown.Value;
                    src.KeywordForExtend2 = this.KeywordToExpand2TextBox.Text;
                    src.RecastTimeExtending2 = (long)this.ExpandSecounds2NumericUpDown.Value;
                    src.ExtendBeyondOriginalRecastTime = this.ExtendBeyondOriginalRecastTimeCheckBox.Checked;
                    src.UpperLimitOfExtension = (long)this.UpperLimitOfExtensionNumericUpDown.Value;

                    src.MatchSound = (string)this.MatchSoundComboBox.SelectedValue ?? string.Empty;
                    src.MatchTextToSpeak = this.MatchTextToSpeakTextBox.Text;

                    src.OverSound = (string)this.OverSoundComboBox.SelectedValue ?? string.Empty;
                    src.OverTextToSpeak = this.OverTextToSpeakTextBox.Text;
                    src.OverTime = (int)this.OverTimeNumericUpDown.Value;

                    src.BeforeSound = (string)this.BeforeSoundComboBox.SelectedValue ?? string.Empty;
                    src.BeforeTextToSpeak = this.BeforeTextToSpeakTextBox.Text;
                    src.BeforeTime = (int)this.BeforeTimeNumericUpDown.Value;

                    src.TimeupSound = (string)this.TimeupSoundComboBox.SelectedValue ?? string.Empty;
                    src.TimeupTextToSpeak = this.TimeupTextToSpeakTextBox.Text;

                    src.IsReverse = this.IsReverseCheckBox.Checked;
                    src.DontHide = this.DontHideCheckBox.Checked;
                    src.HideSpellName = this.HideSpellNameCheckBox.Checked;
                    src.OverlapRecastTime = this.OverlapRecastTimeCheckBox.Checked;
                    src.ReduceIconBrightness = this.ReduceIconBrightnessCheckBox.Checked;

                    src.Font = this.SpellVisualSetting.GetFontInfo();
                    src.FontColor = this.SpellVisualSetting.FontColor.ToHTML();
                    src.FontOutlineColor = this.SpellVisualSetting.FontOutlineColor.ToHTML();
                    src.BarColor = this.SpellVisualSetting.BarColor.ToHTML();
                    src.BarOutlineColor = this.SpellVisualSetting.BarOutlineColor.ToHTML();
                    src.BarWidth = this.SpellVisualSetting.BarSize.Width;
                    src.BarHeight = this.SpellVisualSetting.BarSize.Height;
                    src.BackgroundColor = this.SpellVisualSetting.BackgroundColor.ToHTML();
                    src.BackgroundAlpha = this.SpellVisualSetting.BackgroundColor.A;

                    var panel = SpellTimerTable.Table.Where(x => x.Panel == src.Panel);
                    foreach (var s in panel)
                    {
                        s.BackgroundColor = src.BackgroundColor;
                    }

                    SpellTimerTable.ClearReplacedKeywords();
                    SpellTimerTable.Save();
                    this.LoadSpellTimerTable();

                    // 一度全てのパネルを閉じる
                    SpellTimerCore.Default.ClosePanels();

                    foreach (TreeNode root in this.SpellTimerTreeView.Nodes)
                    {
                        if (root.Nodes != null)
                        {
                            foreach (TreeNode node in root.Nodes)
                            {
                                var ds = node.Tag as SpellTimer;
                                if (ds != null)
                                {
                                    if (ds.ID == src.ID)
                                    {
                                        this.SpellTimerTreeView.SelectedNode = node;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 削除 Click
        /// </summary>
        /// <param name="sender">イベント発生元</param>
        /// <param name="e">イベント引数</param>
        private void DeleteButton_Click(object sender, EventArgs e)
        {
            lock (SpellTimerTable.Table)
            {
                var src = this.DetailGroupBox.Tag as SpellTimer;
                if (src != null)
                {
                    SpellTimerTable.Table.Remove(src);
                    SpellTimerTable.ClearReplacedKeywords();
                    SpellTimerTable.Save();

                    this.DetailGroupBox.Visible = false;
                    this.DetailPanelGroupBox.Visible = false;
                }
            }

            // 今の選択ノードを取り出す
            var targetNode = this.SpellTimerTreeView.SelectedNode;
            if (targetNode != null)
            {
                // 1個前のノードを取り出しておく
                var prevNode = targetNode.PrevNode;

                if (targetNode.Parent != null &&
                    targetNode.Parent.Nodes.Count > 1)
                {
                    targetNode.Remove();

                    if (prevNode != null)
                    {
                        this.SpellTimerTreeView.SelectedNode = prevNode;
                    }
                }
                else
                {
                    targetNode.Parent.Remove();
                }
            }

            // 標準のスペルタイマーへ変更を反映する
            SpellTimerCore.Default.applyToNormalSpellTimer();
        }

        /// <summary>
        /// スペルタイマツリー AfterSelect
        /// </summary>
        /// <param name="sender">イベント発生元</param>
        /// <param name="e">イベント引数</param>
        private void SpellTimerTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var ds = e.Node.Tag as SpellTimer;

            // スペルの詳細？
            if (ds != null)
            {
                this.DetailPanelGroupBox.Visible = false;

                this.ShowDetail(
                    e.Node.Tag as SpellTimer);

                return;
            }

            // パネルの詳細を表示する
            this.DetailGroupBox.Visible = false;
            this.DetailPanelGroupBox.Visible = true;

            // パネル名を取り出す
            var panelName = e.Node.Text;
            this.DetailPanelGroupBox.Tag = panelName;

            // パネルの位置を取得する
            double left, top;
            SpellTimerCore.Default.GetPanelLocation(
                panelName,
                out left,
                out top);

            this.PanelLeftNumericUpDown.Value = (int)left;
            this.PanelTopNumericUpDown.Value = (int)top;

            int margin;
            SpellTimerCore.Default.GetSpellMargin(
                panelName,
                out margin);
            this.MarginUpDown.Value = margin;

            bool horizontal, fixedPositionSpell;
            SpellTimerCore.Default.GetPanelLayout(
                panelName,
                out horizontal,
                out fixedPositionSpell);
            this.HorizontalLayoutCheckBox.Checked = horizontal;
            this.FixedPositionSpellCheckBox.Checked = fixedPositionSpell;

            // 更新ボタンの挙動をセットする
            if (this.UpdatePanelButton.Tag == null ||
                !(bool)(this.UpdatePanelButton.Tag))
            {
                this.UpdatePanelButton.Click += new EventHandler((s1, e1) =>
                {
                    left = (double)this.PanelLeftNumericUpDown.Value;
                    top = (double)this.PanelTopNumericUpDown.Value;
                    margin = (int)this.MarginUpDown.Value;
                    horizontal = this.HorizontalLayoutCheckBox.Checked;
                    fixedPositionSpell = this.FixedPositionSpellCheckBox.Checked;

                    if (this.DetailPanelGroupBox.Tag != null)
                    {
                        var panelNameToUpdate = (string)this.DetailPanelGroupBox.Tag;
                        SpellTimerCore.Default.SetPanelLocation(
                            panelNameToUpdate,
                            left,
                            top);
                        SpellTimerCore.Default.SetSpellMargin(
                            panelNameToUpdate,
                            margin);
                        SpellTimerCore.Default.SetPanelLayout(
                            panelNameToUpdate,
                            horizontal,
                            fixedPositionSpell);
                    }
                });

                this.UpdatePanelButton.Tag = true;
            }
        }

        /// <summary>
        /// スペルタイマテーブルを読み込む
        /// </summary>
        public void LoadSpellTimerTable()
        {
            try
            {
                this.SpellTimerTreeView.SuspendLayout();

                this.SpellTimerTreeView.Nodes.Clear();

                var panels = SpellTimerTable.Table
                    .OrderBy(x => x.Panel)
                    .Select(x => x.Panel)
                    .Distinct();
                foreach (var panelName in panels)
                {
                    var children = new List<TreeNode>();
                    var spells = SpellTimerTable.Table
                        .OrderBy(x => x.DisplayNo)
                        .Where(x => x.Panel == panelName);
                    foreach (var spell in spells)
                    {
                        var nc = new TreeNode()
                        {
                            Text = spell.SpellTitle,
                            ToolTipText = spell.Keyword,
                            Checked = spell.Enabled,
                            Tag = spell,
                        };

                        children.Add(nc);
                    }

                    var n = new TreeNode(
                        panelName,
                        children.ToArray());

                    n.Checked = children.Any(x => x.Checked);

                    this.SpellTimerTreeView.Nodes.Add(n);

                    // バー表示の初期化が必要なことを記録
                    foreach (var spell in spells)
                    {
                        spell.UpdateDone = false;
                    }
                }

                // 標準のスペルタイマーへ変更を反映する
                SpellTimerCore.Default.applyToNormalSpellTimer();

                this.SpellTimerTreeView.ExpandAll();
            }
            finally
            {
                this.SpellTimerTreeView.ResumeLayout();
            }
        }

        /// <summary>
        /// 詳細を表示する
        /// </summary>
        /// <param name="dataSource">データソース</param>
        private void ShowDetail(
            SpellTimer dataSource)
        {
            var src = dataSource;
            if (src == null)
            {
                this.DetailGroupBox.Visible = false;
                return;
            }

            this.DetailGroupBox.Visible = true;

            this.PanelNameTextBox.Text = src.Panel;
            this.SpellTitleTextBox.Text = src.SpellTitle;
            this.SpellIconComboBox.SelectedValue = src.SpellIcon;
            this.SpellIconSizeUpDown.Value = src.SpellIconSize;
            this.DisplayNoNumericUpDown.Value = src.DisplayNo;
            this.KeywordTextBox.Text = src.Keyword;
            this.RegexEnabledCheckBox.Checked = src.RegexEnabled;
            this.RecastTimeNumericUpDown.Value = src.RecastTime;
            this.RepeatCheckBox.Checked = src.RepeatEnabled;
            this.ShowProgressBarCheckBox.Checked = src.ProgressBarVisible;

            this.KeywordToExpand1TextBox.Text = src.KeywordForExtend1;
            this.ExpandSecounds1NumericUpDown.Value = src.RecastTimeExtending1;
            this.KeywordToExpand2TextBox.Text = src.KeywordForExtend2;
            this.ExpandSecounds2NumericUpDown.Value = src.RecastTimeExtending2;
            this.ExtendBeyondOriginalRecastTimeCheckBox.Checked = src.ExtendBeyondOriginalRecastTime;
            this.UpperLimitOfExtensionNumericUpDown.Value = src.UpperLimitOfExtension;

            this.MatchSoundComboBox.SelectedValue = src.MatchSound;
            this.MatchTextToSpeakTextBox.Text = src.MatchTextToSpeak;

            this.OverSoundComboBox.SelectedValue = src.OverSound;
            this.OverTextToSpeakTextBox.Text = src.OverTextToSpeak;
            this.OverTimeNumericUpDown.Value = src.OverTime;

            this.BeforeSoundComboBox.SelectedValue = src.BeforeSound;
            this.BeforeTextToSpeakTextBox.Text = src.BeforeTextToSpeak;
            this.BeforeTimeNumericUpDown.Value = src.BeforeTime;

            this.TimeupSoundComboBox.SelectedValue = src.TimeupSound;
            this.TimeupTextToSpeakTextBox.Text = src.TimeupTextToSpeak;

            this.IsReverseCheckBox.Checked = src.IsReverse;
            this.DontHideCheckBox.Checked = src.DontHide;
            this.HideSpellNameCheckBox.Checked = src.HideSpellName;
            this.OverlapRecastTimeCheckBox.Checked = src.OverlapRecastTime;
            this.ReduceIconBrightnessCheckBox.Checked = src.ReduceIconBrightness;

            this.SpellVisualSetting.SetFontInfo(src.Font);
            this.SpellVisualSetting.BarColor = string.IsNullOrWhiteSpace(src.BarColor) ?
                Settings.Default.ProgressBarColor :
                src.BarColor.FromHTML();
            this.SpellVisualSetting.BarOutlineColor = string.IsNullOrWhiteSpace(src.BarOutlineColor) ?
                Settings.Default.ProgressBarOutlineColor :
                src.BarOutlineColor.FromHTML();
            this.SpellVisualSetting.FontColor = string.IsNullOrWhiteSpace(src.FontColor) ?
                Settings.Default.FontColor :
                src.FontColor.FromHTML();
            this.SpellVisualSetting.FontOutlineColor = string.IsNullOrWhiteSpace(src.FontOutlineColor) ?
                Settings.Default.FontOutlineColor :
                src.FontOutlineColor.FromHTML();
            this.SpellVisualSetting.BarSize = new Size(src.BarWidth, src.BarHeight);
            this.SpellVisualSetting.BackgroundColor = string.IsNullOrWhiteSpace(src.BackgroundColor) ?
                Settings.Default.BackgroundColor :
                Color.FromArgb(src.BackgroundAlpha, src.BackgroundColor.FromHTML());

            this.SpellVisualSetting.SpellIcon = src.SpellIcon;
            this.SpellVisualSetting.SpellIconSize = src.SpellIconSize;
            this.SpellVisualSetting.HideSpellName = src.HideSpellName;
            this.SpellVisualSetting.OverlapRecastTime = src.OverlapRecastTime;

            this.SpellVisualSetting.RefreshSampleImage();

            // データソースをタグに突っ込んでおく
            this.DetailGroupBox.Tag = src;
        }

        /// <summary>
        /// エクスポート Click
        /// </summary>
        /// <param name="sender">イベント発生元</param>
        /// <param name="e">イベント引数</param>
        private void ExportButton_Click(object sender, EventArgs e)
        {
            this.SaveFileDialog.FileName = "ACT.SpecialSpellTimer.Spells.xml";
            if (this.SaveFileDialog.ShowDialog(this) != DialogResult.Cancel)
            {
                SpellTimerTable.Save(
                    this.SaveFileDialog.FileName);
            }
        }

        /// <summary>
        /// インポート Click
        /// </summary>
        /// <param name="sender">イベント発生元</param>
        /// <param name="e">イベント引数</param>
        private void ImportButton_Click(object sender, EventArgs e)
        {
            this.OpenFileDialog.FileName = "ACT.SpecialSpellTimer.Spells.xml";
            if (this.OpenFileDialog.ShowDialog(this) != DialogResult.Cancel)
            {
                SpellTimerTable.Load(
                    this.OpenFileDialog.FileName,
                    false);

                this.LoadSpellTimerTable();
            }
        }

        /// <summary>
        /// 全て削除
        /// </summary>
        /// <param name="sender">イベント発生元</param>
        /// <param name="e">イベント引数</param>
        private void ClearAllButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                this,
                Translate.Get("SpellClearAllPrompt"),
                "ACT.SpecialSpellTimer",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) == DialogResult.OK)
            {
                lock (SpellTimerTable.Table)
                {
                    this.DetailGroupBox.Visible = false;
                    this.DetailPanelGroupBox.Visible = false;
                    SpellTimerTable.Table.Clear();
                }

                this.LoadSpellTimerTable();
            }
        }
    }
}
