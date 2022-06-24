import babel from '@rollup/plugin-babel';
import resolve from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';
import replace from '@rollup/plugin-replace';
import image from '@rollup/plugin-image';
import { terser } from 'rollup-plugin-terser';
import postcss from 'rollup-plugin-postcss';

const dist = process.env.view_dist_path || '../dist/bundle.js';
const outputName = process.env.output_name || 'app';

export default {
  input: "index.js",
  output: {
    file: dist,
    format: "iife",
    name: outputName,
    sourcemap: false,
  },
  plugins: [
    image(),
    postcss({
      extensions: [".css"],
    }),
    resolve({
      moduleDirectories: ['node_modules', '../app~/node_modules'],
      browser: true,
      dedupe: ['svelte']
    }),
    replace({
      'process.env.NODE_ENV': JSON.stringify( 'development' )
    }),
    babel({
      presets: ["@babel/preset-react"],
    }),
    commonjs(),
    terser()
  ]
};
