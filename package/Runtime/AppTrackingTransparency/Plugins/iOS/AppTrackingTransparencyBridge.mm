#import <Foundation/Foundation.h>
#import <AppTrackingTransparency/AppTrackingTransparency.h>

extern "C" {
    typedef void (*ATTCallback)(int status);

    void RequestTrackingAuthorization(ATTCallback callback) {
        if (@available(iOS 14, *)) {
            [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
                if (callback) {
                    callback((int)status);
                }
            }];
        } else {
            if (callback) {
                callback(3); // Authorized
            }
        }
    }

    int GetTrackingAuthorizationStatus() {
        if (@available(iOS 14, *)) {
            return (int)[ATTrackingManager trackingAuthorizationStatus];
        }
        return 3; // Authorized
    }
}
