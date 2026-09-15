import AsyncStorage from '@react-native-async-storage/async-storage';

interface Config {
  cid: string;
  pid: string;
}

/**
 * Reads the Beamable configuration from storage.
 * In React Native environments, it reads from AsyncStorage.
 */
export async function readConfigReactNative(): Promise<Config> {
  const [cid, pid] = await Promise.all([
    AsyncStorage.getItem('beam_cid'),
    AsyncStorage.getItem('beam_pid'),
  ]);
  return {
    cid: cid ?? '',
    pid: pid ?? '',
  };
}

/**
 * Saves the Beamable configuration to storage.
 * In React Native environments, it saves to AsyncStorage.
 */
export async function saveConfigReactNative({
  cid,
  pid,
}: Config): Promise<void> {
  await AsyncStorage.multiSet([
    ['beam_cid', cid],
    ['beam_pid', pid],
  ]);
}
