// Ambient declarations for untyped modules the polyfills depend on.
declare module 'react-native-url-polyfill/auto';
declare module '@ungap/structured-clone' {
  const structuredClone: <T>(value: T) => T;
  export default structuredClone;
}
