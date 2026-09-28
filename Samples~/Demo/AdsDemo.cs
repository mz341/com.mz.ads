using MZ.Ads;
using UnityEngine;

/// <summary>
/// Drop on any GameObject: on-screen buttons for every format plus a live log.
/// Uses only the public <see cref="Ads"/> API, exactly like game code would.
/// </summary>
public class AdsDemo : MonoBehaviour
{
    private string _log = "";
    private int _coins;
    private Vector2 _scroll;

    private void Start()
    {
        Ads.OnAdRevenue += info => Log($"Revenue: {info}");
        Ads.OnConsentChanged += consent => Log($"Consent: {consent}");
        Ads.OnFullScreenAdOpened += format => Log($"Opened {format}");
        Ads.OnFullScreenAdClosed += format => Log($"Closed {format}");

        Log("Initializing…");
        Ads.Initialize(() => Log("Initialized: " + Ads.IsInitialized + (Ads.Config.UseTestAds ? " (test ads)" : "")));
    }

    private void OnGUI()
    {
        var scale = Screen.dpi > 0 ? Screen.dpi / 160f : 1f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        var width = Screen.width / scale;
        var height = Screen.height / scale;

        GUILayout.BeginArea(new Rect(10, 60, width - 20, height - 120));
        GUILayout.Label($"Initialized: {Ads.IsInitialized}   Ads removed: {Ads.AdsRemoved}   Coins: {_coins}");

        Row(
            ("Show banner", Ads.ShowBanner),
            ("Hide banner", Ads.HideBanner),
            ("Destroy banner", Ads.DestroyBanner));
        Row(
            ("Show MREC", Ads.ShowMrec),
            ("Hide MREC", Ads.HideMrec));
        Row(
            ($"Interstitial {Ready(Ads.IsInterstitialReady())}", () =>
                Ads.ShowInterstitial("demo_button", () => Log("Interstitial closed"), r => Log("Interstitial failed: " + r))),
            ($"App Open {Ready(Ads.IsAppOpenReady())}", () =>
                Ads.ShowAppOpen("demo_button", null, r => Log("App Open failed: " + r))));
        Row(
            ($"Rewarded {Ready(Ads.IsRewardedReady())}", () =>
                Ads.ShowRewarded("demo_coins", () => { _coins += 10; Log("Reward earned +10"); },
                    r => Log("Rewarded failed: " + r))),
            ($"Rewarded Inter {Ready(Ads.IsRewardedInterstitialReady())}", () =>
                Ads.ShowRewardedInterstitial("demo_coins", () => { _coins += 5; Log("Reward earned +5"); },
                    r => Log("Rewarded interstitial failed: " + r))));
        Row(
            (Ads.AdsRemoved ? "Restore ads" : "Remove ads", () => Ads.SetAdsRemoved(!Ads.AdsRemoved)),
            ("Privacy options", () => Ads.ShowPrivacyOptions(e => Log("Privacy form: " + (e ?? "ok")))),
            ("Ad Inspector", Ads.OpenAdInspector));

        _scroll = GUILayout.BeginScrollView(_scroll);
        GUILayout.Label(_log);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private static string Ready(bool ready) => ready ? "✓" : "…";

    private static void Row(params (string label, System.Action action)[] buttons)
    {
        GUILayout.BeginHorizontal();
        foreach (var (label, action) in buttons)
        {
            if (GUILayout.Button(label, GUILayout.Height(44)))
            {
                action();
            }
        }

        GUILayout.EndHorizontal();
    }

    private void Log(string message)
    {
        Debug.Log("[AdsDemo] " + message);
        _log = $"{System.DateTime.Now:HH:mm:ss} {message}\n{_log}";
        if (_log.Length > 4000)
        {
            _log = _log.Substring(0, 4000);
        }
    }
}
