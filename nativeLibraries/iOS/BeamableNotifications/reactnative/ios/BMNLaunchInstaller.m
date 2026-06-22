#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <objc/message.h>

/// Installs Beamable's notification-center delegate during app launch.
///
/// `+load` runs at binary load time (before `main`), where we register for
/// `UIApplicationDidFinishLaunchingNotification`. That notification is posted within the
/// launch sequence, *before* the run loop delivers a cold-start notification tap — so by
/// claiming the `UNUserNotificationCenter` delegate there (via the Swift module's
/// `+bmnInstallAtLaunch`), Beamable reliably receives the tap that launched the app. Without
/// this, the delegate is only set later from JS (`initialize()` in a React effect) and the
/// launch tap is lost — which is why a tapped push used to open the app to its home screen
/// instead of the deep link.
///
/// The Swift class is resolved at runtime (like React Native's own bridge does) so this needs
/// no generated `-Swift.h` header — the `@objc` module class is `internal`, so it isn't in the
/// pod's public Swift header. The observer runs synchronously (`queue:nil` → posting thread,
/// i.e. main) so the delegate is in place before any queued response is processed.
@interface BMNLaunchInstaller : NSObject
@end

@implementation BMNLaunchInstaller

+ (void)load {
  @autoreleasepool {
    [[NSNotificationCenter defaultCenter]
        addObserverForName:UIApplicationDidFinishLaunchingNotification
                    object:nil
                     queue:nil
                usingBlock:^(NSNotification *_Nonnull note) {
                  Class cls = NSClassFromString(@"BeamableNotificationsModule");
                  SEL sel = NSSelectorFromString(@"bmnInstallAtLaunch");
                  if (cls && [cls respondsToSelector:sel]) {
                    ((void (*)(id, SEL))objc_msgSend)(cls, sel);
                  }
                }];
  }
}

@end
