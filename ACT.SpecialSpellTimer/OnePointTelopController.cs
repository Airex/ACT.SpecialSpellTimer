﻿namespace ACT.SpecialSpellTimer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using ACT.SpecialSpellTimer.Properties;
    using ACT.SpecialSpellTimer.Sound;
    using ACT.SpecialSpellTimer.Utility;

    /// <summary>
    /// ワンポイントテレロップ Controller
    /// </summary>
    public class OnePointTelopController
    {
        /// <summary>
        /// テロップWindowのリスト
        /// </summary>
        private static Dictionary<long, OnePointTelopWindow> telopWindowList = new Dictionary<long, OnePointTelopWindow>();

        /// <summary>
        /// 位置を設定する
        /// </summary>
        /// <param name="telopID">設定するテロップのID</param>
        /// <param name="left">Left</param>
        /// <param name="top">Top</param>
        public static void SetLocation(
            long telopID,
            double left,
            double top)
        {
            if (telopWindowList != null)
            {
                var telop = telopWindowList.ContainsKey(telopID) ?
                    telopWindowList[telopID] :
                    null;

                if (telop != null)
                {
                    telop.Left = left;
                    telop.Top = top;
                }

                var telopSettings = OnePointTelopTable.Default.Table
                    .Where(x => x.ID == telopID)
                    .FirstOrDefault();

                if (telopSettings != null)
                {
                    telopSettings.Left = left;
                    telopSettings.Top = top;
                }
            }
        }

        /// <summary>
        /// 位置を取得する
        /// </summary>
        /// <param name="telopID">設定するテロップのID</param>
        /// <param name="left">Left</param>
        /// <param name="top">Top</param>
        public static void GettLocation(
            long telopID,
            out double left,
            out double top)
        {
            left = 0;
            top = 0;

            if (telopWindowList != null)
            {
                var telop = telopWindowList.ContainsKey(telopID) ?
                    telopWindowList[telopID] :
                    null;

                if (telop != null)
                {
                    left = telop.Left;
                    top = telop.Top;
                }
                else
                {
                    var telopSettings = OnePointTelopTable.Default.Table
                        .Where(x => x.ID == telopID)
                        .FirstOrDefault();

                    if (telopSettings != null)
                    {
                        left = telopSettings.Left;
                        top = telopSettings.Top;
                    }
                }
            }
        }

        /// <summary>
        /// テロップを閉じる
        /// </summary>
        public static void CloseTelops()
        {
            if (telopWindowList != null)
            {
                ActInvoker.Invoke(() =>
                {
                    foreach (var telop in telopWindowList.Values)
                    {
                        telop.DataSource.Left = telop.Left;
                        telop.DataSource.Top = telop.Top;

                        telop.Close();
                    }
                });

                if (telopWindowList.Count > 0)
                {
                    OnePointTelopTable.Default.Save();
                }

                telopWindowList.Clear();
            }
        }

        /// <summary>
        /// テロップを隠す
        /// </summary>
        public static void HideTelops()
        {
            if (telopWindowList != null)
            {
                ActInvoker.Invoke(() =>
                {
                    foreach (var telop in telopWindowList.Values)
                    {
                        telop.HideOverlay();
                    }
                });
            }
        }

        /// <summary>
        /// テロップをActive化する
        /// </summary>
        public static void ActivateTelops()
        {
            if (telopWindowList != null)
            {
                ActInvoker.Invoke(() =>
                {
                    foreach (var telop in telopWindowList.Values)
                    {
                        telop.Activate();
                    }
                });
            }
        }

        /// <summary>
        /// 不要になったWindowを閉じる
        /// </summary>
        /// <param name="telops">Telops</param>
        public static void GarbageWindows(
            OnePointTelop[] telops)
        {
            // 不要になったWindowを閉じる
            var removeWindowList = new List<OnePointTelopWindow>();
            foreach (var window in telopWindowList.Values)
            {
                if (!telops.Any(x => x.ID == window.DataSource.ID))
                {
                    removeWindowList.Add(window);
                }
            }

            foreach (var window in removeWindowList)
            {
                ActInvoker.Invoke(() =>
                {
                    window.DataSource.Left = window.Left;
                    window.DataSource.Top = window.Top;
                    window.Close();
                });

                telopWindowList.Remove(window.DataSource.ID);
            }
        }

        /// <summary>
        /// ログとマッチングする
        /// </summary>
        /// <param name="telops">Telops</param>
        /// <param name="logLines">ログ行</param>
        public static void Match(
            OnePointTelop[] telops,
            string[] logLines)
        {
            Parallel.ForEach(telops, (telop) =>
            {
                var regex = telop.Regex;
                var regexToHide = telop.RegexToHide;
                var notifyNeeded = false;

                foreach (var log in logLines)
                {
                    // 通常マッチ
                    if (regex == null)
                    {
                        var keyword = telop.KeywordReplaced;
                        if (!string.IsNullOrWhiteSpace(keyword))
                        {
                            if (log.ToUpper().Contains(
                                keyword.ToUpper()))
                            {
                                if (!telop.AddMessageEnabled)
                                {
                                    telop.MessageReplaced = telop.Message;
                                }
                                else
                                {
                                    telop.MessageReplaced += string.IsNullOrWhiteSpace(telop.MessageReplaced) ?
                                        telop.Message :
                                        Environment.NewLine + telop.Message;
                                }

                                telop.MatchDateTime = DateTime.Now;
                                telop.Delayed = false;
                                telop.MatchedLog = log;
                                telop.ForceHide = false;

                                SoundController.Default.Play(telop.MatchSound);
                                SoundController.Default.Play(telop.MatchTextToSpeak);

                                notifyNeeded = true;
                                continue;
                            }
                        }
                    }

                    // 正規表現マッチ
                    if (regex != null)
                    {
                        var match = regex.Match(log);
                        if (match.Success)
                        {
                            if (!telop.AddMessageEnabled)
                            {
                                telop.MessageReplaced = match.Result(telop.Message);
                            }
                            else
                            {
                                telop.MessageReplaced += string.IsNullOrWhiteSpace(telop.MessageReplaced) ?
                                    match.Result(telop.Message) :
                                    Environment.NewLine + match.Result(telop.Message);
                            }

                            telop.MatchDateTime = DateTime.Now;
                            telop.Delayed = false;
                            telop.MatchedLog = log;
                            telop.ForceHide = false;

                            SoundController.Default.Play(telop.MatchSound);
                            if (!string.IsNullOrWhiteSpace(telop.MatchTextToSpeak))
                            {
                                var tts = match.Result(telop.MatchTextToSpeak);
                                SoundController.Default.Play(tts);
                            }

                            notifyNeeded = true;
                            continue;
                        }
                    }

                    // 通常マッチ(強制非表示)
                    if (regexToHide == null)
                    {
                        var keyword = telop.KeywordToHideReplaced;
                        if (!string.IsNullOrWhiteSpace(keyword))
                        {
                            if (log.ToUpper().Contains(
                                keyword.ToUpper()))
                            {
                                telop.ForceHide = true;
                                notifyNeeded = true;
                                continue;
                            }
                        }
                    }

                    // 正規表現マッチ(強制非表示)
                    if (regexToHide != null)
                    {
                        if (regexToHide.IsMatch(log))
                        {
                            telop.ForceHide = true;
                            notifyNeeded = true;
                            continue;
                        }
                    }
                }   // end loop logLines

                // ディレイ時間が経過した？
                if (!telop.Delayed &&
                    telop.MatchDateTime > DateTime.MinValue &&
                    telop.Delay > 0)
                {
                    var delayed = telop.MatchDateTime.AddSeconds(telop.Delay);
                    if (DateTime.Now >= delayed)
                    {
                        telop.Delayed = true;
                        SoundController.Default.Play(telop.DelaySound);
                        var tts = regex != null && !string.IsNullOrWhiteSpace(telop.DelayTextToSpeak) ?
                            regex.Replace(telop.MatchedLog, telop.DelayTextToSpeak) :
                            telop.DelayTextToSpeak;
                        SoundController.Default.Play(tts);
                    }
                }

                if (notifyNeeded)
                {
                    SpellTimerCore.Default.updateNormalSpellTimerForTelop(telop, telop.ForceHide);
                    SpellTimerCore.Default.notifyNormalSpellTimerForTelop(telop.Title);
                }
            }); // end loop telops
        }

        /// <summary>
        /// Windowをリフレッシュする
        /// </summary>
        /// <param name="telop">テロップ</param>
        public static void RefreshTelopWindows(
            OnePointTelop[] telops)
        {
            foreach (var telop in telops)
            {
                var w = telopWindowList.ContainsKey(telop.ID) ? telopWindowList[telop.ID] : null;
                if (w == null)
                {
                    w = new OnePointTelopWindow()
                    {
                        Title = "OnePointTelop - " + telop.Title,
                        DataSource = telop
                    };

                    if (Settings.Default.ClickThroughEnabled)
                    {
                        w.ToTransparentWindow();
                    }

                    w.Opacity = 0;
                    w.Topmost = false;
                    w.Show();

                    telopWindowList.Add(telop.ID, w);
                }

                // telopの位置を保存する
                if (DateTime.Now.Second == 0)
                {
                    telop.Left = w.Left;
                    telop.Top = w.Top;
                    OnePointTelopTable.Default.Save();
                }

                if (Settings.Default.OverlayVisible &&
                    Settings.Default.TelopAlwaysVisible)
                {
                    // ドラッグ中じゃない？
                    if (!w.IsDragging)
                    {
                        w.Refresh();
                        w.ShowOverlay();
                    }

                    continue;
                }

                if (telop.MatchDateTime > DateTime.MinValue)
                {
                    var start = telop.MatchDateTime.AddSeconds(telop.Delay);
                    var end = telop.MatchDateTime.AddSeconds(telop.Delay + telop.DisplayTime);

                    if (start <= DateTime.Now && DateTime.Now <= end)
                    {
                        w.Refresh();
                        w.ShowOverlay();
                    }
                    else
                    {
                        w.HideOverlay();

                        if (DateTime.Now > end)
                        {
                            telop.MatchDateTime = DateTime.MinValue;
                            telop.MessageReplaced = string.Empty;
                        }
                    }

                    if (telop.ForceHide)
                    {
                        w.HideOverlay();
                        telop.MatchDateTime = DateTime.MinValue;
                        telop.MessageReplaced = string.Empty;
                    }
                }
                else
                {
                    w.HideOverlay();
                    telop.MessageReplaced = string.Empty;
                }
            }
        }
    }
}
