namespace SevenStrikeModules.XTween.Timeline.Tests
{
    using NUnit.Framework;
    using SevenStrikeModules.XTween;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.TestTools;

    public class XTweenTimelineSequencePlayModeTests
    {
        [UnityTest]
        public IEnumerator Sequence_CallbackLoop_FiresExpectedCount()
        {
            var callbacks = 0;
            var sequence = new XTweenTimelineSequence()
                .InsertCallback(0.05f, () => callbacks++)
                .SetLoops(2)
                .Play();

            yield return new WaitForSeconds(0.2f);

            Assert.AreEqual(2, callbacks);
            Assert.IsTrue(sequence.IsCompleted);
            Assert.IsFalse(sequence.IsPlaying);
        }

        [UnityTest]
        public IEnumerator Sequence_PauseThenResume_PreservesTime()
        {
            var callbacks = 0;
            var sequence = new XTweenTimelineSequence()
                .InsertCallback(0.1f, () => callbacks++)
                .Play();

            yield return new WaitForSeconds(0.03f);
            sequence.Pause();
            var pausedCallbacks = callbacks;

            yield return new WaitForSeconds(0.12f);
            Assert.AreEqual(pausedCallbacks, callbacks);

            sequence.Resume();
            yield return new WaitForSeconds(0.12f);

            Assert.AreEqual(1, callbacks);
            Assert.IsTrue(sequence.IsCompleted);
        }

        [UnityTest]
        public IEnumerator Frame_Rewind_RestoresOriginalState()
        {
            EnsureManager();

            var go = new GameObject("FrameTestObject");
            var frame = go.AddComponent<XTweenTimelineFrame>();
            var property = frame.Properties[0];
            property.TargetGameObject = go;
            property.Target = go.transform;
            property.Property = XTweenTimelineFrame.FrameProperty.PropertyType.Scale;
            property.EndValueVector3 = Vector3.one * 2f;
            property.IsRelative = false;

            go.transform.localScale = Vector3.one;

            var tween = frame.CreateTween(regenerateIfExists: true, andPlay: true);
            yield return new WaitForSeconds(0.05f);

            Assert.That(go.transform.localScale.x, Is.EqualTo(2f).Within(0.01f));

            tween.Rewind();
            Assert.That(go.transform.localScale.x, Is.EqualTo(1f).Within(0.01f));

            Object.Destroy(go);
        }

        private static void EnsureManager()
        {
            if (Object.FindFirstObjectByType<XTween_Manager>() != null)
            {
                return;
            }

            var managerGo = new GameObject("XTween_Manager_PlayModeTests");
            managerGo.AddComponent<XTween_Manager>();
        }
    }
}
