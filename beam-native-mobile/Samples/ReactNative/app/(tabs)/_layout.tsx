import { useMemo } from 'react';
import { Platform, StyleSheet, View, type ColorValue } from 'react-native';
import { Tabs } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { SafeAreaInsetsContext, useSafeAreaInsets } from 'react-native-safe-area-context';
import Ionicons from '@expo/vector-icons/Ionicons';

import ConnectionBar from '../../src/ui/ConnectionBar';
import DebugConsole from '../../src/ui/DebugConsole';
import { colors } from '../../src/ui/theme';

type IconName = React.ComponentProps<typeof Ionicons>['name'];
/** What react-navigation hands `tabBarIcon`. `color` is a ColorValue, not a plain string. */
type TabIconProps = { focused: boolean; color: ColorValue; size: number };

/**
 * The tab shell.
 *
 * `ConnectionBar` and `DebugConsole` wrap the navigator as flow siblings rather than living
 * inside each page: the status strip is visible from every tab, and opening the console
 * shrinks the active page instead of overlapping it. Both sit outside the root Stack's
 * Details route, which stays a clean full screen.
 *
 * Because those two own the screen's top and bottom edges, THEY apply the safe-area insets —
 * so the navigator subtree is handed zeroed top/bottom insets. Without this the tab header
 * would add a second status-bar's worth of padding and the tab bar would reserve gesture-bar
 * space it no longer sits against. Left/right are passed through for a landscape notch.
 */
export default function TabsLayout() {
  const insets = useSafeAreaInsets();
  const navInsets = useMemo(() => ({ ...insets, top: 0, bottom: 0 }), [insets]);

  return (
    <View style={styles.root}>
      {/* `dark` (dark icons), not `auto`. `auto` derives the icon colour from the system colour
          scheme, so on a device in dark mode it renders WHITE status-bar icons — invisible
          against the ConnectionBar's near-white background, which now paints that strip via the
          safe-area inset. This app's chrome is always light, so the icons must always be dark. */}
      <StatusBar style="dark" />
      <ConnectionBar />
      <SafeAreaInsetsContext.Provider value={navInsets}>
        <Tabs
          screenOptions={{
            tabBarActiveTintColor: colors.primary,
            tabBarInactiveTintColor: colors.mutedSoft,
            tabBarStyle: styles.tabBar,
            tabBarLabelStyle: styles.tabLabel,
          }}
        >
          {/* Tab-bar order follows the order of these children. Push is the `index` route so
              the first tab is also where the app lands on launch. */}
          <Tabs.Screen
            name="index"
            options={{ title: 'Push', tabBarIcon: icon('notifications-outline') }}
          />
          <Tabs.Screen
            name="deeplinks"
            options={{ title: 'Deep links', tabBarIcon: icon('link-outline') }}
          />
          <Tabs.Screen
            name="inbox"
            options={{ title: 'In-game', tabBarIcon: icon('file-tray-outline') }}
          />
          <Tabs.Screen
            name="email"
            options={{ title: 'Email', tabBarIcon: icon('mail-outline') }}
          />
          <Tabs.Screen
            name="analytics"
            options={{ title: 'Analytics', tabBarIcon: icon('stats-chart-outline') }}
          />
          {/* The Unity bridge is only meaningful when the web build is hosted inside a Unity
              WebView. `href: null` hides the tab entirely on native (expo-router turns a null
              href into a hidden tab item + a null tab button). */}
          <Tabs.Screen
            name="unity"
            options={{
              title: 'Unity',
              tabBarIcon: icon('cube-outline'),
              href: Platform.OS === 'web' ? '/unity' : null,
            }}
          />
        </Tabs>
      </SafeAreaInsetsContext.Provider>
      <DebugConsole />
    </View>
  );
}

function icon(name: IconName) {
  return ({ color, size }: TabIconProps) => (
    <Ionicons name={name} color={color} size={size} />
  );
}

const styles = StyleSheet.create({
  root: { flex: 1 },
  tabBar: { borderTopColor: colors.surfaceBorder },
  tabLabel: { fontSize: 11 },
});
