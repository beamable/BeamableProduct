import { useState } from 'react';
import { useRouter } from 'expo-router';

import { detailsPath, detailsUrl, normalizeDeepLink, openUrl } from '../../src/linking/links';
import { useLogActions } from '../../src/state/logContext';
import { useNotifications } from '../../src/state/notificationContext';
import Button from '../../src/ui/Button';
import Field from '../../src/ui/Field';
import { Hint, Value } from '../../src/ui/Hint';
import Screen from '../../src/ui/Screen';
import Section from '../../src/ui/Section';

/**
 * Deep links tab: open a URL through the OS the way a push or an external app would, and see
 * what the native deeplink module captured.
 */
export default function DeepLinksTab() {
  const router = useRouter();
  const { append } = useLogActions();
  const { lastDeepLink, launchNotification, launchDeepLink } = useNotifications();
  const [url, setUrl] = useState(detailsUrl(42));

  const simulate = async () => {
    const target = detailsUrl(123);
    append(`Opening URL: ${target}`);
    await openUrl(target); // routed by the OS, like an external link/push
  };

  const openTyped = async () => {
    const resolved = normalizeDeepLink(url);
    if (!resolved) return append('Open URL: enter a URL or a bare details id first');
    append(`Opening URL: ${resolved}`);
    try {
      await openUrl(resolved);
    } catch (e) {
      append(`Open URL failed: ${e instanceof Error ? e.message : String(e)}`);
    }
  };

  return (
    <Screen>
      <Section title="Simulate">
        <Hint>
          "Simulate" hands the URL to the OS, which routes it back into the app exactly like an
          external link or a tapped push would — the full scheme → route mapping. "Navigate
          directly" skips the OS and pushes the route in-process.
        </Hint>
        <Button label="Simulate deep link → Details #123" onPress={simulate} />
        <Button
          label="Navigate directly → Details #55"
          variant="secondary"
          onPress={() => router.push(detailsPath(55) as never)}
        />
      </Section>

      <Section title="Open any URL">
        <Hint>
          Runs the value through normalizeDeepLink: a full URL opens as-is, while a bare value
          (e.g. "42") is treated as a details id — the same back-stop applied to remote pushes
          whose deeplink arrives schemeless.
        </Hint>
        <Field
          placeholder="beamrnsample://details/42  — or just 42"
          value={url}
          onChangeText={setUrl}
          autoCapitalize="none"
        />
        <Button label="Open URL" onPress={openTyped} />
      </Section>

      <Section title="Last received">
        <Hint>
          Captured by the native deeplink module (Android URL-scheme VIEW intents) and from the
          cold-start launch notification. expo-router already navigates for these, so the app
          only records them here.
        </Hint>
        {lastDeepLink ? (
          <>
            <Value label="Native deep link">{lastDeepLink.url}</Value>
            <Value label="Cold start">{lastDeepLink.isColdStart ? 'yes' : 'no'}</Value>
            <Value label="At">{lastDeepLink.at}</Value>
          </>
        ) : (
          <Hint>No native deep link captured yet.</Hint>
        )}
        {launchNotification ? (
          <Value label="Launch notification">{launchDeepLink ?? '(carried no deep link)'}</Value>
        ) : (
          <Hint>App was not launched from a notification.</Hint>
        )}
      </Section>

      <Section title="From a terminal">
        <Hint>
          xcrun simctl openurl booted "beamrnsample://details/42"{'\n'}
          adb shell am start -a android.intent.action.VIEW -d "beamrnsample://details/42"
          com.beamable.rnsample
        </Hint>
      </Section>
    </Screen>
  );
}
