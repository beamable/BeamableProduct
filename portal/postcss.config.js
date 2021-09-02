const cssnano      = require('cssnano');
const presetEnv    = require('postcss-preset-env');
const pluginImport = require('postcss-import');
const pluginUrl    = require('postcss-url');

const plugins = [
  pluginImport(),
  pluginUrl(),
  presetEnv({
    browsers: ['chrome >= 37']
  }),
  cssnano({
    autoprefixer: false,
    preset: ['default']
  })
];

module.exports = { plugins };
