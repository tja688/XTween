namespace SevenStrikeModules.XTween.Timeline.Editor
{
    using UnityEditor;

    [CustomEditor(typeof(XTweenTimelineLink))]
    public class XTweenTimelineLinkEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var link = (XTweenTimelineLink)target;
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            Undo.RecordObject(link, link.name);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(XTweenTimelineLink.id)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(XTweenTimelineLink.timeline)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(XTweenTimelineLink.delay)));

            serializedObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}
