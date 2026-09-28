# Changelog

## 0.1.0 (2026-09-28)
- First version, built against Google Mobile Ads Unity 11.5.0 and Unity 2022.3.
- Formats: banner, MREC, interstitial, rewarded, rewarded interstitial, App Open.
- Ad unit IDs can be a single ID or three tiers (`ADS_TIERED_IDS`). Each format keeps its own tier and retry state.
- UMP consent, with the IAB TCF result mapped to Firebase consent mode.
- Show rules: interstitial interval, App Open cooldown and resume guards, game pause during ads, Remove Ads.
- Revenue analytics: micros converted to currency units, optional manual `ad_impression`, AppsFlyer ad revenue, milestone and batch events.
- Funnel events use the `ads_` prefix (Firebase reserves `ad_click` / `ad_reward`).
- Editor: Ads Window (IDs + symbols), Config menu, build validator. 16 EditMode unit tests.
- Tested in the Editor: consent, init, preload, show/close, pause/resume, reward, revenue, interval, cooldown, Remove Ads.

## 0.2.0 (2026-09-28)
- Ads Window: one screen with ad keys and symbol Add / Remove buttons. Both follow the active build platform.
- Prefabs: GoogleCMP (consent → ATT → init → next scene), AdsInitializer, AppTracking (iOS).
- iOS App Tracking Transparency: native bridge (`AppTracking.Request`), requested after UMP and before the first ad request.
  The iOS build step links AppTrackingTransparency.framework and ensures NSUserTrackingUsageDescription.
- Tests assembly only compiles when com.unity.test-framework is installed (fixes 99 errors after import into projects without it).
- Release files: com.mz.ads-0.2.0.tgz and MZAds-0.2.0.unitypackage, both verified in fresh Unity 2022.3 projects.
