const {
  preprocess,
  createEnv,
  readConfigFile
} = require("svelte-ts-preprocess");

const path = require('path');
const postcss = require('./postcss.config.js');
const autoPreprocess = require('svelte-preprocess')
const env = createEnv();
module.exports = {
  preprocess: [
    autoPreprocess({
      transformers: {
        postcss,
        scss: {
          sourceMap: true,
          includePaths: [
            path.resolve(__dirname, 'node_modules'),
            path.resolve(__dirname, 'src/styles')
          ]
        }
      }
    }),
   
    preprocess({
      env,
      compilerOptions: {
        ...readConfigFile(env),
        allowNonTsExtensions: true
      }
    }), 
  ]
};
