#import "GoogleSignInAppController.h"
#import <objc/runtime.h>


@implementation UnityAppController (GoogleSignInController)

+ (void)load {
  method_exchangeImplementations(
    class_getInstanceMethod(self, @selector(application:openURL:sourceApplication:annotation:)),
    class_getInstanceMethod(self, @selector(GoogleSignInAppController:openURL:sourceApplication:annotation:))
  );

  method_exchangeImplementations(
    class_getInstanceMethod(self, @selector(application:openURL:options:)),
    class_getInstanceMethod(self, @selector(GoogleSignInAppController:openURL:options:))
  );
}

- (BOOL)GoogleSignInAppController:(UIApplication *)application
                          openURL:(NSURL *)url
                sourceApplication:(NSString *)sourceApplication
                       annotation:(id)annotation {
  return [[GIDSignIn sharedInstance] handleURL:url];
}

- (BOOL)GoogleSignInAppController:(UIApplication *)app
                          openURL:(NSURL *)url
                          options:(NSDictionary *)options {
  return [[GIDSignIn sharedInstance] handleURL:url];
}

@end
