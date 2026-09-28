# MZ Ads (AdMob)

AdMob-only ads plugin for Unity 2022.3+ (Android and iOS).

- Google UMP consent. Ads load only when `CanRequestAds()` is true, and the consent result is mirrored to Firebase.
- Banner (anchored adaptive), MREC, Interstitial, Rewarded, Rewarded Interstitial and App Open.
- Single ad unit ID or three-tier IDs (high → medium → low floor) via `ADS_TIERED_IDS`.
- Each format has its own loader: one load at a time, exponential backoff, and expired ads are thrown away and reloaded.
- Show rules: interstitial interval, App Open cooldown and resume guards, game pause during ads, Remove Ads.
- Revenue from `OnAdPaid` converted from micros to currency units, sent to Firebase / AppsFlyer, plus revenue milestone and batch events.

## Install

1. Add the OpenUPM scoped registry to `Packages/manifest.json`:
   ```json
   "scopedRegistries": [
     {
       "name": "package.openupm.com",
       "url": "https://package.openupm.com",
       "scopes": ["com.google.ads.mobile", "com.google.external-dependency-manager"]
     }
   ]
   ```
2. Add the package, using a local path or a git URL:
   ```json
   "com.mz.ads": "file:../../path/to/com.mz.ads"
   ```
   `com.google.ads.mobile` 11.5.0 is pulled in automatically.
3. Switch the platform to **Android** or **iOS** (File > Build Settings).
4. **Tools > MZ Ads > Ads Window**. Everything follows the active platform, just like switching platform in the old plugin:
   - **Ads keys**: AdMob App Id plus one field per enabled format (Tier 1 / 2 / 3 when *Three-tier IDs* is added).
     On iOS the ATT popup text appears here too.
   - **Symbols**: **Add / Remove** per feature for the active platform.
   - **Prefabs**: buttons that add GoogleCMP / AdsInitializer / AppTracking to the open scene.
   - **Advanced Settings** opens the config asset (intervals, App Open rules, analytics options).
5. Android: set Minimum API Level to 23 or higher. Run **Assets > External Dependency Manager > Android Resolver > Resolve**.

## Prefabs (`Packages/com.mz.ads/Prefabs`)

| Prefab | Put it in | What it does |
|---|---|---|
| **GoogleCMP** | A small first scene (e.g. `Privacy`, build index 0) | Google UMP consent form → iOS ATT popup → AdMob init → loads the next build scene (or a scene by name). |
| **AdsInitializer** | Your first game / plugin scene | Calls `Ads.Initialize()` if not done yet, survives scene loads, can show the banner right away. |
| **AppTracking** | Only if you want ATT at a custom moment | iOS ATT popup on its own. Not needed with GoogleCMP / AdsInitializer (config: *Request App Tracking On iOS*). |

Typical build order: `0 Privacy (GoogleCMP)` → `1 Plugin/Menu (AdsInitializer)` → game scenes.
You can also add them from **GameObject > MZ Ads** or from the Ads Window.

## Scripting symbols

| Symbol | Feature |
|---|---|
| `ADS_ADMOB` | Core AdMob layer (required) |
| `ADS_BANNER` / `ADS_MREC` | Banner / 300x250 MREC |
| `ADS_INTERSTITIAL` | Interstitial |
| `ADS_REWARDED` / `ADS_REWARDED_INTERSTITIAL` | Rewarded formats |
| `ADS_APP_OPEN` | App Open |
| `ADS_TIERED_IDS` | Up to 3 IDs per format; off = first ID only |
| `ADS_FIREBASE` | Firebase Analytics integration (needs Firebase Analytics) |
| `ADS_APPSFLYER` | AppsFlyer ad revenue (needs the AppsFlyer plugin) |
| `ADS_VERBOSE_LOG` | Debug logs (compiled out when off) |

Game code compiles with any combination of symbols. With `ADS_ADMOB` off every call is a safe no-op.

## Usage

```csharp
using MZ.Ads;

Ads.Initialize();                                   // consent → SDK init → preload

Ads.ShowBanner();  Ads.HideBanner();
Ads.ShowMrec();    Ads.HideMrec();

Ads.ShowInterstitial("level_end");                  // placement name goes to analytics

Ads.ShowRewarded("double_coins",
    onReward: () => coins *= 2,                     // only when the reward was earned
    onFailed: reason => ShowToast("No video"));     // not_ready, interval, ads_removed…

if (Ads.IsRewardedReady()) { /* enable the button */ }

Ads.SetAdsRemoved(true);                            // after a Remove Ads purchase; rewarded keeps working

if (Ads.IsPrivacyOptionsRequired) Ads.ShowPrivacyOptions();   // required "Privacy" button

Ads.OnAdRevenue += info => { /* custom tracking, info.Value is in currency units */ };
Ads.OnFullScreenAdOpened += format => { };
Ads.OnFullScreenAdClosed += format => { };
```

App Open shows automatically when the app returns from the background, if enabled in the config.
Call `Ads.ShowAppOpen()` for a manual show, for example after a splash screen.

## Analytics

| Event | When | Parameters |
|---|---|---|
| `ad_impression` | Each paid impression, **only if** `sendManualAdImpression` is on | `ad_platform`, `ad_source`, `ad_format`, `ad_unit_name`, `value`, `currency` |
| `ads_opportunity` | Game asked to show a full-screen ad | `ad_format`, `placement` |
| `ads_show` | Ad opened (banner: first fill) | `ad_format`, `placement`, `tier` |
| `ads_show_failed` | Show refused or failed | `ad_format`, `placement`, `reason` |
| `ads_load_failed` | Every tier failed in one load cycle | `ad_format`, `tier`, `error_code` |
| `ads_click` | Ad clicked | `ad_format`, `placement` |
| `ads_reward` | Reward earned | `ad_format`, `placement` |
| `ads_rev_0_01` … | Lifetime ad revenue reached a milestone (once per user) | `value`, `currency` |
| `ads_revenue_batch` | Accumulated revenue reached the batch threshold | `value`, `currency` |

Notes:
- **Link your AdMob app to Firebase** and leave `sendManualAdImpression` off. Firebase then logs `ad_impression` itself, and a manual event would count revenue twice.
- Custom events use the `ads_` prefix because Firebase reserves `ad_click`, `ad_reward` and similar names, and silently drops manual events that use them.
- Show rate = `ads_show` / `ads_opportunity`. Register `placement`, `tier` and `reason` as custom dimensions in GA4.
- AppsFlyer receives `logAdRevenue` in currency units, with `ad_unit`, `ad_type` and `placement`, plus the milestone and batch events.
- Firebase: the integration runs `CheckAndFixDependenciesAsync` itself. If your game already initializes Firebase, set
  `FirebaseAdAnalytics.AutoInitializeFirebase = false` and call `FirebaseAdAnalytics.MarkFirebaseReady()` once Firebase is ready.

## Testing

- The Editor and Development builds use Google's demo ad units automatically (`useTestAdsInDevelopment`).
- Add your phone's hashed device ID (printed in logcat / Xcode) to `testDeviceIds`, so real IDs also serve test ads.
- `forceEeaConsentInDevelopment` shows the UMP form on test devices.
- **Tools > MZ Ads > Validate Setup** checks the setup. A release build fails when an enabled format has no IDs or uses Google test IDs.
- Unit tests: Window > General > Test Runner > EditMode > `MZ.Ads.Tests`.
