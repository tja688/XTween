namespace SevenStrikeModules.XTween.Timeline.Tests
{
    using NUnit.Framework;
    using UnityEngine;

    public class XTweenTimelineLinkPlayModeTests
    {
        [Test]
        public void Link_Duration_AggregatesLinkedChildren()
        {
            var root = new GameObject("TimelineLinkTestRoot");
            var linked = new GameObject("LinkedTimeline");
            linked.transform.SetParent(root.transform);

            var linkedTimeline = linked.AddComponent<XTweenTimeline>();
            var callback = linked.AddComponent<XTweenTimelineCallback>();
            callback.delay = 0.35f;

            var holder = new GameObject("LinkHolder");
            holder.transform.SetParent(root.transform);
            var link = holder.AddComponent<XTweenTimelineLink>();
            link.timeline = linkedTimeline;
            link.delay = 0.1f;

            var animation = (IXTweenTimelineAnimation)link;
            Assert.That(animation.Duration, Is.GreaterThanOrEqualTo(0.35f));

            Object.DestroyImmediate(root);
        }
    }
}
