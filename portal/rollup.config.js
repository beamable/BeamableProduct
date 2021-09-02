
import svelte from 'rollup-plugin-svelte';
import html from '@rollup/plugin-html';
import cleaner from 'rollup-plugin-cleaner';
import commonjs from '@rollup/plugin-commonjs';
import resolve from '@rollup/plugin-node-resolve';
import serve from 'rollup-plugin-serve'
import postcss from 'rollup-plugin-postcss'
import typescript from '@rollup/plugin-typescript';
import { terser } from "rollup-plugin-terser";
import copy from "rollup-plugin-copy-assets";
import replace from '@rollup/plugin-replace';
import {fileRouter} from 'svelte-filerouter';
import babel from '@rollup/plugin-babel';

const isProd = process.env.NODE_ENV === "production";
const isDev = process.env.NODE_ENV === "development";
const isTest = process.env.NODE_ENV === "test";
const publicPath = (process.env.BUILD_NUMBER === undefined) 
  ? ('/')
  : ('/' + process.env.BUILD_NUMBER + '/')
  
const {
  PORTAL_ENV   = 'dev'
} = process.env;

const outputDir = 'dist';

const plugins = [
  cleaner({
    targets: [outputDir]
  }),
  copy({
    assets: [
      "src/assets"
    ],
  }),

  replace({
    __buildEnv__: PORTAL_ENV
  }),

  fileRouter({
    unknownPropWarnings: true,
    dynamicImports: false,
  }),

  svelte({
    dev: isProd ? false : true,
    preprocess: require("./svelte.config.js").preprocess,
    emitCss: !isTest,
    onwarn: () => {}
  }),

  resolve({ 
    browser: true,
    extensions: [ '.mjs', '.js', '.json', '.ts', '.svelte', '.html', '.css', '.scss', '.sass' ],
    dedupe: ['svelte']
  }),

  commonjs({ include: "node_modules/**" }),

  postcss({
    extract: true,
    sourceMap: true,
    ...require('./postcss.config.js')
  }),

  typescript(),

  babel({
    sourceMaps: true,
    extensions: [ '.ts', '.js', '.mjs', '.html', '.svelte' ],
    exclude: [/\/core-js\//],
    presets: [
      ['@babel/preset-env', {
        loose: true,
        // debug: true,
        modules: false,
        useBuiltIns: 'usage',
        corejs: 3,
        targets: {
          chrome: 37
        }
      }]
    ]
  }),

  html({
    title: 'Beamable Portal',
    meta: [
      { charset: 'utf-8' },
      { 'http-equiv': 'X-UA-Compatible', content: 'IE=edge'},
      { name: 'viewport', content: 'initial-scale=1, width=device-width' }
    ],
    publicPath: publicPath,
    template: generateTemplate
  })
];

if (isDev) {
  plugins.push(
    serve({
      open: false,
      openPage: "/index.html",
      historyApiFallback: "/index.html",
      contentBase: [`./${outputDir}`]

    }),
  );
} else if (isProd) {
  plugins.push(terser());
}
module.exports = {
  input: "src/main.ts",
  inlineDynamicImports: true,
  output: {
    sourcemap: true,
    dir: outputDir,
    entryFileNames: '[name].[hash].js',
    format: "iife"
  },
  plugins
};

function generateTemplate ({ attributes, files, meta, publicPath, title }) {
  const makeHtmlAttributes = (attributes) => {
    if (!attributes) {
      return '';
    }
  
    const keys = Object.keys(attributes);
    // eslint-disable-next-line no-param-reassign
    return keys.reduce((result, key) => (result += ` ${key}="${attributes[key]}"`), '');
  };
  
  const scripts = (files.js || [])
    .map(({ fileName }) => {
      const attrs = makeHtmlAttributes(attributes.script);
      return `<script src="${publicPath}${fileName}"${attrs}></script>`;
    })
    .join('\n');

  const links = (files.css || [])
    .map(({ fileName }) => {
      const attrs = makeHtmlAttributes(attributes.link);
      return `<link href="${publicPath}${fileName}" rel="stylesheet"${attrs}>`;
    })
    .join('\n');

  const metas = meta
    .map((input) => {
      const attrs = makeHtmlAttributes(input);
      return `<meta${attrs}>`;
    })
    .join('\n');

  return `
<!doctype html>
<html${makeHtmlAttributes(attributes.html)}>
  <head>
    ${metas}
    <title>${title}</title>
    ${links}
    <link rel="stylesheet" href="//maxcdn.bootstrapcdn.com/font-awesome/4.7.0/css/font-awesome.min.css">
    <link rel="stylesheet" href="//fonts.googleapis.com/css?family=Questrial&display=swap">
    <link rel="stylesheet" href="//fonts.googleapis.com/css?family=Montserrat:600,700,800,900" />
    <link rel="stylesheet" href="//fonts.googleapis.com/css?family=Roboto:400,500" />
  </head>
  <body class="de-use de-my de-special de-sauce de-styles de-please">
    ${scripts}
  </body>
</html>`;
}