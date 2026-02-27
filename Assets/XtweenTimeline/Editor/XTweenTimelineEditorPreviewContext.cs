namespace SevenStrikeModules.XTween.Timeline.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SevenStrikeModules.XTween;
    using UnityEditor;
    using UnityEngine;

    public static class XTweenTimelineEditorPreviewContext
    {
        private readonly struct TweenData
        {
            public readonly XTween_Interface Tween;
            public readonly bool IsFrom;
            public readonly bool AllowCallbacks;

            public TweenData(XTween_Interface tween, bool isFrom, bool allowCallbacks)
            {
                Tween = tween;
                IsFrom = isFrom;
                AllowCallbacks = allowCallbacks;
            }
        }

        private static readonly List<TweenData> Tweens = new List<TweenData>();

        public static bool IsPlaying { get; private set; }
        public static double CurrentTime { get; private set; }
        public static event Action Completed;

        public static void Start()
        {
            if (IsPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            IsPlaying = true;
            CurrentTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += Update;
        }

        public static void Stop()
        {
            IsPlaying = false;
            EditorApplication.update -= Update;
            CurrentTime = 0;

            for (var i = Tweens.Count - 1; i >= 0; i--)
            {
                var tweenData = Tweens[i];
                var tween = tweenData.Tween;
                if (tween == null) continue;

                if (tweenData.IsFrom)
                {
                    tween.Rewind();
                }
                else
                {
                    tween.Rewind();
                }

                tween.Kill();
            }

            Tweens.Clear();
            QueuePlayerLoopUpdate();
        }

        public static void GoTo(float time)
        {
            CurrentTime = Mathf.Max(0f, time);
            for (var i = 0; i < Tweens.Count; i++)
            {
                var tweenData = Tweens[i];
                if (tweenData.Tween == null) continue;
                XTweenTimelineCompat.SeekTweenInEditor(tweenData.Tween, (float)CurrentTime, !tweenData.AllowCallbacks);
            }

            QueuePlayerLoopUpdate();
        }

        public static void Add(XTween_Interface tween, bool isFrom, bool allowCallbacks)
        {
            if (tween == null)
            {
                return;
            }

            Tweens.Add(new TweenData(tween, isFrom, allowCallbacks));
            tween.SetAutoKill(false);
            tween.Play();
        }

        public static void QueuePlayerLoopUpdate()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }

        private static void Update()
        {
            var prevTime = CurrentTime;
            CurrentTime = EditorApplication.timeSinceStartup;
            var _ = CurrentTime - prevTime;

            var activeTweens = false;
            for (var i = 0; i < Tweens.Count; i++)
            {
                var tween = Tweens[i].Tween;
                if (tween == null || tween.IsKilled || !tween.IsPlaying || tween.IsPaused) continue;

                tween.Update((float)EditorApplication.timeSinceStartup);
                activeTweens |= tween.IsPlaying;
            }

            QueuePlayerLoopUpdate();
            if (!activeTweens)
            {
                Completed?.Invoke();
            }
        }
    }
}
