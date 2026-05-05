// AUTO-GENERATED — do not edit manually.
// Run `pnpm sync-components` to regenerate from agentic-portal's custom-elements.json.
//
// Augments Svelte's element type map so .svelte templates get autocomplete
// for Beamable web components.
//
// Add one line to your project's app.d.ts (or any .d.ts file):
//   /// <reference types="@beamable/portal-toolkit/svelte" />

declare module 'svelte/elements' {
  interface SvelteHTMLElements {
    'beam-icon': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      name?: string | undefined;
      family?: string;
      variant?: string;
      'auto-width'?: boolean;
      'swap-opacity'?: boolean;
      src?: string | undefined;
      label?: string;
      library?: string;
      rotate?: number;
      flip?: 'x' | 'y' | 'both' | undefined;
      animation?: unknown;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-checkbox': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      title?: string;
      name?: string | null;
      value?: string | null;
      size?: 'small' | 'medium' | 'large';
      disabled?: boolean;
      indeterminate?: boolean;
      checked?: boolean;
      required?: boolean;
      hint?: string;
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-spinner': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-tree-item': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      expanded?: boolean;
      selected?: boolean;
      disabled?: boolean;
      lazy?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-button': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      title?: string;
      variant?: 'neutral' | 'brand' | 'success' | 'warning' | 'danger';
      appearance?: 'accent' | 'filled' | 'outlined' | 'filled-outlined' | 'plain';
      size?: 'small' | 'medium' | 'large';
      'with-caret'?: boolean;
      'with-start'?: boolean;
      'with-end'?: boolean;
      disabled?: boolean;
      loading?: boolean;
      pill?: boolean;
      type?: 'button' | 'submit' | 'reset';
      name?: string | null;
      value?: string;
      href?: string;
      target?: '_blank' | '_parent' | '_self' | '_top';
      rel?: string | undefined;
      download?: string | undefined;
      formaction?: string;
      formenctype?: 'application/x-www-form-urlencoded' | 'multipart/form-data' | 'text/plain';
      formmethod?: 'post' | 'get';
      formnovalidate?: boolean;
      formtarget?: '_self' | '_blank' | '_parent' | '_top' | string;
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-animation': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      name?: string;
      play?: boolean;
      delay?: number;
      direction?: unknown;
      duration?: number;
      easing?: string;
      'end-delay'?: number;
      fill?: unknown;
      iterations?: number;
      'iteration-start'?: number;
      'playback-rate'?: number;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-avatar': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      image?: string;
      label?: string;
      initials?: string;
      loading?: 'eager' | 'lazy';
      shape?: 'circle' | 'square' | 'rounded';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-badge': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      variant?: 'brand' | 'neutral' | 'success' | 'warning' | 'danger';
      appearance?: 'accent' | 'filled' | 'outlined' | 'filled-outlined';
      pill?: boolean;
      attention?: 'none' | 'pulse' | 'bounce';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-breadcrumb-item': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      href?: string | undefined;
      target?: '_blank' | '_parent' | '_self' | '_top' | undefined;
      rel?: string;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-breadcrumb': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      label?: string;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-button-group': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      label?: string;
      orientation?: 'horizontal' | 'vertical';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-callout': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      variant?: 'brand' | 'neutral' | 'success' | 'warning' | 'danger';
      appearance?: 'accent' | 'filled' | 'outlined' | 'plain' | 'filled-outlined';
      size?: 'small' | 'medium' | 'large';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-card': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      appearance?: 'accent' | 'filled' | 'outlined' | 'filled-outlined' | 'plain';
      'with-header'?: boolean;
      'with-media'?: boolean;
      'with-footer'?: boolean;
      orientation?: 'horizontal' | 'vertical';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-input': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      title?: string;
      type?: | 'date'
    | 'datetime-local'
    | 'email'
    | 'number'
    | 'password'
    | 'search'
    | 'tel'
    | 'text'
    | 'time'
    | 'url';
      value?: string | null;
      size?: 'small' | 'medium' | 'large';
      appearance?: 'filled' | 'outlined' | 'filled-outlined';
      pill?: boolean;
      label?: string;
      hint?: string;
      'with-clear'?: boolean;
      placeholder?: string;
      readonly?: boolean;
      'password-toggle'?: boolean;
      'password-visible'?: boolean;
      'without-spin-buttons'?: boolean;
      required?: boolean;
      pattern?: string;
      minlength?: number;
      maxlength?: number;
      min?: number | string;
      max?: number | string;
      step?: number | 'any';
      autocapitalize?: 'off' | 'none' | 'on' | 'sentences' | 'words' | 'characters';
      autocorrect?: boolean;
      autocomplete?: string;
      autofocus?: boolean;
      enterkeyhint?: 'enter' | 'done' | 'go' | 'next' | 'previous' | 'search' | 'send';
      spellcheck?: boolean;
      inputmode?: 'none' | 'text' | 'decimal' | 'numeric' | 'tel' | 'search' | 'email' | 'url';
      'with-label'?: boolean;
      'with-hint'?: boolean;
      name?: string | null;
      disabled?: boolean;
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-popup': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      anchor?: unknown;
      active?: boolean;
      placement?: | 'top'
    | 'top-start'
    | 'top-end'
    | 'bottom'
    | 'bottom-start'
    | 'bottom-end'
    | 'right'
    | 'right-start'
    | 'right-end'
    | 'left'
    | 'left-start'
    | 'left-end';
      boundary?: 'viewport' | 'scroll';
      distance?: number;
      skidding?: number;
      arrow?: boolean;
      'arrow-placement'?: 'start' | 'end' | 'center' | 'anchor';
      'arrow-padding'?: number;
      flip?: boolean;
      'flip-fallback-placements'?: string;
      'flip-fallback-strategy'?: 'best-fit' | 'initial';
      flipBoundary?: Element | Element[];
      'flip-padding'?: number;
      shift?: boolean;
      shiftBoundary?: Element | Element[];
      'shift-padding'?: number;
      'auto-size'?: 'horizontal' | 'vertical' | 'both';
      sync?: 'width' | 'height' | 'both';
      autoSizeBoundary?: Element | Element[];
      'auto-size-padding'?: number;
      'hover-bridge'?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-color-picker': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      value?: string | null;
      'with-label'?: boolean;
      'with-hint'?: boolean;
      label?: string;
      hint?: string;
      format?: 'hex' | 'rgb' | 'hsl' | 'hsv';
      size?: 'small' | 'medium' | 'large';
      placement?: | 'top'
    | 'top-start'
    | 'top-end'
    | 'bottom'
    | 'bottom-start'
    | 'bottom-end'
    | 'right'
    | 'right-start'
    | 'right-end'
    | 'left'
    | 'left-start'
    | 'left-end';
      'without-format-toggle'?: boolean;
      name?: string | null;
      disabled?: boolean;
      open?: boolean;
      opacity?: boolean;
      uppercase?: boolean;
      swatches?: unknown;
      required?: boolean;
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-tooltip': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      placement?: | 'top'
    | 'top-start'
    | 'top-end'
    | 'right'
    | 'right-start'
    | 'right-end'
    | 'bottom'
    | 'bottom-start'
    | 'bottom-end'
    | 'left'
    | 'left-start'
    | 'left-end';
      disabled?: boolean;
      distance?: number;
      open?: boolean;
      skidding?: number;
      'show-delay'?: number;
      'hide-delay'?: number;
      trigger?: string;
      'without-arrow'?: boolean;
      for?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-copy-button': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      value?: string;
      from?: string;
      disabled?: boolean;
      'copy-label'?: string;
      'success-label'?: string;
      'error-label'?: string;
      'feedback-duration'?: number;
      'tooltip-placement'?: 'top' | 'right' | 'bottom' | 'left';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-details': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      open?: boolean;
      summary?: string;
      name?: string;
      disabled?: boolean;
      appearance?: 'filled' | 'outlined' | 'filled-outlined' | 'plain';
      'icon-placement'?: 'start' | 'end';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-dialog': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      open?: boolean;
      label?: string;
      'without-header'?: boolean;
      'light-dismiss'?: boolean;
      'with-footer'?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-divider': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      orientation?: 'horizontal' | 'vertical';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-drawer': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      open?: boolean;
      label?: string;
      placement?: 'top' | 'end' | 'bottom' | 'start';
      'without-header'?: boolean;
      'light-dismiss'?: boolean;
      'with-footer'?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-dropdown-item': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      variant?: 'danger' | 'default';
      value?: string;
      type?: 'normal' | 'checkbox';
      checked?: boolean;
      disabled?: boolean;
      submenuOpen?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-dropdown': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      open?: boolean;
      size?: 'small' | 'medium' | 'large';
      placement?: | 'top'
    | 'top-start'
    | 'top-end'
    | 'bottom'
    | 'bottom-start'
    | 'bottom-end'
    | 'right'
    | 'right-start'
    | 'right-end'
    | 'left'
    | 'left-start'
    | 'left-end';
      distance?: number;
      skidding?: number;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-format-bytes': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      value?: number;
      unit?: 'byte' | 'bit';
      display?: 'long' | 'short' | 'narrow';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-format-date': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      date?: Date | string;
      weekday?: 'narrow' | 'short' | 'long';
      era?: 'narrow' | 'short' | 'long';
      year?: 'numeric' | '2-digit';
      month?: 'numeric' | '2-digit' | 'narrow' | 'short' | 'long';
      day?: 'numeric' | '2-digit';
      hour?: 'numeric' | '2-digit';
      minute?: 'numeric' | '2-digit';
      second?: 'numeric' | '2-digit';
      'time-zone-name'?: 'short' | 'long';
      'time-zone'?: string;
      'hour-format'?: 'auto' | '12' | '24';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-format-number': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      value?: number;
      type?: 'currency' | 'decimal' | 'percent';
      'without-grouping'?: boolean;
      currency?: string;
      'currency-display'?: 'symbol' | 'narrowSymbol' | 'code' | 'name';
      'minimum-integer-digits'?: number;
      'minimum-fraction-digits'?: number;
      'maximum-fraction-digits'?: number;
      'minimum-significant-digits'?: number;
      'maximum-significant-digits'?: number;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-intersection-observer': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      root?: string | null;
      'root-margin'?: string;
      threshold?: string;
      'intersect-class'?: string;
      once?: boolean;
      disabled?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-mutation-observer': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      attr?: string;
      'attr-old-value'?: boolean;
      'char-data'?: boolean;
      'char-data-old-value'?: boolean;
      'child-list'?: boolean;
      disabled?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-number-input': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      title?: string;
      value?: string | null;
      size?: 'small' | 'medium' | 'large';
      appearance?: 'filled' | 'outlined' | 'filled-outlined';
      pill?: boolean;
      label?: string;
      hint?: string;
      placeholder?: string;
      readonly?: boolean;
      required?: boolean;
      min?: number;
      max?: number;
      step?: number | 'any';
      'without-steppers'?: boolean;
      autocomplete?: string;
      autofocus?: boolean;
      enterkeyhint?: 'enter' | 'done' | 'go' | 'next' | 'previous' | 'search' | 'send';
      inputmode?: 'numeric' | 'decimal';
      'with-label'?: boolean;
      'with-hint'?: boolean;
      name?: string | null;
      disabled?: boolean;
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-tag': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      variant?: 'brand' | 'neutral' | 'success' | 'warning' | 'danger';
      appearance?: 'accent' | 'filled' | 'outlined' | 'filled-outlined';
      size?: 'small' | 'medium' | 'large';
      pill?: boolean;
      'with-remove'?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-option': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      value?: string;
      disabled?: boolean;
      selected?: boolean;
      label?: string;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-select': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      name?: string | null;
      value?: unknown;
      size?: 'small' | 'medium' | 'large';
      placeholder?: string;
      multiple?: boolean;
      'max-options-visible'?: number;
      disabled?: boolean;
      'with-clear'?: boolean;
      open?: boolean;
      appearance?: 'filled' | 'outlined' | 'filled-outlined';
      pill?: boolean;
      label?: string;
      placement?: 'top' | 'bottom';
      hint?: string;
      'with-label'?: boolean;
      'with-hint'?: boolean;
      required?: boolean;
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-popover': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      placement?: | 'top'
    | 'top-start'
    | 'top-end'
    | 'right'
    | 'right-start'
    | 'right-end'
    | 'bottom'
    | 'bottom-start'
    | 'bottom-end'
    | 'left'
    | 'left-start'
    | 'left-end';
      open?: boolean;
      distance?: number;
      skidding?: number;
      for?: string | null;
      'without-arrow'?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-progress-bar': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      value?: number;
      indeterminate?: boolean;
      label?: string;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-progress-ring': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      value?: number;
      label?: string;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-qr-code': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      value?: string;
      label?: string;
      size?: number;
      fill?: string;
      background?: string;
      radius?: number;
      'error-correction'?: 'L' | 'M' | 'Q' | 'H';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-radio': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      value?: string;
      appearance?: 'default' | 'button';
      size?: 'small' | 'medium' | 'large';
      disabled?: boolean;
      name?: string | null;
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-radio-group': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      label?: string;
      hint?: string;
      name?: string | null;
      disabled?: boolean;
      orientation?: 'horizontal' | 'vertical';
      value?: string | null;
      size?: 'small' | 'medium' | 'large';
      required?: boolean;
      'with-label'?: boolean;
      'with-hint'?: boolean;
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-rating': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      name?: string | null;
      label?: string;
      value?: number;
      'default-value'?: number;
      max?: number;
      precision?: number;
      readonly?: boolean;
      disabled?: boolean;
      required?: boolean;
      getSymbol?: unknown;
      size?: 'small' | 'medium' | 'large';
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-relative-time': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      'with-utc-popover'?: boolean;
      date?: Date | string;
      format?: 'long' | 'short' | 'narrow';
      numeric?: 'always' | 'auto';
      sync?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-scroller': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      orientation?: 'horizontal' | 'vertical';
      'without-scrollbar'?: boolean;
      'without-shadow'?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-resize-observer': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      disabled?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-skeleton': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      effect?: 'pulse' | 'sheen' | 'none';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-slider': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      label?: string;
      hint?: string;
      name?: string | null;
      'min-value'?: number;
      'max-value'?: number;
      value?: number;
      range?: boolean;
      disabled?: boolean;
      readonly?: boolean;
      orientation?: 'horizontal' | 'vertical';
      size?: 'small' | 'medium' | 'large';
      'indicator-offset'?: number;
      min?: number;
      max?: number;
      step?: number;
      autofocus?: boolean;
      'tooltip-distance'?: number;
      'tooltip-placement'?: 'top' | 'right' | 'bottom' | 'left';
      'with-markers'?: boolean;
      'with-tooltip'?: boolean;
      'with-label'?: boolean;
      'with-hint'?: boolean;
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-switch': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      title?: string;
      name?: string | null;
      value?: string | null;
      size?: 'small' | 'medium' | 'large';
      disabled?: boolean;
      checked?: boolean;
      required?: boolean;
      hint?: string;
      'with-hint'?: boolean;
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-tab': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      panel?: string;
      disabled?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-split-panel': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      position?: number;
      'position-in-pixels'?: number;
      orientation?: 'horizontal' | 'vertical';
      disabled?: boolean;
      primary?: 'start' | 'end' | undefined;
      snap?: string | undefined;
      'snap-threshold'?: number;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-tab-panel': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      name?: string;
      active?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-tab-group': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      active?: string;
      placement?: 'top' | 'bottom' | 'start' | 'end';
      activation?: 'auto' | 'manual';
      'without-scroll-controls'?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-textarea': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      title?: string;
      name?: string | null;
      value?: string;
      size?: 'small' | 'medium' | 'large';
      appearance?: 'filled' | 'outlined' | 'filled-outlined';
      label?: string;
      hint?: string;
      placeholder?: string;
      rows?: number;
      resize?: 'none' | 'vertical' | 'horizontal' | 'both' | 'auto';
      disabled?: boolean;
      readonly?: boolean;
      required?: boolean;
      minlength?: number;
      maxlength?: number;
      autocapitalize?: 'off' | 'none' | 'on' | 'sentences' | 'words' | 'characters';
      autocorrect?: boolean;
      autocomplete?: string;
      autofocus?: boolean;
      enterkeyhint?: 'enter' | 'done' | 'go' | 'next' | 'previous' | 'search' | 'send';
      spellcheck?: boolean;
      inputmode?: 'none' | 'text' | 'decimal' | 'numeric' | 'tel' | 'search' | 'email' | 'url';
      'with-label'?: boolean;
      'with-hint'?: boolean;
      'with-count'?: boolean;
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-tree': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      selection?: 'single' | 'multiple' | 'leaf';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-markdown': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      'tab-size'?: number;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-page': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      view?: 'mobile' | 'desktop';
      'nav-open'?: boolean;
      'mobile-breakpoint'?: string;
      'navigation-placement'?: 'start' | 'end';
      'disable-navigation-toggle'?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-table': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      columns?: unknown;
      data?: any[];
      'group-by'?: string | undefined;
      'page-size'?: number | undefined;
      'empty-message'?: string;
      loading?: boolean;
      'loading-message'?: string;
      'default-collapsed'?: boolean;
      'table-title'?: string | undefined;
      'has-top-row'?: boolean;
      'has-group-header'?: boolean;
    };

    'beam-json': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      label?: string;
      indent?: number;
      'no-toolbar'?: boolean;
    };

    'beam-page-header': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      label?: string;
      description?: string;
    };

    'beam-confirm-dialog': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      open?: boolean;
      label?: string;
      message?: string;
      'confirm-label'?: string;
      'cancel-label'?: string;
      'confirm-variant'?: unknown;
    };

    'beam-pagination': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      page?: number;
      'page-size'?: number;
      total?: number;
      'page-size-options'?: string;
      'hide-info'?: boolean;
      'hide-page-input'?: boolean;
    };

    'beam-toast': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      variant?: unknown;
      message?: string;
      duration?: number;
      toastId?: number;
      'action-label'?: string;
      'no-close'?: boolean;
    };

    'beam-toast-stack': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      placement?: 'top-start' | 'top-end' | 'bottom-start' | 'bottom-end';
      'max-toasts'?: number;
    };

    'beam-code-snippet': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      code?: string;
      lang?: string;
    };
  }
}

export {};
