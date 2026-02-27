namespace SevenStrikeModules.XTween.Timeline
{
    using System;
    using UnityEngine;

    public partial class XTweenTimelineFrame
    {
        [Serializable]
        public class FrameProperty
        {
            public enum PropertyType
            {
                None,
                Position,
                LocalPosition,
                Scale,
                Fade,
                Color,
                Active,
                Enabled
            }

            public GameObject TargetGameObject;
            public Component Target;
            public PropertyType Property;
            public bool IsRelative;

            public Vector3 EndValueVector3;
            public float EndValueFloat;
            public Color EndValueColor;

            public bool OptionalBool;
        }
    }
}