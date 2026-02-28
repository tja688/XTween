namespace SevenStrikeModules.XTween.Timeline
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using SevenStrikeModules.XTween;
    using UnityEngine;

    public static class XTweenTimelineCompat
    {
        public static void PlayTweenWithDelay(XTween_Interface tween, float delay)
        {
            if (tween == null) return;

            tween.Rewind();
            tween.SetDelay(Mathf.Max(0f, delay));
            tween.Play();
            ForceDelayState(tween, Mathf.Max(0f, delay));
        }

        public static void ForceDelayState(XTween_Interface tween, float delay)
        {
            if (tween == null) return;

            SetFieldValue(tween, "_hasStarted", false);
            SetFieldValue(tween, "_ElapsedTime", 0f);
            SetFieldValue(tween, "_CurrentLinearProgress", 0f);
            SetFieldValue(tween, "_CurrentEasedProgress", 0f);
            SetFieldValue(tween, "_IsCompleted", false);
            SetFieldValue(tween, "_StartTime", Time.time + Mathf.Max(0f, delay));

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var editorType = Type.GetType("UnityEditor.EditorApplication, UnityEditor");
                if (editorType != null)
                {
                    var prop = editorType.GetProperty("timeSinceStartup", BindingFlags.Public | BindingFlags.Static);
                    if (prop != null)
                    {
                        var now = Convert.ToDouble(prop.GetValue(null, null));
                        SetFieldValue(tween, "_editorStartTime", now + Mathf.Max(0f, delay));
                    }
                }
            }
#endif
        }

#if UNITY_EDITOR
        public static void SeekTweenInEditor(XTween_Interface tween, float time, bool suppressCallbacks)
        {
            if (tween == null) return;

            var clampedTime = Mathf.Max(0f, time);
            if (suppressCallbacks)
            {
                using (new CallbackMuteScope(tween))
                {
                    SeekTweenInEditorInternal(tween, clampedTime, suppressCallbacks);
                }
            }
            else
            {
                SeekTweenInEditorInternal(tween, clampedTime, suppressCallbacks);
            }
        }

        private static void SeekTweenInEditorInternal(XTween_Interface tween, float time, bool suppressCallbacks)
        {
            var editorType = Type.GetType("UnityEditor.EditorApplication, UnityEditor");
            if (editorType == null) return;

            var prop = editorType.GetProperty("timeSinceStartup", BindingFlags.Public | BindingFlags.Static);
            if (prop == null) return;

            var now = Convert.ToDouble(prop.GetValue(null, null));

            tween.Rewind();
            tween.Play();

            var delay = Mathf.Max(0f, tween.Delay);
            if (time <= delay)
            {
                SetFieldValue(tween, "_hasStarted", false);
                SetFieldValue(tween, "_ElapsedTime", 0f);
                SetFieldValue(tween, "_CurrentLinearProgress", 0f);
                var remain = delay - time;
                SetFieldValue(tween, "_editorStartTime", now + remain);
                SetFieldValue(tween, "_StartTime", Time.time + remain);
            }
            else
            {
                var elapsed = time - delay;
                SetFieldValue(tween, "_hasStarted", true);
                SetFieldValue(tween, "_editorStartTime", now - elapsed);
                SetFieldValue(tween, "_StartTime", Time.time - elapsed);
                SetFieldValue(tween, "_ElapsedTime", elapsed);
            }

            var shouldContinue = tween.Update((float)now);
            if (time <= delay || !shouldContinue)
            {
                ForceInvokeUpdate(tween, invokeAllCallbacks: !suppressCallbacks);
            }
        }

        private static void ForceInvokeUpdate(XTween_Interface tween, bool invokeAllCallbacks)
        {
            if (tween == null) return;

            var type = tween.GetType();
            while (type != null)
            {
                var field = type.GetField("act_on_UpdateCallbacks", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    if (field.GetValue(tween) is Delegate callback)
                    {
                        var targetCallback = invokeAllCallbacks ? callback : GetFirstInvocation(callback);
                        targetCallback?.DynamicInvoke(tween.CurrentValue, tween.CurrentLinearProgress, tween.ElapsedTime);
                    }
                    return;
                }
                type = type.BaseType;
            }
        }

        private static Delegate GetFirstInvocation(Delegate callback)
        {
            if (callback == null) return null;
            var invocationList = callback.GetInvocationList();
            return invocationList.Length > 0 ? invocationList[0] : callback;
        }
#endif

        private static void SetFieldValue(object target, string fieldName, object value)
        {
            var field = FindField(target.GetType(), fieldName);
            if (field == null) return;
            field.SetValue(target, value);
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null) return field;
                type = type.BaseType;
            }

            return null;
        }

#if UNITY_EDITOR
        private sealed class CallbackMuteScope : IDisposable
        {
            private readonly object target;
            private readonly List<(FieldInfo field, object value)> backups = new List<(FieldInfo field, object value)>();
            private static readonly HashSet<string> KeepFirstDelegateFields = new HashSet<string>
            {
                "act_on_UpdateCallbacks",
                "act_on_RewindCallbacks"
            };

            public CallbackMuteScope(object target)
            {
                this.target = target;
                BackupAndMute(this.target.GetType());
            }

            private void BackupAndMute(Type type)
            {
                while (type != null)
                {
                    var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    for (var i = 0; i < fields.Length; i++)
                    {
                        var field = fields[i];
                        if (!typeof(Delegate).IsAssignableFrom(field.FieldType)) continue;
                        if (!(field.GetValue(target) is Delegate original)) continue;

                        if (KeepFirstDelegateFields.Contains(field.Name))
                        {
                            var first = GetFirstInvocation(original);
                            if (ReferenceEquals(first, original))
                            {
                                continue;
                            }

                            backups.Add((field, original));
                            field.SetValue(target, first);
                            continue;
                        }

                        backups.Add((field, original));
                        field.SetValue(target, null);
                    }

                    type = type.BaseType;
                }
            }

            public void Dispose()
            {
                for (var i = 0; i < backups.Count; i++)
                {
                    backups[i].field.SetValue(target, backups[i].value);
                }
            }
        }
#endif
    }
}
