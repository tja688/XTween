namespace SevenStrikeModules.XTween.Timeline
{
    using System;
    using System.Collections.Generic;
    using SevenStrikeModules.XTween;
    using UnityEngine;

    public sealed class XTweenTimelineSequence
    {
        private readonly List<TweenEntry> tweenEntries = new List<TweenEntry>();
        private readonly List<SequenceEntry> sequenceEntries = new List<SequenceEntry>();
        private readonly List<CallbackEntry> callbackEntries = new List<CallbackEntry>();
        private readonly HashSet<int> firedCallbackIds = new HashSet<int>();
        private int nextCallbackId;

        private float delay;
        private bool prependDelay = true;
        private int loops = 1;
        private int currentLoop;
        private double loopStartTime;
        private double pausedAt;

        public float Duration { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsKilled { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool IsActive => !IsKilled;
        public int LoopCount => loops;
        public int CurrentLoop => currentLoop;
        public float Delay => delay;

        private Action<float> onComplete;
        private Action onKill;
        private Action onRewind;

        public XTweenTimelineSequence Insert(float atTime, XTween_Interface tween)
        {
            if (tween == null) return this;
            tweenEntries.Add(new TweenEntry(Mathf.Max(0f, atTime), tween));
            RecalculateDuration();
            return this;
        }

        public XTweenTimelineSequence Insert(float atTime, XTweenTimelineSequence sequence)
        {
            if (sequence == null) return this;
            sequenceEntries.Add(new SequenceEntry(Mathf.Max(0f, atTime), sequence));
            RecalculateDuration();
            return this;
        }

        public XTweenTimelineSequence InsertCallback(float atTime, Action callback)
        {
            if (callback == null) return this;
            callbackEntries.Add(new CallbackEntry(nextCallbackId++, Mathf.Max(0f, atTime), callback));
            RecalculateDuration();
            return this;
        }

        public XTweenTimelineSequence SetDelay(float value, bool asPrependedIntervalIfSequence = true)
        {
            delay = Mathf.Max(0f, value);
            prependDelay = asPrependedIntervalIfSequence;
            RecalculateDuration();
            return this;
        }

        public XTweenTimelineSequence SetLoops(int value)
        {
            if (value == -1)
            {
                loops = -1;
            }
            else
            {
                loops = Mathf.Max(1, value);
            }

            return this;
        }

        public XTweenTimelineSequence Play()
        {
            if (IsKilled) return this;

            if (IsPaused)
            {
                Resume();
                return this;
            }

            if (IsCompleted)
            {
                Restart();
                return this;
            }

            StartLoop(resetChildren: true);
            return this;
        }

        public XTweenTimelineSequence Pause()
        {
            if (!IsPlaying || IsKilled) return this;

            IsPlaying = false;
            IsPaused = true;
            pausedAt = Time.timeAsDouble;

            for (var i = 0; i < tweenEntries.Count; i++) tweenEntries[i].Pause();
            for (var i = 0; i < sequenceEntries.Count; i++) sequenceEntries[i].Pause();
            XTweenTimelineSequenceRunner.Unregister(this);
            return this;
        }

        public XTweenTimelineSequence Resume()
        {
            if (!IsPaused || IsKilled) return this;

            IsPaused = false;
            IsPlaying = true;
            loopStartTime += Time.timeAsDouble - pausedAt;

            for (var i = 0; i < tweenEntries.Count; i++) tweenEntries[i].Resume();
            for (var i = 0; i < sequenceEntries.Count; i++) sequenceEntries[i].Resume();
            XTweenTimelineSequenceRunner.Register(this);
            return this;
        }

        public XTweenTimelineSequence TogglePause()
        {
            if (IsPaused) return Resume();
            return Pause();
        }

        public XTweenTimelineSequence Rewind(bool andKill = false)
        {
            if (IsKilled && !andKill) return this;

            IsPlaying = false;
            IsPaused = false;
            IsCompleted = false;
            currentLoop = 0;
            firedCallbackIds.Clear();

            for (var i = 0; i < tweenEntries.Count; i++) tweenEntries[i].Rewind();
            for (var i = 0; i < sequenceEntries.Count; i++) sequenceEntries[i].Rewind();

            onRewind?.Invoke();
            XTweenTimelineSequenceRunner.Unregister(this);

            if (andKill) Kill();
            return this;
        }

        public XTweenTimelineSequence Restart()
        {
            Rewind();
            return Play();
        }

        public XTweenTimelineSequence Kill()
        {
            if (IsKilled) return this;

            IsKilled = true;
            IsPlaying = false;
            IsPaused = false;
            IsCompleted = true;

            for (var i = 0; i < tweenEntries.Count; i++) tweenEntries[i].Kill();
            for (var i = 0; i < sequenceEntries.Count; i++) sequenceEntries[i].Kill();

            XTweenTimelineSequenceRunner.Unregister(this);
            onKill?.Invoke();
            return this;
        }

        public XTweenTimelineSequence OnComplete(Action<float> callback)
        {
            onComplete += callback;
            return this;
        }

        public XTweenTimelineSequence OnKill(Action callback)
        {
            onKill += callback;
            return this;
        }

        public XTweenTimelineSequence OnRewind(Action callback)
        {
            onRewind += callback;
            return this;
        }

        internal void InternalUpdate(double now)
        {
            if (!IsPlaying || IsKilled) return;

            var elapsed = (float)(now - loopStartTime);
            TriggerCallbacks(elapsed);

            var durationReached = elapsed >= Duration;
            var allChildrenCompleted = AreAllChildrenCompleted();
            if (durationReached || (allChildrenCompleted && !HasPendingCallbacks(elapsed)))
            {
                CompleteOrLoop();
            }
        }

        private void StartLoop(bool resetChildren)
        {
            IsKilled = false;
            IsCompleted = false;
            IsPaused = false;
            IsPlaying = true;
            loopStartTime = Time.timeAsDouble;
            firedCallbackIds.Clear();

            if (resetChildren)
            {
                for (var i = 0; i < tweenEntries.Count; i++) tweenEntries[i].Rewind();
                for (var i = 0; i < sequenceEntries.Count; i++) sequenceEntries[i].Rewind();
            }

            var prepended = prependDelay ? delay : 0f;
            for (var i = 0; i < tweenEntries.Count; i++) tweenEntries[i].Play(prepended);
            for (var i = 0; i < sequenceEntries.Count; i++) sequenceEntries[i].Play(prepended);

            XTweenTimelineSequenceRunner.Register(this);
        }

        private void CompleteOrLoop()
        {
            if (loops == -1 || currentLoop + 1 < loops)
            {
                currentLoop++;
                StartLoop(resetChildren: true);
                return;
            }

            IsPlaying = false;
            IsPaused = false;
            IsCompleted = true;
            XTweenTimelineSequenceRunner.Unregister(this);
            onComplete?.Invoke(Duration);
        }

        private bool AreAllChildrenCompleted()
        {
            for (var i = 0; i < tweenEntries.Count; i++)
            {
                if (!tweenEntries[i].IsCompleted) return false;
            }

            for (var i = 0; i < sequenceEntries.Count; i++)
            {
                if (!sequenceEntries[i].IsCompleted) return false;
            }

            return true;
        }

        private bool HasPendingCallbacks(float elapsed)
        {
            if (callbackEntries.Count == 0)
            {
                return false;
            }

            var prepended = prependDelay ? delay : 0f;
            for (var i = 0; i < callbackEntries.Count; i++)
            {
                var callback = callbackEntries[i];
                if (firedCallbackIds.Contains(callback.Id))
                {
                    continue;
                }

                if (elapsed < prepended + callback.InsertTime)
                {
                    return true;
                }
            }

            return false;
        }

        private void TriggerCallbacks(float elapsed)
        {
            if (callbackEntries.Count == 0) return;

            var prepended = prependDelay ? delay : 0f;
            for (var i = 0; i < callbackEntries.Count; i++)
            {
                var callback = callbackEntries[i];
                if (firedCallbackIds.Contains(callback.Id)) continue;
                if (elapsed < prepended + callback.InsertTime) continue;

                firedCallbackIds.Add(callback.Id);
                callback.Callback.Invoke();
            }
        }

        private void RecalculateDuration()
        {
            var maxDuration = prependDelay ? delay : 0f;
            for (var i = 0; i < tweenEntries.Count; i++)
            {
                maxDuration = Mathf.Max(maxDuration, (prependDelay ? delay : 0f) + tweenEntries[i].FullDuration);
            }

            for (var i = 0; i < sequenceEntries.Count; i++)
            {
                maxDuration = Mathf.Max(maxDuration, (prependDelay ? delay : 0f) + sequenceEntries[i].FullDuration);
            }

            for (var i = 0; i < callbackEntries.Count; i++)
            {
                maxDuration = Mathf.Max(maxDuration, (prependDelay ? delay : 0f) + callbackEntries[i].InsertTime);
            }

            Duration = maxDuration;
        }

        private static float ComputeTweenDuration(XTween_Interface tween)
        {
            if (tween == null) return 0f;
            if (tween.LoopCount < 0) return float.PositiveInfinity;
            var full = tween.Duration;
            if (tween.LoopCount > 0)
            {
                full += tween.LoopCount * (tween.Duration + tween.LoopingDelay);
            }

            return full;
        }

        private readonly struct CallbackEntry
        {
            public readonly int Id;
            public readonly float InsertTime;
            public readonly Action Callback;

            public CallbackEntry(int id, float insertTime, Action callback)
            {
                Id = id;
                InsertTime = insertTime;
                Callback = callback;
            }
        }

        private readonly struct TweenEntry
        {
            private readonly float insertTime;
            private readonly XTween_Interface tween;
            private readonly float originalDelay;
            public float FullDuration { get; }
            public bool IsCompleted => tween == null || tween.IsCompleted || tween.IsKilled;

            public TweenEntry(float insertTime, XTween_Interface tween)
            {
                this.insertTime = insertTime;
                this.tween = tween;
                originalDelay = Mathf.Max(0f, tween != null ? tween.Delay : 0f);
                FullDuration = insertTime + originalDelay + ComputeTweenDuration(tween);
            }

            public void Play(float prependedDelay)
            {
                if (tween == null) return;
                var delay = prependedDelay + insertTime + originalDelay;
                XTweenTimelineCompat.PlayTweenWithDelay(tween, delay);
            }

            public void Pause()
            {
                tween?.Pause();
            }

            public void Resume()
            {
                tween?.Resume();
            }

            public void Rewind()
            {
                tween?.Rewind();
            }

            public void Kill()
            {
                tween?.Kill();
            }
        }

        private readonly struct SequenceEntry
        {
            private readonly float insertTime;
            private readonly XTweenTimelineSequence sequence;
            private readonly float originalDelay;
            public float FullDuration { get; }
            public bool IsCompleted => sequence == null || sequence.IsCompleted || sequence.IsKilled;

            public SequenceEntry(float insertTime, XTweenTimelineSequence sequence)
            {
                this.insertTime = insertTime;
                this.sequence = sequence;
                originalDelay = sequence != null ? sequence.Delay : 0f;
                FullDuration = insertTime + originalDelay + (sequence != null ? sequence.Duration : 0f);
            }

            public void Play(float prependedDelay)
            {
                if (sequence == null) return;
                sequence.SetDelay(prependedDelay + insertTime + originalDelay, true).Play();
            }

            public void Pause()
            {
                sequence?.Pause();
            }

            public void Resume()
            {
                sequence?.Resume();
            }

            public void Rewind()
            {
                sequence?.Rewind();
            }

            public void Kill()
            {
                sequence?.Kill();
            }
        }
    }
}
