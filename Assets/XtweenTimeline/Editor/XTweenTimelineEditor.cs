namespace SevenStrikeModules.XTween.Timeline.Editor
{
    using System;
    using System.Linq;
    using SevenStrikeModules.XTween;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;

    [CustomEditor(typeof(XTweenTimeline))]
    public class XTweenTimelineEditor : UnityEditor.Editor
    {
        private XTweenTimeline Timeline => (XTweenTimeline)target;

        private XTweenTimelineController controller;
        private XTweenTimelineSelection selection;
        private XTweenTimelineView view;
        private float? dragTweenTimeShift;
        private IXTweenTimelineAnimation[] animations;

        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        public override void OnInspectorGUI()
        {
            animations = Timeline.GetComponents<MonoBehaviour>()
                .Select(XTweenTimelineAnimation.FromComponent)
                .Where(animation => animation != null)
                .ToArray();
            selection.Validate(animations);

            view.DrawTimeline(
                animations,
                selection.Animation,
                controller.IsPlaying,
                controller.ElapsedTime,
                controller.Loop,
                controller.Paused);

            DrawValidationHints();

            if (selection.Animation != null)
            {
                view.DrawInspector(selection.GetAnimationEditor());
            }

            if (controller.Paused && Event.current.type == EventType.Repaint)
            {
                controller.GoTo(animations, controller.ElapsedTime);
            }

            if (controller.IsPlaying || view.IsTimeDragging || view.IsTweenDragging)
            {
                Repaint();
            }
        }

        private void DrawValidationHints()
        {
            var invalidAnimations = animations.Where(animation => !animation.IsValid).ToArray();
            if (invalidAnimations.Length == 0)
            {
                return;
            }

            var first = invalidAnimations[0];
            var message = string.IsNullOrWhiteSpace(first.ValidationMessage)
                ? "Some tracks are currently not supported by XTweenTimeline v1."
                : first.ValidationMessage;

            EditorGUILayout.HelpBox(message, MessageType.Warning);
        }

        private void OnEnable()
        {
            controller = new XTweenTimelineController();
            selection = new XTweenTimelineSelection();
            view = new XTweenTimelineView();

            view.IsSnapping = EditorPrefs.GetBool("XTweenTimeline.Snap", true);

            view.TweenSelected += OnTweenSelected;
            view.TweenDrag += DragSelectedAnimation;

            view.TimeDragEnd += OnTimeDragEnd;
            view.TimeDrag += GoTo;
            view.PreviewDisabled += controller.Stop;

            view.AddClicked += AddAnimation;
            view.AddMore += AddMore;
            view.RemoveClicked += Remove;
            view.DuplicateClicked += Duplicate;

            view.PlayClicked += Play;
            view.StopClicked += controller.Stop;
            view.LoopToggled += ToggleLoop;
            view.SnapToggled += ToggleSnap;

            view.InspectorUpButtonClicked += MoveSelectedUp;
            view.InspectorDownButtonClicked += MoveSelectedDown;

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            view.TweenSelected -= OnTweenSelected;
            view.TweenDrag -= DragSelectedAnimation;

            view.TimeDragEnd -= OnTimeDragEnd;
            view.TimeDrag -= GoTo;
            view.PreviewDisabled -= controller.Stop;

            view.AddClicked -= AddAnimation;
            view.AddMore -= AddMore;
            view.RemoveClicked -= Remove;
            view.DuplicateClicked -= Duplicate;

            view.PlayClicked -= Play;
            view.StopClicked -= controller.Stop;
            view.LoopToggled -= ToggleLoop;
            view.SnapToggled -= ToggleSnap;

            view.InspectorUpButtonClicked -= MoveSelectedUp;
            view.InspectorDownButtonClicked -= MoveSelectedDown;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            controller.Dispose();
            controller = null;

            selection.Dispose();
            selection = null;

            view = null;
            animations = null;
        }

        private void Play()
        {
            controller.Play(animations);
        }

        private void GoTo(float time)
        {
            controller.GoTo(animations, time);
        }

        private void OnTimeDragEnd(Event mouseEvent)
        {
            const int mouseButtonMiddle = 2;
            if (mouseEvent.IsRightMouseButton() || mouseEvent.button == mouseButtonMiddle)
            {
                controller.Stop();
                return;
            }

            controller.Pause();
        }

        private void DragSelectedAnimation(float time)
        {
            if (selection.Animation == null || selection.Animation.Component == null)
            {
                return;
            }

            Undo.RecordObject(selection.Animation.Component, $"Drag {selection.Animation.Label}");
            dragTweenTimeShift ??= time - selection.Animation.Delay;

            var delay = time - dragTweenTimeShift.Value;
            delay = Mathf.Max(0f, delay);
            delay = TrySnapTime(selection.Animation, delay, view.TimeScale);
            delay = (float)Math.Round(delay, 2);
            selection.Animation.Delay = delay;

            Undo.FlushUndoRecordObjects();
        }

        private float TrySnapTime(IXTweenTimelineAnimation target, float newDelay, float timeScale)
        {
            if (!IsSnapActive() || animations == null || animations.Length < 2)
            {
                return newDelay;
            }

            var snapThreshold = 1f / 40f / Mathf.Max(0.0001f, timeScale);
            var snapPoints = animations
                .Where(animation => animation.Component != target.Component)
                .SelectMany(animation => Enumerable.Empty<float>()
                    .Append(animation.Delay)
                    .Append(animation.Delay + animation.Duration * Mathf.Max(1, animation.Loops)))
                .Distinct()
                .ToArray();
            if (snapPoints.Length == 0)
            {
                return newDelay;
            }

            var snapTime = snapPoints.OrderBy(snapPoint => Mathf.Abs(snapPoint - newDelay)).First();
            if (Math.Abs(snapTime - newDelay) < snapThreshold)
            {
                return snapTime;
            }

            if (target.Loops == -1)
            {
                return newDelay;
            }

            var targetFullDuration = target.Duration * Mathf.Max(1, target.Loops);
            var newEndTime = newDelay + targetFullDuration;
            var snapEndTime = snapPoints.OrderBy(snapPoint => Mathf.Abs(snapPoint - newEndTime)).First();
            if (Math.Abs(snapEndTime - newEndTime) < snapThreshold)
            {
                return snapEndTime - targetFullDuration;
            }

            return newDelay;
        }

        private bool IsSnapActive()
        {
            var reverseSnap = Event.current.control;
            var snapEnabled = view.IsSnapping;
            return reverseSnap ? !snapEnabled : snapEnabled;
        }

        private void OnTweenSelected(IXTweenTimelineAnimation animation)
        {
            selection.Set(animation);
            GUIUtility.keyboardControl = 0;
            dragTweenTimeShift = null;
        }

        private void AddAnimation()
        {
            Add(Timeline, typeof(XTween_Controller));
        }

        private void AddMore(Type type)
        {
            Add(Timeline, type);
        }

        private void Add(XTweenTimeline timeline, Type type)
        {
            if (type == null || !typeof(Component).IsAssignableFrom(type))
            {
                return;
            }

            var component = ObjectFactory.AddComponent(timeline.gameObject, type) as Component;
            if (component == null)
            {
                return;
            }

            var animation = XTweenTimelineAnimation.FromComponent(component);
            if (animation != null && controller.Paused)
            {
                animation.Delay = (float)Math.Round(controller.ElapsedTime, 2);
            }

            selection.Set(animation);
        }

        private void Remove()
        {
            if (selection.Animation?.Component == null)
            {
                return;
            }

            Undo.DestroyObjectImmediate(selection.Animation.Component);
            selection.Clear();
        }

        private void Duplicate()
        {
            if (selection.Animation?.Component == null)
            {
                return;
            }

            Undo.SetCurrentGroupName($"Duplicate {selection.Animation.Label}");

            var source = selection.Animation.Component;
            var destination = Undo.AddComponent(source.gameObject, source.GetType());
            EditorUtility.CopySerialized(source, destination);

            var animation = XTweenTimelineAnimation.FromComponent(destination);
            selection.Set(animation);

            var components = source.GetComponents<Component>();
            var targetIndex = Array.IndexOf(components, source) + 1;
            var index = Array.IndexOf(components, destination);
            while (index > targetIndex)
            {
                ComponentUtility.MoveComponentUp(destination);
                index--;
            }
        }

        private void ToggleLoop(bool value)
        {
            controller.Loop = value;
        }

        private void ToggleSnap()
        {
            EditorPrefs.SetBool("XTweenTimeline.Snap", view.IsSnapping);
        }

        private void MoveSelectedUp()
        {
            if (selection.Animation?.Component == null)
            {
                return;
            }

            var index = animations.FindIndex(animation => animation.Component == selection.Animation.Component);
            if (index > 0)
            {
                ComponentUtility.MoveComponentUp(selection.Animation.Component);
            }
        }

        private void MoveSelectedDown()
        {
            if (selection.Animation?.Component == null)
            {
                return;
            }

            var index = animations.FindIndex(animation => animation.Component == selection.Animation.Component);
            if (index < animations.Length - 1)
            {
                ComponentUtility.MoveComponentDown(selection.Animation.Component);
            }
        }

        private void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange == PlayModeStateChange.ExitingEditMode)
            {
                controller.Stop();
            }
        }
    }
}
