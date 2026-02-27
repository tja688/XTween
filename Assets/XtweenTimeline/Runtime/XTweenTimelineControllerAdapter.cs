namespace SevenStrikeModules.XTween.Timeline
{
    using System.Collections.Generic;
    using System.Linq;
    using SevenStrikeModules.XTween;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public sealed class XTweenTimelineControllerAdapter : IXTweenTimelineAnimation
    {
        private readonly XTween_Controller controller;

        public XTweenTimelineControllerAdapter(XTween_Controller controller)
        {
            this.controller = controller;
        }

        public static bool IsSupportedTweenType(XTweenTypes tweenType)
        {
            return tweenType == XTweenTypes.位置_Position
                   || tweenType == XTweenTypes.缩放_Scale
                   || tweenType == XTweenTypes.旋转_Rotation
                   || tweenType == XTweenTypes.透明度_Alpha
                   || tweenType == XTweenTypes.颜色_Color;
        }

        public static bool IsSupported(XTween_Controller targetController)
        {
            return targetController != null && IsSupportedTweenType(targetController.TweenTypes);
        }

        public XTween_Interface CreateTween(bool regenerateIfExists, bool andPlay = true)
        {
            if (controller == null || !IsSupported(controller))
            {
                return null;
            }

            if (regenerateIfExists && controller.CurrentTweener != null)
            {
                controller.CurrentTweener.Kill();
                controller.CurrentTweener = null;
            }

            if (controller.CurrentTweener == null)
            {
                controller.Tween_Create();
            }

            if (andPlay)
            {
                controller.Tween_Play();
            }

            return controller.CurrentTweener;
        }

        public float Delay
        {
            get => controller != null ? controller.Delay : 0f;
            set
            {
                if (controller != null)
                {
                    controller.Delay = value;
                }
            }
        }

        public float Duration => controller != null ? Mathf.Max(0f, controller.Duration) : 0f;
        public int Loops => controller != null ? controller.LoopCount : 0;
        public bool IsValid => IsSupported(controller);
        public bool IsActive => controller != null && controller.isActiveAndEnabled && IsValid;
        public bool IsFrom => controller != null && controller.IsFromMode;
        public string ValidationMessage
        {
            get
            {
                if (controller == null)
                {
                    return "Controller is missing.";
                }

                if (controller.TweenTypes == XTweenTypes.无_None)
                {
                    return "Tween type is None and cannot be used by timeline.";
                }

                if (IsSupported(controller))
                {
                    return null;
                }

                return $"XTweenTimeline v1 only supports Position/Scale/Rotation/Alpha/Color. Current: {controller.TweenTypes}.";
            }
        }

        public bool AllowEditorCallbacks => false;
        public bool CallbackView => false;
        public Texture2D CustomIcon => null;

        public string Label
        {
            get
            {
                if (controller == null)
                {
                    return "Invalid Controller";
                }

                var infiniteSuffix = Loops == -1 ? "∞" : string.Empty;
                var invalidSuffix = IsValid ? string.Empty : " [Unsupported]";
                return $"{controller.TweenTypes}.{controller.name} {infiniteSuffix}{invalidSuffix}".Trim();
            }
        }

        public Component Component => controller;

        public XTween_Interface CreateEditorPreview()
        {
            return CreateTween(regenerateIfExists: true, andPlay: false);
        }

        public IEnumerable<Object> Targets
        {
            get
            {
                if (controller == null) return Enumerable.Empty<Object>();
                var targets = new List<Object>();

                if (controller.Target_RectTransform != null) targets.Add(controller.Target_RectTransform);
                if (controller.Target_Image != null) targets.Add(controller.Target_Image);
                if (controller.Target_CanvasGroup != null) targets.Add(controller.Target_CanvasGroup);
                if (controller.Target_Text != null) targets.Add(controller.Target_Text);
                if (controller.Target_TmpText != null) targets.Add(controller.Target_TmpText);
                if (controller.Target_PathTool != null) targets.Add(controller.Target_PathTool);
                targets.Add(controller.gameObject);

                return targets.Where(t => t != null).Distinct().ToArray();
            }
        }

        public override bool Equals(object obj)
        {
            return obj is XTweenTimelineControllerAdapter other && controller == other.controller;
        }

        public override int GetHashCode()
        {
            return controller != null ? controller.GetHashCode() : 0;
        }
    }
}
