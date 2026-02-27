namespace SevenStrikeModules.XTween.Timeline
{
    using UnityEngine;
    using UnityEngine.UI;

    public partial class XTweenTimelineFrame
    {
        private static PropertyScope CreateScope(FrameProperty property)
        {
            if (property == null)
            {
                return null;
            }

            if (property.Target == null)
            {
                return null;
            }

            switch (property.Property)
            {
                case FrameProperty.PropertyType.None:
                    return null;
                case FrameProperty.PropertyType.Position:
                    return new MoveScope(property, isLocal: false);
                case FrameProperty.PropertyType.LocalPosition:
                    return new MoveScope(property, isLocal: true);
                case FrameProperty.PropertyType.Scale:
                    return new ScaleScope(property);
                case FrameProperty.PropertyType.Fade:
                    return new FadeScope(property);
                case FrameProperty.PropertyType.Color:
                    return new ColorScope(property);
                case FrameProperty.PropertyType.Active:
                    return new ActiveScope(property);
                case FrameProperty.PropertyType.Enabled:
                    return new EnabledScope(property);
                default:
                    return null;
            }
        }

        private abstract class PropertyScope
        {
            protected readonly FrameProperty Property;

            protected PropertyScope(FrameProperty property)
            {
                Property = property;
            }

            public abstract void Open();
            public abstract void Close();
        }

        private sealed class MoveScope : PropertyScope
        {
            private Transform Target => (Transform)Property.Target;
            private Vector3 startValue;
            private readonly bool isLocal;

            public MoveScope(FrameProperty property, bool isLocal) : base(property)
            {
                this.isLocal = isLocal;
            }

            public override void Open()
            {
                startValue = GetValue();
                var endValue = Property.EndValueVector3;
                if (Property.IsRelative)
                {
                    endValue += startValue;
                }

                Apply(endValue);
            }

            public override void Close()
            {
                Apply(startValue);
            }

            private Vector3 GetValue()
            {
                if (isLocal)
                {
                    return Target.localPosition;
                }

                if (Target is RectTransform rectTransform)
                {
                    return rectTransform.anchoredPosition3D;
                }

                return Target.position;
            }

            private void Apply(Vector3 value)
            {
                if (isLocal)
                {
                    Target.localPosition = value;
                    return;
                }

                if (Target is RectTransform rectTransform)
                {
                    rectTransform.anchoredPosition3D = value;
                    return;
                }

                Target.position = value;
            }
        }

        private sealed class ScaleScope : PropertyScope
        {
            private Transform Target => (Transform)Property.Target;
            private Vector3 startValue;

            public ScaleScope(FrameProperty property) : base(property)
            {
            }

            public override void Open()
            {
                startValue = Target.localScale;
                var value = Property.EndValueVector3;
                if (Property.IsRelative)
                {
                    value += startValue;
                }

                Target.localScale = value;
            }

            public override void Close()
            {
                Target.localScale = startValue;
            }
        }

        private sealed class FadeScope : PropertyScope
        {
            private float startValue;

            public FadeScope(FrameProperty property) : base(property)
            {
            }

            public override void Open()
            {
                Apply(Property.EndValueFloat, out startValue);
            }

            public override void Close()
            {
                Apply(startValue, out _);
            }

            private void Apply(float value, out float previousValue)
            {
                switch (Property.Target)
                {
                    case Graphic graphic:
                        previousValue = graphic.color.a;
                        var graphicColor = graphic.color;
                        graphicColor.a = value;
                        graphic.color = graphicColor;
                        break;
                    case CanvasGroup canvasGroup:
                        previousValue = canvasGroup.alpha;
                        canvasGroup.alpha = value;
                        break;
                    default:
                        previousValue = value;
                        break;
                }
            }
        }

        private sealed class ColorScope : PropertyScope
        {
            private Color startValue;

            public ColorScope(FrameProperty property) : base(property)
            {
            }

            public override void Open()
            {
                Apply(Property.EndValueColor, out startValue);
            }

            public override void Close()
            {
                Apply(startValue, out _);
            }

            private void Apply(Color color, out Color previousValue)
            {
                switch (Property.Target)
                {
                    case Graphic graphic:
                        previousValue = graphic.color;
                        graphic.color = color;
                        break;
                    case Camera camera:
                        previousValue = camera.backgroundColor;
                        camera.backgroundColor = color;
                        break;
                    default:
                        previousValue = color;
                        break;
                }
            }
        }

        private sealed class ActiveScope : PropertyScope
        {
            private GameObject Target => Property.TargetGameObject;
            private bool startValue;

            public ActiveScope(FrameProperty property) : base(property)
            {
            }

            public override void Open()
            {
                startValue = Target != null && Target.activeSelf;
                if (Target != null)
                {
                    Target.SetActive(Property.OptionalBool);
                }
            }

            public override void Close()
            {
                if (Target != null)
                {
                    Target.SetActive(startValue);
                }
            }
        }

        private sealed class EnabledScope : PropertyScope
        {
            private Behaviour Target => (Behaviour)Property.Target;
            private bool startValue;

            public EnabledScope(FrameProperty property) : base(property)
            {
            }

            public override void Open()
            {
                if (Target == null) return;
                startValue = Target.enabled;
                Target.enabled = Property.OptionalBool;
            }

            public override void Close()
            {
                if (Target != null)
                {
                    Target.enabled = startValue;
                }
            }
        }
    }
}
