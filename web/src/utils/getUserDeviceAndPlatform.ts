type DeviceType = 'Desktop' | 'Mobile' | 'Tablet';

/** Utility function to detect a user device type and platform type (browser or node). */
export function getUserDeviceAndPlatform(): {
  deviceType: DeviceType;
  platform: string;
} {
  if (typeof window === 'undefined' || typeof navigator === 'undefined') {
    return { deviceType: 'Desktop', platform: 'Node' };
  }

  const ua = navigator.userAgent;
  let deviceType: DeviceType = 'Desktop';

  // Modern API
  const uaData = (navigator as any).userAgentData;
  if (uaData?.mobile !== undefined) {
    deviceType = uaData.mobile ? 'Mobile' : 'Desktop';
  } else {
    // Fallback to old API
    const isiPadUA = /\b(iPad)\b/.test(ua);
    const isiPadTouchMac =
      /\bMacintosh\b/.test(ua) && navigator.maxTouchPoints > 1;
    if (isiPadUA || isiPadTouchMac) {
      deviceType = 'Tablet';
    } else if (/Android/i.test(ua)) {
      deviceType = /Mobile/i.test(ua) ? 'Mobile' : 'Tablet';
    } else if (/iPhone|iPod/i.test(ua)) {
      deviceType = 'Mobile';
    }
  }

  // Browser detection in order
  let platform = 'Web';
  if (/OPR|Opera/i.test(ua)) {
    platform = 'Opera';
  } else if (/Edg|Edge/i.test(ua)) {
    platform = 'Edge';
  } else if (/Chrome/i.test(ua) && !/Chromium/i.test(ua)) {
    platform = 'Chrome';
  } else if (/Firefox/i.test(ua)) {
    platform = 'Firefox';
  } else if (
    /Safari/i.test(ua) &&
    !/Chrome|Chromium|Edg|OPR|SamsungBrowser/i.test(ua)
  ) {
    platform = 'Safari';
  } else if (/SamsungBrowser/i.test(ua)) {
    platform = 'Samsung Browser';
  } else if (/MSIE|Trident/i.test(ua)) {
    platform = 'Internet Explorer';
  }

  return { deviceType, platform };
}
