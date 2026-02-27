namespace SevenStrikeModules.XTween.Timeline
{
    using System.Linq;
    using SevenStrikeModules.XTween;
    using UnityEngine;
    using UnityEngine.UI;

    [AddComponentMenu("XTween/XTween Timeline Runtime Smoke")]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class XTweenTimelineRuntimeSmoke : MonoBehaviour
    {
        [SerializeField] private RectTransform targetRect;
        [SerializeField] private Image targetImage;
        [SerializeField] private CanvasGroup targetCanvasGroup;
        [SerializeField] private XTweenTimeline linkedTimeline;
        [SerializeField] private bool rebuildOnValidate = true;
        private bool rebuildQueued;

        private void Awake()
        {
            if (Application.isPlaying)
            {
                Rebuild();
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                if (targetRect == null || linkedTimeline == null)
                {
                    Rebuild();
                }

                return;
            }

#if UNITY_EDITOR
            if (targetRect == null || linkedTimeline == null)
            {
                QueueRebuild();
            }
#endif
        }

        private void Reset()
        {
            QueueRebuild();
        }

        private void OnValidate()
        {
            QueueRebuild();
        }

        [ContextMenu("Rebuild Smoke Setup")]
        public void Rebuild()
        {
            EnsurePrimaryTarget();
            EnsureCoreComponents();
            ConfigurePrimaryControllers();
            ConfigureActionComponents();
            EnsureLinkedTimeline();
            ConfigureLinkComponent();
        }

        private void QueueRebuild()
        {
            if (Application.isPlaying || !rebuildOnValidate)
            {
                return;
            }

#if UNITY_EDITOR
            if (rebuildQueued)
            {
                return;
            }

            rebuildQueued = true;
            UnityEditor.EditorApplication.delayCall += DelayedRebuild;
#endif
        }

#if UNITY_EDITOR
        private void DelayedRebuild()
        {
            rebuildQueued = false;
            if (this == null || gameObject == null || Application.isPlaying || !rebuildOnValidate)
            {
                return;
            }

            Rebuild();
        }
#endif

        private void EnsureCoreComponents()
        {
            if (GetComponent<XTweenTimeline>() == null)
            {
                gameObject.AddComponent<XTweenTimeline>();
            }

            if (GetComponent<XTweenTimelinePlayer>() == null)
            {
                gameObject.AddComponent<XTweenTimelinePlayer>();
            }
        }

        private void EnsurePrimaryTarget()
        {
            var child = transform.Find("SmokeTarget") as RectTransform;
            if (child == null)
            {
                var childGo = new GameObject("SmokeTarget", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                childGo.transform.SetParent(transform, false);
                child = childGo.GetComponent<RectTransform>();
            }

            targetRect = child;
            targetImage = child.GetComponent<Image>();
            targetCanvasGroup = child.GetComponent<CanvasGroup>();

            targetRect.sizeDelta = new Vector2(220f, 56f);
            targetRect.anchoredPosition = new Vector2(1040f, 720f);

            targetImage.color = new Color(0.95f, 0.55f, 0.2f, 1f);
            targetCanvasGroup.alpha = 1f;
        }

        private void ConfigurePrimaryControllers()
        {
            ConfigureController(XTweenTypes.位置_Position, 0.00f, 0.90f, controller =>
            {
                controller.TweenTypes_Positions = XTweenTypes_Positions.锚点位置3D_AnchoredPosition3D;
                controller.EndValue_Vector3 = new Vector3(1260f, 720f, 0f);
            });

            ConfigureController(XTweenTypes.缩放_Scale, 0.12f, 0.75f, controller =>
            {
                controller.EndValue_Vector3 = new Vector3(1.35f, 1.35f, 1f);
            });

            ConfigureController(XTweenTypes.旋转_Rotation, 0.24f, 0.80f, controller =>
            {
                controller.TweenTypes_Rotations = XTweenTypes_Rotations.欧拉角度_Euler;
                controller.EndValue_Vector3 = new Vector3(0f, 0f, 28f);
            });

            ConfigureController(XTweenTypes.透明度_Alpha, 0.40f, 0.70f, controller =>
            {
                controller.TweenTypes_Alphas = XTweenTypes_Alphas.CanvasGroup组件;
                controller.EndValue_Float = 0.22f;
            });

            ConfigureController(XTweenTypes.颜色_Color, 0.56f, 0.75f, controller =>
            {
                controller.EndValue_Color = new Color(0.25f, 0.85f, 1f, 1f);
            });
        }

        private void ConfigureController(XTweenTypes tweenType, float delay, float duration, System.Action<XTween_Controller> extraSetup)
        {
            var controller = GetOrAddController(tweenType);

            controller.TweenTypes = tweenType;
            controller.Delay = delay;
            controller.Duration = duration;
            controller.LoopCount = 0;
            controller.LoopDelay = 0f;
            controller.IsAutoKill = false;
            controller.AutoStart = false;
            controller.IsRelative = false;
            controller.IsFromMode = false;
            controller.Target_RectTransform = targetRect;
            controller.Target_Image = targetImage;
            controller.Target_CanvasGroup = targetCanvasGroup;

            extraSetup?.Invoke(controller);
        }

        private XTween_Controller GetOrAddController(XTweenTypes tweenType)
        {
            var controllers = GetComponents<XTween_Controller>();
            var existing = controllers.FirstOrDefault(controller => controller != null && controller.TweenTypes == tweenType);
            if (existing != null)
            {
                return existing;
            }

            return gameObject.AddComponent<XTween_Controller>();
        }

        private void ConfigureActionComponents()
        {
            var callback = GetComponent<XTweenTimelineCallback>() ?? gameObject.AddComponent<XTweenTimelineCallback>();
            callback.id = "Smoke.Callback";
            callback.delay = 1.08f;
            if (callback.onCallback == null)
            {
                callback.onCallback = new UnityEngine.Events.UnityEvent();
            }
            callback.onCallback.RemoveAllListeners();
            callback.onCallback.AddListener(OnSmokeCallback);

            var frame = GetComponent<XTweenTimelineFrame>() ?? gameObject.AddComponent<XTweenTimelineFrame>();
            ((IXTweenTimelineAnimation)frame).Delay = 0.88f;

            if (frame.Properties.Length == 0 || frame.Properties[0] == null)
            {
                frame.Properties[0] = new XTweenTimelineFrame.FrameProperty();
            }

            var property = frame.Properties[0];
            property.TargetGameObject = targetRect.gameObject;
            property.Target = targetRect;
            property.Property = XTweenTimelineFrame.FrameProperty.PropertyType.Scale;
            property.IsRelative = false;
            property.EndValueVector3 = new Vector3(1.05f, 1.05f, 1f);
        }

        private void EnsureLinkedTimeline()
        {
            var linkedRoot = transform.Find("SmokeLinkedTimeline") as RectTransform;
            if (linkedRoot == null)
            {
                var linkedGo = new GameObject("SmokeLinkedTimeline", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                linkedGo.transform.SetParent(transform, false);
                linkedRoot = linkedGo.GetComponent<RectTransform>();
            }

            linkedRoot.sizeDelta = new Vector2(140f, 40f);
            linkedRoot.anchoredPosition = new Vector2(1120f, 620f);

            var image = linkedRoot.GetComponent<Image>();
            image.color = new Color(0.35f, 0.35f, 0.35f, 1f);
            var canvasGroup = linkedRoot.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;

            linkedTimeline = linkedRoot.GetComponent<XTweenTimeline>() ?? linkedRoot.gameObject.AddComponent<XTweenTimeline>();

            var controller = linkedRoot.GetComponent<XTween_Controller>() ?? linkedRoot.gameObject.AddComponent<XTween_Controller>();
            controller.TweenTypes = XTweenTypes.颜色_Color;
            controller.Delay = 0f;
            controller.Duration = 0.5f;
            controller.LoopCount = 0;
            controller.LoopDelay = 0f;
            controller.IsAutoKill = false;
            controller.AutoStart = false;
            controller.Target_RectTransform = linkedRoot;
            controller.Target_Image = image;
            controller.Target_CanvasGroup = canvasGroup;
            controller.EndValue_Color = new Color(0.95f, 0.35f, 0.45f, 1f);
        }

        private void ConfigureLinkComponent()
        {
            var link = GetComponent<XTweenTimelineLink>() ?? gameObject.AddComponent<XTweenTimelineLink>();
            link.id = "Smoke.Link";
            link.timeline = linkedTimeline;
            link.delay = 0.45f;
        }

        private static void OnSmokeCallback()
        {
            Debug.Log("[XTweenTimelineRuntimeSmoke] Callback fired.");
        }
    }
}
