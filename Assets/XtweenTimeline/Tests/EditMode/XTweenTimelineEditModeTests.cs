namespace SevenStrikeModules.XTween.Timeline.Tests
{
    using NUnit.Framework;
    using SevenStrikeModules.XTween;
    using UnityEngine;

    public class XTweenTimelineEditModeTests
    {
        [Test]
        public void ControllerAdapter_UnsupportedType_IsReported()
        {
            var go = new GameObject("ControllerAdapterTest");
            var controller = go.AddComponent<XTween_Controller>();
            controller.TweenTypes = XTweenTypes.填充_Fill;

            var adapter = new XTweenTimelineControllerAdapter(controller);
            Assert.IsFalse(adapter.IsValid);
            StringAssert.Contains("Position/Scale/Rotation/Alpha/Color", adapter.ValidationMessage);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void AnimationAdapter_ForSameController_IsComparable()
        {
            var go = new GameObject("ControllerAdapterEqualsTest");
            var controller = go.AddComponent<XTween_Controller>();
            controller.TweenTypes = XTweenTypes.位置_Position;

            var first = XTweenTimelineAnimationAdapter.FromComponent(controller);
            var second = XTweenTimelineAnimationAdapter.FromComponent(controller);

            Assert.AreEqual(first, second);
            Assert.AreEqual(first.GetHashCode(), second.GetHashCode());

            Object.DestroyImmediate(go);
        }

        [Test]
        public void CompatSeek_SuppressedCallback_DoesNotInvokeComplete()
        {
            float value = 0f;
            var completed = 0;

            var tween = XTween.To(() => value, v => value = v, 1f, 0.1f, autokill: false)
                .OnComplete(_ => completed++);

            XTweenTimelineCompat.SeekTweenInEditor(tween, 1f, suppressCallbacks: true);
            Assert.AreEqual(0, completed);

            XTweenTimelineCompat.SeekTweenInEditor(tween, 1f, suppressCallbacks: false);
            Assert.GreaterOrEqual(completed, 1);

            tween.Kill();
        }
    }
}
