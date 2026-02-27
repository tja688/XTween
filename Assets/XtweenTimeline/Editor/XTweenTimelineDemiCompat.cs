namespace SevenStrikeModules.XTween.Timeline.Editor
{
    using System;
    using UnityEngine;

    public static class XTweenTimelineDemiCompat
    {
        public static Color SetAlpha(this Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static Rect SetHeight(this Rect rect, float height)
        {
            rect.height = height;
            return rect;
        }

        public static Rect SetWidth(this Rect rect, float width)
        {
            rect.width = width;
            return rect;
        }

        public static Rect ShiftY(this Rect rect, float amount)
        {
            rect.y += amount;
            return rect;
        }

        public static Rect ShiftX(this Rect rect, float amount)
        {
            rect.x += amount;
            return rect;
        }

        public static Rect Shift(this Rect rect, float left, float top, float right, float bottom)
        {
            rect.xMin += left;
            rect.yMin += top;
            rect.xMax += right;
            rect.yMax += bottom;
            return rect;
        }

        public static Rect Expand(this Rect rect, float amount)
        {
            rect.xMin -= amount;
            rect.yMin -= amount;
            rect.xMax += amount;
            rect.yMax += amount;
            return rect;
        }

        public static Rect Expand(this Rect rect, float x, float y)
        {
            rect.xMin -= x;
            rect.xMax += x;
            rect.yMin -= y;
            rect.yMax += y;
            return rect;
        }

        public static Rect Add(this Rect rect, Rect other)
        {
            return Rect.MinMaxRect(
                Mathf.Min(rect.xMin, other.xMin),
                Mathf.Min(rect.yMin, other.yMin),
                Mathf.Max(rect.xMax, other.xMax),
                Mathf.Max(rect.yMax, other.yMax));
        }
    }

    public static class DeGUI
    {
        public readonly struct ColorScope : IDisposable
        {
            private readonly Color oldBackground;
            private readonly Color oldMain;

            public ColorScope(Color background, Color main)
            {
                oldBackground = GUI.backgroundColor;
                oldMain = GUI.color;
                GUI.backgroundColor = background;
                GUI.color = main;
            }

            public void Dispose()
            {
                GUI.backgroundColor = oldBackground;
                GUI.color = oldMain;
            }
        }
    }
}