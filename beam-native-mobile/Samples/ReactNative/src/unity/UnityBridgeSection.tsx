import { useEffect, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';

import {
  addUnityMessageListener,
  addUnityPlatformListener,
  getUnityHostPlatform,
  isUnityWebView,
  sendToUnity,
  type UnityHostPlatform,
} from '@beamable/notifications-react-native';

import { useLogActions } from '../state/logContext';
import Button from '../ui/Button';
import { Hint, Value } from '../ui/Hint';
import Section from '../ui/Section';
import { colors, space } from '../ui/theme';

/**
 * Demo panel for the Unity ↔ React bridge. Rendered on the web-only Unity tab; it is
 * meaningful when the web build is hosted inside a Unity WebView (gree/unity-webview).
 *
 * Bridge traffic goes to the shared Activity log rather than a private list, so it interleaves
 * with everything else the app is doing.
 */
export default function UnityBridgeSection() {
  const { append } = useLogActions();
  const [inUnity, setInUnity] = useState(isUnityWebView());
  const [host, setHost] = useState<UnityHostPlatform | null>(getUnityHostPlatform());
  const [sendCount, setSendCount] = useState(0);

  useEffect(() => {
    // On iOS/macOS the window.Unity shim is injected by Unity after page load,
    // so re-check for a while instead of trusting the value at mount time.
    const interval = setInterval(() => setInUnity(isUnityWebView()), 500);
    const msgSub = addUnityMessageListener((msg) => append(`Unity → React: ${msg}`));
    const platformSub = addUnityPlatformListener(setHost);
    return () => {
      clearInterval(interval);
      msgSub.remove();
      platformSub.remove();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const ping = () => {
    const n = sendCount + 1;
    setSendCount(n);
    const message = JSON.stringify({ type: 'hello', from: 'react', n });
    if (sendToUnity(message)) {
      append(`React → Unity: ${message}`);
    } else {
      append('Not inside a Unity WebView — message not sent');
    }
  };

  return (
    <Section title="Unity bridge">
      <View style={styles.statusRow}>
        <View
          style={[
            styles.dot,
            { backgroundColor: inUnity ? colors.statusReady : colors.statusIdle },
          ]}
        />
        <Text style={styles.statusText}>
          {inUnity ? 'Running inside a Unity WebView' : 'Not inside Unity (plain browser)'}
        </Text>
      </View>
      {host && (
        <Value label="Host">
          {host.os}
          {host.isEditor ? ' (editor)' : ''} · native notifications{' '}
          {host.nativeSupported ? 'available' : 'unavailable'}
        </Value>
      )}
      <Hint>
        Sends window.Unity.call(json) to Unity; receives messages Unity pushes via
        EvaluateJS("window.onUnityMessage('…')"). Both directions land in the Activity log.
      </Hint>
      <Button label="Send message to Unity" onPress={ping} />
    </Section>
  );
}

const styles = StyleSheet.create({
  statusRow: { flexDirection: 'row', alignItems: 'center', gap: space.sm },
  dot: { width: 10, height: 10, borderRadius: 5 },
  statusText: { fontSize: 14, color: colors.inkSoft, flexShrink: 1 },
});
