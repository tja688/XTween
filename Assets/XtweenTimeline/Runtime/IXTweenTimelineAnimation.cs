namespace SevenStrikeModules.XTween.Timeline
{
    using System.Collections.Generic;
    using SevenStrikeModules.XTween;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public interface IXTweenTimelineAnimation
    {
        XTween_Interface CreateTween(bool regenerateIfExists, bool andPlay = true);

        float Delay { get; set; }
        float Duration { get; }
        int Loops { get; }
        bool IsValid { get; }
        bool IsActive { get; }
        bool IsFrom { get; }
        string ValidationMessage => null;
        bool AllowEditorCallbacks => false;
        bool CallbackView => false;
        Texture2D CustomIcon => null;
        string Label { get; }
        Component Component { get; }
        XTween_Interface CreateEditorPreview();
        IEnumerable<Object> Targets { get; }
    }
}
