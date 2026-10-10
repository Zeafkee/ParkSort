using System;
using DG.Tweening;

namespace ParkSort.Services
{
    public interface IAdService
    {
        bool RewardedReady { get; }
        void ShowRewarded(string placement, Action<bool> finished);
    }

    public static class Ads
    {
        public static IAdService Current = new PlaceholderAds();
    }

    public sealed class PlaceholderAds : IAdService
    {
        public const float Duration = 1.6f;

        public bool RewardedReady => true;

        public void ShowRewarded(string placement, Action<bool> finished)
        {
            DOVirtual.DelayedCall(Duration, () => finished(true)).SetUpdate(true);
        }
    }
}
