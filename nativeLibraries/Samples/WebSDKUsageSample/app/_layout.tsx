// Load SDK polyfills as early as possible (from the RN SDK adapter package).
import '@beamable/sdk-react-native/polyfills';

import { Stack } from 'expo-router';

export default function RootLayout() {
  return (
    <Stack>
      <Stack.Screen name="index" options={{ title: 'Beamable Web SDK' }} />
    </Stack>
  );
}
