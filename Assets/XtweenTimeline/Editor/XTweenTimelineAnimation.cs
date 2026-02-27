namespace SevenStrikeModules.XTween.Timeline.Editor
{
    using UnityEngine;

    public static class XTweenTimelineAnimation
    {
        public static IXTweenTimelineAnimation FromComponent(Component component)
        {
            return XTweenTimelineAnimationAdapter.FromComponent(component);
        }
    }
}
