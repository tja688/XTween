namespace SevenStrikeModules.XTween.Timeline
{
    using System.Collections.Generic;
    using System.Linq;
    using SevenStrikeModules.XTween;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public partial class XTweenTimelineCallback : IXTweenTimelineAnimation
    {
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
        bool IXTweenTimelineAnimation.CallbackView => true;
        string IXTweenTimelineAnimation.Label => string.IsNullOrEmpty(id) ? "<i>Callback</i>" : id;
        Component IXTweenTimelineAnimation.Component => this;
        public IEnumerable<Object> Targets => Enumerable.Empty<Object>();

        XTween_Interface IXTweenTimelineAnimation.CreateEditorPreview()
        {
            return XTween.To(() => 0f, _ => { }, 1f, 0.0001f, autokill: false).SetDelay(delay);
        }
    }
}
