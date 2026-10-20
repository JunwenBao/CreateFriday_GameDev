namespace Game.UI
{
    public enum UIEase
    {
        Linear,
        InQuad,
        OutQuad,
        InOutQuad,
        InCubic,
        OutCubic,
        InOutCubic,
        OutBack,
    }

    public static class UIEaseExtensions
    {
        public static float Evaluate(this UIEase ease, float t)
        {
            switch (ease)
            {
                case UIEase.InQuad:
                    return t * t;
                case UIEase.OutQuad:
                    return t * (2f - t);
                case UIEase.InOutQuad:
                    return t < 0.5f ? 2f * t * t : 1f - 2f * (1f - t) * (1f - t);
                case UIEase.InCubic:
                    return t * t * t;
                case UIEase.OutCubic:
                    return 1f - (1f - t) * (1f - t) * (1f - t);
                case UIEase.InOutCubic:
                    return t < 0.5f ? 4f * t * t * t : 1f - 4f * (1f - t) * (1f - t) * (1f - t);
                case UIEase.OutBack:
                {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    float u = t - 1f;
                    return 1f + c3 * u * u * u + c1 * u * u;
                }
                default:
                    return t;
            }
        }
    }
}
