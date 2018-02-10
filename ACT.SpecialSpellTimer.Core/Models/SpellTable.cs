using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ACT.SpecialSpellTimer.Config;
using FFXIV.Framework.Common;
using FFXIV.Framework.Extensions;

namespace ACT.SpecialSpellTimer.Models
{
    /// <summary>
    /// SpellTimerテーブル
    /// </summary>
    public class SpellTable
    {
        #region Singleton

        private static SpellTable instance = new SpellTable();
        public static SpellTable Instance => instance;

        #endregion Singleton

        /// <summary>
        /// SpellTimerデータテーブル
        /// </summary>
        private volatile ObservableCollection<Spell> table = new ObservableCollection<Spell>();

        /// <summary>
        /// デフォルトのファイル
        /// </summary>
        public string DefaultFile => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"anoyetta\ACT\ACT.SpecialSpellTimer.Spells.xml");

        /// <summary>
        /// SpellTimerデータテーブル
        /// </summary>
        public ObservableCollection<Spell> Table => this.table;

        /// <summary>
        /// カウントをリセットする
        /// </summary>
        public static void ResetCount()
        {
            foreach (var row in TableCompiler.Instance.SpellList)
            {
                row.MatchDateTime = DateTime.MinValue;
                row.UpdateDone = false;
                row.OverDone = false;
                row.BeforeDone = false;
                row.TimeupDone = false;
                row.CompleteScheduledTime = DateTime.MinValue;

                row.StartOverSoundTimer();
                row.StartBeforeSoundTimer();
                row.StartTimeupSoundTimer();
                row.StartGarbageInstanceTimer();
            }
        }

        /// <summary>
        /// テーブルファイルをバックアップする
        /// </summary>
        public void Backup()
        {
            var file = this.DefaultFile;

            if (File.Exists(file))
            {
                var backupFile = Path.Combine(
                    Path.Combine(Path.GetDirectoryName(file), "backup"),
                    Path.GetFileNameWithoutExtension(file) + "." + DateTime.Now.ToString("yyyy-MM-dd") + ".bak");

                if (!Directory.Exists(Path.GetDirectoryName(backupFile)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(backupFile));
                }

                File.Copy(
                    file,
                    backupFile,
                    true);

                // 古いバックアップを消す
                foreach (var bak in
                    Directory.GetFiles(Path.GetDirectoryName(backupFile), "*.bak"))
                {
                    var timeStamp = File.GetCreationTime(bak);
                    if ((DateTime.Now - timeStamp).TotalDays >= 3.0d)
                    {
                        File.Delete(bak);
                    }
                }
            }
        }

        /// <summary>
        /// スペルの描画済みフラグをクリアする
        /// </summary>
        public void ClearUpdateFlags()
        {
            foreach (var item in this.table)
            {
                item.UpdateDone = false;
            }
        }

        /// <summary>
        /// 指定されたGuidを持つSpellTimerを取得する
        /// </summary>
        /// <param name="guid">Guid</param>
        public Spell GetSpellTimerByGuid(
            Guid guid)
        {
            return this.table
                .AsParallel()
                .Where(x => x.Guid == guid)
                .FirstOrDefault();
        }

        /// <summary>
        /// 読み込む
        /// </summary>
        public void Load()
        {
            this.Load(this.DefaultFile, true);
        }

        /// <summary>
        /// 読み込む
        /// </summary>
        /// <param name="file">ファイルパス</param>
        /// <param name="isClear">消去してからロードする？</param>
        public void Load(
            string file,
            bool isClear)
        {
            try
            {
                if (!File.Exists(file))
                {
                    return;
                }

                using (var sr = new StreamReader(file, new UTF8Encoding(false)))
                {
                    if (sr.BaseStream.Length > 0)
                    {
                        var xs = new XmlSerializer(table.GetType());
                        var data = xs.Deserialize(sr) as IList<Spell>;

                        if (isClear)
                        {
                            this.table.Clear();
                        }

                        foreach (var item in data)
                        {
                            // パネルIDを補完する
                            if (item.PanelID == Guid.Empty)
                            {
                                item.PanelID = SpellPanelTable.Instance.Table
                                    .FirstOrDefault(x => x.PanelName == item.PanelName)?
                                    .ID ?? Guid.Empty;
                            }

                            this.table.Add(item);
                        }
                    }
                }
            }
            finally
            {
                if (!this.table.Any())
                {
                    this.table.AddRange(Spell.SampleSpells);
                }

                this.Reset();
            }
        }

        /// <summary>
        /// スペルテーブルを初期化する
        /// </summary>
        public void Reset()
        {
            var id = 0L;
            foreach (var row in this.table)
            {
                id++;
                row.ID = id;
                if (row.Guid == Guid.Empty)
                {
                    row.Guid = Guid.NewGuid();
                }

                row.MatchDateTime = DateTime.MinValue;
                row.Regex = null;
                row.RegexPattern = string.Empty;
                row.KeywordReplaced = string.Empty;
                row.RegexForExtend1 = null;
                row.RegexForExtendPattern1 = string.Empty;
                row.KeywordForExtendReplaced1 = string.Empty;
                row.RegexForExtend2 = null;
                row.RegexForExtendPattern2 = string.Empty;
                row.KeywordForExtendReplaced2 = string.Empty;

                if (string.IsNullOrWhiteSpace(row.BackgroundColor))
                {
                    row.BackgroundColor = Color.Transparent.ToHTML();
                }
            }
        }

        /// <summary>
        /// 保存する
        /// </summary>
        public void Save(
            bool force = false)
        {
            this.Save(this.DefaultFile, force);
        }

        /// <summary>
        /// 保存する
        /// </summary>
        /// <param name="file">ファイルパス</param>
        public void Save(
            string file,
            bool force,
            string panelName = "")
        {
            if (this.table == null)
            {
                return;
            }

            if (!force)
            {
                if (this.table.Count <= 0)
                {
                    return;
                }
            }

            var work = this.table.Where(x =>
                !x.IsInstance &&
                (
                    string.IsNullOrEmpty(panelName) ||
                    x.PanelName == panelName
                )).ToList();

            this.Save(file, work);
        }

        public void Save(
            string file,
            List<Spell> list)
        {
            lock (this)
            {
                var dir = Path.GetDirectoryName(file);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var sb = new StringBuilder();
                using (var sw = new StringWriter(sb))
                {
                    var xs = new XmlSerializer(list.GetType());
                    xs.Serialize(sw, list);
                }

                sb.Replace("utf-16", "utf-8");

                File.WriteAllText(
                    file,
                    sb.ToString(),
                    new UTF8Encoding(false));
            }
        }

        public IList<Spell> LoadFromFile(
            string file)
        {
            var data = default(IList<Spell>);

            if (!File.Exists(file))
            {
                return data;
            }

            using (var sr = new StreamReader(file, new UTF8Encoding(false)))
            {
                if (sr.BaseStream.Length > 0)
                {
                    var xs = new XmlSerializer(table.GetType());
                    data = xs.Deserialize(sr) as IList<Spell>;

                    // IDは振り直す
                    if (data != null)
                    {
                        var id = this.table.Any() ?
                            this.table.Max(x => x.ID) + 1 :
                            1;
                        foreach (var item in data)
                        {
                            item.ID = id++;
                            item.Guid = Guid.NewGuid();
                        }
                    }
                }
            }

            return data;
        }

        #region To Instance spells

        private static readonly object lockObject = new object();

        /// <summary>
        /// インスタンス化されたスペルの辞書 key : スペルの表示名
        /// </summary>
        private volatile ConcurrentDictionary<string, Spell> instanceSpells =
            new ConcurrentDictionary<string, Spell>();

        /// <summary>
        /// 同じスペル表示名のインスタンスを取得するか新たに作成する
        /// </summary>
        /// <param name="spellTitle">スペル表示名</param>
        /// <param name="sourceSpell">インスタンスの元となるスペル</param>
        /// <returns>インスタンススペル</returns>
        public Spell GetOrAddInstance(
            string spellTitle,
            Spell sourceSpell)
        {
            var instance = default(Spell);

            lock (sourceSpell)
            {
                instance = this.instanceSpells.GetOrAdd(
                    spellTitle,
                    (title) => sourceSpell.CreateInstanceNew(title));
            }

            lock (instance)
            {
                instance.CompleteScheduledTime = DateTime.MinValue;

                // スペルテーブル本体に登録する
                lock (lockObject)
                {
                    instance.ID = this.table.Max(y => y.ID) + 1;

                    WPFHelper.BeginInvoke(() =>
                    {
                        lock (lockObject)
                        {
                            this.table.Add(instance);
                        }
                    });

                    TableCompiler.Instance.AddSpell(instance);
                }
            }

            return instance;
        }

        /// <summary>
        /// インスタンス化されたスペルをすべて削除する
        /// </summary>
        public void RemoveInstanceSpellsAll()
        {
            this.instanceSpells.Clear();

            lock (lockObject)
            {
                var targets = this.table.Where(x => x.IsInstance).ToArray();
                foreach (var item in targets)
                {
                    this.table.Remove(item);
                }
            }

            TableCompiler.Instance.RemoveInstanceSpells();
        }

        /// <summary>
        /// instanceが不要になっていたらコレクションから除去する
        /// </summary>
        /// <param name="instance">インスタンス</param>
        public void TryRemoveInstance(
            Spell instance)
        {
            var ttl = Settings.Default.TimeOfHideSpell + 30;

            lock (instance)
            {
                if (instance.CompleteScheduledTime != DateTime.MinValue &&
                    (DateTime.Now - instance.CompleteScheduledTime).TotalSeconds >= ttl)
                {
                    if (!instance.IsInstance ||
                        instance.IsDesignMode)
                    {
                        return;
                    }

                    // ガーベージタイマを止める
                    instance.StopGarbageInstanceTimer();

                    this.instanceSpells.TryRemove(instance.SpellTitleReplaced, out Spell o);

                    // スペルコレクション本体から除去する
                    WPFHelper.BeginInvoke(() =>
                    {
                        lock (lockObject)
                        {
                            this.table.Remove(instance);
                        }
                    });

                    // コンパイル済みリストから除去する
                    TableCompiler.Instance.RemoveSpell(instance);

                    instance.Dispose();
                }
            }
        }

        #endregion To Instance spells
    }
}
