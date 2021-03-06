using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using ACT.SpecialSpellTimer.Image;
using Prism.Mvvm;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    public enum NoticeDevices
    {
        Both = 0,
        Main,
        Sub,
    }

    public enum TimelineElementTypes
    {
        Timeline = 0,
        Default,
        Activity,
        Trigger,
        Subroutine,
        Load,
        VisualNotice,
    }

    public static class TimelineElementTypesEx
    {
        public static string ToText(
            this TimelineElementTypes t)
            => new[]
            {
                "timeline",
                "default",
                "activity",
                "trigger",
                "subroutine",
                "load",
                "visualnotice",
            }[(int)t];

        public static TimelineElementTypes FromText(
            string text)
        {
            if (Enum.TryParse<TimelineElementTypes>(text, out TimelineElementTypes e))
            {
                return e;
            }

            return TimelineElementTypes.Timeline;
        }
    }

    [Serializable]
    public abstract class TimelineBase :
        BindableBase
    {
        [XmlIgnore]
        public abstract TimelineElementTypes TimelineType { get; }

        protected Guid id = Guid.NewGuid();

        [XmlIgnore]
        public Guid ID => this.id;

        protected string name = null;

        [XmlAttribute(AttributeName = "name")]
        public virtual string Name
        {
            get => this.name;
            set => this.SetProperty(ref this.name, value);
        }

        private bool? enabled = null;

        [XmlIgnore]
        public virtual bool? Enabled
        {
            get => this.enabled;
            set => this.SetProperty(ref this.enabled, value);
        }

        [XmlAttribute(AttributeName = "enabled")]
        public string EnabledXML
        {
            get => this.Enabled?.ToString();
            set => this.Enabled = bool.TryParse(value, out var v) ? v : (bool?)null;
        }

        private TimelineBase parent = null;

        [XmlIgnore]
        public TimelineBase Parent
        {
            get => this.parent;
            set => this.SetProperty(ref this.parent, value);
        }

        public T GetParent<T>() where T : TimelineBase
            => this.parent as T;

        [XmlIgnore]
        public abstract IList<TimelineBase> Children { get; }

        public void Walk(
            Action<TimelineBase> action)
        {
            if (action == null)
            {
                return;
            }

            action.Invoke(this);

            if (this.Children != null)
            {
                foreach (var element in this.Children)
                {
                    element.Walk(action);
                }
            }
        }
    }

    public interface ISynchronizable
    {
        string SyncKeyword { get; set; }

        string SyncKeywordReplaced { get; set; }

        Regex SynqRegex { get; }

        string Text { get; set; }

        string TextReplaced { get; set; }

        string Notice { get; set; }

        string NoticeReplaced { get; set; }
    }

    public interface IStylable
    {
        string Style { get; set; }

        TimelineStyle StyleModel { get; set; }

        string Icon { get; set; }

        bool ExistsIcon { get; }

        BitmapImage IconImage { get; }

        BitmapImage ThisIconImage { get; }
    }

    public static class IStylableEx
    {
        public static bool GetExistsIcon(
            this IStylable element) =>
            !string.IsNullOrEmpty(element.Icon) ||
            !string.IsNullOrEmpty(element.StyleModel?.Icon);

        public static BitmapImage GetIconImage(
            this IStylable element) =>
            element.ThisIconImage ?? element.StyleModel?.IconImage;

        public static BitmapImage GetThisIconImage(
            this IStylable element) =>
            string.IsNullOrEmpty(element.Icon) ?
            null :
            IconController.Instance.GetIconFile(element.Icon)?.CreateBitmapImage();
    }
}
