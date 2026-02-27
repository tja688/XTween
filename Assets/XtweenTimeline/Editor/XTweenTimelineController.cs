using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SevenStrikeModules.XTween;
using UnityEditor;

namespace SevenStrikeModules.XTween.Timeline.Editor
{
    public class XTweenTimelineController : IDisposable
    {
        private double startTime;
        private IXTweenTimelineAnimation[] currentPlayAnimations;
        private readonly XTweenTimelineDrivenProperties drivenProperties;

        public bool IsPlaying => XTweenTimelineEditorPreviewContext.IsPlaying;
        public float ElapsedTime => (float)(XTweenTimelineEditorPreviewContext.CurrentTime - startTime);
        public bool Paused { get; private set; }

        public bool Loop
        {
            get => EditorPrefs.GetBool("XTweenTimeline.Loop", false);
            set => EditorPrefs.SetBool("XTweenTimeline.Loop", value);
        }

        public XTweenTimelineController()
        {
            XTweenTimelineEditorPreviewContext.Completed += XTweenTimelineEditorPreviewContextOnCompleted;
            drivenProperties = new XTweenTimelineDrivenProperties();
        }

        public void Play(IXTweenTimelineAnimation[] animations)
        {
            currentPlayAnimations = animations;

            var shift = (float)XTweenTimelineEditorPreviewContext.CurrentTime;
            GoTo(animations, shift);
            XTweenTimelineEditorPreviewContext.Start();
            startTime = XTweenTimelineEditorPreviewContext.CurrentTime - shift;
            Paused = false;
        }

        public void GoTo(IXTweenTimelineAnimation[] animations, in float time)
        {
            XTweenTimelineEditorPreviewContext.Stop();

            drivenProperties.Register(animations);
            Sort(animations).ForEach(PreviewTween);
            XTweenTimelineEditorPreviewContext.GoTo(time);
            startTime = 0;
        }

        public void Stop()
        {
            currentPlayAnimations = null;
            Paused = false;
            XTweenTimelineEditorPreviewContext.Stop();
            drivenProperties.Unregister();
        }

        public void Pause()
        {
            Paused = true;
        }

        private static XTween_Interface PreviewTween(IXTweenTimelineAnimation animation)
        {
            if (!animation.IsValid || !animation.IsActive) { return null; }

            var tween = animation.CreateEditorPreview();
            if (tween == null) { return null; }

            XTweenTimelineEditorPreviewContext.Add(tween, animation.IsFrom, animation.AllowEditorCallbacks);
            return tween;
        }

        private static IEnumerable<IXTweenTimelineAnimation> Sort(IXTweenTimelineAnimation[] animations)
        {
            return animations.OrderBy(animation => animation.Delay);
        }

        private void XTweenTimelineEditorPreviewContextOnCompleted()
        {
            // Hotfix to prevent exception when two Inspector tabs with a Timeline component are open
            if (currentPlayAnimations == null) { return; }

            if (!Loop)
            {
                Stop();
                return;
            }

            XTweenTimelineEditorPreviewContext.Stop();
            Play(currentPlayAnimations);
        }

        public void Dispose()
        {
            Stop();
            drivenProperties.Dispose();
            XTweenTimelineEditorPreviewContext.Completed -= XTweenTimelineEditorPreviewContextOnCompleted;
        }
    }
}
