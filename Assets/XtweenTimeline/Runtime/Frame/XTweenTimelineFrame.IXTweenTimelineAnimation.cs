namespace SevenStrikeModules.XTween.Timeline
{
    using System.Collections.Generic;
    using System.Linq;
    using SevenStrikeModules.XTween;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public partial class XTweenTimelineFrame : IXTweenTimelineAnimation
    {
        private const string ICON_FRAME =
            "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAACXBIWXMAAAsTAAALEwEAmpwYAAAA1klEQVR4nM2WPQ6CQBBGsaPQeAAVC2oLT8EhrIxaqBXXUW9goyextPAQ2hjKZyZZIon8DOxCfNUEsu9LdpcZPE8JMAY2wFpq7TqtPAJefJE6ciUPgSe/yLPQVu4DN4qRd75NwIlqjk3lC4U8ZVlXPgPeNQISYK6VD4A79XkAwyp5DzjTnKs4ygJi7ImL5FOzl7YkQJAXsMcd27yAXdsBgcMtmnR/yI6u6aX0mpqQfmsfWietopNmlyKtWCE/eH87cIRWR2aKGfrZEHdDPxMyMr8sK6m1Cz8PKLC39qdilAAAAABJRU5ErkJggg==";

        float IXTweenTimelineAnimation.Delay
        {
            get => delay;
            set => delay = value;
        }

        float IXTweenTimelineAnimation.Duration => 0f;
        int IXTweenTimelineAnimation.Loops => 0;
        bool IXTweenTimelineAnimation.IsValid => true;
        bool IXTweenTimelineAnimation.IsActive => isActiveAndEnabled;
        bool IXTweenTimelineAnimation.IsFrom => false;
        public bool AllowEditorCallbacks => true;
        public bool CallbackView => true;
        Texture2D IXTweenTimelineAnimation.CustomIcon => XTweenTimelineUtils.ImageFromString(ICON_FRAME);
        string IXTweenTimelineAnimation.Label => string.IsNullOrEmpty(id) ? "<i>Frame</i>" : id;
        Component IXTweenTimelineAnimation.Component => this;

        public IEnumerable<Object> Targets => properties.Select(property =>
        {
            if (property.Property == FrameProperty.PropertyType.Active)
            {
                return (Object)property.TargetGameObject;
            }

            return property.Target;
        });

        XTween_Interface IXTweenTimelineAnimation.CreateEditorPreview() => CreateTween(regenerateIfExists: true);
    }
}
