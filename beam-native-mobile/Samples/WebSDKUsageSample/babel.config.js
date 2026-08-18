module.exports = function (api) {
  api.cache(true);
  return {
    presets: ['babel-preset-expo'],
    // The Beamable SDK's bundled build ships an ES2022 static class block;
    // transform it so Metro/Hermes can consume the SDK.
    plugins: ['@babel/plugin-transform-class-static-block'],
  };
};
