namespace SevenStrikeModules.XTween.Timeline
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public static class XTweenTimelineUtils
    {
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        public static Texture2D ImageFromString(string source)
        {
            if (Cache.TryGetValue(source, out var cachedTexture) && cachedTexture != null)
            {
                return cachedTexture;
            }

            var bytes = Convert.FromBase64String(source);
            var texture = new Texture2D(1, 1);
            texture.LoadImage(bytes);
            Cache[source] = texture;
            return texture;
        }
    }
}
