import { useEffect, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import {
  addUnityMessageListener,
  addUnityPlatformListener,
  getUnityHostPlatform,
  isUnityWebView,
  sendToUnity,
  type UnityHostPlatform,
} from './unityBridge';

/**
 * Demo panel for the Unity ↔ React bridge. Rendered only on web
 * (app/index.tsx gates it behind Platform.OS === 'web'); it is meaningful when
 * the web build is hosted inside a Unity WebView (gree/unity-webview).
 */
export default function UnityBridgeSection() {
  const [inUnity, setInUnity] = useState(isUnityWebView());
  const [host, setHost] = useState<UnityHostPlatform | null>(getUnityHostPlatform());
  const [log, setLog] = useState<string[]>([]);
  const [sendCount, setSendCount] = useState(0);

  const append = (msg: string) =>
    setLog((prev) => [`${new Date().toLocaleTimeString()}  ${msg}`, ...prev].slice(0, 20));

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
    <View style={styles.section}>
      <Text style={styles.sectionTitle}>6 · Unity bridge</Text>
      <View style={styles.statusRow}>
        <View style={[styles.dot, { backgroundColor: inUnity ? '#22c55e' : '#9ca3af' }]} />
        <Text style={styles.statusText}>
          {inUnity ? 'Running inside a Unity WebView' : 'Not inside Unity (plain browser)'}
        </Text>
      </View>
      {host && (
        <Text style={styles.hint}>
          Host: {host.os}
          {host.isEditor ? ' (editor)' : ''} · native notifications{' '}
          {host.nativeSupported ? 'available' : 'unavailable'}
        </Text>
      )}
      <Text style={styles.hint}>
        Sends window.Unity.call(json) to Unity; receives messages Unity pushes
        via EvaluateJS("window.onUnityMessage('…')").
      </Text>
      <Pressable
        style={({ pressed }) => [styles.button, pressed && styles.buttonPressed]}
        onPress={ping}
      >
        <Text style={styles.buttonText}>Send message to Unity</Text>
      </Pressable>
      {log.length === 0 ? (
        <Text style={styles.hint}>No bridge messages yet.</Text>
      ) : (
        log.map((line, i) => (
          <Text key={i} style={styles.logLine}>
            {line}
          </Text>
        ))
      )}
    </View>
  );
}

// Mirrors the section/button styling in app/index.tsx.
const styles = StyleSheet.create({
  section: {
    backgroundColor: '#f9fafb',
    borderRadius: 12,
    padding: 14,
    gap: 10,
    borderWidth: 1,
    borderColor: '#e5e7eb',
  },
  sectionTitle: { fontSize: 16, fontWeight: '600', color: '#111827' },
  button: {
    backgroundColor: '#5A31F4',
    borderRadius: 10,
    paddingVertical: 12,
    paddingHorizontal: 14,
  },
  buttonPressed: { opacity: 0.7 },
  buttonText: { color: 'white', fontWeight: '600', textAlign: 'center' },
  statusRow: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  dot: { width: 10, height: 10, borderRadius: 5 },
  statusText: { fontSize: 14, color: '#374151', flexShrink: 1 },
  hint: { color: '#6b7280', fontSize: 12, fontFamily: 'Courier' },
  logLine: { fontSize: 12, color: '#374151', fontFamily: 'Courier' },
});
