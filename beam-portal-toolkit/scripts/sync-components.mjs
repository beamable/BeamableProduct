#!/usr/bin/env node
/**
 * sync-components.mjs
 *
 * Regenerates from the CEM source:
 *   - web-types.json             (JetBrains IDE autocomplete)
 *   - src/globals.ts             (TypeScript HTMLElementTagNameMap augmentations)
 *   - src/svelte-elements.ts     (Svelte SvelteHTMLElements augmentations)
 *   - src/react-elements.ts      (React JSX IntrinsicElements augmentations)
 *
 * By default also copies custom-elements.json from the agentic-portal repo
 * into this project's custom-elements.json first. Pass --no-copy to skip that
 * step and use the existing custom-elements.json instead.
 *
 * Run `npm run generate:cem` in the agentic-portal repo first to refresh its
 * manifest before syncing.
 *
 * Usage:
 *   pnpm sync-components            # copy from agentic-portal, then regenerate
 *   pnpm sync-components --no-copy  # regenerate from existing custom-elements.json
 *
 * When copying, the agentic-portal repo is expected to be a sibling of
 * BeamableProduct:
 *   ~/Documents/Github/agentic-portal/
 *
 * Override via env var if it lives elsewhere:
 *   PORTAL_REPO_PATH=/path/to/agentic-portal pnpm sync-components
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
  process.env.PORTAL_REPO_PATH ?? resolve(root, '../../agentic-portal');
const cemSource = resolve(portalRepo, 'custom-elements.json');
const cemDest = resolve(root, 'custom-elements.json');
const webTypesDest = resolve(root, 'src/generated/web-types.json');
const globalsDest = resolve(root, 'src/generated/globals.ts');
const svelteElementsDest = resolve(root, 'src/generated/svelte-elements.ts');
const reactElementsDest = resolve(root, 'src/generated/react-elements.ts');
const reactComponentsDest = resolve(root, 'src/generated/react-components.ts');

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
      'Run `npm run generate:cem` in the agentic-portal repo to produce it,\n' +
      'set the PORTAL_REPO_PATH env var to point to your agentic-portal repo root,\n' +
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
// agentic-portal's CEM is produced by @custom-elements-manifest/analyzer, which
// emits `kind: "class"` with a `tagName` for custom elements. The old Portal
// repo emitted `kind: "custom-element"`. Filter on `tagName` so both shapes work.
const declarations = cem.modules.flatMap((m) =>
  (m.declarations ?? []).filter((d) => d.tagName),
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
 * Identifiers we trust to resolve at the toolkit's compile time without an
 * import (TS built-ins + lib.dom). Any identifier outside this set in a CEM
 * type string would dangle, so we fall back to `unknown` for the whole type.
 *
 * This is conservative on purpose: consumers get `unknown` (still type-safe,
 * still permits assignment from inferred values) instead of a hard TS error.
 */
const SAFE_TYPE_IDENTS = new Set([
  // primitives + special forms
  'string', 'number', 'boolean', 'bigint', 'symbol',
  'null', 'undefined', 'void', 'any', 'unknown', 'never', 'object',
  'true', 'false',
  // structural built-ins
  'Array', 'ReadonlyArray', 'Record', 'Map', 'ReadonlyMap', 'Set', 'ReadonlySet',
  'WeakMap', 'WeakSet', 'Promise', 'Partial', 'Required', 'Readonly', 'Pick',
  'Omit', 'Exclude', 'Extract', 'NonNullable', 'Date', 'RegExp', 'Error',
  // DOM
  'HTMLElement', 'HTMLInputElement', 'HTMLTextAreaElement', 'HTMLSelectElement',
  'HTMLFormElement', 'HTMLTemplateElement', 'HTMLImageElement', 'HTMLAnchorElement',
  'HTMLButtonElement', 'HTMLDivElement', 'HTMLSpanElement', 'HTMLLabelElement',
  'SVGElement', 'Element', 'Node', 'Event', 'CustomEvent', 'EventTarget',
  'ElementInternals', 'CustomStateSet', 'NodeList', 'NodeListOf',
  'HTMLCollection', 'DOMTokenList', 'ShadowRoot', 'File', 'FileList', 'Blob',
]);

/**
 * Convert a CEM type text string to a safe TypeScript type string. If the
 * type references any identifier we don't have visibility into (component
 * internals like IconAnimation, Lit's CSSResultGroup, etc.), fall back to
 * `unknown` so the generated declaration still compiles.
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
  if (overrides[normalized]) return overrides[normalized];

  // Strip string/number literals before scanning for identifiers — literals
  // happen to spell things like `'large'` that look like idents to a regex.
  const stripped = normalized.replace(/'[^']*'|"[^"]*"|`[^`]*`|-?\d+(?:\.\d+)?/g, '');
  const idents = stripped.match(/[A-Za-z_$][A-Za-z0-9_$]*/g) || [];
  for (const id of idents) {
    if (!SAFE_TYPE_IDENTS.has(id)) return 'unknown';
  }
  return normalized;
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

// ---------------------------------------------------------------------------
// JSDoc helpers — turn CEM metadata into /** … */ blocks for generated types.
// ---------------------------------------------------------------------------

function escapeJsDoc(text) {
  if (!text) return '';
  return text.replace(/\*\//g, '*\\/');
}

/**
 * @param {string | undefined} description
 * @param {string | undefined} defaultValue
 * @param {string} indent
 */
function formatPropJsDoc(description, defaultValue, indent = '  ') {
  const desc = escapeJsDoc(description?.trim() ?? '');
  const def = defaultValue?.trim();
  if (!desc && !def) return '';

  const parts = [];
  if (desc) parts.push(desc.replace(/\n\s*/g, ' '));
  if (def) parts.push(`@default ${def}`);

  const oneLiner = parts.join(' ');
  if (oneLiner.length <= 100) {
    return `${indent}/** ${oneLiner} */\n`;
  }
  const lines = parts.map((p) => `${indent} * ${p}`);
  return `${indent}/**\n${lines.join('\n')}\n${indent} */\n`;
}

/** @param {object} decl  @param {string} indent */
function formatComponentJsDoc(decl, indent = '') {
  const lines = [];

  const desc = escapeJsDoc(decl.description?.trim() ?? '');
  if (desc && !desc.startsWith('<')) {
    lines.push(`${indent} * ${desc.replace(/\n\s*/g, ' ')}`);
    lines.push(`${indent} *`);
  }

  for (const slot of decl.slots ?? []) {
    const d = escapeJsDoc(slot.description?.trim() ?? '');
    const n = slot.name || '(default)';
    lines.push(d ? `${indent} * @slot ${n} - ${d.replace(/\n\s*/g, ' ')}` : `${indent} * @slot ${n}`);
  }

  for (const part of decl.cssParts ?? []) {
    const d = escapeJsDoc(part.description?.trim() ?? '');
    lines.push(d ? `${indent} * @csspart ${part.name} - ${d.replace(/\n\s*/g, ' ')}` : `${indent} * @csspart ${part.name}`);
  }

  for (const prop of decl.cssProperties ?? []) {
    const d = escapeJsDoc(prop.description?.trim() ?? '');
    const defHint = prop.default ? ` [default: ${prop.default}]` : '';
    lines.push(d ? `${indent} * @cssproperty ${prop.name} - ${d.replace(/\n\s*/g, ' ')}${defHint}` : `${indent} * @cssproperty ${prop.name}${defHint}`);
  }

  for (const evt of decl.events ?? []) {
    if (!evt.name.startsWith('wa-')) continue;
    const d = escapeJsDoc(evt.description?.trim() ?? '');
    const reactHint = evt.reactName ? ` (React: \`${evt.reactName}\`)` : '';
    lines.push(d ? `${indent} * @event ${evt.name} - ${d.replace(/\n\s*/g, ' ')}${reactHint}` : `${indent} * @event ${evt.name}${reactHint}`);
  }

  if (lines.length === 0) return '';
  return `${indent}/**\n${lines.join('\n')}\n${indent} */\n`;
}

// HTMLElementTagNameMap entries
const tagMapLines = declarations
  .map((d) => `    '${d.tagName}': ${toIfaceName(d.tagName)};`)
  .join('\n');

// Names that are already declared on HTMLElement (or on lib.dom mixins it
// pulls in). Re-declaring them as `?: T` produces `T | undefined`, which is
// not assignable to the stricter base type and breaks `extends HTMLElement`.
// We rely on the inherited declarations instead.
const HTML_ELEMENT_INHERITED = new Set([
  'id', 'className', 'slot', 'style', 'title', 'lang', 'dir', 'hidden',
  'tabIndex', 'role', 'inert', 'accessKey', 'draggable', 'spellcheck',
  'translate', 'contentEditable', 'enterKeyHint', 'inputMode', 'autocapitalize',
  'autocorrect', 'autofocus', 'popover', 'nonce',
]);

// Names that are technically public on the underlying class (often inherited
// from WaElement / Lit base), but we don't want to advertise them in the
// generated `BeamFooElement` interfaces — they're framework plumbing, not
// part of the component's user-facing API.
//
// Add to this set when you spot another field leaking into globals.ts that
// shouldn't be in the toolkit's public surface. These are quiet skips: they
// don't fix any error, they just keep the generated types honest.
const WA_INTERNAL_LEAKAGE = new Set([
  'internals',                  // ElementInternals — set by WA for form association
  'initialReflectedProperties', // Lit reactive-property bookkeeping
  'didSSR',                     // SSR marker flag
  'hasSlotController',          // WA slot-detection helper
  'validators',                 // WA form-validation helpers
  'allValidators',
  'assumeInteractionOn',
  'valueHasChanged',
  'hasInteracted',
  'emittedEvents',
  'states',                     // CustomStateSet — exposed via internals.states
  'emitInvalid',
  'formAssociated',
  'shadowRootOptions',
]);

// Per-element interface blocks (use members for JS property access).
// Filter to public, instance, non-internal fields:
//   - kind=field (drop methods — interface methods would need full signatures)
//   - not static (class-level, not an instance prop)
//   - not private/protected (not part of the public surface)
//   - name doesn't start with `#` (private fields are TS-syntax-illegal in an interface)
//   - name doesn't start with `_` (toolkit convention for internal state)
//   - name not inherited from HTMLElement (would conflict on extends)
//   - name not in WA_INTERNAL_LEAKAGE (framework plumbing we don't broadcast)
//   - type is something more useful than bare `undefined`
const ifaceBlocks = declarations.map((decl) => {
  const members = (decl.members ?? [])
    .filter(
      (m) =>
        m.kind === 'field' &&
        m.name &&
        !m.static &&
        m.privacy !== 'private' &&
        m.privacy !== 'protected' &&
        !m.name.startsWith('#') &&
        !m.name.startsWith('_') &&
        !HTML_ELEMENT_INHERITED.has(m.name) &&
        !WA_INTERNAL_LEAKAGE.has(m.name) &&
        m.type?.text?.trim() !== 'undefined',
    )
    .map((m) => {
      const doc = formatPropJsDoc(m.description, m.default, '    ');
      return `${doc}    ${m.name}?: ${toTsType(m.type?.text)};`;
    })
    .join('\n');

  const body = members || '    // No public properties defined in CEM.';
  const ifaceDoc = formatComponentJsDoc(decl, '  ');
  return `${ifaceDoc}  interface ${toIfaceName(decl.tagName)} extends HTMLElement {\n${body}\n  }`;
});

const globalsTs = `// AUTO-GENERATED — do not edit manually.
// Run \`pnpm sync-components\` to regenerate from agentic-portal's custom-elements.json.

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
      const doc = formatPropJsDoc(a.description, a.default, '      ');
      return `${doc}      ${key}?: ${toTsType(a.type?.text)};`;
    })
    .join('\n');

  const body = attrs || '      // No attributes defined in CEM.';
  const entryDoc = formatComponentJsDoc(decl, '    ');
  return `${entryDoc}    '${decl.tagName}': import('svelte/elements').HTMLAttributes<HTMLElement> & {\n${body}\n    };`;
});

const svelteElementsTs = `// AUTO-GENERATED — do not edit manually.
// Run \`pnpm sync-components\` to regenerate from agentic-portal's custom-elements.json.
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

// ---------------------------------------------------------------------------
// Step 6: Generate src/generated/react-elements.ts
// ---------------------------------------------------------------------------

// Same attribute set as Svelte (kebab-case attribute names), but typed against
// React's HTMLAttributes/DetailedHTMLProps and surfaced via JSX.IntrinsicElements.
const reactElementEntries = declarations.map((decl) => {
  const attrs = (decl.attributes ?? [])
    .map((a) => {
      const key = a.name.includes('-') ? `'${a.name}'` : a.name;
      const doc = formatPropJsDoc(a.description, a.default, '      ');
      return `${doc}      ${key}?: ${toTsType(a.type?.text)};`;
    })
    .join('\n');

  const body = attrs || '      // No attributes defined in CEM.';
  const entryDoc = formatComponentJsDoc(decl, '    ');
  return `${entryDoc}    '${decl.tagName}': import('react').DetailedHTMLProps<import('react').HTMLAttributes<HTMLElement>, HTMLElement> & {\n${body}\n    };`;
});

const reactElementsTs = `// AUTO-GENERATED — do not edit manually.
// Run \`pnpm sync-components\` to regenerate from agentic-portal's custom-elements.json.
//
// Augments React's JSX.IntrinsicElements so .tsx files get autocomplete and
// per-attribute typing for Beamable web components.
//
// Add one line to your project's app.d.ts (or any .d.ts file):
//   /// <reference types="@beamable/portal-toolkit/react" />
//
// React 19 hosts the JSX namespace inside the \`react\` module rather than on
// the global \`JSX\` namespace, so we augment via \`declare module 'react'\`.

declare module 'react' {
  // eslint-disable-next-line @typescript-eslint/no-namespace
  namespace JSX {
    interface IntrinsicElements {
${reactElementEntries.join('\n\n')}
    }
  }
}

export {};
`;

writeFileSync(reactElementsDest, reactElementsTs);
console.log(`✓ Generated   →  src/generated/react-elements.ts`);

// ---------------------------------------------------------------------------
// Step 7: Generate src/generated/react-components.ts
// ---------------------------------------------------------------------------
//
// Real React forwarder components — one per CEM custom element except the
// ones we hand-write (BeamTable; see `react-custom.ts`). Each forwarder looks
// the host's `Beam*` component up off `window.__beamPortal.react` at render
// time and returns `createElement(host, { ...props, ref })`.
//
// Prop shape: each component emits a `BeamFooProps` interface that mirrors
// the `react-elements.d.ts` attribute set + standard React HTMLAttributes.
// This is the same surface authors get today by writing `<beam-foo …>` raw,
// so migrating to `<BeamFoo …>` is a name change only.

/** Components that have hand-written wrappers in `react-custom.ts`. */
const REACT_HANDWRITTEN = new Set(['beam-table']);

/** Bindable components — input-like components whose React wrappers
 *  surface typed `onValueChange` / `onCheckedChange` callbacks for ergonomic
 *  controlled-component usage. We generate a separate `react-bindable.ts`
 *  file for these: same attribute shape as the regular auto-generated
 *  forwarders, plus the bindable callbacks. */
const REACT_BINDABLE = {
  'beam-input':        { mode: 'value',   valueType: 'string', hasInput: true },
  'beam-textarea':     { mode: 'value',   valueType: 'string', hasInput: true },
  'beam-select':       { mode: 'value',   valueType: 'string', hasInput: false },
  'beam-checkbox':     { mode: 'checked' },
  'beam-switch':       { mode: 'checked' },
  'beam-radio-group':  { mode: 'value',   valueType: 'string', hasInput: false },
  'beam-slider':       { mode: 'value',   valueType: 'number', hasInput: true },
  'beam-rating':       { mode: 'value',   valueType: 'number', hasInput: false },
  'beam-color-picker': { mode: 'value',   valueType: 'string', hasInput: true },
  'beam-number-input': { mode: 'value',   valueType: 'number', hasInput: true },
};
const REACT_BINDABLE_TAGS = new Set(Object.keys(REACT_BINDABLE));

/** 'beam-some-thing' → 'BeamSomeThing' */
function toReactName(tagName) {
  return toPascalCase(tagName);
}

const reactComponentDeclarations = declarations.filter(
  (d) => !REACT_HANDWRITTEN.has(d.tagName) && !REACT_BINDABLE_TAGS.has(d.tagName),
);
const reactBindableDeclarations = declarations.filter((d) => REACT_BINDABLE_TAGS.has(d.tagName));

const reactComponentEntries = reactComponentDeclarations.map((decl) => {
  const propsName = `${toReactName(decl.tagName)}Props`;
  const componentName = toReactName(decl.tagName);

  const attrs = (decl.attributes ?? [])
    .map((a) => {
      const key = a.name.includes('-') ? `'${a.name}'` : a.name;
      const doc = formatPropJsDoc(a.description, a.default, '  ');
      return `${doc}  ${key}?: ${toTsType(a.type?.text)};`;
    })
    .join('\n');

  const propsBody = attrs || '  // No attributes defined in CEM.';
  const ifaceDoc = formatComponentJsDoc(decl);
  const funcDoc = `/** React forwarder for \`<${decl.tagName}>\`. */\n`;

  return `${ifaceDoc}export interface ${propsName} extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
${propsBody}
}

${funcDoc}export function ${componentName}(props: ${propsName} & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('${componentName}'), props);
}
${componentName}.displayName = '${componentName}';`;
});

const reactComponentsTs = `// AUTO-GENERATED — do not edit manually.
// Run \`pnpm sync-components\` to regenerate from agentic-portal's custom-elements.json.
//
// React forwarder components for every \`beam-*\` web element. Each one
// resolves the host portal's real implementation at render time via
// \`window.__beamPortal.react.<Name>\` and renders it with the forwarded
// props + ref. The toolkit ships only the forwarder shell; the host owns
// the implementation, so a bugfix in the host doesn't require republishing
// the toolkit or rebuilding every extension.
//
// React 19's ref-as-prop semantics let us use plain function components
// instead of \`forwardRef\` — that keeps the dts bundler from collapsing
// each component to \`any\`.

import { createElement, type DetailedHTMLProps, type HTMLAttributes, type ReactElement, type Ref } from 'react';
import { hostComponent } from './react-host';

${reactComponentEntries.join('\n\n')}
`;

writeFileSync(reactComponentsDest, reactComponentsTs);
console.log(
  `✓ Generated   →  src/generated/react-components.ts  (${reactComponentDeclarations.length} components)`,
);

// ---------------------------------------------------------------------------
// Step 8: Generate src/generated/react-bindable.ts
// ---------------------------------------------------------------------------
//
// Same forwarder pattern as `react-components.ts`, but for input-like
// components — adds typed `onValueChange` / `onValueInput` (value-bound) or
// `onCheckedChange` (checked-bound) callbacks alongside the standard
// `onChange` / `onInput`. The host's wrapper (see the portal's
// `beam-components/react.ts`) does the actual event interception and
// value extraction; the toolkit forwarder only needs to declare the
// callbacks in its prop type so authors get IntelliSense.

const reactBindableDest = resolve(root, 'src/generated/react-bindable.ts');

const reactBindableEntries = reactBindableDeclarations.map((decl) => {
  const meta = REACT_BINDABLE[decl.tagName];
  const propsName = `${toReactName(decl.tagName)}Props`;
  const componentName = toReactName(decl.tagName);

  const attrs = (decl.attributes ?? [])
    .map((a) => {
      const key = a.name.includes('-') ? `'${a.name}'` : a.name;
      const doc = formatPropJsDoc(a.description, a.default, '  ');
      return `${doc}  ${key}?: ${toTsType(a.type?.text)};`;
    })
    .join('\n');

  const attrsBody = attrs || '  // No attributes defined in CEM.';

  const callbacks = [];
  if (meta.mode === 'value') {
    callbacks.push(`  /** Fired on commit (change). Receives the typed value. */`);
    callbacks.push(`  onValueChange?: (value: ${meta.valueType}) => void;`);
    if (meta.hasInput) {
      callbacks.push(`  /** Fired continuously while the user edits. Receives the typed value. */`);
      callbacks.push(`  onValueInput?: (value: ${meta.valueType}) => void;`);
    }
  } else {
    callbacks.push(`  /** Fired when the checked state toggles. Receives the new boolean. */`);
    callbacks.push(`  onCheckedChange?: (checked: boolean) => void;`);
  }
  const callbackBlock = callbacks.join('\n');

  const ifaceDoc = formatComponentJsDoc(decl);
  const funcDoc = `/** React forwarder for \`<${decl.tagName}>\` with controlled-component bindings. */\n`;

  return `${ifaceDoc}export interface ${propsName} extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
${attrsBody}
${callbackBlock}
}

${funcDoc}export function ${componentName}(props: ${propsName} & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('${componentName}'), props);
}
${componentName}.displayName = '${componentName}';`;
});

const reactBindableTs = `// AUTO-GENERATED — do not edit manually.
// Run \`pnpm sync-components\` to regenerate from agentic-portal's custom-elements.json.
//
// React forwarder components for input-like \`beam-*\` elements that support
// controlled-component binding. Same pattern as \`react-components.ts\`, plus
// typed value callbacks:
//
//   <BeamInput     value={x} onValueChange={setX} />   // (value: string) => void
//   <BeamSlider    value={x} onValueChange={setX} />   // (value: number) => void
//   <BeamCheckbox  checked={x} onCheckedChange={setX} /> // (checked: boolean) => void
//   <BeamNumberInput value={x} onValueChange={setX} /> // empty → NaN
//
// The standard \`onChange\` / \`onInput\` still works and fires alongside —
// these callbacks are purely additive.

import { createElement, type DetailedHTMLProps, type HTMLAttributes, type ReactElement, type Ref } from 'react';
import { hostComponent } from './react-host';

${reactBindableEntries.join('\n\n')}
`;

writeFileSync(reactBindableDest, reactBindableTs);
console.log(
  `✓ Generated   →  src/generated/react-bindable.ts   (${reactBindableDeclarations.length} components)`,
);

console.log(`\nDone. Run \`pnpm build\` to compile the updated types.\n`);
