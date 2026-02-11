
[System.Serializable]
public class SuAdsSaveData
{
    public uint InterstitialCount, BannerCount, RewardedVideoCount, AppOpenCount,RewardedInterstitialCount;
    public double InterstitialRevenue, BannerRevenue, RewardedVideoRevenue, AppOpenRevenue, RewardedInterstitialRevenue;

    public double AdsShowed5, AdsShowed6, AdsShowed8, AdsShowed9, AdsShowed10, AdsShowed20;
    public uint AdsShowedCount5, AdsShowedCount6, AdsShowedCount8, AdsShowedCount9, AdsShowedCount10, AdsShowedCount20;
    public double TotalRevenue
    {
        get
        {
            return InterstitialRevenue + BannerRevenue + RewardedVideoRevenue + AppOpenRevenue + RewardedInterstitialRevenue;
        }
    }
}
