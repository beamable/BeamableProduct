import UnityBridgeSection from '../../src/unity/UnityBridgeSection';
import Screen from '../../src/ui/Screen';

/**
 * Unity tab — only reachable on web (the tab is hidden on native via `href: null` in the tabs
 * layout). Meaningful when the web build is hosted inside a Unity WebView.
 */
export default function UnityTab() {
  return (
    <Screen>
      <UnityBridgeSection />
    </Screen>
  );
}
