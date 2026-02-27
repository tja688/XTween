namespace SevenStrikeModules.XTween.Timeline
{
    using UnityEngine;

    public static class XTweenTimelineAnimationAdapter
    {
        public static IXTweenTimelineAnimation FromComponent(Component component)
        {
            switch (component)
            {
                case null:
                    return null;
                case IXTweenTimelineAnimation animation:
                    return animation;
                case XTween_Controller controller:
                    return new XTweenTimelineControllerAdapter(controller);
                default:
                    return null;
            }
        }
    }
}
