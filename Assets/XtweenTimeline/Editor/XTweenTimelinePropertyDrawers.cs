namespace SevenStrikeModules.XTween.Timeline.Editor
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using SevenStrikeModules.XTween;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(XTween_Controller))]
    public class XTweenControllerPropertyDrawer : XTweenTimelineComponentPropertyDrawer<XTween_Controller>
    {
        private readonly Dictionary<XTween_Controller, IXTweenTimelineAnimation> adapters = new Dictionary<XTween_Controller, IXTweenTimelineAnimation>();

        protected override string GetId(XTween_Controller component)
        {
            if (!adapters.ContainsKey(component))
            {
                adapters[component] = XTweenTimelineAnimation.FromComponent(component);
            }

            return adapters[component] != null ? adapters[component].Label : component.name;
        }
    }

    [CustomPropertyDrawer(typeof(IXTweenTimelineAnimation), true)]
    public class XTweenTimelineAnimationPropertyDrawer : XTweenTimelineComponentPropertyDrawer<IXTweenTimelineAnimation>
    {
        protected override string GetId(IXTweenTimelineAnimation component)
        {
            return component.Label;
        }
    }

    public abstract class XTweenTimelineComponentPropertyDrawer<T> : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var selected = property.objectReferenceValue as Component;
            if (selected == null)
            {
                DrawDefault(position, property, label);
                return;
            }

            var type = selected.GetType();
            var components = selected.GetComponents(type);

            EditorGUI.BeginProperty(position, label, property);

            var controlRect = EditorGUI.PrefixLabel(position, label);
            var halfWidth = controlRect.width / 2f;
            const int halfSpacing = 1;

            var popupRect = controlRect.SetWidth(halfWidth - halfSpacing);
            var index = DrawIdPopup(popupRect, components, selected);
            if (index >= 0)
            {
                property.objectReferenceValue = components[index];
            }

            var selectedRect = controlRect;
            selectedRect.xMin += halfWidth + halfSpacing;
            EditorGUI.PropertyField(selectedRect, property, GUIContent.none);

            EditorGUI.EndProperty();

            if (GUI.changed)
            {
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        private int DrawIdPopup(Rect popupRect, Component[] options, Component selected)
        {
            var prefix = options.Length > 1;
            var ids = options
                .Select((component, i) => prefix ? $"{i}: {GetPopupOption(component)}" : GetPopupOption(component))
                .ToArray();
            var index = options.IndexOf(selected);
            index = EditorGUI.Popup(popupRect, index, ids);
            return index;
        }

        private string GetPopupOption(Component component)
        {
            if (component is not T typedComponent)
            {
                return string.Empty;
            }

            var id = GetId(typedComponent);
            id = Regex.Replace(id, "/", " ");
            id = Regex.Replace(id, "<[^>]+>", string.Empty);
            return id;
        }

        protected abstract string GetId(T component);

        private static void DrawDefault(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(position, property, label);
            EditorGUI.EndProperty();
        }
    }
}
