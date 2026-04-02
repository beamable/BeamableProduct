// AUTO-GENERATED — do not edit manually.
// Run `pnpm sync-components` to regenerate from Portal's beam-components.json.
//
// Augments Svelte's element type map so .svelte templates get autocomplete
// for Beamable web components.
//
// Add one line to your project's app.d.ts (or any .d.ts file):
//   /// <reference types="@beamable/portal-toolkit/svelte" />

declare module 'svelte/elements' {
  interface SvelteHTMLElements {
    'beam-btn': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      color?: string;
      outlined?: boolean;
      text?: boolean;
      block?: boolean;
      disabled?: boolean;
      small?: boolean;
      large?: boolean;
      'x-small'?: boolean;
      'x-large'?: boolean;
      icon?: boolean;
      fab?: boolean;
      tile?: boolean;
      rounded?: boolean;
      depressed?: boolean;
      dark?: boolean;
      light?: boolean;
      loading?: boolean;
      href?: string;
      target?: string;
      type?: string;
      elevation?: string;
    };

    'beam-icon': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      icon?: string;
      color?: string;
      small?: boolean;
      large?: boolean;
      'x-small'?: boolean;
      'x-large'?: boolean;
      dense?: boolean;
      disabled?: boolean;
      dark?: boolean;
      light?: boolean;
      left?: boolean;
      right?: boolean;
    };

    'beam-chip': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      color?: string;
      outlined?: boolean;
      small?: boolean;
      large?: boolean;
      'x-small'?: boolean;
      close?: boolean;
      disabled?: boolean;
      dark?: boolean;
      light?: boolean;
      label?: boolean;
      pill?: boolean;
    };

    'beam-alert': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      color?: string;
      type?: string;
      outlined?: boolean;
      text?: boolean;
      dense?: boolean;
      dismissible?: boolean;
      icon?: string;
      border?: string;
      elevation?: string;
      dark?: boolean;
      light?: boolean;
      prominent?: boolean;
      tile?: boolean;
    };

    'beam-card': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      elevation?: string;
    };

    'beam-card-title': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      // No attributes defined in CEM.
    };

    'beam-card-subtitle': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      // No attributes defined in CEM.
    };

    'beam-card-text': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      // No attributes defined in CEM.
    };

    'beam-card-actions': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      // No attributes defined in CEM.
    };

    'beam-data-table': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      headers?: unknown[];
      items?: unknown[];
      'items-per-page'?: string;
      loading?: boolean;
      search?: string;
      'sort-by'?: string;
      'sort-desc'?: string;
      'multi-sort'?: boolean;
      'must-sort'?: boolean;
      'show-select'?: boolean;
      'single-select'?: boolean;
      value?: string;
      dense?: boolean;
      'fixed-header'?: boolean;
      height?: string;
      'hide-default-header'?: boolean;
      'hide-default-footer'?: boolean;
      'no-data-text'?: string;
      'no-results-text'?: string;
      dark?: boolean;
      light?: boolean;
      'item-key'?: string;
      'item-class'?: string;
      page?: string;
      'server-items-length'?: string;
      'disable-pagination'?: boolean;
      'disable-sort'?: boolean;
      'show-expand'?: boolean;
      expanded?: string;
      'group-by'?: string;
      'group-desc'?: string;
      'show-group-by'?: boolean;
      'mobile-breakpoint'?: string;
      'loader-height'?: string;
      'loading-text'?: string;
      'footer-props'?: string;
      'header-props'?: string;
      'calculate-widths'?: boolean;
      caption?: string;
    };
  }
}

export {};
