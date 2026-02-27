namespace SevenStrikeModules.XTween.Timeline.Editor
{
    using System;
    using System.Reflection;
    using System.Linq;
    using SevenStrikeModules.XTween;
    using UnityEngine;

    public class XTweenTimelineView
    {
        private bool isTimeDragging;
        private bool isTweenDragging;
        private static readonly AddMoreItem[] AddMoreItems = CreateAddMoreItems();

        public float TimeScale { get; private set; }
        public bool IsTimeDragging => isTimeDragging;
        public bool IsTweenDragging => isTweenDragging;
        public bool IsSnapping { get; set; }

        public event Action<Event> TimeDragEnd;
        public event Action<float> TimeDrag;
        public event Action<IXTweenTimelineAnimation> TweenSelected;
        public event Action<float> TweenDrag;
        public event Action AddClicked;
        public event Action<Type> AddMore;
        public event Action RemoveClicked;
        public event Action DuplicateClicked;
        public event Action StopClicked;
        public event Action PlayClicked;
        public event Action<bool> LoopToggled;
        public event Action SnapToggled;
        public event Action PreviewDisabled;
        public event Action InspectorUpButtonClicked;
        public event Action InspectorDownButtonClicked;

        public void DrawTimeline(
            IXTweenTimelineAnimation[] animations,
            IXTweenTimelineAnimation selected,
            bool isPlaying,
            float currentPlayingTime,
            bool isLooping,
            bool isPaused)
        {
            var rect = XTweenTimelineGUI.GetTimelineControlRect(animations.Length);

            XTweenTimelineGUI.Background(rect);
            var headerRect = XTweenTimelineGUI.Header(rect);

            TimeScale = CalculateTimeScale(animations);
            var timeDragStarted = false;
            var timeRect = XTweenTimelineGUI.Time(rect, TimeScale, ref isTimeDragging, () => timeDragStarted = true, TimeDragEnd);
            var tweensRect = XTweenTimelineGUI.Tweens(rect, animations, TimeScale, selected, ref isTweenDragging, TweenSelected);

            if (XTweenTimelineGUI.AddButton(rect))
            {
                AddClicked?.Invoke();
            }

            XTweenTimelineGUI.AddMoreButton(rect, AddMoreItems, item => AddMore?.Invoke(item.Type));

            if (selected != null && XTweenTimelineGUI.RemoveButton(rect))
            {
                RemoveClicked?.Invoke();
            }

            if (selected != null && XTweenTimelineGUI.DuplicateButton(rect))
            {
                DuplicateClicked?.Invoke();
            }

            if (isPlaying || isPaused)
            {
                var scaledTime = currentPlayingTime * TimeScale;
                var verticalRect = timeRect.Add(tweensRect);
                XTweenTimelineGUI.TimeVerticalLine(verticalRect, scaledTime, isPaused);

                if (isPaused)
                {
                    XTweenTimelineGUI.PlayheadLabel(timeRect, scaledTime, currentPlayingTime);
                }
            }

            if (isTimeDragging)
            {
                var scaledTime = XTweenTimelineGUI.GetScaledTimeUnderMouse(timeRect);
                var rawTime = scaledTime / TimeScale;
                XTweenTimelineGUI.TimeVerticalLine(timeRect.Add(tweensRect), scaledTime, underLabel: true);
                XTweenTimelineGUI.PlayheadLabel(timeRect, scaledTime, rawTime);

                if (Event.current.type == EventType.MouseDrag || timeDragStarted)
                {
                    TimeDrag?.Invoke(rawTime);
                }
            }

            if (isTweenDragging && selected != null)
            {
                var scaledTime = XTweenTimelineGUI.GetScaledTimeUnderMouse(timeRect);
                if (Event.current.type == EventType.MouseDrag)
                {
                    var rawTime = scaledTime / TimeScale;
                    TweenDrag?.Invoke(rawTime);
                }
            }

            if (isPlaying)
            {
                if (XTweenTimelineGUI.StopButton(rect))
                {
                    StopClicked?.Invoke();
                }
            }
            else
            {
                if (XTweenTimelineGUI.PlayButton(rect))
                {
                    PlayClicked?.Invoke();
                }
            }

            var snapToggle = XTweenTimelineGUI.SnapToggle(rect, IsSnapping);
            if (snapToggle != IsSnapping)
            {
                IsSnapping = snapToggle;
                SnapToggled?.Invoke();
            }

            var loopResult = XTweenTimelineGUI.LoopToggle(rect, isLooping);
            if (loopResult != isLooping)
            {
                LoopToggled?.Invoke(loopResult);
            }

            if (XTweenTimelineGUI.PreviewEye(headerRect, isPlaying, isPaused, isTimeDragging))
            {
                PreviewDisabled?.Invoke();
            }

            if (Event.current.type == EventType.MouseDown)
            {
                var mousePosition = Event.current.mousePosition;
                if (selected != null && rect.Contains(mousePosition))
                {
                    TweenSelected?.Invoke(null);
                }
            }
        }

        public void DrawInspector(UnityEditor.Editor editor)
        {
            XTweenTimelineGUI.Inspector(editor, InspectorUpButtonClicked, InspectorDownButtonClicked);
        }

        private static float CalculateTimeScale(IXTweenTimelineAnimation[] animations)
        {
            var maxTime = animations.Length > 0
                ? animations.Max(animation => animation.Delay + animation.Duration * Mathf.Max(1, animation.Loops))
                : 1f;
            maxTime = Mathf.Max(0.0001f, maxTime);
            return 1f / maxTime;
        }

        private static AddMoreItem[] CreateAddMoreItems()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !assembly.IsDynamic)
                .SelectMany(GetLoadableTypes)
                .Where(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    typeof(MonoBehaviour).IsAssignableFrom(type) &&
                    (typeof(IXTweenTimelineAnimation).IsAssignableFrom(type) || type == typeof(XTween_Controller)))
                .Distinct()
                .Where(type => type != typeof(XTween_Controller))
                .OrderBy(type => type.Name)
                .ToArray();

            return types
                .Select(type => new AddMoreItem(new GUIContent($"Add {PrettyTypeName(type)}"), type))
                .Prepend(new AddMoreItem(new GUIContent("Add Tween"), typeof(XTween_Controller)))
                .ToArray();
        }

        private static Type[] GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetExportedTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null).ToArray();
            }
        }

        private static string PrettyTypeName(Type type)
        {
            return type.Name
                .Replace("XTweenTimeline", string.Empty)
                .Replace("XTween", string.Empty);
        }

        public readonly struct AddMoreItem
        {
            public readonly GUIContent Content;
            public readonly Type Type;

            public AddMoreItem(GUIContent content, Type type)
            {
                Content = content;
                Type = type;
            }
        }
    }
}
