using System.Linq;
using UnityEngine;

namespace SevenStrikeModules.XTween.Timeline.Editor
{
    public class XTweenTimelineSelection
    {
        private static IXTweenTimelineAnimation animation;
        private UnityEditor.Editor editor;

        public IXTweenTimelineAnimation Animation => animation;

        public void Validate(IXTweenTimelineAnimation[] animations)
        {
            if (animation != null && !animations.Contains(animation))
            {
                Clear();
            }
        }

        public void Set(IXTweenTimelineAnimation animation)
        {
            XTweenTimelineSelection.animation = animation;
        }

        public void Clear() => Set(null);

        public UnityEditor.Editor GetAnimationEditor()
        {
            if (editor != null && editor.target != animation.Component)
            {
                DisposeEditor();
            }

            if (animation == null)
            {
                return null;
            }

            if (editor == null)
            {
                editor = UnityEditor.Editor.CreateEditor(animation.Component);
            }

            return editor;
        }

        public void Dispose()
        {
            if (editor != null)
            {
                DisposeEditor();
            }
        }

        private void DisposeEditor()
        {
            Object.DestroyImmediate(editor);
            editor = null;
        }
    }
}
