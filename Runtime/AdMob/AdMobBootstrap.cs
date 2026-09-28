using UnityEngine;

namespace MZ.Ads.AdMob
{
    /// <summary>Plugs the AdMob provider into <see cref="Ads"/> before the first scene loads.</summary>
    internal static class AdMobBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            Ads.RegisterProvider(new AdMobProvider());
        }
    }
}
