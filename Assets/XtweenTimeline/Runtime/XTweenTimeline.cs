namespace SevenStrikeModules.XTween.Timeline
{
    using UnityEngine;

    [AddComponentMenu("XTween/XTween Timeline")]
    public class XTweenTimeline : MonoBehaviour
    {
        public XTweenTimelineSequence Sequence { get; private set; }

        public XTweenTimelineSequence Play()
        {
            TryGenerateSequence();
            return Sequence.Play();
        }

        public void DOPlay()
        {
            Play();
        }

        public XTweenTimelineSequence Restart()
        {
            TryGenerateSequence();
            Sequence.Restart();
            return Sequence;
        }

        internal XTweenTimelineSequence GetOrCreateSequence()
        {
            TryGenerateSequence();
            return Sequence;
        }

        private void TryGenerateSequence()
        {
            if (Sequence != null) return;

            Sequence = new XTweenTimelineSequence();
            Sequence.OnKill(() => Sequence = null);

            var components = GetComponents<MonoBehaviour>();
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null || component == this) continue;

                if (component is XTweenTimelineLink link)
                {
                    if (!link.IsActive || !link.IsValid) continue;
                    var linked = link.CreateLinkedSequence(regenerateIfExists: true);
                    if (linked != null)
                    {
                        Sequence.Insert(0f, linked.SetDelay(link.delay, true));
                    }

                    continue;
                }

                var animation = XTweenTimelineAnimationAdapter.FromComponent(component);
                if (animation == null || !animation.IsValid || !animation.IsActive) continue;

                var tween = animation.CreateTween(regenerateIfExists: true, andPlay: false);
                if (tween != null)
                {
                    Sequence.Insert(0f, tween);
                }
            }
        }

        private void OnDestroy()
        {
            Sequence?.Kill();
        }
    }
}
