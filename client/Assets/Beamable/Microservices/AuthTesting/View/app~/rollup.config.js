import svelte from 'rollup-plugin-svelte';
import commonjs from '@rollup/plugin-commonjs';
import resolve from '@rollup/plugin-node-resolve';
import { terser } from 'rollup-plugin-terser';
import sveltePreprocess from 'svelte-preprocess';
import typescript from '@rollup/plugin-typescript';

const production = true;
const dist = process.env.view_dist_path || '../dist/bundle.js';
const outputName = process.env.output_name || 'app';

export default {
	input: 'index.ts',
	output: {
		sourcemap: false,
		format: 'iife',
		name: outputName,
		file: dist
	},
	plugins: [
		svelte({
			emitCss: false,
			preprocess: sveltePreprocess({ sourceMap: !production }),
			compilerOptions: {
				// enable run-time checks when not in production
				dev: !production
			}
		}),

		resolve({
			moduleDirectories: ['node_modules', '../app~/node_modules'],
			browser: true,
			dedupe: ['svelte']
		}),
		commonjs(),
		typescript({
			sourceMap: !production,
			inlineSources: !production
		}),
		production && terser()
	]
};
