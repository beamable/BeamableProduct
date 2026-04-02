// AUTO-GENERATED — do not edit manually.
// Run `pnpm sync-components` to regenerate from Portal's beam-components.json.

export {};

declare global {
  // ---------------------------------------------------------------------------
  // Tag → element type mapping.
  // Gives TypeScript callers correct types for:
  //   document.querySelector('beam-btn')  →  BeamBtnElement
  //   document.createElement('beam-btn')  →  BeamBtnElement
  // ---------------------------------------------------------------------------
  interface HTMLElementTagNameMap {
    'beam-btn': BeamBtnElement;
    'beam-icon': BeamIconElement;
    'beam-chip': BeamChipElement;
    'beam-alert': BeamAlertElement;
    'beam-card': BeamCardElement;
    'beam-card-title': BeamCardTitleElement;
    'beam-card-subtitle': BeamCardSubtitleElement;
    'beam-card-text': BeamCardTextElement;
    'beam-card-actions': BeamCardActionsElement;
    'beam-data-table': BeamDataTableElement;
  }

  // ---------------------------------------------------------------------------
  // Per-element interfaces
  // ---------------------------------------------------------------------------
  interface BeamBtnElement extends HTMLElement {
    color?: string;
    outlined?: boolean;
    text?: boolean;
    block?: boolean;
    disabled?: boolean;
    small?: boolean;
    large?: boolean;
    xSmall?: boolean;
    xLarge?: boolean;
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
  }

  interface BeamIconElement extends HTMLElement {
    icon?: string;
    color?: string;
    small?: boolean;
    large?: boolean;
    xSmall?: boolean;
    xLarge?: boolean;
    dense?: boolean;
    disabled?: boolean;
    dark?: boolean;
    light?: boolean;
    left?: boolean;
    right?: boolean;
  }

  interface BeamChipElement extends HTMLElement {
    color?: string;
    outlined?: boolean;
    small?: boolean;
    large?: boolean;
    xSmall?: boolean;
    close?: boolean;
    disabled?: boolean;
    dark?: boolean;
    light?: boolean;
    label?: boolean;
    pill?: boolean;
  }

  interface BeamAlertElement extends HTMLElement {
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
  }

  interface BeamCardElement extends HTMLElement {
    elevation?: string;
  }

  interface BeamCardTitleElement extends HTMLElement {
    // No public properties defined in CEM.
  }

  interface BeamCardSubtitleElement extends HTMLElement {
    // No public properties defined in CEM.
  }

  interface BeamCardTextElement extends HTMLElement {
    // No public properties defined in CEM.
  }

  interface BeamCardActionsElement extends HTMLElement {
    // No public properties defined in CEM.
  }

  interface BeamDataTableElement extends HTMLElement {
    headers?: unknown[];
    items?: unknown[];
    itemsPerPage?: string;
    loading?: boolean;
    search?: string;
    sortBy?: string;
    sortDesc?: string;
    multiSort?: boolean;
    mustSort?: boolean;
    showSelect?: boolean;
    singleSelect?: boolean;
    value?: string;
    dense?: boolean;
    fixedHeader?: boolean;
    height?: string;
    hideDefaultHeader?: boolean;
    hideDefaultFooter?: boolean;
    noDataText?: string;
    noResultsText?: string;
    dark?: boolean;
    light?: boolean;
    itemKey?: string;
    itemClass?: string;
    page?: string;
    serverItemsLength?: string;
    disablePagination?: boolean;
    disableSort?: boolean;
    showExpand?: boolean;
    expanded?: string;
    groupBy?: string;
    groupDesc?: string;
    showGroupBy?: boolean;
    mobileBreakpoint?: string;
    loaderHeight?: string;
    loadingText?: string;
    footerProps?: string;
    headerProps?: string;
    calculateWidths?: boolean;
    caption?: string;
  }
}
