// AUTO-GENERATED — do not edit manually.
// Run `pnpm sync-components` to regenerate from agentic-portal's custom-elements.json.

export {};

declare global {
  // ---------------------------------------------------------------------------
  // Tag → element type mapping.
  // Gives TypeScript callers correct types for:
  //   document.querySelector('beam-btn')  →  BeamBtnElement
  //   document.createElement('beam-btn')  →  BeamBtnElement
  // ---------------------------------------------------------------------------
  interface HTMLElementTagNameMap {
    'beam-icon': BeamIconElement;
    'beam-checkbox': BeamCheckboxElement;
    'beam-spinner': BeamSpinnerElement;
    'beam-tree-item': BeamTreeItemElement;
    'beam-button': BeamButtonElement;
    'beam-animation': BeamAnimationElement;
    'beam-avatar': BeamAvatarElement;
    'beam-badge': BeamBadgeElement;
    'beam-breadcrumb-item': BeamBreadcrumbItemElement;
    'beam-breadcrumb': BeamBreadcrumbElement;
    'beam-button-group': BeamButtonGroupElement;
    'beam-callout': BeamCalloutElement;
    'beam-card': BeamCardElement;
    'beam-input': BeamInputElement;
    'beam-popup': BeamPopupElement;
    'beam-color-picker': BeamColorPickerElement;
    'beam-tooltip': BeamTooltipElement;
    'beam-copy-button': BeamCopyButtonElement;
    'beam-details': BeamDetailsElement;
    'beam-dialog': BeamDialogElement;
    'beam-divider': BeamDividerElement;
    'beam-drawer': BeamDrawerElement;
    'beam-dropdown-item': BeamDropdownItemElement;
    'beam-dropdown': BeamDropdownElement;
    'beam-format-bytes': BeamFormatBytesElement;
    'beam-format-date': BeamFormatDateElement;
    'beam-format-number': BeamFormatNumberElement;
    'beam-intersection-observer': BeamIntersectionObserverElement;
    'beam-mutation-observer': BeamMutationObserverElement;
    'beam-number-input': BeamNumberInputElement;
    'beam-tag': BeamTagElement;
    'beam-option': BeamOptionElement;
    'beam-select': BeamSelectElement;
    'beam-popover': BeamPopoverElement;
    'beam-progress-bar': BeamProgressBarElement;
    'beam-progress-ring': BeamProgressRingElement;
    'beam-qr-code': BeamQrCodeElement;
    'beam-radio': BeamRadioElement;
    'beam-radio-group': BeamRadioGroupElement;
    'beam-rating': BeamRatingElement;
    'beam-relative-time': BeamRelativeTimeElement;
    'beam-scroller': BeamScrollerElement;
    'beam-resize-observer': BeamResizeObserverElement;
    'beam-skeleton': BeamSkeletonElement;
    'beam-slider': BeamSliderElement;
    'beam-switch': BeamSwitchElement;
    'beam-tab': BeamTabElement;
    'beam-split-panel': BeamSplitPanelElement;
    'beam-tab-panel': BeamTabPanelElement;
    'beam-tab-group': BeamTabGroupElement;
    'beam-textarea': BeamTextareaElement;
    'beam-tree': BeamTreeElement;
    'beam-markdown': BeamMarkdownElement;
    'beam-page': BeamPageElement;
    'beam-table': BeamTableElement;
    'beam-json': BeamJsonElement;
    'beam-page-header': BeamPageHeaderElement;
    'beam-confirm-dialog': BeamConfirmDialogElement;
    'beam-pagination': BeamPaginationElement;
    'beam-toast': BeamToastElement;
    'beam-toast-stack': BeamToastStackElement;
    'beam-code-snippet': BeamCodeSnippetElement;
  }

  // ---------------------------------------------------------------------------
  // Per-element interfaces
  // ---------------------------------------------------------------------------
  interface BeamIconElement extends HTMLElement {
    name?: string | undefined;
    family?: string;
    variant?: string;
    autoWidth?: boolean;
    swapOpacity?: boolean;
    src?: string | undefined;
    label?: string;
    library?: string;
    rotate?: number;
    flip?: 'x' | 'y' | 'both' | undefined;
    animation?: unknown;
  }

  interface BeamCheckboxElement extends HTMLElement {
    input?: unknown;
    name?: string | null;
    value?: string | null;
    size?: 'small' | 'medium' | 'large';
    disabled?: boolean;
    indeterminate?: boolean;
    checked?: unknown;
    defaultChecked?: boolean;
    required?: boolean;
    hint?: string;
    customError?: string | null;
    labels?: unknown;
    form?: HTMLFormElement | null;
    validity?: unknown;
    willValidate?: unknown;
    validationMessage?: unknown;
    validationTarget?: undefined | HTMLElement;
  }

  interface BeamSpinnerElement extends HTMLElement {
    // No public properties defined in CEM.
  }

  interface BeamTreeItemElement extends HTMLElement {
    indeterminate?: boolean;
    isLeaf?: boolean;
    loading?: boolean;
    selectable?: boolean;
    expanded?: boolean;
    selected?: boolean;
    disabled?: boolean;
    lazy?: boolean;
    defaultSlot?: unknown;
    childrenSlot?: unknown;
    itemElement?: HTMLDivElement;
    childrenContainer?: HTMLDivElement;
    expandButtonSlot?: unknown;
  }

  interface BeamButtonElement extends HTMLElement {
    button?: unknown;
    labelSlot?: unknown;
    invalid?: boolean;
    isIconButton?: boolean;
    variant?: 'neutral' | 'brand' | 'success' | 'warning' | 'danger';
    appearance?: 'accent' | 'filled' | 'outlined' | 'filled-outlined' | 'plain';
    size?: 'small' | 'medium' | 'large';
    withCaret?: boolean;
    withStart?: boolean;
    withEnd?: boolean;
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
    formAction?: string;
    formEnctype?: 'application/x-www-form-urlencoded' | 'multipart/form-data' | 'text/plain';
    formMethod?: 'post' | 'get';
    formNoValidate?: boolean;
    formTarget?: '_self' | '_blank' | '_parent' | '_top' | string;
    required?: boolean;
    input?: unknown;
    customError?: string | null;
    labels?: unknown;
    form?: HTMLFormElement | null;
    validity?: unknown;
    willValidate?: unknown;
    validationMessage?: unknown;
    validationTarget?: undefined | HTMLElement;
  }

  interface BeamAnimationElement extends HTMLElement {
    defaultSlot?: unknown;
    name?: string;
    play?: boolean;
    delay?: number;
    direction?: unknown;
    duration?: number;
    easing?: string;
    endDelay?: number;
    fill?: unknown;
    iterations?: number;
    iterationStart?: number;
    keyframes?: unknown;
    playbackRate?: number;
    currentTime?: unknown;
  }

  interface BeamAvatarElement extends HTMLElement {
    image?: string;
    label?: string;
    initials?: string;
    loading?: 'eager' | 'lazy';
    shape?: 'circle' | 'square' | 'rounded';
  }

  interface BeamBadgeElement extends HTMLElement {
    variant?: 'brand' | 'neutral' | 'success' | 'warning' | 'danger';
    appearance?: 'accent' | 'filled' | 'outlined' | 'filled-outlined';
    pill?: boolean;
    attention?: 'none' | 'pulse' | 'bounce';
  }

  interface BeamBreadcrumbItemElement extends HTMLElement {
    defaultSlot?: unknown;
    href?: string | undefined;
    target?: '_blank' | '_parent' | '_self' | '_top' | undefined;
    rel?: string;
  }

  interface BeamBreadcrumbElement extends HTMLElement {
    defaultSlot?: unknown;
    separatorSlot?: unknown;
    label?: string;
  }

  interface BeamButtonGroupElement extends HTMLElement {
    defaultSlot?: unknown;
    disableRole?: boolean;
    hasOutlined?: boolean;
    label?: string;
    orientation?: 'horizontal' | 'vertical';
  }

  interface BeamCalloutElement extends HTMLElement {
    variant?: 'brand' | 'neutral' | 'success' | 'warning' | 'danger';
    appearance?: 'accent' | 'filled' | 'outlined' | 'plain' | 'filled-outlined';
    size?: 'small' | 'medium' | 'large';
  }

  interface BeamCardElement extends HTMLElement {
    appearance?: 'accent' | 'filled' | 'outlined' | 'filled-outlined' | 'plain';
    withHeader?: boolean;
    withMedia?: boolean;
    withFooter?: boolean;
    orientation?: 'horizontal' | 'vertical';
  }

  interface BeamInputElement extends HTMLElement {
    input?: unknown;
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
    value?: unknown;
    defaultValue?: string | null;
    size?: 'small' | 'medium' | 'large';
    appearance?: 'filled' | 'outlined' | 'filled-outlined';
    pill?: boolean;
    label?: string;
    hint?: string;
    withClear?: boolean;
    placeholder?: string;
    readonly?: boolean;
    passwordToggle?: boolean;
    passwordVisible?: boolean;
    withoutSpinButtons?: boolean;
    required?: boolean;
    pattern?: string;
    minlength?: number;
    maxlength?: number;
    min?: number | string;
    max?: number | string;
    step?: number | 'any';
    autocomplete?: string;
    enterkeyhint?: 'enter' | 'done' | 'go' | 'next' | 'previous' | 'search' | 'send';
    inputmode?: 'none' | 'text' | 'decimal' | 'numeric' | 'tel' | 'search' | 'email' | 'url';
    withLabel?: boolean;
    withHint?: boolean;
    name?: string | null;
    disabled?: boolean;
    customError?: string | null;
    labels?: unknown;
    form?: HTMLFormElement | null;
    validity?: unknown;
    willValidate?: unknown;
    validationMessage?: unknown;
    validationTarget?: undefined | HTMLElement;
  }

  interface BeamPopupElement extends HTMLElement {
    popup?: HTMLElement;
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
    arrowPlacement?: 'start' | 'end' | 'center' | 'anchor';
    arrowPadding?: number;
    flip?: boolean;
    flipFallbackPlacements?: string;
    flipFallbackStrategy?: 'best-fit' | 'initial';
    flipBoundary?: Element | Element[];
    flipPadding?: number;
    shift?: boolean;
    shiftBoundary?: Element | Element[];
    shiftPadding?: number;
    autoSize?: 'horizontal' | 'vertical' | 'both';
    sync?: 'width' | 'height' | 'both';
    autoSizeBoundary?: Element | Element[];
    autoSizePadding?: number;
    hoverBridge?: boolean;
  }

  interface BeamColorPickerElement extends HTMLElement {
    base?: HTMLElement;
    input?: unknown;
    triggerLabel?: HTMLElement;
    triggerButton?: HTMLButtonElement;
    validationTarget?: undefined | HTMLElement;
    popup?: unknown;
    previewButton?: HTMLButtonElement;
    trigger?: HTMLButtonElement;
    value?: unknown;
    defaultValue?: string | null;
    withLabel?: boolean;
    withHint?: boolean;
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
    withoutFormatToggle?: boolean;
    name?: string | null;
    disabled?: boolean;
    open?: boolean;
    opacity?: boolean;
    uppercase?: boolean;
    swatches?: unknown;
    required?: boolean;
    customError?: string | null;
    labels?: unknown;
    form?: HTMLFormElement | null;
    validity?: unknown;
    willValidate?: unknown;
    validationMessage?: unknown;
  }

  interface BeamTooltipElement extends HTMLElement {
    defaultSlot?: unknown;
    body?: HTMLElement;
    popup?: unknown;
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
    showDelay?: number;
    hideDelay?: number;
    trigger?: string;
    withoutArrow?: boolean;
    for?: string | null;
    anchor?: null | Element;
  }

  interface BeamCopyButtonElement extends HTMLElement {
    copyIcon?: unknown;
    successIcon?: unknown;
    errorIcon?: unknown;
    tooltip?: unknown;
    isCopying?: boolean;
    status?: 'rest' | 'success' | 'error';
    value?: string;
    from?: string;
    disabled?: boolean;
    copyLabel?: string;
    successLabel?: string;
    errorLabel?: string;
    feedbackDuration?: number;
    tooltipPlacement?: 'top' | 'right' | 'bottom' | 'left';
  }

  interface BeamDetailsElement extends HTMLElement {
    details?: unknown;
    header?: HTMLElement;
    body?: HTMLElement;
    expandIconSlot?: unknown;
    isAnimating?: boolean;
    open?: boolean;
    summary?: string;
    name?: string;
    disabled?: boolean;
    appearance?: 'filled' | 'outlined' | 'filled-outlined' | 'plain';
    iconPlacement?: 'start' | 'end';
  }

  interface BeamDialogElement extends HTMLElement {
    dialog?: unknown;
    open?: boolean;
    label?: string;
    withoutHeader?: boolean;
    lightDismiss?: boolean;
    withFooter?: boolean;
  }

  interface BeamDividerElement extends HTMLElement {
    orientation?: 'horizontal' | 'vertical';
  }

  interface BeamDrawerElement extends HTMLElement {
    drawer?: unknown;
    open?: boolean;
    label?: string;
    placement?: 'top' | 'end' | 'bottom' | 'start';
    withoutHeader?: boolean;
    lightDismiss?: boolean;
    withFooter?: boolean;
    modal?: unknown;
  }

  interface BeamDropdownItemElement extends HTMLElement {
    submenuElement?: HTMLDivElement;
    variant?: 'danger' | 'default';
    value?: string;
    type?: 'normal' | 'checkbox';
    checked?: boolean;
    disabled?: boolean;
    submenuOpen?: boolean;
  }

  interface BeamDropdownElement extends HTMLElement {
    defaultSlot?: unknown;
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
  }

  interface BeamFormatBytesElement extends HTMLElement {
    value?: number;
    unit?: 'byte' | 'bit';
    display?: 'long' | 'short' | 'narrow';
  }

  interface BeamFormatDateElement extends HTMLElement {
    date?: Date | string;
    weekday?: 'narrow' | 'short' | 'long';
    era?: 'narrow' | 'short' | 'long';
    year?: 'numeric' | '2-digit';
    month?: 'numeric' | '2-digit' | 'narrow' | 'short' | 'long';
    day?: 'numeric' | '2-digit';
    hour?: 'numeric' | '2-digit';
    minute?: 'numeric' | '2-digit';
    second?: 'numeric' | '2-digit';
    timeZoneName?: 'short' | 'long';
    timeZone?: string;
    hourFormat?: 'auto' | '12' | '24';
  }

  interface BeamFormatNumberElement extends HTMLElement {
    value?: number;
    type?: 'currency' | 'decimal' | 'percent';
    withoutGrouping?: boolean;
    currency?: string;
    currencyDisplay?: 'symbol' | 'narrowSymbol' | 'code' | 'name';
    minimumIntegerDigits?: number;
    minimumFractionDigits?: number;
    maximumFractionDigits?: number;
    minimumSignificantDigits?: number;
    maximumSignificantDigits?: number;
  }

  interface BeamIntersectionObserverElement extends HTMLElement {
    root?: string | null;
    rootMargin?: string;
    threshold?: string;
    intersectClass?: string;
    once?: boolean;
    disabled?: boolean;
  }

  interface BeamMutationObserverElement extends HTMLElement {
    attr?: string;
    attrOldValue?: boolean;
    charData?: boolean;
    charDataOldValue?: boolean;
    childList?: boolean;
    disabled?: boolean;
  }

  interface BeamNumberInputElement extends HTMLElement {
    input?: unknown;
    value?: unknown;
    defaultValue?: string | null;
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
    withoutSteppers?: boolean;
    autocomplete?: string;
    enterkeyhint?: 'enter' | 'done' | 'go' | 'next' | 'previous' | 'search' | 'send';
    inputmode?: 'numeric' | 'decimal';
    withLabel?: boolean;
    withHint?: boolean;
    name?: string | null;
    disabled?: boolean;
    customError?: string | null;
    labels?: unknown;
    form?: HTMLFormElement | null;
    validity?: unknown;
    willValidate?: unknown;
    validationMessage?: unknown;
    validationTarget?: undefined | HTMLElement;
  }

  interface BeamTagElement extends HTMLElement {
    variant?: 'brand' | 'neutral' | 'success' | 'warning' | 'danger';
    appearance?: 'accent' | 'filled' | 'outlined' | 'filled-outlined';
    size?: 'small' | 'medium' | 'large';
    pill?: boolean;
    withRemove?: boolean;
  }

  interface BeamOptionElement extends HTMLElement {
    defaultSlot?: unknown;
    current?: boolean;
    value?: string;
    disabled?: boolean;
    defaultSelected?: boolean;
    label?: string;
    defaultLabel?: string;
  }

  interface BeamSelectElement extends HTMLElement {
    popup?: unknown;
    combobox?: unknown;
    displayInput?: HTMLInputElement;
    valueInput?: HTMLInputElement;
    listbox?: unknown;
    validationTarget?: undefined | HTMLElement;
    displayLabel?: string;
    currentOption?: unknown;
    selectedOptions?: unknown;
    name?: string | null;
    defaultValue?: unknown;
    value?: unknown;
    size?: 'small' | 'medium' | 'large';
    placeholder?: string;
    multiple?: boolean;
    maxOptionsVisible?: number;
    disabled?: boolean;
    withClear?: boolean;
    open?: boolean;
    appearance?: 'filled' | 'outlined' | 'filled-outlined';
    pill?: boolean;
    label?: string;
    placement?: 'top' | 'bottom';
    hint?: string;
    withLabel?: boolean;
    withHint?: boolean;
    required?: boolean;
    getTag?: unknown;
    input?: unknown;
    customError?: string | null;
    labels?: unknown;
    form?: HTMLFormElement | null;
    validity?: unknown;
    willValidate?: unknown;
    validationMessage?: unknown;
  }

  interface BeamPopoverElement extends HTMLElement {
    dialog?: unknown;
    body?: HTMLElement;
    popup?: unknown;
    anchor?: null | Element;
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
    withoutArrow?: boolean;
  }

  interface BeamProgressBarElement extends HTMLElement {
    value?: number;
    indeterminate?: boolean;
    label?: string;
  }

  interface BeamProgressRingElement extends HTMLElement {
    indicator?: unknown;
    indicatorOffset?: string;
    value?: number;
    label?: string;
  }

  interface BeamQrCodeElement extends HTMLElement {
    canvas?: HTMLElement;
    value?: string;
    label?: string;
    size?: number;
    fill?: string;
    background?: string;
    radius?: number;
    errorCorrection?: 'L' | 'M' | 'Q' | 'H';
  }

  interface BeamRadioElement extends HTMLElement {
    checked?: boolean;
    value?: string;
    appearance?: 'default' | 'button';
    size?: 'small' | 'medium' | 'large';
    disabled?: boolean;
    name?: string | null;
    required?: boolean;
    input?: unknown;
    customError?: string | null;
    labels?: unknown;
    form?: HTMLFormElement | null;
    validity?: unknown;
    willValidate?: unknown;
    validationMessage?: unknown;
    validationTarget?: undefined | HTMLElement;
  }

  interface BeamRadioGroupElement extends HTMLElement {
    defaultSlot?: unknown;
    label?: string;
    hint?: string;
    name?: string | null;
    disabled?: boolean;
    orientation?: 'horizontal' | 'vertical';
    value?: unknown;
    defaultValue?: string | null;
    size?: 'small' | 'medium' | 'large';
    required?: boolean;
    withLabel?: boolean;
    withHint?: boolean;
    validationTarget?: undefined | HTMLElement;
    input?: unknown;
    customError?: string | null;
    labels?: unknown;
    form?: HTMLFormElement | null;
    validity?: unknown;
    willValidate?: unknown;
    validationMessage?: unknown;
  }

  interface BeamRatingElement extends HTMLElement {
    name?: string | null;
    label?: string;
    value?: number;
    defaultValue?: number;
    max?: number;
    precision?: number;
    readonly?: boolean;
    disabled?: boolean;
    required?: boolean;
    getSymbol?: unknown;
    size?: 'small' | 'medium' | 'large';
    input?: unknown;
    customError?: string | null;
    labels?: unknown;
    form?: HTMLFormElement | null;
    validity?: unknown;
    willValidate?: unknown;
    validationMessage?: unknown;
    validationTarget?: undefined | HTMLElement;
  }

  interface BeamRelativeTimeElement extends HTMLElement {
    withUtcPopover?: boolean;
    date?: Date | string;
    format?: 'long' | 'short' | 'narrow';
    numeric?: 'always' | 'auto';
    sync?: boolean;
  }

  interface BeamScrollerElement extends HTMLElement {
    content?: HTMLElement;
    canScroll?: boolean;
    orientation?: 'horizontal' | 'vertical';
    withoutScrollbar?: boolean;
    withoutShadow?: boolean;
  }

  interface BeamResizeObserverElement extends HTMLElement {
    disabled?: boolean;
  }

  interface BeamSkeletonElement extends HTMLElement {
    effect?: 'pulse' | 'sheen' | 'none';
  }

  interface BeamSliderElement extends HTMLElement {
    validationTarget?: undefined | HTMLElement;
    slider?: HTMLElement;
    thumb?: HTMLElement;
    thumbMin?: HTMLElement;
    thumbMax?: HTMLElement;
    track?: HTMLElement;
    tooltip?: unknown;
    label?: string;
    hint?: string;
    name?: string | null;
    minValue?: number;
    maxValue?: number;
    defaultValue?: number;
    value?: number;
    range?: boolean;
    isRange?: boolean;
    disabled?: boolean;
    readonly?: boolean;
    orientation?: 'horizontal' | 'vertical';
    size?: 'small' | 'medium' | 'large';
    indicatorOffset?: number;
    min?: number;
    max?: number;
    step?: number;
    tooltipDistance?: number;
    tooltipPlacement?: 'top' | 'right' | 'bottom' | 'left';
    withMarkers?: boolean;
    withTooltip?: boolean;
    withLabel?: boolean;
    withHint?: boolean;
    valueFormatter?: unknown;
    required?: boolean;
    input?: unknown;
    customError?: string | null;
    labels?: unknown;
    form?: HTMLFormElement | null;
    validity?: unknown;
    willValidate?: unknown;
    validationMessage?: unknown;
  }

  interface BeamSwitchElement extends HTMLElement {
    input?: unknown;
    name?: string | null;
    value?: string | null;
    size?: 'small' | 'medium' | 'large';
    disabled?: boolean;
    checked?: unknown;
    defaultChecked?: boolean;
    required?: boolean;
    hint?: string;
    withHint?: boolean;
    customError?: string | null;
    labels?: unknown;
    form?: HTMLFormElement | null;
    validity?: unknown;
    willValidate?: unknown;
    validationMessage?: unknown;
    validationTarget?: undefined | HTMLElement;
  }

  interface BeamTabElement extends HTMLElement {
    tab?: HTMLElement;
    panel?: string;
    disabled?: boolean;
  }

  interface BeamSplitPanelElement extends HTMLElement {
    divider?: HTMLElement;
    position?: number;
    positionInPixels?: number;
    orientation?: 'horizontal' | 'vertical';
    disabled?: boolean;
    primary?: 'start' | 'end' | undefined;
    snap?: string | undefined;
    snapThreshold?: number;
  }

  interface BeamTabPanelElement extends HTMLElement {
    name?: string;
    active?: boolean;
  }

  interface BeamTabGroupElement extends HTMLElement {
    tabGroup?: HTMLElement;
    defaultSlot?: unknown;
    nav?: HTMLElement;
    active?: string;
    placement?: 'top' | 'bottom' | 'start' | 'end';
    activation?: 'auto' | 'manual';
    withoutScrollControls?: boolean;
  }

  interface BeamTextareaElement extends HTMLElement {
    input?: unknown;
    base?: HTMLDivElement;
    sizeAdjuster?: HTMLTextAreaElement;
    name?: string | null;
    value?: unknown;
    defaultValue?: string;
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
    autocomplete?: string;
    enterkeyhint?: 'enter' | 'done' | 'go' | 'next' | 'previous' | 'search' | 'send';
    inputmode?: 'none' | 'text' | 'decimal' | 'numeric' | 'tel' | 'search' | 'email' | 'url';
    withLabel?: boolean;
    withHint?: boolean;
    withCount?: boolean;
    customError?: string | null;
    labels?: unknown;
    form?: HTMLFormElement | null;
    validity?: unknown;
    willValidate?: unknown;
    validationMessage?: unknown;
    validationTarget?: undefined | HTMLElement;
  }

  interface BeamTreeElement extends HTMLElement {
    defaultSlot?: unknown;
    expandedIconSlot?: unknown;
    collapsedIconSlot?: unknown;
    selection?: 'single' | 'multiple' | 'leaf';
  }

  interface BeamMarkdownElement extends HTMLElement {
    tabSize?: number;
    marked?: unknown;
  }

  interface BeamPageElement extends HTMLElement {
    header?: HTMLElement;
    menu?: HTMLElement;
    main?: HTMLElement;
    aside?: HTMLElement;
    subheader?: HTMLElement;
    footer?: HTMLElement;
    banner?: HTMLElement;
    navigationDrawer?: unknown;
    navigationToggleSlot?: unknown;
    view?: 'mobile' | 'desktop';
    navOpen?: boolean;
    mobileBreakpoint?: string;
    navigationPlacement?: 'start' | 'end';
    disableNavigationToggle?: boolean;
    pageResizeObserver?: unknown;
    updateAsideAndMenuHeights?: unknown;
  }

  interface BeamTableElement extends HTMLElement {
    columns?: unknown;
    data?: any[];
    groupBy?: string | undefined;
    groupOrder?: string[] | undefined;
    pageSize?: number | undefined;
    pageSizeOptions?: number[] | undefined;
    emptyMessage?: string;
    loading?: boolean;
    loadingMessage?: string;
    defaultSort?: unknown;
    defaultCollapsed?: boolean;
    tableTitle?: string | undefined;
    hasTopRow?: boolean;
    hasGroupHeader?: boolean;
    onGroupAction?: unknown;
    rowKey?: unknown;
    renderedGroups?: unknown;
  }

  interface BeamJsonElement extends HTMLElement {
    data?: any;
    label?: string;
    indent?: number;
    noToolbar?: boolean;
  }

  interface BeamPageHeaderElement extends HTMLElement {
    label?: string;
    description?: string;
  }

  interface BeamConfirmDialogElement extends HTMLElement {
    open?: boolean;
    label?: string;
    message?: string;
    confirmLabel?: string;
    cancelLabel?: string;
    confirmVariant?: unknown;
  }

  interface BeamPaginationElement extends HTMLElement {
    page?: number;
    pageSize?: number;
    total?: number;
    pageSizeOptions?: string;
    hideInfo?: boolean;
    hidePageInput?: boolean;
    totalPages?: number;
    rangeStart?: number;
    rangeEnd?: number;
  }

  interface BeamToastElement extends HTMLElement {
    variant?: unknown;
    message?: string;
    duration?: number;
    toastId?: number;
    actionLabel?: string;
    noClose?: boolean;
  }

  interface BeamToastStackElement extends HTMLElement {
    placement?: 'top-start' | 'top-end' | 'bottom-start' | 'bottom-end';
    maxToasts?: number;
    toasts?: unknown;
  }

  interface BeamCodeSnippetElement extends HTMLElement {
    code?: string;
  }
}
