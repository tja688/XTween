namespace SevenStrikeModules.XTween.Timeline.Editor
{
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    public static class XTweenTimelineSmokeTools
    {
        [MenuItem("XTweenTimeline/Rebuild Smoke In Scene")]
        private static void RebuildSmokeInScene()
        {
            var smoke = Object.FindObjectsByType<XTweenTimelineRuntimeSmoke>(FindObjectsSortMode.None).FirstOrDefault();
            if (smoke == null)
            {
                Debug.LogWarning("[XTweenTimeline] No XTweenTimelineRuntimeSmoke component found in the current scene.");
                return;
            }

            Undo.RecordObject(smoke.gameObject, "Rebuild XTweenTimeline Runtime Smoke");
            smoke.Rebuild();
            EditorUtility.SetDirty(smoke.gameObject);
            if (smoke.transform.parent != null)
            {
                EditorUtility.SetDirty(smoke.transform.parent.gameObject);
            }

            Debug.Log("[XTweenTimeline] Runtime smoke rebuilt.");
        }
    }
}
