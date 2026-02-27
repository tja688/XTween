namespace SevenStrikeModules.XTween.Timeline
{
    using UnityEngine;

    [AddComponentMenu("XTween/XTween Timeline Player")]
    [RequireComponent(typeof(XTweenTimeline))]
    public class XTweenTimelinePlayer : MonoBehaviour
    {
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private int loops = 1;

        private XTweenTimeline timeline;

        private void Awake()
        {
            timeline = GetComponent<XTweenTimeline>();
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                timeline.Play().SetLoops(loops);
            }
        }
    }
}
