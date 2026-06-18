// Ambient declarations for modules that ship no types.
declare module 'react-native-url-polyfill/auto';
declare module '@ungap/structured-clone' {
  const structuredClone: <T>(value: T, options?: unknown) => T;
  export default structuredClone;
}
