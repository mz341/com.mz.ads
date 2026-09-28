// App Tracking Transparency bridge for MZ.Ads (AppTracking.cs).
#import <Foundation/Foundation.h>
#import <AppTrackingTransparency/AppTrackingTransparency.h>

typedef void (*MZAttCallback)(int status);

extern "C" {

// 0 = not determined, 1 = restricted, 2 = denied, 3 = authorized. iOS < 14 has no ATT: authorized.
int _MZAttStatus()
{
    if (@available(iOS 14, *)) {
        return (int)[ATTrackingManager trackingAuthorizationStatus];
    }
    return 3;
}

void _MZAttRequest(MZAttCallback callback)
{
    if (@available(iOS 14, *)) {
        [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
            dispatch_async(dispatch_get_main_queue(), ^{
                if (callback != NULL) {
                    callback((int)status);
                }
            });
        }];
        return;
    }

    if (callback != NULL) {
        callback(3);
    }
}

}
