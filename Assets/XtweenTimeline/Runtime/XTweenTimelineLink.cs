namespace SevenStrikeModules.XTween.Timeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SevenStrikeModules.XTween;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [AddComponentMenu("XTween/XTween Timeline Link")]
    public class XTweenTimelineLink : MonoBehaviour, IXTweenTimelineAnimation
    {
        private const float ProxyTweenDuration = 0.0001f;

        [SerializeField] public string id;
        [SerializeField] public XTweenTimeline timeline;
        [Min(0f)] [SerializeField] public float delay;

        private XTween_Interface proxyTween;
        public bool IsValid => timeline != null;
        public bool IsActive => isActiveAndEnabled && (timeline == null || timeline.isActiveAndEnabled);

        public XTweenTimelineSequence CreateLinkedSequence(bool regenerateIfExists)
        {
            if (!timeline || !timeline.isActiveAndEnabled)
            {
                return null;
            }

            return timeline.GetOrCreateSequence();
        }

        XTween_Interface IXTweenTimelineAnimation.CreateTween(bool regenerateIfExists, bool andPlay)
        {
            if (!timeline || !timeline.isActiveAndEnabled)
            {
                return null;
            }

            if (proxyTween != null && proxyTween.IsActive)
            {
                if (!regenerateIfExists)
                {
                    return proxyTween;
                }

                proxyTween.Kill();
                proxyTween = null;
            }

            proxyTween = XTween.To(() => 0f, _ => { }, 1f, ProxyTweenDuration, autokill: false)
                .SetDelay(delay)
                .OnComplete(_ => timeline.Play());

            if (andPlay)
            {
                proxyTween.Play();
            }
            else
            {
                proxyTween.Pause();
            }

            return proxyTween;
        }

        XTween_Interface IXTweenTimelineAnimation.CreateEditorPreview()
        {
#if UNITY_EDITOR
            if (!timeline || !timeline.isActiveAndEnabled)
            {
                return null;
            }

            var previewEntries = Children(timeline)
                .Where(child => child != null && child.IsValid && child.IsActive)
                .Select(child => new PreviewEntry(child, child.CreateEditorPreview()))
                .Where(entry => entry.Tween != null)
                .ToArray();

            var previewDuration = Mathf.Max(
                ProxyTweenDuration,
                previewEntries.Aggregate(
                    0f,
                    (current, entry) =>
                        Mathf.Max(current, entry.Animation.Delay + entry.Animation.Duration * Mathf.Max(1, entry.Animation.Loops))));

            return XTween.To(() => 0f, _ => { }, 1f, previewDuration, autokill: false)
                .SetDelay(delay)
                .OnStart(() =>
                {
                    for (var i = 0; i < previewEntries.Length; i++)
                    {
                        var tween = previewEntries[i].Tween;
                        tween.Rewind();
                        tween.Play();
                        tween.Pause();
                    }
                })
                .OnUpdate<float>((_, _, elapsedTime) =>
                {
                    for (var i = 0; i < previewEntries.Length; i++)
                    {
                        var entry = previewEntries[i];
                        XTweenTimelineCompat.SeekTweenInEditor(entry.Tween, elapsedTime, !entry.Animation.AllowEditorCallbacks);
                    }
                })
                .OnRewind(() =>
                {
                    for (var i = 0; i < previewEntries.Length; i++)
                    {
                        previewEntries[i].Tween.Rewind();
                    }
                })
                .OnKill(() =>
                {
                    for (var i = 0; i < previewEntries.Length; i++)
                    {
                        previewEntries[i].Tween.Kill();
                    }
                });
#else
            return XTween.To(() => 0f, _ => { }, 1f, ProxyTweenDuration, autokill: false).SetDelay(delay);
#endif
        }

        float IXTweenTimelineAnimation.Delay
        {
            get => delay;
            set => delay = value;
        }

        float IXTweenTimelineAnimation.Duration => timeline
            ? Children(timeline)
                .Where(child => child != null && child.IsValid && child.IsActive)
                .Aggregate(0f, (current, child) => Mathf.Max(current, child.Delay + child.Duration * Mathf.Max(1, child.Loops)))
            : 1f;

        int IXTweenTimelineAnimation.Loops => 0;
        bool IXTweenTimelineAnimation.IsValid => timeline != null;
        bool IXTweenTimelineAnimation.IsActive => IsActive;
        bool IXTweenTimelineAnimation.IsFrom => false;
        Component IXTweenTimelineAnimation.Component => this;

        string IXTweenTimelineAnimation.Label
        {
            get
            {
                if (timeline == null)
                {
                    return "↪ None";
                }

                if (!string.IsNullOrEmpty(id))
                {
                    return $"↪ {id}";
                }

                return $"↪ {timeline.name}";
            }
        }

        IEnumerable<Object> IXTweenTimelineAnimation.Targets => timeline
            ? Children(timeline).SelectMany(child => child.Targets)
            : Enumerable.Empty<Object>();

        private static IEnumerable<IXTweenTimelineAnimation> Children(XTweenTimeline targetTimeline)
        {
            var components = targetTimeline.GetComponents<MonoBehaviour>();
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null || component == targetTimeline) continue;
                if (component is XTweenTimelineLink link && link.timeline == targetTimeline) continue;

                var child = XTweenTimelineAnimationAdapter.FromComponent(component);
                if (child != null)
                {
                    yield return child;
                }
            }
        }

        private readonly struct PreviewEntry
        {
            public readonly IXTweenTimelineAnimation Animation;
            public readonly XTween_Interface Tween;

            public PreviewEntry(IXTweenTimelineAnimation animation, XTween_Interface tween)
            {
                Animation = animation;
                Tween = tween;
            }
        }
    }
}
