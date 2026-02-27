namespace SevenStrikeModules.XTween.Timeline
{
    using System.Linq;
    using SevenStrikeModules.XTween;
    using UnityEngine;

    [AddComponentMenu("XTween/XTween Timeline Frame")]
    public partial class XTweenTimelineFrame : MonoBehaviour
    {
        private const float FrameTweenDuration = 0.0001f;

        [SerializeField] private string id;
        [Min(0f), SerializeField] private float delay;
        [SerializeField] private FrameProperty[] properties = new FrameProperty[1];

        private XTween_Interface tween;
        public FrameProperty[] Properties => properties;

        private void Awake()
        {
            EnsureProperties();
        }

        private void Reset()
        {
            EnsureProperties();
        }

        private void OnValidate()
        {
            EnsureProperties();
        }

        public XTween_Interface CreateTween(bool regenerateIfExists, bool andPlay = true)
        {
            if (tween != null)
            {
                if (tween.IsActive)
                {
                    if (!regenerateIfExists)
                    {
                        return tween;
                    }

                    tween.Kill();
                }

                tween = null;
            }

            var scopes = GenerateScopes();
            var isScopeOpen = false;
            tween = XTween.To(() => 0f, _ => { }, 1f, FrameTweenDuration, autokill: false)
                .SetDelay(delay)
                .OnComplete(_ =>
                {
                    if (isScopeOpen)
                    {
                        return;
                    }

                    isScopeOpen = true;
                    for (var i = 0; i < scopes.Length; i++)
                    {
                        scopes[i].Open();
                    }
                })
                .OnRewind(() =>
                {
                    if (!isScopeOpen)
                    {
                        return;
                    }

                    isScopeOpen = false;
                    for (var i = 0; i < scopes.Length; i++)
                    {
                        scopes[i].Close();
                    }
                });

            if (andPlay)
            {
                tween.Play();
            }
            else
            {
                tween.Pause();
            }

            return tween;
        }

        private PropertyScope[] GenerateScopes()
        {
            return properties.Select(CreateScope).Where(scope => scope != null).ToArray();
        }

        private void EnsureProperties()
        {
            if (properties == null || properties.Length == 0)
            {
                properties = new FrameProperty[1];
            }

            for (var i = 0; i < properties.Length; i++)
            {
                if (properties[i] == null)
                {
                    properties[i] = new FrameProperty();
                }
            }
        }
    }
}
