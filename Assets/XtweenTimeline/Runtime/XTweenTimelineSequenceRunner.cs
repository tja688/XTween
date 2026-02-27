namespace SevenStrikeModules.XTween.Timeline
{
    using System.Collections.Generic;
    using UnityEngine;

    [DefaultExecutionOrder(-9999)]
    public sealed class XTweenTimelineSequenceRunner : MonoBehaviour
    {
        private static XTweenTimelineSequenceRunner instance;
        private readonly HashSet<XTweenTimelineSequence> active = new HashSet<XTweenTimelineSequence>();

        internal static void Register(XTweenTimelineSequence sequence)
        {
            EnsureInstance();
            if (instance != null && sequence != null)
            {
                instance.active.Add(sequence);
            }
        }

        internal static void Unregister(XTweenTimelineSequence sequence)
        {
            if (instance == null || sequence == null) return;
            instance.active.Remove(sequence);
        }

        private static void EnsureInstance()
        {
            if (instance != null) return;
            var host = new GameObject("~XTweenTimelineRunner");
            host.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(host);
            instance = host.AddComponent<XTweenTimelineSequenceRunner>();
        }

        private void Update()
        {
            if (active.Count == 0) return;

            var snapshot = ListPool<XTweenTimelineSequence>.Get();
            snapshot.AddRange(active);
            var now = Time.timeAsDouble;

            for (var i = 0; i < snapshot.Count; i++)
            {
                var sequence = snapshot[i];
                if (sequence == null || sequence.IsKilled)
                {
                    active.Remove(sequence);
                    continue;
                }

                sequence.InternalUpdate(now);
            }

            ListPool<XTweenTimelineSequence>.Release(snapshot);
        }

        private static class ListPool<T>
        {
            private static readonly Stack<List<T>> Pool = new Stack<List<T>>();

            public static List<T> Get()
            {
                return Pool.Count > 0 ? Pool.Pop() : new List<T>();
            }

            public static void Release(List<T> list)
            {
                list.Clear();
                Pool.Push(list);
            }
        }
    }
}
