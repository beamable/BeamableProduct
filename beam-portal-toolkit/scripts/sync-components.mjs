#!/usr/bin/env node
/**
 * sync-components.mjs
 *
 * Regenerates from the CEM source:
 *   - web-types.json             (JetBrains IDE autocomplete)
 *   - src/globals.ts             (TypeScript HTMLElementTagNameMap augmentations)
 *   - src/svelte-elements.ts     (Svelte SvelteHTMLElements augmentations)
 *
 * By default also copies beam-components.json from the Portal repo into
 * custom-elements.json first. Pass --no-copy to skip that step and use the
 * existing custom-elements.json instead.
 *
 * Usage:
 *   pnpm sync-components            # copy from Portal, then regenerate
 *   pnpm sync-components --no-copy  # regenerate from existing custom-elements.json
 *
 * When copying, the Portal repo is expected to be a sibling of BeamableProduct:
 *   ~/Documents/Github/Portal/
 *
 * Override via env var if it lives elsewhere:
 *   PORTAL_REPO_PATH=/path/to/Portal pnpm sync-components
 */

import { readFileSync, writeFileSync, existsSync, mkdirSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const root = resolve(__dirname, '..');

const noCopy = process.argv.includes('--no-copy');

// ---------------------------------------------------------------------------
// Paths
// ---------------------------------------------------------------------------

const portalRepo =
  process.env.PORTAL_REPO_PATH ?? resolve(root, '../../Portal');
const cemSource = resolve(portalRepo, 'static/beam-components.json');
const cemDest = resolve(root, 'custom-elements.json');
const webTypesDest = resolve(root, 'src/generated/web-types.json');
const globalsDest = resolve(root, 'src/generated/globals.ts');
const svelteElementsDest = resolve(root, 'src/generated/svelte-elements.ts');

// ---------------------------------------------------------------------------
// Step 1: Read (and optionally copy) CEM
// ---------------------------------------------------------------------------

let cem;

if (noCopy) {
  if (!existsSync(cemDest)) {
    console.error(`\nERROR: custom-elements.json not found at:\n  ${cemDest}\n`);
    console.error('Run without --no-copy to fetch it from Portal first.');
    process.exit(1);
  }
  cem = JSON.parse(readFileSync(cemDest, 'utf8'));
  console.log(`✓ Using existing  custom-elements.json  (--no-copy)`);
} else {
  if (!existsSync(cemSource)) {
    console.error(`\nERROR: CEM file not found at:\n  ${cemSource}\n`);
    console.error(
      'Set the PORTAL_REPO_PATH env var to point to your Portal repo root,\n' +
      'or pass --no-copy to regenerate from the existing custom-elements.json.',
    );
    process.exit(1);
  }
  cem = JSON.parse(readFileSync(cemSource, 'utf8'));
  writeFileSync(cemDest, JSON.stringify(cem, null, 2) + '\n');
  console.log(`✓ Copied CEM  →  custom-elements.json`);
}

// ---------------------------------------------------------------------------
// Step 2: Collect declarations
// ---------------------------------------------------------------------------

/** @type {Array<{kind: string, tagName: string, name: string, description?: string, attributes?: Array<{name: string, description?: string, type?: {text: string}}>, members?: Array<{kind: string, name: string, type?: {text: string}}>}>} */
const declarations = cem.modules.flatMap((m) =>
  (m.declarations ?? []).filter(
    (d) => d.kind === 'custom-element' && d.tagName,
  ),
);

if (declarations.length === 0) {
  console.warn('⚠  No custom-element declarations found in CEM. Aborting.');
  process.exit(0);
}

// ---------------------------------------------------------------------------
// Step 3: Generate web-types.json
// ---------------------------------------------------------------------------

const pkgVersion = JSON.parse(
  readFileSync(resolve(root, 'package.json'), 'utf8'),
).version;

const webTypes = {
  $schema:
    'https://raw.githubusercontent.com/JetBrains/web-types/master/schema/web-types.json',
  name: '@beamable/portal-toolkit',
  version: pkgVersion,
  'js-types-syntax': 'typescript',
  'description-markup': 'markdown',
  contributions: {
    html: {
      elements: declarations.map((decl) => ({
        name: decl.tagName,
        description: decl.description ?? '',
        attributes: (decl.attributes ?? []).map((attr) => ({
          name: attr.name,
          description: attr.description ?? '',
          value: {
            type: attr.type?.text ?? 'any',
          },
        })),
      })),
    },
  },
};

writeFileSync(webTypesDest, JSON.stringify(webTypes, null, 2) + '\n');
console.log(
  `✓ Generated   →  src/generated/web-types.json  (${declarations.length} components)`,
);

// ---------------------------------------------------------------------------
// Step 4: Generate src/globals.ts
// ---------------------------------------------------------------------------

/**
 * Convert a CEM type text string to a TypeScript type string.
 * CEM type.text is usually already valid TS, but a few common names differ.
 */
function toTsType(typeText) {
  if (!typeText) return 'unknown';
  const normalized = typeText.trim();
  const overrides = {
    array: 'unknown[]',
    Array: 'unknown[]',
    object: 'Record<string, unknown>',
    Object: 'Record<string, unknown>',
  };
  return overrides[normalized] ?? normalized;
}

/** 'beam-some-thing' → 'BeamSomeThing' */
function toPascalCase(tagName) {
  return tagName
    .split('-')
    .map((s) => s.charAt(0).toUpperCase() + s.slice(1))
    .join('');
}

/** 'beam-btn' → 'BeamBtnElement' */
function toIfaceName(tagName) {
  return `${toPascalCase(tagName)}Element`;
}

// HTMLElementTagNameMap entries
const tagMapLines = declarations
  .map((d) => `    '${d.tagName}': ${toIfaceName(d.tagName)};`)
  .join('\n');

// Per-element interface blocks (use members for JS property access)
const ifaceBlocks = declarations.map((decl) => {
  const members = (decl.members ?? [])
    .filter((m) => m.kind === 'field' && m.name)
    .map((m) => `    ${m.name}?: ${toTsType(m.type?.text)};`)
    .join('\n');

  const body = members || '    // No public properties defined in CEM.';
  return `  interface ${toIfaceName(decl.tagName)} extends HTMLElement {\n${body}\n  }`;
});

const globalsTs = `// AUTO-GENERATED — do not edit manually.
// Run \`pnpm sync-components\` to regenerate from Portal's beam-components.json.

export {};

declare global {
  // ---------------------------------------------------------------------------
  // Tag → element type mapping.
  // Gives TypeScript callers correct types for:
  //   document.querySelector('beam-btn')  →  BeamBtnElement
  //   document.createElement('beam-btn')  →  BeamBtnElement
  // ---------------------------------------------------------------------------
  interface HTMLElementTagNameMap {
${tagMapLines}
  }

  // ---------------------------------------------------------------------------
  // Per-element interfaces
  // ---------------------------------------------------------------------------
${ifaceBlocks.join('\n\n')}
}
`;

mkdirSync(resolve(root, 'src/generated'), { recursive: true });
writeFileSync(globalsDest, globalsTs);
console.log(`✓ Generated   →  src/generated/globals.ts`);

// ---------------------------------------------------------------------------
// Step 5: Generate src/generated/svelte-elements.ts
// ---------------------------------------------------------------------------

// Use attributes (kebab-case) for Svelte since templates use HTML attribute names.
const svelteElementEntries = declarations.map((decl) => {
  const attrs = (decl.attributes ?? [])
    .map((a) => {
      const key = a.name.includes('-') ? `'${a.name}'` : a.name;
      return `      ${key}?: ${toTsType(a.type?.text)};`;
    })
    .join('\n');

  const body = attrs || '      // No attributes defined in CEM.';
  return `    '${decl.tagName}': import('svelte/elements').HTMLAttributes<HTMLElement> & {\n${body}\n    };`;
});

const svelteElementsTs = `// AUTO-GENERATED — do not edit manually.
// Run \`pnpm sync-components\` to regenerate from Portal's beam-components.json.
//
// Augments Svelte's element type map so .svelte templates get autocomplete
// for Beamable web components.
//
// Add one line to your project's app.d.ts (or any .d.ts file):
//   /// <reference types="@beamable/portal-toolkit/svelte" />

declare module 'svelte/elements' {
  interface SvelteHTMLElements {
${svelteElementEntries.join('\n\n')}
  }
}

export {};
`;

writeFileSync(svelteElementsDest, svelteElementsTs);
console.log(`✓ Generated   →  src/generated/svelte-elements.ts`);

console.log(`\nDone. Run \`pnpm build\` to compile the updated types.\n`);
