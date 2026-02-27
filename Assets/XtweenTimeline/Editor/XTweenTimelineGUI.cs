namespace SevenStrikeModules.XTween.Timeline.Editor
{
    using System;
    using SevenStrikeModules.XTween.Timeline;
    using UnityEditor;
    using UnityEngine;
    using Random = UnityEngine.Random;

    public static class XTweenTimelineGUI
    {
        private const float RowHeight = 20f;
        private const int BottomHeight = 30;
        private const int TimeHeight = 20;
        private static readonly Vector2 PlayButtonSize = new Vector2(44f, 24f);
        private static readonly Vector2 LoopToggleSize = new Vector2(24f, 24f);
        private static readonly Color ToggleFadeColor = new Color(1f, 1f, 1f, 0.7f);
        private static readonly Color PlayheadColor = new Color(0.19f, 0.44f, 0.89f);

        private static readonly Color[] Colors =
        {
            Color.red, Color.green, Color.blue,
            Color.yellow, Color.cyan, Color.magenta
        };

        public static Rect GetTimelineControlRect(int tweenCount)
        {
            return EditorGUILayout.GetControlRect(false, TimelineHeaderHeight + TimeHeight + tweenCount * RowHeight + BottomHeight);
        }

        public static void Background(Rect rect)
        {
            RoundRect(rect, Color.black.SetAlpha(0.3f), borderRadius: 4f);
            RoundRect(rect, Color.black, borderRadius: 4f, borderWidth: 1f);
        }

        public static Rect Header(Rect rect)
        {
            rect = rect.SetHeight(TimelineHeaderHeight);
            GUI.Label(rect, "XTween Timeline", TimelineHeaderStyle);

            var bottomLine = new Rect(rect.x, rect.y + rect.height, rect.width, 1f);
            EditorGUI.DrawRect(bottomLine, Color.black);
            return rect;
        }

        public static bool PreviewEye(Rect headerRect, bool isPlaying, bool isPaused, bool isTimeDragging)
        {
            if (!isPlaying && !isPaused && !isTimeDragging)
            {
                return false;
            }

            var iconSize = Vector2.one * 16f;
            var eyeShift = new Vector2(45f, 0f);
            var iconRect = new Rect(
                headerRect.x + headerRect.width * 0.5f + eyeShift.x,
                headerRect.y + (headerRect.height - iconSize.y) / 2f + eyeShift.y,
                iconSize.x,
                iconSize.y);

            var clickArea = isPlaying ? iconRect : headerRect.Expand(-48f, 0f);
            var hover = !isTimeDragging && clickArea.Contains(Event.current.mousePosition);

            var eyeIcon = EditorGUIUtility.TrIconContent(
                hover ? "animationvisibilitytoggleoff" : "animationvisibilitytoggleon",
                "Disable scene preview mode.");

            using (new DeGUI.ColorScope(background: Color.white.SetAlpha(0f), main: Color.white.SetAlpha(0.3f)))
            {
                EditorGUIUtility.AddCursorRect(iconRect, MouseCursor.Link);
                if (GUI.Button(iconRect, eyeIcon, EditorStyles.iconButton))
                {
                    return true;
                }
            }

            if (Event.current.type == EventType.MouseDown && clickArea.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                return true;
            }

            return false;
        }

        public static Rect Time(Rect rect, float timeScale, ref bool isDragging, Action start, Action<Event> end)
        {
            rect = rect.ShiftY(TimelineHeaderHeight).SetHeight(TimeHeight);

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                normal = { textColor = Color.white.SetAlpha(0.5f) }
            };

            const int count = 10;
            const float step = 1f / count;
            for (var i = 0; i < count; i++)
            {
                var time = i * step;
                var position = new Rect(rect.x + i * step * rect.width, rect.y, step * rect.width, rect.height);
                time /= Mathf.Max(0.0001f, timeScale);
                GUI.Label(position, time.ToString("0.00"), style);
            }

            var bottomLine = new Rect(rect.x, rect.y + rect.height, rect.width, 1f);
            EditorGUI.DrawRect(bottomLine, Color.black);
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            ProcessDragEvents(rect, ref isDragging, start, end);
            return rect;
        }

        public static void PlayheadLabel(Rect timeRect, float scaledTime, float rawTime)
        {
            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                hover = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            var position = new Vector2(timeRect.x + scaledTime * timeRect.width, timeRect.y);
            var labelContent = new GUIContent(rawTime.ToString("0.00"));

            const int yShift = 1;
            var labelRect = new Rect(position.x, position.y + yShift, 32f, timeRect.height - yShift * 2);
            labelRect.x -= labelRect.width * 0.5f;
            const int maxXShift = 4;
            labelRect.x = Mathf.Clamp(labelRect.x, timeRect.x - maxXShift, timeRect.xMax - labelRect.width + maxXShift);

            var labelBackground = new Rect(labelRect.x, labelRect.y, labelRect.width, labelRect.height);
            RoundRect(labelBackground, PlayheadColor, borderRadius: 8f);
            GUI.Label(labelRect, labelContent, labelStyle);
        }

        public static float GetScaledTimeUnderMouse(Rect timeRect)
        {
            var time = (Event.current.mousePosition.x - timeRect.x) / timeRect.width;
            return Mathf.Clamp01(time);
        }

        public static Rect Tweens(
            Rect rect,
            IXTweenTimelineAnimation[] animations,
            float timeScale,
            IXTweenTimelineAnimation selected,
            ref bool isTweenDragging,
            Action<IXTweenTimelineAnimation> tweenSelected)
        {
            rect = rect.ShiftY(TimelineHeaderHeight + TimeHeight).SetHeight(animations.Length * RowHeight);

            for (var i = 0; i < animations.Length; i++)
            {
                var animation = animations[i];
                var rowRect = new Rect(rect.x, rect.y + i * RowHeight, rect.width, RowHeight);
                var isSelected = selected?.Component == animation.Component;
                var tweenRect = Element(animation, rowRect, isSelected, timeScale);

                ProcessDragEvents(tweenRect, ref isTweenDragging, () => tweenSelected?.Invoke(animation), null);

                var bottomLine = new Rect(rowRect.x, rowRect.y + rowRect.height, rowRect.width, 1f);
                EditorGUI.DrawRect(bottomLine, Color.black);
            }

            return rect;
        }

        public static void TimeVerticalLine(Rect rect, float scaledTime, bool underLabel)
        {
            var shift = underLabel ? 10f : 1f;
            var verticalLine = new Rect(rect.x + scaledTime * rect.width, rect.y + shift, 1f, rect.height - shift);
            EditorGUI.DrawRect(verticalLine, PlayheadColor);
        }

        public static void Inspector(UnityEditor.Editor editor, Action onButtonUp, Action onButtonDown)
        {
            EditorGUILayout.Space();
            Splitter(new Color(0.12f, 0.12f, 0.12f, 1.333f));

            var backgroundRect = GUILayoutUtility.GetRect(1f, 20f);
            var labelRect = backgroundRect;
            backgroundRect = ToFullWidth(backgroundRect);
            EditorGUI.DrawRect(backgroundRect, new Color(0.1f, 0.1f, 0.1f, 0.2f));
            EditorGUI.LabelField(labelRect, "Inspector", InspectorHeaderStyle);
            CreateInspectorButtons(backgroundRect, onButtonUp, onButtonDown);

            Splitter(new Color(0.19f, 0.19f, 0.19f, 1.333f));
            editor.OnInspectorGUI();
        }

        public static bool AddButton(Rect timelineRect)
        {
            var buttonRect = CalculateAddButtonRect(timelineRect);
            var content = EditorGUIUtility.IconContent("Toolbar Plus");
            content.tooltip = "Add tween";
            return GUI.Button(buttonRect, content, AddTweenButtonStyle);
        }

        public static void AddMoreButton(Rect timelineRect, XTweenTimelineView.AddMoreItem[] items, Action<XTweenTimelineView.AddMoreItem> clicked)
        {
            const float buttonWidth = 20f;
            var addButtonRect = CalculateAddButtonRect(timelineRect);
            var buttonRect = addButtonRect.ShiftX(addButtonRect.width).SetWidth(buttonWidth);
            var dropDownIcon = EditorGUIUtility.IconContent("icon dropdown");

            var backgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = backgroundColor.SetAlpha(0.55f);
            var result = EditorGUI.DropdownButton(buttonRect, dropDownIcon, FocusType.Passive, AddMoreButtonStyle);
            GUI.backgroundColor = backgroundColor;
            if (!result)
            {
                return;
            }

            var menu = new GenericMenu();
            foreach (var item in items)
            {
                menu.AddItem(item.Content, false, userData => clicked?.Invoke((XTweenTimelineView.AddMoreItem)userData), item);
            }

            menu.DropDown(addButtonRect.ShiftX(-4f));
        }

        public static bool RemoveButton(Rect timelineRect)
        {
            var buttonSize = new Vector2(50f, 24f);
            var position = new Vector2(
                timelineRect.x + timelineRect.width - buttonSize.x - (BottomHeight - buttonSize.y) / 2f,
                timelineRect.y + timelineRect.height - BottomHeight + (BottomHeight - buttonSize.y) / 2f);
            var buttonRect = new Rect(position, buttonSize);
            return GUI.Button(buttonRect, "Delete");
        }

        public static bool DuplicateButton(Rect rect)
        {
            var buttonSize = new Vector2(66f, 24f);
            var position = new Vector2(
                rect.x + rect.width - buttonSize.x - (BottomHeight - buttonSize.y) / 2f - 50f - 2f,
                rect.y + rect.height - BottomHeight + (BottomHeight - buttonSize.y) / 2f);
            var buttonRect = new Rect(position, buttonSize);
            return GUI.Button(buttonRect, "Duplicate");
        }

        public static bool PlayButton(Rect rect)
        {
            var content = EditorGUIUtility.IconContent("d_PlayButton");
            var position = rect.position + new Vector2(2f, (TimelineHeaderHeight - PlayButtonSize.y) / 2f);
            var buttonRect = new Rect(position, PlayButtonSize);
            var contentColor = GUI.contentColor;
            GUI.contentColor = Color.cyan;
            var result = GUI.Button(buttonRect, content);
            GUI.contentColor = contentColor;
            return result;
        }

        public static bool StopButton(Rect rect)
        {
            var position = rect.position + new Vector2(2f, (TimelineHeaderHeight - PlayButtonSize.y) / 2f);
            var buttonRect = new Rect(position, PlayButtonSize);
            return GUI.Button(buttonRect, "■");
        }

        public static bool LoopToggle(Rect rect, bool value)
        {
            var position = rect.position + new Vector2(rect.width - LoopToggleSize.x - 2f, (TimelineHeaderHeight - LoopToggleSize.y) / 2f);
            var toggleRect = new Rect(position, LoopToggleSize);
            var iconContent = EditorGUIUtility.TrIconContent("preAudioLoopOff", "Toggle loop playback");
            var style = new GUIStyle(GUI.skin.button) { padding = new RectOffset(0, 0, 0, 0) };
            using var colorScope = new DeGUI.ColorScope(ToggleFadeColor, ToggleFadeColor);
            return GUI.Toggle(toggleRect, value, iconContent, style);
        }

        public static bool SnapToggle(Rect rect, bool value)
        {
            var position = rect.position + new Vector2(rect.width - (LoopToggleSize.x + 1f) * 2f - 2f, (TimelineHeaderHeight - LoopToggleSize.y) / 2f);
            var toggleRect = new Rect(position, LoopToggleSize);
            var iconContent = EditorGUIUtility.TrIconContent(
                "SceneViewSnap",
                $"Toggle snapping while dragging tweens\n\nHold <b>Ctrl</b> to temporarily {(value ? "disable" : "enable")} snapping.");
            var style = new GUIStyle(GUI.skin.button) { padding = new RectOffset(0, 0, 0, 0) };
            using var colorScope = new DeGUI.ColorScope(ToggleFadeColor, ToggleFadeColor);
            return GUI.Toggle(toggleRect, value, iconContent, style);
        }

        private static Rect Element(IXTweenTimelineAnimation animation, Rect rowRect, bool isSelected, float timeScale)
        {
            return animation.CallbackView
                ? Callback(animation, rowRect, isSelected, timeScale)
                : Tween(animation, rowRect, isSelected, timeScale);
        }

        private static Rect Callback(IXTweenTimelineAnimation animation, Rect rowRect, bool isSelected, float timeScale)
        {
            var textStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                richText = true
            };

            var iconX = CalculateX(rowRect, animation.Delay, timeScale);
            var iconRect = new Rect(iconX, rowRect.y, 10f, 20f);
            var labelContent = new GUIContent(animation.Label);

            textStyle.padding = new RectOffset((int)iconRect.width + 4, 0, 0, 1);
            var textWidth = textStyle.CalcSize(labelContent).x;
            var rect = new Rect(iconRect.x, rowRect.y, textWidth, rowRect.height);

            var onRightSide = rect.x > rowRect.x + rowRect.width * 0.5f;
            var outOfBounds = rect.xMax > rowRect.xMax;
            if (onRightSide && outOfBounds)
            {
                (textStyle.padding.right, textStyle.padding.left) = (textStyle.padding.left, textStyle.padding.right);
                rect.x = iconRect.xMax - textWidth;
            }

            var textOnlyRect = rect.Shift(textStyle.padding.left, 0, -textStyle.padding.horizontal, 0);
            var isHovered = rect.Contains(Event.current.mousePosition);

            var iconColor = Color.white.SetAlpha(0.6f);
            if (isSelected)
            {
                iconColor = new Color(0.2f, 0.6f, 1f);
            }
            else if (isHovered)
            {
                iconColor = Color.white.SetAlpha(0.5f);
            }

            var icon = animation.CustomIcon ?? IconCallback;
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true, 0f, iconColor, 0f, 0f);

            if (isSelected || isHovered)
            {
                var underlineRect = new Rect(textOnlyRect.x, textOnlyRect.yMax - 4f, textOnlyRect.width, 1f);
                var color = isHovered ? Color.white.SetAlpha(0.7f) : Color.white;
                EditorGUI.DrawRect(underlineRect, color);
            }

            GUI.Label(rect, labelContent, textStyle);
            return rect;
        }

        private static Rect Tween(IXTweenTimelineAnimation animation, Rect rowRect, bool isSelected, float timeScale)
        {
            var isInfinite = animation.Loops == -1;
            var loops = Mathf.Max(1, animation.Loops);
            var start = CalculateX(rowRect, animation.Delay, timeScale);
            var width = isInfinite
                ? rowRect.width - start + rowRect.x
                : animation.Duration * loops * timeScale * rowRect.width;
            width = Mathf.Max(width, MinTweenRectWidth);

            var tweenRect = new Rect(start, rowRect.y, width, rowRect.height).Expand(-1f);
            var alphaMultiplier = animation.IsActive ? 1f : 0.4f;

            RoundRect(tweenRect, Color.gray.SetAlpha(0.3f * alphaMultiplier), borderRadius: 4f);

            var mouseHover = tweenRect.Contains(Event.current.mousePosition);
            if (isSelected)
            {
                RoundRect(tweenRect, Color.white.SetAlpha(0.9f * alphaMultiplier), borderRadius: 4f, borderWidth: 2f);
            }
            else if (mouseHover)
            {
                RoundRect(tweenRect, Color.white.SetAlpha(0.9f), borderRadius: 4f, borderWidth: 1f);
            }

            var colorLine = new Rect(tweenRect.x + 1f, tweenRect.y + tweenRect.height - 3f, tweenRect.width - 2f, 2f);
            Random.InitState(animation.Component.GetInstanceID());
            var color = Colors.GetRandom();
            EditorGUI.DrawRect(colorLine, color.SetAlpha(0.6f * alphaMultiplier));

            var label = new GUIContent(animation.Label);
            var style = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white.SetAlpha(alphaMultiplier) }
            };
            var labelWidth = style.CalcSize(label).x;
            var labelRect = tweenRect;
            if (labelWidth > labelRect.width)
            {
                label.tooltip = animation.Label;
                style.alignment = mouseHover ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
                labelRect.xMin += 4f;
            }

            GUI.Label(labelRect, label, style);
            return tweenRect;
        }

        private static void ProcessDragEvents(Rect rect, ref bool isDragging, Action start, Action<Event> end)
        {
            var current = Event.current;
            switch (current.type)
            {
                case EventType.MouseDown when !isDragging && rect.Contains(current.mousePosition):
                    isDragging = true;
                    start?.Invoke();
                    current.Use();
                    break;

                case EventType.MouseUp when isDragging:
                    isDragging = false;
                    end?.Invoke(current);
                    current.Use();
                    break;
            }
        }

        private static float CalculateX(Rect rowRect, float time, float timeScale)
        {
            return rowRect.x + time * timeScale * rowRect.width;
        }

        private static void CreateInspectorButtons(Rect backgroundRect, Action onButtonUp, Action onButtonDown)
        {
            const int rightMargin = 6;
            var downButtonRect = new Rect(backgroundRect.xMax - InspectorButtonSize.x - rightMargin, backgroundRect.y, InspectorButtonSize.x, InspectorButtonSize.y);
            var upButtonRect = downButtonRect.ShiftX(-InspectorButtonSize.x);

            if (GUI.Button(upButtonRect, InspectorUpButton, InspectorButtonStyle))
            {
                onButtonUp?.Invoke();
            }

            if (GUI.Button(downButtonRect, InspectorDownButton, InspectorButtonStyle))
            {
                onButtonDown?.Invoke();
            }
        }

        private static void Splitter(Color color)
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f);
            rect = ToFullWidth(rect);
            EditorGUI.DrawRect(rect, color);
        }

        private static Rect ToFullWidth(Rect rect)
        {
            rect.xMin = 0f;
            rect.width += 4f;
            return rect;
        }

        private static Rect CalculateAddButtonRect(Rect timelineRect)
        {
            var buttonSize = new Vector2(32f, 24f);
            var position = new Vector2(
                timelineRect.x + (BottomHeight - buttonSize.y) / 2f,
                timelineRect.y + timelineRect.height - BottomHeight + (BottomHeight - buttonSize.y) / 2f);
            return new Rect(position, buttonSize);
        }

        private static void RoundRect(Rect rect, Color color, float borderRadius, float borderWidth = 0f)
        {
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false, 0f, color, borderWidth, borderRadius);
        }

        private static Texture2D IconCallback => XTweenTimelineUtils.ImageFromString(IconCallbackBase64);

        private const string IconCallbackBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAABQAAAAoCAYAAAD+MdrbAAAACXBIWXMAAAsTAAALEwEAmpwYAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAD4SURBVHgB7ZWxCoJQFIaPkkuOQVtDLQ02tPQGDrn6CvU+tbeH4NTmCzgLRWM0NGhBuCiBBXZO3eISFXppMLgf/ChX7ufPWQ6ApHIo3HsX08ZoUI4zZodZ84c9x3GmWZYleUmSJNnTXVboiUUfckGoCDqGJFKZsKbrehME0TRNBzYqFX6MFEqhFErhnwsvcRxvQZA0TQ/4OPFnHdu2RyJrAIts8O4YXnYK0cKYvu/Pi4hoj3ieN8M7Fty35VvqmD61jaJo+UkWhuGKtRpAwbV7a+u67oQfA9fKxDSgJPRng9oGQbCgsFYGfGmlFBDTGB4zijBHkEgqzhX38zVoGGkfagAAAABJRU5ErkJggg==";

        private static readonly GUIStyle InspectorHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleLeft
        };

        private static readonly Vector2 InspectorButtonSize = new Vector2(24f, 20f);
        private static readonly GUIContent InspectorDownButton = EditorGUIUtility.TrTextContent("↓", "Move Down");
        private static readonly GUIContent InspectorUpButton = EditorGUIUtility.TrTextContent("↑", "Move Up");
        private static readonly GUIStyle InspectorButtonStyle = new GUIStyle(EditorStyles.iconButton)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
            fixedWidth = 0f,
            fixedHeight = 0f
        };

        private static readonly GUIStyle TimelineHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };

        private const int TimelineHeaderHeight = 28;

        private static readonly GUIStyle AddTweenButtonStyle = new GUIStyle(EditorStyles.miniButtonLeft) { fixedHeight = 0f };
        private static readonly GUIStyle AddMoreButtonStyle = new GUIStyle(EditorStyles.miniButtonRight) { fixedHeight = 0f };

        private const float MinTweenRectWidth = 16f;
    }
}
