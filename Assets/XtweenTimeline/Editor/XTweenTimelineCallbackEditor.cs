namespace SevenStrikeModules.XTween.Timeline.Editor
{
    using UnityEditor;

    [CustomEditor(typeof(XTweenTimelineCallback))]
    public class XTweenTimelineCallbackEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var callback = (XTweenTimelineCallback)target;

            serializedObject.Update();
            Undo.RecordObject(callback, "XTweenTimelineCallback");

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(XTweenTimelineCallback.id)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(XTweenTimelineCallback.delay)));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(XTweenTimelineCallback.onCallback)));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
