import tsPlugin from '@typescript-eslint/eslint-plugin';
import tsParser from '@typescript-eslint/parser';
import js from '@eslint/js';
import globals from 'globals';

/** @type {import('eslint').Linter.FlatConfig[]} */
export default [
  {
    // NOTE: must use a trailing `/**` (or `/**/*`) so the directory itself and everything inside it are skipped.
    ignores: ['src/__generated__/**'],
  },
  js.configs.recommended,
  {
    files: ['**/*.ts'],
    languageOptions: {
      parser: tsParser,
      parserOptions: {
        project: './tsconfig.json',
        sourceType: 'module',
        tsconfigRootDir: import.meta.dirname,
      },
      globals: {
        ...globals.node,
        ...globals.browser,
        ...globals.es2017,
      },
    },
    plugins: {
      '@typescript-eslint': tsPlugin,
    },
    rules: {
      // turn off the JS version
      'no-unused-vars': 'off',
      'no-redeclare': 'off',
      // turn on the TS-aware replacement
      '@typescript-eslint/no-unused-vars': [
        'error',
        {
          /* optional fine-tuning */
          argsIgnorePattern: '^_', // ignore args you prefix with _
          ignoreRestSiblings: true,
        },
      ],
    },
  },
];
