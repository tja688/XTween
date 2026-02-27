namespace SevenStrikeModules.XTween.Timeline
{
    using SevenStrikeModules.XTween;
    using UnityEngine;
    using UnityEngine.Events;

    [AddComponentMenu("XTween/XTween Timeline Callback")]
    public partial class XTweenTimelineCallback : MonoBehaviour
    {
        private const float CallbackTweenDuration = 0.0001f;

        [SerializeField] public string id;
        [Min(0f)] [SerializeField] public float delay;
        [SerializeField] public UnityEvent onCallback = new UnityEvent();

        private XTween_Interface tween;

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

            tween = XTween.To(() => 0f, _ => { }, 1f, CallbackTweenDuration, autokill: false)
                .SetDelay(delay)
                .OnComplete(_ => onCallback?.Invoke());

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
    }
}
