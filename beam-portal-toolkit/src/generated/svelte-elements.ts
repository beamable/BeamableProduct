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
    /**
     * @csspart svg - The internal SVG element.
     * @csspart use - The `<use>` element generated when using `spriteSheet: true`
     * @cssproperty --animation-delay - Sets when the animation will start. [default: 0]
     * @cssproperty --animation-direction - Defines whether or not the animation should play in reverse on alternate cycles. [default: normal]
     * @cssproperty --animation-duration - Defines the length of time that an animation takes to complete one cycle. [default: 1s]
     * @cssproperty --animation-iteration-count - Defines the number of times an animation cycle is played. [default: infinite]
     * @cssproperty --animation-timing - Describes how the animation will progress over one cycle of its duration.
     * @cssproperty --beat-fade-opacity - Set lowest opacity value an icon with `beat-fade` animation will fade to and from.
     * @cssproperty --beat-fade-scale - Set max value that an icon with `beat-fade` animation will scale.
     * @cssproperty --beat-scale - Set max value that an icon with `beat` animation will scale.
     * @cssproperty --bounce-height - Set the max height an icon with `bounce` animation will jump to when bouncing.
     * @cssproperty --bounce-jump-scale-x - Set the icon’s horizontal distortion (“squish”) at the top of the jump.
     * @cssproperty --bounce-jump-scale-y - Set the icon’s vertical distortion (“squish”) at the top of the jump.
     * @cssproperty --bounce-land-scale-x - Set the icon’s horizontal distortion (“squish”) when landing after the jump.
     * @cssproperty --bounce-land-scale-y - Set the icon’s vertical distortion (“squish”) when landing after the jump.
     * @cssproperty --bounce-rebound - Set the amount of rebound an icon with `bounce` animation has when landing after the jump.
     * @cssproperty --bounce-start-scale-x - Set the icon’s horizontal distortion (“squish”) when starting to bounce.
     * @cssproperty --bounce-start-scale-y - Set the icon’s vertical distortion (“squish”) when starting to bounce.
     * @cssproperty --fade-opacity - Set lowest opacity value an icon with `fade` animation will fade to and from.
     * @cssproperty --flip-angle - Set rotation angle of flip for an icon with `flip` animation. A positive angle denotes a clockwise rotation, a negative angle a counter-clockwise one.
     * @cssproperty --flip-x - Set x-coordinate of the vector denoting the axis of rotation (between 0 and 1) for an icon with `flip` animation.
     * @cssproperty --flip-y - Set y-coordinate of the vector denoting the axis of rotation (between 0 and 1) for an icon with `flip` animation.
     * @cssproperty --flip-z - Set z-coordinate of the vector denoting the axis of rotation (between 0 and 1) for an icon with `flip` animation.
     * @cssproperty --primary-color - Sets a duotone icon's primary color. [default: currentColor]
     * @cssproperty --primary-opacity - Sets a duotone icon's primary opacity. [default: 1]
     * @cssproperty --secondary-color - Sets a duotone icon's secondary color. [default: currentColor]
     * @cssproperty --secondary-opacity - Sets a duotone icon's secondary opacity. [default: 0.4]
     * @event wa-load - Emitted when the icon has loaded. When using `spriteSheet: true` this will not emit. (React: `onWaLoad`)
     * @event wa-error - Emitted when the icon fails to load due to an error. When using `spriteSheet: true` this will not emit. (React: `onWaError`)
     */
    'beam-icon': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** The name of the icon to draw. Available names depend on the icon library being used. */
      name?: string | undefined;
      /**
       * The family of icons to choose from. For Font Awesome Free, valid options include `classic` and `brands`. For Font Awesome Pro subscribers, valid options include, `classic`, `sharp`, `duotone`, `sharp-duotone`, and `brands`. A valid kit code must be present to show pro icons via CDN. You can set `<html data-fa-kit-code="...">` to provide one.
       */
      family?: string;
      /**
       * The name of the icon's variant. For Font Awesome, valid options include `thin`, `light`, `regular`, and `solid` for the `classic` and `sharp` families. Some variants require a Font Awesome Pro subscription. Custom icon libraries may or may not use this property.
       */
      variant?: string;
      /**
       * Sets the width of the icon to match the cropped SVG viewBox. This operates like the Font `fa-width-auto` class.
       * @default false
       */
      'auto-width'?: boolean;
      /** Swaps the opacity of duotone icons. @default false */
      'swap-opacity'?: boolean;
      /**
       * An external URL of an SVG file. Be sure you trust the content you are including, as it will be executed as code and can result in XSS attacks.
       */
      src?: string | undefined;
      /**
       * An alternate description to use for assistive devices. If omitted, the icon will be considered presentational and ignored by assistive devices.
       * @default ''
       */
      label?: string;
      /** The name of a registered custom icon library. @default 'default' */
      library?: string;
      /** Sets the rotation degree of the icon @default 0 */
      rotate?: number;
      /** Sets the flip direction of the icon along the 'x' (horizontal), 'y' (vertical), or 'both' axes. */
      flip?: 'x' | 'y' | 'both' | undefined;
      /** Sets the animation for the icon */
      animation?: unknown;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The checkbox's label.
     * @slot hint - Text that describes how to use the checkbox. Alternatively, you can use the `hint` attribute.
     * @csspart base - The component's label .
     * @csspart control - The square container that wraps the checkbox's checked state.
     * @csspart checked-icon - The checked icon, a `<wa-icon>` element.
     * @csspart indeterminate-icon - The indeterminate icon, a `<wa-icon>` element.
     * @csspart label - The container that wraps the checkbox's label.
     * @csspart hint - The hint's wrapper.
     * @cssproperty --checked-icon-color - The color of the checked and indeterminate icons.
     * @cssproperty --checked-icon-scale - The size of the checked and indeterminate icons relative to the checkbox.
     * @event wa-invalid - Emitted when the form control has been checked for validity and its constraints aren't satisfied. (React: `onWaInvalid`)
     */
    'beam-checkbox': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** @default '' */
      title?: string;
      /** The name of the checkbox, submitted as a name/value pair with form data. @default null */
      name?: string | null;
      /** The value of the checkbox, submitted as a name/value pair with form data. */
      value?: string | null;
      /** The checkbox's size. @default 'medium' */
      size?: 'small' | 'medium' | 'large';
      /** Disables the checkbox. @default false */
      disabled?: boolean;
      /**
       * Draws the checkbox in an indeterminate state. This is usually applied to checkboxes that represents a "select all/none" behavior when associated checkboxes have a mix of checked and unchecked states.
       * @default false
       */
      indeterminate?: boolean;
      /** The default value of the form control. Primarily used for resetting the form control. */
      checked?: boolean;
      /** Makes the checkbox a required field. @default false */
      required?: boolean;
      /** The checkbox's hint. If you need to display HTML, use the `hint` slot instead. @default '' */
      hint?: string;
      /** @default null */
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @csspart base - The component's base wrapper.
     * @cssproperty --track-width - The width of the track.
     * @cssproperty --track-color - The color of the track.
     * @cssproperty --indicator-color - The color of the spinner's indicator.
     * @cssproperty --speed - The time it takes for the spinner to complete one animation cycle.
     */
    'beam-spinner': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The default slot.
     * @slot expand-icon - The icon to show when the tree item is expanded.
     * @slot collapse-icon - The icon to show when the tree item is collapsed.
     * @csspart base - The component's base wrapper.
     * @csspart item - The tree item's container. This element wraps everything except slotted tree item children.
     * @csspart indentation - The tree item's indentation container.
     * @csspart expand-button - The container that wraps the tree item's expand button and spinner.
     * @csspart spinner - The spinner that shows when a lazy tree item is in the loading state.
     * @csspart spinner__base - The spinner's base part.
     * @csspart label - The tree item's label.
     * @csspart children - The container that wraps the tree item's nested children.
     * @csspart checkbox - The checkbox that shows when using multiselect.
     * @csspart checkbox__base - The checkbox's exported `base` part.
     * @csspart checkbox__control - The checkbox's exported `control` part.
     * @csspart checkbox__checked-icon - The checkbox's exported `checked-icon` part.
     * @csspart checkbox__indeterminate-icon - The checkbox's exported `indeterminate-icon` part.
     * @csspart checkbox__label - The checkbox's exported `label` part.
     * @cssproperty --show-duration - The animation duration when expanding tree items. [default: 200ms]
     * @cssproperty --hide-duration - The animation duration when collapsing tree items. [default: 200ms]
     * @event wa-expand - Emitted when the tree item expands. (React: `onWaExpand`)
     * @event wa-after-expand - Emitted after the tree item expands and all animations are complete. (React: `onWaAfterExpand`)
     * @event wa-collapse - Emitted when the tree item collapses. (React: `onWaCollapse`)
     * @event wa-after-collapse - Emitted after the tree item collapses and all animations are complete. (React: `onWaAfterCollapse`)
     * @event wa-lazy-change - Emitted when the tree item's lazy state changes. (React: `onWaLazyChange`)
     * @event wa-lazy-load - Emitted when a lazy item is selected. Use this event to asynchronously load data and append items to the tree before expanding. After appending new items, remove the `lazy` attribute to remove the loading state and update the tree. (React: `onWaLazyLoad`)
     */
    'beam-tree-item': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** Expands the tree item. @default false */
      expanded?: boolean;
      /** Draws the tree item in a selected state. @default false */
      selected?: boolean;
      /** Disables the tree item. @default false */
      disabled?: boolean;
      /** Enables lazy loading behavior. @default false */
      lazy?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The button's label.
     * @slot start - An element, such as `<wa-icon>`, placed before the label.
     * @slot end - An element, such as `<wa-icon>`, placed after the label.
     * @csspart base - The component's base wrapper.
     * @csspart start - The container that wraps the `start` slot.
     * @csspart label - The button's label.
     * @csspart end - The container that wraps the `end` slot.
     * @csspart caret - The button's caret icon, a `<wa-icon>` element.
     * @csspart spinner - The spinner that shows when the button is in the loading state.
     * @event wa-invalid - Emitted when the form control has been checked for validity and its constraints aren't satisfied. (React: `onWaInvalid`)
     */
    'beam-button': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** @default '' */
      title?: string;
      /**
       * The button's theme variant. Defaults to `neutral` if not within another element with a variant.
       * @default 'neutral'
       */
      variant?: 'neutral' | 'brand' | 'success' | 'warning' | 'danger';
      /** The button's visual appearance. @default 'accent' */
      appearance?: 'accent' | 'filled' | 'outlined' | 'filled-outlined' | 'plain';
      /** The button's size. @default 'medium' */
      size?: 'small' | 'medium' | 'large';
      /**
       * Draws the button with a caret. Used to indicate that the button triggers a dropdown menu or similar behavior.
       * @default false
       */
      'with-caret'?: boolean;
      /**
       * Only required for SSR. Set to `true` if you're slotting in a `start` element so the server-rendered markup includes the start slot before the component hydrates on the client.
       * @default false
       */
      'with-start'?: boolean;
      /**
       * Only required for SSR. Set to `true` if you're slotting in an `end` element so the server-rendered markup includes the end slot before the component hydrates on the client.
       * @default false
       */
      'with-end'?: boolean;
      /** Disables the button. @default false */
      disabled?: boolean;
      /** Draws the button in a loading state. @default false */
      loading?: boolean;
      /** Draws a pill-style button with rounded edges. @default false */
      pill?: boolean;
      /**
       * The type of button. Note that the default value is `button` instead of `submit`, which is opposite of how native `<button>` elements behave. When the type is `submit`, the button will submit the surrounding form.
       * @default 'button'
       */
      type?: 'button' | 'submit' | 'reset';
      /**
       * The name of the button, submitted as a name/value pair with form data, but only when this button is the submitter. This attribute is ignored when `href` is present.
       * @default null
       */
      name?: string | null;
      /**
       * The value of the button, submitted as a pair with the button's name as part of the form data, but only when this button is the submitter. This attribute is ignored when `href` is present.
       */
      value?: string;
      /**
       * When set, the underlying button will be rendered as an `<a>` with this `href` instead of a `<button>`.
       */
      href?: string;
      /** Tells the browser where to open the link. Only used when `href` is present. */
      target?: '_blank' | '_parent' | '_self' | '_top';
      /** When using `href`, this attribute will map to the underlying link's `rel` attribute. */
      rel?: string | undefined;
      /** Tells the browser to download the linked file as this filename. Only used when `href` is present. */
      download?: string | undefined;
      /** Used to override the form owner's `action` attribute. */
      formaction?: string;
      /** Used to override the form owner's `enctype` attribute. */
      formenctype?: 'application/x-www-form-urlencoded' | 'multipart/form-data' | 'text/plain';
      /** Used to override the form owner's `method` attribute. */
      formmethod?: 'post' | 'get';
      /** Used to override the form owner's `novalidate` attribute. */
      formnovalidate?: boolean;
      /** Used to override the form owner's `target` attribute. */
      formtarget?: '_self' | '_blank' | '_parent' | '_top' | string;
      /** @default null */
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The element to animate. Avoid slotting in more than one element, as subsequent ones will be ignored. To animate multiple elements, either wrap them in a single container or use multiple `<wa-animation>` elements.
     * @event wa-cancel - Emitted when the animation is canceled. (React: `onWaCancel`)
     * @event wa-finish - Emitted when the animation finishes. (React: `onWaFinish`)
     * @event wa-start - Emitted when the animation starts or restarts. (React: `onWaStart`)
     */
    'beam-animation': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * The name of the built-in animation to use. For custom animations, use the `keyframes` prop.
       * @default 'none'
       */
      name?: string;
      /**
       * Plays the animation. When omitted, the animation will be paused. This attribute will be automatically removed when the animation finishes or gets canceled.
       * @default false
       */
      play?: boolean;
      /** The number of milliseconds to delay the start of the animation. @default 0 */
      delay?: number;
      /**
       * Determines the direction of playback as well as the behavior when reaching the end of an iteration. [Learn more](https://developer.mozilla.org/en-US/docs/Web/CSS/animation-direction)
       * @default 'normal'
       */
      direction?: unknown;
      /** The number of milliseconds each iteration of the animation takes to complete. @default 1000 */
      duration?: number;
      /**
       * The easing function to use for the animation. This can be a Web Awesome easing function or a custom easing function such as `cubic-bezier(0, 1, .76, 1.14)`.
       * @default 'linear'
       */
      easing?: string;
      /** The number of milliseconds to delay after the active period of an animation sequence. @default 0 */
      'end-delay'?: number;
      /** Sets how the animation applies styles to its target before and after its execution. @default 'auto' */
      fill?: unknown;
      /**
       * The number of iterations to run before the animation completes. Defaults to `Infinity`, which loops.
       * @default Infinity
       */
      iterations?: number;
      /** The offset at which to start the animation, usually between 0 (start) and 1 (end). @default 0 */
      'iteration-start'?: number;
      /**
       * Sets the animation's playback rate. The default is `1`, which plays the animation at a normal speed. Setting this to `2`, for example, will double the animation's speed. A negative value can be used to reverse the animation. This value can be changed without causing the animation to restart.
       * @default 1
       */
      'playback-rate'?: number;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot icon - The default icon to use when no image or initials are present. Works best with `<wa-icon>`.
     * @csspart icon - The container that wraps the avatar's icon.
     * @csspart initials - The container that wraps the avatar's initials.
     * @csspart image - The avatar image. Only shown when the `image` attribute is set.
     * @cssproperty --size - The size of the avatar.
     * @event wa-error - The image could not be loaded. This may because of an invalid URL, a temporary network condition, or some unknown cause. (React: `onWaError`)
     */
    'beam-avatar': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** The image source to use for the avatar. @default '' */
      image?: string;
      /** A label to use to describe the avatar to assistive devices. @default '' */
      label?: string;
      /**
       * Initials to use as a fallback when no image is available (1-2 characters max recommended).
       * @default ''
       */
      initials?: string;
      /** Indicates how the browser should load the image. @default 'eager' */
      loading?: 'eager' | 'lazy';
      /** The shape of the avatar. @default 'circle' */
      shape?: 'circle' | 'square' | 'rounded';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The badge's content.
     * @slot start - An element, such as `<wa-icon>`, placed before the label.
     * @slot end - An element, such as `<wa-icon>`, placed after the label.
     * @csspart base - The component's base wrapper.
     * @csspart start - The container that wraps the `start` slot.
     * @csspart end - The container that wraps the `end` slot.
     * @cssproperty --pulse-color - The color of the badge's pulse effect when using `attention="pulse"`.
     */
    'beam-badge': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * The badge's theme variant. Defaults to `brand` if not within another element with a variant.
       * @default 'brand'
       */
      variant?: 'brand' | 'neutral' | 'success' | 'warning' | 'danger';
      /** The badge's visual appearance. @default 'accent' */
      appearance?: 'accent' | 'filled' | 'outlined' | 'filled-outlined';
      /** Draws a pill-style badge with rounded edges. @default false */
      pill?: boolean;
      /** Adds an animation to draw attention to the badge. @default 'none' */
      attention?: 'none' | 'pulse' | 'bounce';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The breadcrumb item's label.
     * @slot start - An element, such as `<wa-icon>`, placed before the label.
     * @slot end - An element, such as `<wa-icon>`, placed after the label.
     * @slot separator - The separator to use for the breadcrumb item. This will only change the separator for this item. If you want to change it for all items in the group, set the separator on `<wa-breadcrumb>` instead.
     * @csspart label - The breadcrumb item's label.
     * @csspart start - The container that wraps the `start` slot.
     * @csspart end - The container that wraps the `end` slot.
     * @csspart separator - The container that wraps the separator.
     */
    'beam-breadcrumb-item': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * Optional URL to direct the user to when the breadcrumb item is activated. When set, a link will be rendered internally. When unset, a button will be rendered instead.
       */
      href?: string | undefined;
      /** Tells the browser where to open the link. Only used when `href` is set. */
      target?: '_blank' | '_parent' | '_self' | '_top' | undefined;
      /** The `rel` attribute to use on the link. Only used when `href` is set. @default 'noreferrer noopener' */
      rel?: string;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - One or more breadcrumb items to display.
     * @slot separator - The separator to use between breadcrumb items. Works best with `<wa-icon>`.
     * @csspart base - The component's base wrapper.
     */
    'beam-breadcrumb': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * The label to use for the breadcrumb control. This will not be shown on the screen, but it will be announced by screen readers and other assistive devices to provide more context for users.
       * @default ''
       */
      label?: string;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - One or more `<wa-button>` elements to display in the button group.
     * @csspart base - The component's base wrapper.
     */
    'beam-button-group': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * A label to use for the button group. This won't be displayed on the screen, but it will be announced by assistive devices when interacting with the control and is strongly recommended.
       * @default ''
       */
      label?: string;
      /** The button group's orientation. @default 'horizontal' */
      orientation?: 'horizontal' | 'vertical';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The callout's main content.
     * @slot icon - An icon to show in the callout. Works best with `<wa-icon>`.
     * @csspart icon - The container that wraps the optional icon.
     * @csspart message - The container that wraps the callout's main content.
     */
    'beam-callout': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * The callout's theme variant. Defaults to `brand` if not within another element with a variant.
       * @default 'brand'
       */
      variant?: 'brand' | 'neutral' | 'success' | 'warning' | 'danger';
      /** The callout's visual appearance. */
      appearance?: 'accent' | 'filled' | 'outlined' | 'plain' | 'filled-outlined';
      /** The callout's size. @default 'medium' */
      size?: 'small' | 'medium' | 'large';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The card's main content.
     * @slot header - An optional header for the card.
     * @slot footer - An optional footer for the card.
     * @slot media - An optional media section to render at the start of the card.
     * @slot actions - An optional actions section to render at the end for the horizontal card.
     * @slot header-actions - An optional actions section to render in the header of the vertical card.
     * @slot footer-actions - An optional actions section to render in the footer of the vertical card.
     * @csspart media - The container that wraps the card's media.
     * @csspart header - The container that wraps the card's header.
     * @csspart body - The container that wraps the card's main content.
     * @csspart footer - The container that wraps the card's footer.
     * @cssproperty --spacing - The amount of space around and between sections of the card. Expects a single value. [default: var(--wa-space-l)]
     */
    'beam-card': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** The card's visual appearance. @default 'outlined' */
      appearance?: 'accent' | 'filled' | 'outlined' | 'filled-outlined' | 'plain';
      /**
       * Only required for SSR. Set to `true` if you're slotting in a `header` element so the server-rendered markup includes the header before the component hydrates on the client.
       * @default false
       */
      'with-header'?: boolean;
      /**
       * Only required for SSR. Set to `true` if you're slotting in a `media` element so the server-rendered markup includes the media before the component hydrates on the client.
       * @default false
       */
      'with-media'?: boolean;
      /**
       * Only required for SSR. Set to `true` if you're slotting in a `footer` element so the server-rendered markup includes the footer before the component hydrates on the client.
       * @default false
       */
      'with-footer'?: boolean;
      /** Renders the card's orientation * @default 'vertical' */
      orientation?: 'horizontal' | 'vertical';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot label - The input's label. Alternatively, you can use the `label` attribute.
     * @slot start - An element, such as `<wa-icon>`, placed at the start of the input control.
     * @slot end - An element, such as `<wa-icon>`, placed at the end of the input control.
     * @slot clear-icon - An icon to use in lieu of the default clear icon.
     * @slot show-password-icon - An icon to use in lieu of the default show password icon.
     * @slot hide-password-icon - An icon to use in lieu of the default hide password icon.
     * @slot hint - Text that describes how to use the input. Alternatively, you can use the `hint` attribute.
     * @csspart label - The label
     * @csspart hint - The hint's wrapper.
     * @csspart base - The wrapper being rendered as an input
     * @csspart input - The internal `<input>` control.
     * @csspart start - The container that wraps the `start` slot.
     * @csspart clear-button - The clear button.
     * @csspart password-toggle-button - The password toggle button.
     * @csspart end - The container that wraps the `end` slot.
     * @event wa-clear - Emitted when the clear button is activated. (React: `onWaClear`)
     * @event wa-invalid - Emitted when the form control has been checked for validity and its constraints aren't satisfied. (React: `onWaInvalid`)
     */
    'beam-input': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** @default '' */
      title?: string;
      /**
       * The type of input. Works the same as a native `<input>` element, but only a subset of types are supported. Defaults to `text`.
       * @default 'text'
       */
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
      /** The default value of the form control. Primarily used for resetting the form control. */
      value?: string | null;
      /** The input's size. @default 'medium' */
      size?: 'small' | 'medium' | 'large';
      /** The input's visual appearance. @default 'outlined' */
      appearance?: 'filled' | 'outlined' | 'filled-outlined';
      /** Draws a pill-style input with rounded edges. @default false */
      pill?: boolean;
      /** The input's label. If you need to display HTML, use the `label` slot instead. @default '' */
      label?: string;
      /** The input's hint. If you need to display HTML, use the `hint` slot instead. @default '' */
      hint?: string;
      /** Adds a clear button when the input is not empty. @default false */
      'with-clear'?: boolean;
      /** Placeholder text to show as a hint when the input is empty. @default '' */
      placeholder?: string;
      /** Makes the input readonly. @default false */
      readonly?: boolean;
      /** Adds a button to toggle the password's visibility. Only applies to password types. @default false */
      'password-toggle'?: boolean;
      /**
       * Determines whether or not the password is currently visible. Only applies to password input types.
       * @default false
       */
      'password-visible'?: boolean;
      /** Hides the browser's built-in increment/decrement spin buttons for number inputs. @default false */
      'without-spin-buttons'?: boolean;
      /** Makes the input a required field. @default false */
      required?: boolean;
      /** A regular expression pattern to validate input against. */
      pattern?: string;
      /** The minimum length of input that will be considered valid. */
      minlength?: number;
      /** The maximum length of input that will be considered valid. */
      maxlength?: number;
      /** The input's minimum value. Only applies to date and number input types. */
      min?: number | string;
      /** The input's maximum value. Only applies to date and number input types. */
      max?: number | string;
      /**
       * Specifies the granularity that the value must adhere to, or the special value `any` which means no stepping is implied, allowing any numeric value. Only applies to date and number input types.
       */
      step?: number | 'any';
      /** Controls whether and how text input is automatically capitalized as it is entered by the user. */
      autocapitalize?: 'off' | 'none' | 'on' | 'sentences' | 'words' | 'characters';
      /**
       * Indicates whether the browser's autocorrect feature is on or off. When set as an attribute, use `"off"` or `"on"`. When set as a property, use `true` or `false`.
       */
      autocorrect?: boolean;
      /**
       * Specifies what permission the browser has to provide assistance in filling out form field values. Refer to [this page on MDN](https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/autocomplete) for available values.
       */
      autocomplete?: string;
      /** Indicates that the input should receive focus on page load. */
      autofocus?: boolean;
      /** Used to customize the label or icon of the Enter key on virtual keyboards. */
      enterkeyhint?: 'enter' | 'done' | 'go' | 'next' | 'previous' | 'search' | 'send';
      /** Enables spell checking on the input. @default true */
      spellcheck?: boolean;
      /**
       * Tells the browser what type of data will be entered by the user, allowing it to display the appropriate virtual keyboard on supportive devices.
       */
      inputmode?: 'none' | 'text' | 'decimal' | 'numeric' | 'tel' | 'search' | 'email' | 'url';
      /**
       * Only required for SSR. Set to `true` if you're slotting in a `label` element so the server-rendered markup includes the label before the component hydrates on the client.
       * @default false
       */
      'with-label'?: boolean;
      /**
       * Only required for SSR. Set to `true` if you're slotting in a `hint` element so the server-rendered markup includes the hint before the component hydrates on the client.
       * @default false
       */
      'with-hint'?: boolean;
      /** The name of the input, submitted as a name/value pair with form data. @default null */
      name?: string | null;
      /** Disables the form control. @default false */
      disabled?: boolean;
      /** @default null */
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The popup's content.
     * @slot anchor - The element the popup will be anchored to. If the anchor lives outside of the popup, you can use the `anchor` attribute or property instead.
     * @csspart arrow - The arrow's container. Avoid setting `top|bottom|left|right` properties, as these values are assigned dynamically as the popup moves. This is most useful for applying a background color to match the popup, and maybe a border or box shadow.
     * @csspart popup - The popup's container. Useful for setting a background color, box shadow, etc.
     * @csspart hover-bridge - The hover bridge element. Only available when the `hover-bridge` option is enabled.
     * @cssproperty --arrow-size - The size of the arrow. Note that an arrow won't be shown unless the `arrow` attribute is used. [default: 6px]
     * @cssproperty --popup-border-width - The width of any custom border applied to the popup. This is used to reposition the arrow to overlap to the inside edge of the popup border.
     * @cssproperty --arrow-color - The color of the arrow. [default: black]
     * @cssproperty --auto-size-available-width - A read-only custom property that determines the amount of width the popup can be before overflowing. Useful for positioning child elements that need to overflow. This property is only available when using `auto-size`.
     * @cssproperty --auto-size-available-height - A read-only custom property that determines the amount of height the popup can be before overflowing. Useful for positioning child elements that need to overflow. This property is only available when using `auto-size`.
     * @cssproperty --show-duration - The show duration to use when applying built-in animation classes. [default: 100ms]
     * @cssproperty --hide-duration - The hide duration to use when applying built-in animation classes. [default: 100ms]
     * @event wa-reposition - Emitted when the popup is repositioned. This event can fire a lot, so avoid putting expensive operations in your listener or consider debouncing it. (React: `onWaReposition`)
     */
    'beam-popup': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * The element the popup will be anchored to. If the anchor lives outside of the popup, you can provide the anchor element `id`, a DOM element reference, or a `VirtualElement`. If the anchor lives inside the popup, use the `anchor` slot instead.
       */
      anchor?: unknown;
      /**
       * Activates the positioning logic and shows the popup. When this attribute is removed, the positioning logic is torn down and the popup will be hidden.
       * @default false
       */
      active?: boolean;
      /**
       * The preferred placement of the popup. Note that the actual placement will vary as configured to keep the panel inside of the viewport.
       * @default 'top'
       */
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
      /** The bounding box to use for flipping, shifting, and auto-sizing. @default 'viewport' */
      boundary?: 'viewport' | 'scroll';
      /** The distance in pixels from which to offset the panel away from its anchor. @default 0 */
      distance?: number;
      /** The distance in pixels from which to offset the panel along its anchor. @default 0 */
      skidding?: number;
      /**
       * Attaches an arrow to the popup. The arrow's size and color can be customized using the `--arrow-size` and `--arrow-color` custom properties. For additional customizations, you can also target the arrow using `::part(arrow)` in your stylesheet.
       * @default false
       */
      arrow?: boolean;
      /**
       * The placement of the arrow. The default is `anchor`, which will align the arrow as close to the center of the anchor as possible, considering available space and `arrow-padding`. A value of `start`, `end`, or `center` will align the arrow to the start, end, or center of the popover instead.
       * @default 'anchor'
       */
      'arrow-placement'?: 'start' | 'end' | 'center' | 'anchor';
      /**
       * The amount of padding between the arrow and the edges of the popup. If the popup has a border-radius, for example, this will prevent it from overflowing the corners.
       * @default 10
       */
      'arrow-padding'?: number;
      /**
       * When set, placement of the popup will flip to the opposite site to keep it in view. You can use `flipFallbackPlacements` to further configure how the fallback placement is determined.
       * @default false
       */
      flip?: boolean;
      /**
       * If the preferred placement doesn't fit, popup will be tested in these fallback placements until one fits. Must be a string of any number of placements separated by a space, e.g. "top bottom left". If no placement fits, the flip fallback strategy will be used instead.
       * @default ''
       */
      'flip-fallback-placements'?: string;
      /**
       * When neither the preferred placement nor the fallback placements fit, this value will be used to determine whether the popup should be positioned using the best available fit based on available space or as it was initially preferred.
       * @default 'best-fit'
       */
      'flip-fallback-strategy'?: 'best-fit' | 'initial';
      /**
       * The flip boundary describes clipping element(s) that overflow will be checked relative to when flipping. By default, the boundary includes overflow ancestors that will cause the element to be clipped. If needed, you can change the boundary by passing a reference to one or more elements to this property.
       */
      flipBoundary?: Element | Element[];
      /** The amount of padding, in pixels, to exceed before the flip behavior will occur. @default 0 */
      'flip-padding'?: number;
      /** Moves the popup along the axis to keep it in view when clipped. @default false */
      shift?: boolean;
      /**
       * The shift boundary describes clipping element(s) that overflow will be checked relative to when shifting. By default, the boundary includes overflow ancestors that will cause the element to be clipped. If needed, you can change the boundary by passing a reference to one or more elements to this property.
       */
      shiftBoundary?: Element | Element[];
      /** The amount of padding, in pixels, to exceed before the shift behavior will occur. @default 0 */
      'shift-padding'?: number;
      /** When set, this will cause the popup to automatically resize itself to prevent it from overflowing. */
      'auto-size'?: 'horizontal' | 'vertical' | 'both';
      /** Syncs the popup's width or height to that of the anchor element. */
      sync?: 'width' | 'height' | 'both';
      /**
       * The auto-size boundary describes clipping element(s) that overflow will be checked relative to when resizing. By default, the boundary includes overflow ancestors that will cause the element to be clipped. If needed, you can change the boundary by passing a reference to one or more elements to this property.
       */
      autoSizeBoundary?: Element | Element[];
      /** The amount of padding, in pixels, to exceed before the auto-size behavior will occur. @default 0 */
      'auto-size-padding'?: number;
      /**
       * When a gap exists between the anchor and the popup element, this option will add a "hover bridge" that fills the gap using an invisible element. This makes listening for events such as `mouseenter` and `mouseleave` more sane because the pointer never technically leaves the element. The hover bridge will only be drawn when the popover is active.
       * @default false
       */
      'hover-bridge'?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot label - The color picker's form label. Alternatively, you can use the `label` attribute.
     * @slot hint - The color picker's form hint. Alternatively, you can use the `hint` attribute.
     * @csspart base - The component's base wrapper.
     * @csspart trigger - The color picker's dropdown trigger.
     * @csspart swatches - The container that holds the swatches.
     * @csspart swatch - Each individual swatch.
     * @csspart grid - The color grid.
     * @csspart grid-handle - The color grid's handle.
     * @csspart slider - Hue and opacity sliders.
     * @csspart slider-handle - Hue and opacity slider handles.
     * @csspart hue-slider - The hue slider.
     * @csspart hue-slider-handle - The hue slider's handle.
     * @csspart opacity-slider - The opacity slider.
     * @csspart opacity-slider-handle - The opacity slider's handle.
     * @csspart preview - The preview color.
     * @csspart input - The text input.
     * @csspart eyedropper-button - The eye dropper button.
     * @csspart eyedropper-button__base - The eye dropper button's exported `button` part.
     * @csspart eyedropper-button__start - The eye dropper button's exported `start` part.
     * @csspart eyedropper-button__label - The eye dropper button's exported `label` part.
     * @csspart eyedropper-button__end - The eye dropper button's exported `end` part.
     * @csspart eyedropper-button__caret - The eye dropper button's exported `caret` part.
     * @csspart format-button - The format button.
     * @csspart format-button__base - The format button's exported `button` part.
     * @csspart format-button__start - The format button's exported `start` part.
     * @csspart format-button__label - The format button's exported `label` part.
     * @csspart format-button__end - The format button's exported `end` part.
     * @csspart format-button__caret - The format button's exported `caret` part.
     * @cssproperty --grid-width - The width of the color grid.
     * @cssproperty --grid-height - The height of the color grid.
     * @cssproperty --grid-handle-size - The size of the color grid's handle.
     * @cssproperty --slider-height - The height of the hue and alpha sliders.
     * @cssproperty --slider-handle-size - The diameter of the slider's handle.
     * @event wa-show (React: `onWaShow`)
     * @event wa-after-show (React: `onWaAfterShow`)
     * @event wa-hide (React: `onWaHide`)
     * @event wa-after-hide (React: `onWaAfterHide`)
     * @event wa-invalid - Emitted when the form control has been checked for validity and its constraints aren't satisfied. (React: `onWaInvalid`)
     */
    'beam-color-picker': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** The default value of the form control. Primarily used for resetting the form control. */
      value?: string | null;
      /**
       * Only required for SSR. Set to `true` if you're slotting in a `label` element so the server-rendered markup includes the label before the component hydrates on the client.
       * @default false
       */
      'with-label'?: boolean;
      /**
       * Only required for SSR. Set to `true` if you're slotting in a `hint` element so the server-rendered markup includes the hint before the component hydrates on the client.
       * @default false
       */
      'with-hint'?: boolean;
      /**
       * The color picker's label. This will not be displayed, but it will be announced by assistive devices. If you need to display HTML, you can use the `label` slot` instead.
       * @default ''
       */
      label?: string;
      /** The color picker's hint. If you need to display HTML, use the `hint` slot instead. @default '' */
      hint?: string;
      /**
       * The format to use. If opacity is enabled, these will translate to HEXA, RGBA, HSLA, and HSVA respectively. The color picker will accept user input in any format (including CSS color names) and convert it to the desired format.
       * @default 'hex'
       */
      format?: 'hex' | 'rgb' | 'hsl' | 'hsv';
      /** Determines the size of the color picker's trigger @default 'medium' */
      size?: 'small' | 'medium' | 'large';
      /**
       * The preferred placement of the color picker's popup. Note that the actual placement will vary as configured to keep the panel inside of the viewport.
       * @default 'bottom-start'
       */
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
      /** Removes the button that lets users toggle between format. @default false */
      'without-format-toggle'?: boolean;
      /** The name of the form control, submitted as a name/value pair with form data. @default null */
      name?: string | null;
      /** Disables the color picker. @default false */
      disabled?: boolean;
      /**
       * Indicates whether or not the popup is open. You can toggle this attribute to show and hide the popup, or you can use the `show()` and `hide()` methods and this attribute will reflect the popup's open state.
       * @default false
       */
      open?: boolean;
      /**
       * Shows the opacity slider. Enabling this will cause the formatted value to be HEXA, RGBA, or HSLA.
       * @default false
       */
      opacity?: boolean;
      /**
       * By default, values are lowercase. With this attribute, values will be uppercase instead.
       * @default false
       */
      uppercase?: boolean;
      /**
       * One or more predefined color swatches to display as presets in the color picker. Can include any format the color picker can parse, including HEX(A), RGB(A), HSL(A), HSV(A), and CSS color names. Each color must be separated by a semicolon (`;`). Alternatively, you can pass an array of color values or an array of `{ color, label }` objects to this property using JavaScript. When using objects with labels, the label will be used for the swatch's accessible name instead of the raw color value.
       * @default ''
       */
      swatches?: unknown;
      /** Makes the color picker a required field. @default false */
      required?: boolean;
      /** @default null */
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The tooltip's default slot where any content should live. Interactive content should be avoided.
     * @csspart base - The component's base wrapper, an `<wa-popup>` element.
     * @csspart base__popup - The popup's exported `popup` part. Use this to target the tooltip's popup container.
     * @csspart base__arrow - The popup's exported `arrow` part. Use this to target the tooltip's arrow.
     * @csspart body - The tooltip's body where its content is rendered.
     * @cssproperty --max-width - The maximum width of the tooltip before its content will wrap.
     * @event wa-show - Emitted when the tooltip begins to show. (React: `onWaShow`)
     * @event wa-after-show - Emitted after the tooltip has shown and all animations are complete. (React: `onWaAfterShow`)
     * @event wa-hide - Emitted when the tooltip begins to hide. (React: `onWaHide`)
     * @event wa-after-hide - Emitted after the tooltip has hidden and all animations are complete. (React: `onWaAfterHide`)
     */
    'beam-tooltip': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * The preferred placement of the tooltip. Note that the actual placement may vary as needed to keep the tooltip inside of the viewport.
       * @default 'top'
       */
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
      /** Disables the tooltip so it won't show when triggered. @default false */
      disabled?: boolean;
      /** The distance in pixels from which to offset the tooltip away from its target. @default 8 */
      distance?: number;
      /**
       * Indicates whether or not the tooltip is open. You can use this in lieu of the show/hide methods.
       * @default false
       */
      open?: boolean;
      /** The distance in pixels from which to offset the tooltip along its target. @default 0 */
      skidding?: number;
      /** The amount of time to wait before showing the tooltip when the user mouses in. @default 150 */
      'show-delay'?: number;
      /** The amount of time to wait before hiding the tooltip when the user mouses out. @default 0 */
      'hide-delay'?: number;
      /**
       * Controls how the tooltip is activated. Possible options include `click`, `hover`, `focus`, and `manual`. Multiple options can be passed by separating them with a space. When manual is used, the tooltip must be activated programmatically.
       * @default 'hover focus'
       */
      trigger?: string;
      /** Removes the arrow from the tooltip. @default false */
      'without-arrow'?: boolean;
      /** @default null */
      for?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The trigger element. By default, a copy icon button is rendered so this is optional. If desired, you can slot in a custom element such as `<wa-button>` or `<button>`.
     * @slot copy-icon - The icon to show in the default copy state. Works best with `<wa-icon>`.
     * @slot success-icon - The icon to show when the content is copied. Works best with `<wa-icon>`.
     * @slot error-icon - The icon to show when a copy error occurs. Works best with `<wa-icon>`.
     * @csspart button - The internal `<button>` element.
     * @csspart copy-icon - The container that holds the copy icon.
     * @csspart success-icon - The container that holds the success icon.
     * @csspart error-icon - The container that holds the error icon.
     * @csspart tooltip__base - The tooltip's exported `base` part.
     * @csspart tooltip__base__popup - The tooltip's exported `popup` part.
     * @csspart tooltip__base__arrow - The tooltip's exported `arrow` part.
     * @csspart tooltip__body - The tooltip's exported `body` part.
     * @event wa-copy - Emitted when the data has been copied. (React: `onWaCopy`)
     * @event wa-error - Emitted when the data could not be copied. (React: `onWaError`)
     */
    'beam-copy-button': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** The text value to copy. @default '' */
      value?: string;
      /**
       * An id that references an element in the same document from which data will be copied. If both this and `value` are present, this value will take precedence. By default, the target element's `textContent` will be copied. To copy an attribute, append the attribute name wrapped in square brackets, e.g. `from="el[value]"`. To copy a property, append a dot and the property name, e.g. `from="el.value"`.
       * @default ''
       */
      from?: string;
      /** Disables the copy button. @default false */
      disabled?: boolean;
      /** A custom label to show in the tooltip. @default '' */
      'copy-label'?: string;
      /** A custom label to show in the tooltip after copying. @default '' */
      'success-label'?: string;
      /** A custom label to show in the tooltip when a copy error occurs. @default '' */
      'error-label'?: string;
      /** The length of time to show feedback before restoring the default trigger. @default 1000 */
      'feedback-duration'?: number;
      /** The preferred placement of the tooltip. @default 'top' */
      'tooltip-placement'?: 'top' | 'right' | 'bottom' | 'left';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The details' main content.
     * @slot summary - The details' summary. Alternatively, you can use the `summary` attribute.
     * @slot expand-icon - Optional expand icon to use instead of the default. Works best with `<wa-icon>`.
     * @slot collapse-icon - Optional collapse icon to use instead of the default. Works best with `<wa-icon>`.
     * @csspart base - The inner `<details>` element used to render the component. Styles you apply to the component are automatically applied to this part, so you usually don't need to deal with it unless you need to set the `display` property.
     * @csspart header - The header that wraps both the summary and the expand/collapse icon.
     * @csspart summary - The container that wraps the summary.
     * @csspart icon - The container that wraps the expand/collapse icons.
     * @csspart content - The details content.
     * @cssproperty --spacing - The amount of space around and between the details' content. Expects a single value.
     * @cssproperty --show-duration - The show duration to use when applying built-in animation classes. [default: 200ms]
     * @cssproperty --hide-duration - The hide duration to use when applying built-in animation classes. [default: 200ms]
     * @event wa-show - Emitted when the details opens. (React: `onWaShow`)
     * @event wa-after-show - Emitted after the details opens and all animations are complete. (React: `onWaAfterShow`)
     * @event wa-hide - Emitted when the details closes. (React: `onWaHide`)
     * @event wa-after-hide - Emitted after the details closes and all animations are complete. (React: `onWaAfterHide`)
     */
    'beam-details': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * Indicates whether or not the details is open. You can toggle this attribute to show and hide the details, or you can use the `show()` and `hide()` methods and this attribute will reflect the details' open state.
       * @default false
       */
      open?: boolean;
      /** The summary to show in the header. If you need to display HTML, use the `summary` slot instead. */
      summary?: string;
      /** Groups related details elements. When one opens, others with the same name will close. */
      name?: string;
      /** Disables the details so it can't be toggled. @default false */
      disabled?: boolean;
      /** The element's visual appearance. @default 'outlined' */
      appearance?: 'filled' | 'outlined' | 'filled-outlined' | 'plain';
      /** The location of the expand/collapse icon. @default 'end' */
      'icon-placement'?: 'start' | 'end';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The dialog's main content.
     * @slot label - The dialog's label. Alternatively, you can use the `label` attribute.
     * @slot header-actions - Optional actions to add to the header. Works best with `<wa-button>`.
     * @slot footer - The dialog's footer, usually one or more buttons representing various options.
     * @csspart dialog - The dialog's internal `<dialog>` element.
     * @csspart header - The dialog's header. This element wraps the title and header actions.
     * @csspart header-actions - Optional actions to add to the header. Works best with `<wa-button>`.
     * @csspart title - The dialog's title.
     * @csspart close-button - The close button, a `<wa-button>`.
     * @csspart close-button__base - The close button's exported `base` part.
     * @csspart body - The dialog's body.
     * @csspart footer - The dialog's footer.
     * @cssproperty --spacing - The amount of space around and between the dialog's content.
     * @cssproperty --width - The preferred width of the dialog. Note that the dialog will shrink to accommodate smaller screens.
     * @cssproperty --show-duration - The animation duration when showing the dialog. [default: 200ms]
     * @cssproperty --hide-duration - The animation duration when hiding the dialog. [default: 200ms]
     * @event wa-show - Emitted when the dialog opens. (React: `onWaShow`)
     * @event wa-after-show - Emitted after the dialog opens and all animations are complete. (React: `onWaAfterShow`)
     * @event wa-hide - Emitted when the dialog is requested to close. Calling `event.preventDefault()` will prevent the dialog from closing. You can inspect `event.detail.source` to see which element caused the dialog to close. If the source is the dialog element itself, the user has pressed [[Escape]] or the dialog has been closed programmatically. Avoid using this unless closing the dialog will result in destructive behavior such as data loss. (React: `onWaHide`)
     * @event wa-after-hide - Emitted after the dialog closes and all animations are complete. (React: `onWaAfterHide`)
     */
    'beam-dialog': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * Indicates whether or not the dialog is open. Toggle this attribute to show and hide the dialog.
       * @default false
       */
      open?: boolean;
      /**
       * The dialog's label as displayed in the header. You should always include a relevant label, as it is required for proper accessibility. If you need to display HTML, use the `label` slot instead.
       * @default ''
       */
      label?: string;
      /** Disables the header. This will also remove the default close button. @default false */
      'without-header'?: boolean;
      /** When enabled, the dialog will be closed when the user clicks outside of it. @default false */
      'light-dismiss'?: boolean;
      /**
       * Only required for SSR. Set to `true` if you're slotting in a `footer` element so the server-rendered markup includes the footer before the component hydrates on the client.
       * @default false
       */
      'with-footer'?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @cssproperty --color - The color of the divider.
     * @cssproperty --width - The width of the divider.
     * @cssproperty --spacing - The spacing of the divider.
     */
    'beam-divider': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** Sets the divider's orientation. @default 'horizontal' */
      orientation?: 'horizontal' | 'vertical';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The drawer's main content.
     * @slot label - The drawer's label. Alternatively, you can use the `label` attribute.
     * @slot header-actions - Optional actions to add to the header. Works best with `<wa-button>`.
     * @slot footer - The drawer's footer, usually one or more buttons representing various options.
     * @csspart dialog - The drawer's internal `<dialog>` element.
     * @csspart header - The drawer's header. This element wraps the title and header actions.
     * @csspart header-actions - Optional actions to add to the header. Works best with `<wa-button>`.
     * @csspart title - The drawer's title.
     * @csspart close-button - The close button, a `<wa-button>`.
     * @csspart close-button__base - The close button's exported `base` part.
     * @csspart body - The drawer's body.
     * @csspart footer - The drawer's footer.
     * @cssproperty --spacing - The amount of space around and between the drawer's content.
     * @cssproperty --size - The preferred size of the drawer. This will be applied to the drawer's width or height depending on its `placement`. Note that the drawer will shrink to accommodate smaller screens.
     * @cssproperty --show-duration - The animation duration when showing the drawer. [default: 200ms]
     * @cssproperty --hide-duration - The animation duration when hiding the drawer. [default: 200ms]
     * @event wa-show - Emitted when the drawer opens. (React: `onWaShow`)
     * @event wa-after-show - Emitted after the drawer opens and all animations are complete. (React: `onWaAfterShow`)
     * @event wa-hide - Emitted when the drawer is requesting to close. Calling `event.preventDefault()` will prevent the drawer from closing. You can inspect `event.detail.source` to see which element caused the drawer to close. If the source is the drawer element itself, the user has pressed [[Escape]] or the drawer has been closed programmatically. Avoid using this unless closing the drawer will result in destructive behavior such as data loss. (React: `onWaHide`)
     * @event wa-after-hide - Emitted after the drawer closes and all animations are complete. (React: `onWaAfterHide`)
     */
    'beam-drawer': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * Indicates whether or not the drawer is open. Toggle this attribute to show and hide the drawer.
       * @default false
       */
      open?: boolean;
      /**
       * The drawer's label as displayed in the header. You should always include a relevant label, as it is required for proper accessibility. If you need to display HTML, use the `label` slot instead.
       * @default ''
       */
      label?: string;
      /** The direction from which the drawer will open. @default 'end' */
      placement?: 'top' | 'end' | 'bottom' | 'start';
      /** Disables the header. This will also remove the default close button. @default false */
      'without-header'?: boolean;
      /** When enabled, the drawer will be closed when the user clicks outside of it. @default true */
      'light-dismiss'?: boolean;
      /**
       * Only required for SSR. Set to `true` if you're slotting in a `footer` element so the server-rendered markup includes the footer before the component hydrates on the client.
       * @default false
       */
      'with-footer'?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The dropdown item's label.
     * @slot icon - An optional icon to display before the label.
     * @slot details - Additional content or details to display after the label.
     * @slot submenu - Submenu items, typically `<wa-dropdown-item>` elements, to create a nested menu.
     * @csspart checkmark - The checkmark icon (a `<wa-icon>` element) when the item is a checkbox.
     * @csspart icon - The container for the icon slot.
     * @csspart label - The container for the label slot.
     * @csspart details - The container for the details slot.
     * @csspart submenu-icon - The submenu indicator icon (a `<wa-icon>` element).
     * @csspart submenu - The submenu container.
     */
    'beam-dropdown-item': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** The type of menu item to render. @default 'default' */
      variant?: 'danger' | 'default';
      /**
       * An optional value for the menu item. This is useful for determining which item was selected when listening to the dropdown's `wa-select` event.
       */
      value?: string;
      /** Set to `checkbox` to make the item a checkbox. @default 'normal' */
      type?: 'normal' | 'checkbox';
      /** Set to true to check the dropdown item. Only valid when `type` is `checkbox`. @default false */
      checked?: boolean;
      /** Disables the dropdown item. @default false */
      disabled?: boolean;
      /** Whether the submenu is currently open. @default false */
      submenuOpen?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The dropdown's items, typically `<wa-dropdown-item>` elements.
     * @slot trigger - The element that triggers the dropdown, such as a `<wa-button>` or `<button>`.
     * @csspart base - The component's host element.
     * @csspart menu - The dropdown menu container.
     * @cssproperty --show-duration - The duration of the show animation.
     * @cssproperty --hide-duration - The duration of the hide animation.
     * @event wa-show - Emitted when the dropdown is about to show. (React: `onWaShow`)
     * @event wa-after-show - Emitted after the dropdown has been shown. (React: `onWaAfterShow`)
     * @event wa-hide - Emitted when the dropdown is about to hide. (React: `onWaHide`)
     * @event wa-after-hide - Emitted after the dropdown has been hidden. (React: `onWaAfterHide`)
     * @event wa-select - Emitted when an item in the dropdown is selected. (React: `onWaSelect`)
     */
    'beam-dropdown': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** Opens or closes the dropdown. @default false */
      open?: boolean;
      /** The dropdown's size. @default 'medium' */
      size?: 'small' | 'medium' | 'large';
      /**
       * The placement of the dropdown menu in reference to the trigger. The menu will shift to a more optimal location if the preferred placement doesn't have enough room.
       * @default 'bottom-start'
       */
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
      /** The distance of the dropdown menu from its trigger. @default 0 */
      distance?: number;
      /** The offset of the dropdown menu along its trigger. @default 0 */
      skidding?: number;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-format-bytes': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** The number to format in bytes. @default 0 */
      value?: number;
      /** The type of unit to display. @default 'byte' */
      unit?: 'byte' | 'bit';
      /** Determines how to display the result, e.g. "100 bytes", "100 b", or "100b". @default 'short' */
      display?: 'long' | 'short' | 'narrow';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-format-date': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * The date/time to format. If not set, the current date and time will be used. When passing a string, it's strongly recommended to use the ISO 8601 format to ensure timezones are handled correctly. To convert a date to this format in JavaScript, use [`date.toISOString()`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Date/toISOString).
       * @default new Date()
       */
      date?: Date | string;
      /** The format for displaying the weekday. */
      weekday?: 'narrow' | 'short' | 'long';
      /** The format for displaying the era. */
      era?: 'narrow' | 'short' | 'long';
      /** The format for displaying the year. */
      year?: 'numeric' | '2-digit';
      /** The format for displaying the month. */
      month?: 'numeric' | '2-digit' | 'narrow' | 'short' | 'long';
      /** The format for displaying the day. */
      day?: 'numeric' | '2-digit';
      /** The format for displaying the hour. */
      hour?: 'numeric' | '2-digit';
      /** The format for displaying the minute. */
      minute?: 'numeric' | '2-digit';
      /** The format for displaying the second. */
      second?: 'numeric' | '2-digit';
      /** The format for displaying the time. */
      'time-zone-name'?: 'short' | 'long';
      /** The time zone to express the time in. */
      'time-zone'?: string;
      /** The format for displaying the hour. @default 'auto' */
      'hour-format'?: 'auto' | '12' | '24';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-format-number': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** The number to format. @default 0 */
      value?: number;
      /** The formatting style to use. @default 'decimal' */
      type?: 'currency' | 'decimal' | 'percent';
      /** Turns off grouping separators. @default false */
      'without-grouping'?: boolean;
      /**
       * The [ISO 4217](https://en.wikipedia.org/wiki/ISO_4217) currency code to use when formatting.
       * @default 'USD'
       */
      currency?: string;
      /** How to display the currency. @default 'symbol' */
      'currency-display'?: 'symbol' | 'narrowSymbol' | 'code' | 'name';
      /** The minimum number of integer digits to use. Possible values are 1-21. */
      'minimum-integer-digits'?: number;
      /** The minimum number of fraction digits to use. Possible values are 0-100. */
      'minimum-fraction-digits'?: number;
      /** The maximum number of fraction digits to use. Possible values are 0-100. */
      'maximum-fraction-digits'?: number;
      /** The minimum number of significant digits to use. Possible values are 1-21. */
      'minimum-significant-digits'?: number;
      /** The maximum number of significant digits to use,. Possible values are 1-21. */
      'maximum-significant-digits'?: number;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - Elements to track. Only immediate children of the host are monitored.
     * @event wa-intersect - Fired when a tracked element begins or ceases intersecting. (React: `onWaIntersect`)
     */
    'beam-intersection-observer': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** Element ID to define the viewport boundaries for tracked targets. @default null */
      root?: string | null;
      /** Offset space around the root boundary. Accepts values like CSS margin syntax. @default '0px' */
      'root-margin'?: string;
      /**
       * One or more space-separated values representing visibility percentages that trigger the observer callback.
       * @default '0'
       */
      threshold?: string;
      /**
       * CSS class applied to elements during intersection. Automatically removed when elements leave the viewport, enabling pure CSS styling based on visibility state.
       * @default ''
       */
      'intersect-class'?: string;
      /** If enabled, observation ceases after initial intersection. @default false */
      once?: boolean;
      /** Deactivates the intersection observer functionality. @default false */
      disabled?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The content to watch for mutations.
     * @event wa-mutation - Emitted when a mutation occurs. (React: `onWaMutation`)
     */
    'beam-mutation-observer': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * Watches for changes to attributes. To watch only specific attributes, separate them by a space, e.g. `attr="class id title"`. To watch all attributes, use `*`.
       */
      attr?: string;
      /**
       * Indicates whether or not the attribute's previous value should be recorded when monitoring changes.
       * @default false
       */
      'attr-old-value'?: boolean;
      /** Watches for changes to the character data contained within the node. @default false */
      'char-data'?: boolean;
      /** Indicates whether or not the previous value of the node's text should be recorded. @default false */
      'char-data-old-value'?: boolean;
      /** Watches for the addition or removal of new child nodes. @default false */
      'child-list'?: boolean;
      /** Disables the observer. @default false */
      disabled?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot label - The input's label. Alternatively, you can use the `label` attribute.
     * @slot start - An element, such as `<wa-icon>`, placed at the start of the input control.
     * @slot end - An element, such as `<wa-icon>`, placed at the end of the input control (before steppers).
     * @slot increment-icon - An icon to use in lieu of the default increment icon.
     * @slot decrement-icon - An icon to use in lieu of the default decrement icon.
     * @slot hint - Text that describes how to use the input. Alternatively, you can use the `hint` attribute.
     * @csspart label - The label element.
     * @csspart form-control-label - Alias for the label element.
     * @csspart hint - The hint element.
     * @csspart base - The wrapper containing the input and steppers.
     * @csspart input - The internal `<input>` control.
     * @csspart start - The container that wraps the `start` slot.
     * @csspart end - The container that wraps the `end` slot.
     * @csspart stepper - Both stepper buttons (for shared styling).
     * @csspart stepper-increment - The increment (+) button on the end side.
     * @csspart stepper-decrement - The decrement (-) button on the start side.
     * @event wa-invalid - Emitted when the form control has been checked for validity and its constraints aren't satisfied. (React: `onWaInvalid`)
     */
    'beam-number-input': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** @default '' */
      title?: string;
      /** The default value of the form control. Primarily used for resetting the form control. */
      value?: string | null;
      /** The input's size. @default 'medium' */
      size?: 'small' | 'medium' | 'large';
      /** The input's visual appearance. @default 'outlined' */
      appearance?: 'filled' | 'outlined' | 'filled-outlined';
      /** Draws a pill-style input with rounded edges. @default false */
      pill?: boolean;
      /** The input's label. If you need to display HTML, use the `label` slot instead. @default '' */
      label?: string;
      /** The input's hint. If you need to display HTML, use the `hint` slot instead. @default '' */
      hint?: string;
      /** Placeholder text to show as a hint when the input is empty. @default '' */
      placeholder?: string;
      /** Makes the input readonly. @default false */
      readonly?: boolean;
      /** Makes the input a required field. @default false */
      required?: boolean;
      /** The input's minimum value. */
      min?: number;
      /** The input's maximum value. */
      max?: number;
      /**
       * Specifies the granularity that the value must adhere to, or the special value `any` which means no stepping is implied, allowing any numeric value.
       * @default 1
       */
      step?: number | 'any';
      /** Hides the increment/decrement stepper buttons. @default false */
      'without-steppers'?: boolean;
      /**
       * Specifies what permission the browser has to provide assistance in filling out form field values. Refer to [this page on MDN](https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/autocomplete) for available values.
       */
      autocomplete?: string;
      /** Indicates that the input should receive focus on page load. */
      autofocus?: boolean;
      /** Used to customize the label or icon of the Enter key on virtual keyboards. */
      enterkeyhint?: 'enter' | 'done' | 'go' | 'next' | 'previous' | 'search' | 'send';
      /**
       * Tells the browser what type of data will be entered by the user, allowing it to display the appropriate virtual keyboard on supportive devices.
       * @default 'numeric'
       */
      inputmode?: 'numeric' | 'decimal';
      /**
       * Only required for SSR. Set to `true` if you're slotting in a `label` element so the server-rendered markup includes the label before the component hydrates on the client.
       * @default false
       */
      'with-label'?: boolean;
      /**
       * Only required for SSR. Set to `true` if you're slotting in a `hint` element so the server-rendered markup includes the hint before the component hydrates on the client.
       * @default false
       */
      'with-hint'?: boolean;
      /** The name of the input, submitted as a name/value pair with form data. @default null */
      name?: string | null;
      /** Disables the form control. @default false */
      disabled?: boolean;
      /** @default null */
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The tag's content.
     * @csspart base - The component's base wrapper.
     * @csspart content - The tag's content.
     * @csspart remove-button - The tag's remove button, a `<wa-button>`.
     * @csspart remove-button__base - The remove button's exported `base` part.
     * @event wa-remove - Emitted when the remove button is activated. (React: `onWaRemove`)
     */
    'beam-tag': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * The tag's theme variant. Defaults to `neutral` if not within another element with a variant.
       * @default 'neutral'
       */
      variant?: 'brand' | 'neutral' | 'success' | 'warning' | 'danger';
      /** The tag's visual appearance. @default 'filled-outlined' */
      appearance?: 'accent' | 'filled' | 'outlined' | 'filled-outlined';
      /** The tag's size. @default 'medium' */
      size?: 'small' | 'medium' | 'large';
      /** Draws a pill-style tag with rounded edges. @default false */
      pill?: boolean;
      /** Makes the tag removable and shows a remove button. @default false */
      'with-remove'?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The option's label.
     * @slot start - An element, such as `<wa-icon>`, placed before the label.
     * @slot end - An element, such as `<wa-icon>`, placed after the label.
     * @csspart checked-icon - The checked icon, a `<wa-icon>` element.
     * @csspart label - The option's label.
     * @csspart start - The container that wraps the `start` slot.
     * @csspart end - The container that wraps the `end` slot.
     */
    'beam-option': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * The option's value. When selected, the containing form control will receive this value. The value must be unique from other options in the same group. Values may not contain spaces, as spaces are used as delimiters when listing multiple values.
       * @default ''
       */
      value?: string;
      /** Draws the option in a disabled state, preventing selection. @default false */
      disabled?: boolean;
      /** Selects an option initially. @default false */
      selected?: boolean;
      /**
       * The option’s plain text label. Usually automatically generated, but can be useful to provide manually for cases involving complex content.
       */
      label?: string;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The listbox options. Must be `<wa-option>` elements. You can use `<wa-divider>` to group items visually.
     * @slot label - The input's label. Alternatively, you can use the `label` attribute.
     * @slot start - An element, such as `<wa-icon>`, placed at the start of the combobox.
     * @slot end - An element, such as `<wa-icon>`, placed at the end of the combobox.
     * @slot clear-icon - An icon to use in lieu of the default clear icon.
     * @slot expand-icon - The icon to show when the control is expanded and collapsed. Rotates on open and close.
     * @slot hint - Text that describes how to use the input. Alternatively, you can use the `hint` attribute.
     * @csspart form-control - The form control that wraps the label, input, and hint.
     * @csspart form-control-label - The label's wrapper.
     * @csspart form-control-input - The select's wrapper.
     * @csspart hint - The hint's wrapper.
     * @csspart combobox - The container the wraps the start, end, value, clear icon, and expand button.
     * @csspart start - The container that wraps the `start` slot.
     * @csspart end - The container that wraps the `end` slot.
     * @csspart display-input - The element that displays the selected option's label, an `<input>` element.
     * @csspart listbox - The listbox container where options are slotted.
     * @csspart tags - The container that houses option tags when `multiselect` is used.
     * @csspart tag - The individual tags that represent each multiselect option.
     * @csspart tag__content - The tag's content part.
     * @csspart tag__remove-button - The tag's remove button.
     * @csspart tag__remove-button__base - The tag's remove button base part.
     * @csspart clear-button - The clear button.
     * @csspart expand-icon - The container that wraps the expand icon.
     * @cssproperty --show-duration - The duration of the show animation. [default: 100ms]
     * @cssproperty --hide-duration - The duration of the hide animation. [default: 100ms]
     * @cssproperty --tag-max-size - When using `multiple`, the max size of tags before their content is truncated. [default: 10ch]
     * @event wa-clear - Emitted when the control's value is cleared. (React: `onWaClear`)
     * @event wa-show - Emitted when the select's menu opens. (React: `onWaShow`)
     * @event wa-after-show - Emitted after the select's menu opens and all animations are complete. (React: `onWaAfterShow`)
     * @event wa-hide - Emitted when the select's menu closes. (React: `onWaHide`)
     * @event wa-after-hide - Emitted after the select's menu closes and all animations are complete. (React: `onWaAfterHide`)
     * @event wa-invalid - Emitted when the form control has been checked for validity and its constraints aren't satisfied. (React: `onWaInvalid`)
     */
    'beam-select': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** The name of the select, submitted as a name/value pair with form data. @default '' */
      name?: string | null;
      /** The select's value. This will be a string for single select or an array for multi-select. */
      value?: unknown;
      /** The select's size. @default 'medium' */
      size?: 'small' | 'medium' | 'large';
      /** Placeholder text to show as a hint when the select is empty. @default '' */
      placeholder?: string;
      /** Allows more than one option to be selected. @default false */
      multiple?: boolean;
      /**
       * The maximum number of selected options to show when `multiple` is true. After the maximum, "+n" will be shown to indicate the number of additional items that are selected. Set to 0 to remove the limit.
       * @default 3
       */
      'max-options-visible'?: number;
      /** Disables the select control. @default false */
      disabled?: boolean;
      /** Adds a clear button when the select is not empty. @default false */
      'with-clear'?: boolean;
      /**
       * Indicates whether or not the select is open. You can toggle this attribute to show and hide the menu, or you can use the `show()` and `hide()` methods and this attribute will reflect the select's open state.
       * @default false
       */
      open?: boolean;
      /** The select's visual appearance. @default 'outlined' */
      appearance?: 'filled' | 'outlined' | 'filled-outlined';
      /** Draws a pill-style select with rounded edges. @default false */
      pill?: boolean;
      /** The select's label. If you need to display HTML, use the `label` slot instead. @default '' */
      label?: string;
      /**
       * The preferred placement of the select's menu. Note that the actual placement may vary as needed to keep the listbox inside of the viewport.
       * @default 'bottom'
       */
      placement?: 'top' | 'bottom';
      /** The select's hint. If you need to display HTML, use the `hint` slot instead. @default '' */
      hint?: string;
      /**
       * Only required for SSR. Set to `true` if you're slotting in a `label` element so the server-rendered markup includes the label before the component hydrates on the client.
       * @default false
       */
      'with-label'?: boolean;
      /**
       * Only required for SSR. Set to `true` if you're slotting in a `hint` element so the server-rendered markup includes the hint before the component hydrates on the client.
       * @default false
       */
      'with-hint'?: boolean;
      /** The select's required attribute. @default false */
      required?: boolean;
      /** @default null */
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The popover's content. Interactive elements such as buttons and links are supported.
     * @csspart dialog - The native dialog element that contains the popover content.
     * @csspart body - The popover's body where its content is rendered.
     * @csspart popup - The internal `<wa-popup>` element that positions the popover.
     * @csspart popup__popup - The popup's exported `popup` part. Use this to target the popover's popup container.
     * @csspart popup__arrow - The popup's exported `arrow` part. Use this to target the popover's arrow.
     * @cssproperty --arrow-size - The size of the tiny arrow that points to the popover (set to zero to remove). [default: 0.375rem]
     * @cssproperty --max-width - The maximum width of the popover's body content. [default: 25rem]
     * @cssproperty --show-duration - The speed of the show animation. [default: 100ms]
     * @cssproperty --hide-duration - The speed of the hide animation. [default: 100ms]
     * @event wa-show - Emitted when the popover begins to show. Canceling this event will stop the popover from showing. (React: `onWaShow`)
     * @event wa-after-show - Emitted after the popover has shown and all animations are complete. (React: `onWaAfterShow`)
     * @event wa-hide - Emitted when the popover begins to hide. Canceling this event will stop the popover from hiding. (React: `onWaHide`)
     * @event wa-after-hide - Emitted after the popover has hidden and all animations are complete. (React: `onWaAfterHide`)
     */
    'beam-popover': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * The preferred placement of the popover. Note that the actual placement may vary as needed to keep the popover inside of the viewport.
       * @default 'top'
       */
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
      /** Shows or hides the popover. @default false */
      open?: boolean;
      /** The distance in pixels from which to offset the popover away from its target. @default 8 */
      distance?: number;
      /** The distance in pixels from which to offset the popover along its target. @default 0 */
      skidding?: number;
      /**
       * The ID of the popover's anchor element. This must be an interactive/focusable element such as a button.
       * @default null
       */
      for?: string | null;
      /** Removes the arrow from the popover. @default false */
      'without-arrow'?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - A label to show inside the progress indicator.
     * @csspart base - The component's base wrapper.
     * @csspart indicator - The progress bar's indicator.
     * @csspart label - The progress bar's label.
     * @cssproperty --track-height - The color of the track. [default: 1rem]
     * @cssproperty --track-color - The color of the track. [default: var(--wa-color-neutral-fill-normal)]
     * @cssproperty --indicator-color - The color of the indicator. [default: var(--wa-color-brand-fill-loud)]
     */
    'beam-progress-bar': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** The current progress as a percentage, 0 to 100. @default 0 */
      value?: number;
      /**
       * When true, percentage is ignored, the label is hidden, and the progress bar is drawn in an indeterminate state.
       * @default false
       */
      indeterminate?: boolean;
      /** A custom label for assistive devices. @default '' */
      label?: string;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - A label to show inside the ring.
     * @csspart base - The component's base wrapper.
     * @csspart label - The progress ring label.
     * @csspart track - The progress ring's track.
     * @csspart indicator - The progress ring's indicator.
     * @cssproperty --size - The diameter of the progress ring (cannot be a percentage).
     * @cssproperty --track-width - The width of the track.
     * @cssproperty --track-color - The color of the track.
     * @cssproperty --indicator-width - The width of the indicator. Defaults to the track width.
     * @cssproperty --indicator-color - The color of the indicator.
     * @cssproperty --indicator-transition-duration - The duration of the indicator's transition when the value changes.
     */
    'beam-progress-ring': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** The current progress as a percentage, 0 to 100. @default 0 */
      value?: number;
      /** A custom label for assistive devices. @default '' */
      label?: string;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @csspart base - The component's base wrapper.
     */
    'beam-qr-code': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** The QR code's value. @default '' */
      value?: string;
      /**
       * The label for assistive devices to announce. If unspecified, the value will be used instead.
       * @default ''
       */
      label?: string;
      /** The size of the QR code, in pixels. @default 128 */
      size?: number;
      /** The fill color. This can be any valid CSS color, but not a CSS custom property. @default '' */
      fill?: string;
      /**
       * The background color. This can be any valid CSS color or `transparent`. It cannot be a CSS custom property.
       * @default ''
       */
      background?: string;
      /** The edge radius of each module. Must be between 0 and 0.5. @default 0 */
      radius?: number;
      /**
       * The level of error correction to use. [Learn more](https://www.qrcode.com/en/about/error_correction.html)
       * @default 'H'
       */
      'error-correction'?: 'L' | 'M' | 'Q' | 'H';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The radio's label.
     * @csspart control - The circular container that wraps the radio's checked state.
     * @csspart checked-icon - The checked icon.
     * @csspart label - The container that wraps the radio's label.
     * @cssproperty --checked-icon-color - The color of the checked icon.
     * @cssproperty --checked-icon-scale - The size of the checked icon relative to the radio.
     */
    'beam-radio': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** The radio's value. When selected, the radio group will receive this value. */
      value?: string;
      /** The radio's visual appearance. @default 'default' */
      appearance?: 'default' | 'button';
      /**
       * The radio's size. When used inside a radio group, the size will be determined by the radio group's size, which will override this attribute.
       */
      size?: 'small' | 'medium' | 'large';
      /** Disables the radio. @default false */
      disabled?: boolean;
      /** The name of the input, submitted as a name/value pair with form data. @default null */
      name?: string | null;
      /** @default null */
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The default slot where `<wa-radio>` elements are placed.
     * @slot label - The radio group's label. Required for proper accessibility. Alternatively, you can use the `label` attribute.
     * @slot hint - Text that describes how to use the radio group. Alternatively, you can use the `hint` attribute.
     * @csspart form-control - The form control that wraps the label, input, and hint.
     * @csspart form-control-label - The label's wrapper.
     * @csspart form-control-input - The input's wrapper.
     * @csspart radios - The wrapper than surrounds radio items, styled as a flex container by default.
     * @csspart hint - The hint's wrapper.
     * @event wa-invalid - Emitted when the form control has been checked for validity and its constraints aren't satisfied. (React: `onWaInvalid`)
     */
    'beam-radio-group': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * The radio group's label. Required for proper accessibility. If you need to display HTML, use the `label` slot instead.
       * @default ''
       */
      label?: string;
      /** The radio groups's hint. If you need to display HTML, use the `hint` slot instead. @default '' */
      hint?: string;
      /** The name of the radio group, submitted as a name/value pair with form data. @default null */
      name?: string | null;
      /** Disables the radio group and all child radios. @default false */
      disabled?: boolean;
      /** The orientation in which to show radio items. @default 'vertical' */
      orientation?: 'horizontal' | 'vertical';
      /** The default value of the form control. Primarily used for resetting the form control. */
      value?: string | null;
      /** The radio group's size. When present, this size will be applied to all `<wa-radio>` items inside. */
      size?: 'small' | 'medium' | 'large';
      /** Ensures a child radio is checked before allowing the containing form to submit. @default false */
      required?: boolean;
      /**
       * Only required for SSR. Set to `true` if you're slotting in a `label` element so the server-rendered markup includes the label before the component hydrates on the client.
       * @default false
       */
      'with-label'?: boolean;
      /**
       * Only required for SSR. Set to `true` if you're slotting in a `hint` element so the server-rendered markup includes the hint before the component hydrates on the client.
       * @default false
       */
      'with-hint'?: boolean;
      /** @default null */
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @csspart base - The component's base wrapper.
     * @cssproperty --symbol-color - The inactive color for symbols.
     * @cssproperty --symbol-color-active - The active color for symbols.
     * @cssproperty --symbol-spacing - The spacing to use around symbols.
     * @event wa-hover - Emitted when the user hovers over a value. The `phase` property indicates when hovering starts, moves to a new value, or ends. The `value` property tells what the rating's value would be if the user were to commit to the hovered value. (React: `onWaHover`)
     * @event wa-invalid - Emitted when the form control has been checked for validity and its constraints aren't satisfied. (React: `onWaInvalid`)
     */
    'beam-rating': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** The name of the rating, submitted as a name/value pair with form data. @default null */
      name?: string | null;
      /** A label that describes the rating to assistive devices. @default '' */
      label?: string;
      /** The current rating. @default 0 */
      value?: number;
      /** The default value of the form control. Used to reset the rating to its initial value. @default 0 */
      'default-value'?: number;
      /** The highest rating to show. @default 5 */
      max?: number;
      /**
       * The precision at which the rating will increase and decrease. For example, to allow half-star ratings, set this attribute to `0.5`.
       * @default 1
       */
      precision?: number;
      /** Makes the rating readonly. @default false */
      readonly?: boolean;
      /** Disables the rating. @default false */
      disabled?: boolean;
      /** Makes the rating a required field. @default false */
      required?: boolean;
      /**
       * A function that customizes the symbol to be rendered. The first and only argument is the rating's current value. The function should return a string containing trusted HTML of the symbol to render at the specified value. Works well with `<wa-icon>` elements.
       */
      getSymbol?: unknown;
      /** The component's size. @default 'medium' */
      size?: 'small' | 'medium' | 'large';
      /** @default null */
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @cssproperty --beam-rt-popover-bg - Background of the popover panel.
     * @cssproperty --beam-rt-popover-border - Border color of the popover panel.
     * @cssproperty --beam-rt-popover-color - Text color inside the popover.
     * @cssproperty --beam-rt-anchor-underline - Color of the dashed underline shown under the time when the popover is enabled. Defaults to a muted text color.
     * @cssproperty --beam-rt-anchor-underline-hover - Underline color on hover/focus.
     * @cssproperty --beam-rt-anchor-hover-bg - Background color on hover/focus.
     */
    'beam-relative-time': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** Enables the hover popover. @default false */
      'with-utc-popover'?: boolean;
      /**
       * The date from which to calculate time from. If not set, the current date and time will be used. When passing a string, it's strongly recommended to use the ISO 8601 format to ensure timezones are handled correctly. To convert a date to this format in JavaScript, use [`date.toISOString()`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Date/toISOString).
       * @default new Date()
       */
      date?: Date | string;
      /** The formatting style to use. @default 'long' */
      format?: 'long' | 'short' | 'narrow';
      /**
       * When `auto`, values such as "yesterday" and "tomorrow" will be shown when possible. When `always`, values such as "1 day ago" and "in 1 day" will be shown.
       * @default 'auto'
       */
      numeric?: 'always' | 'auto';
      /** Keep the displayed value up to date as time passes. @default false */
      sync?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The content to show inside the scroller.
     * @csspart content - The container that wraps the slotted content.
     * @cssproperty --shadow-color - The base color of the shadow. [default: var(--wa-color-surface-default)]
     * @cssproperty --shadow-size - The size of the shadow. [default: 2rem]
     */
    'beam-scroller': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** The scroller's orientation. @default 'horizontal' */
      orientation?: 'horizontal' | 'vertical';
      /** Removes the visible scrollbar. @default false */
      'without-scrollbar'?: boolean;
      /** Removes the shadows. @default false */
      'without-shadow'?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - One or more elements to watch for resizing.
     * @event wa-resize - Emitted when the element is resized. (React: `onWaResize`)
     */
    'beam-resize-observer': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** Disables the observer. @default false */
      disabled?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @csspart indicator - The skeleton's indicator which is responsible for its color and animation.
     * @cssproperty --color - The color of the skeleton.
     * @cssproperty --sheen-color - The sheen color when the skeleton is in its loading state.
     */
    'beam-skeleton': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** Determines which effect the skeleton will use. @default 'none' */
      effect?: 'pulse' | 'sheen' | 'none';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot label - The slider label. Alternatively, you can use the `label` attribute.
     * @slot hint - Text that describes how to use the input. Alternatively, you can use the `hint` attribute. instead.
     * @slot reference - One or more reference labels to show visually below the slider.
     * @csspart label - The element that contains the sliders's label.
     * @csspart hint - The element that contains the slider's description.
     * @csspart slider - The focusable element with `role="slider"`. Contains the track and reference slot.
     * @csspart track - The slider's track.
     * @csspart indicator - The colored indicator that shows from the start of the slider to the current value.
     * @csspart markers - The container that holds all the markers when `with-markers` is used.
     * @csspart marker - The individual markers that are shown when `with-markers` is used.
     * @csspart references - The container that holds references that get slotted in.
     * @csspart thumb - The slider's thumb.
     * @csspart thumb-min - The min value thumb in a range slider.
     * @csspart thumb-max - The max value thumb in a range slider.
     * @csspart tooltip - The tooltip, a `<wa-tooltip>` element.
     * @csspart tooltip__tooltip - The tooltip's `tooltip` part.
     * @csspart tooltip__content - The tooltip's `content` part.
     * @csspart tooltip__arrow - The tooltip's `arrow` part.
     * @cssproperty --track-size - The height or width of the slider's track. [default: 0.75em]
     * @cssproperty --marker-width - The width of each individual marker. [default: 0.1875em]
     * @cssproperty --marker-height - The height of each individual marker. [default: 0.1875em]
     * @cssproperty --thumb-width - The width of the thumb. [default: 1.25em]
     * @cssproperty --thumb-height - The height of the thumb. [default: 1.25em]
     * @event wa-invalid - Emitted when the form control has been checked for validity and its constraints aren't satisfied. (React: `onWaInvalid`)
     */
    'beam-slider': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * The slider's label. If you need to provide HTML in the label, use the `label` slot instead.
       * @default ''
       */
      label?: string;
      /** The slider hint. If you need to display HTML, use the hint slot instead. @default '' */
      hint?: string;
      /** The name of the slider. This will be submitted with the form as a name/value pair. @default null */
      name?: string | null;
      /** The minimum value of a range selection. Used only when range attribute is set. @default 0 */
      'min-value'?: number;
      /** The maximum value of a range selection. Used only when range attribute is set. @default 50 */
      'max-value'?: number;
      /** The default value of the form control. Primarily used for resetting the form control. */
      value?: number;
      /** Converts the slider to a range slider with two thumbs. @default false */
      range?: boolean;
      /** Disables the slider. @default false */
      disabled?: boolean;
      /** Makes the slider a read-only field. @default false */
      readonly?: boolean;
      /** The orientation of the slider. @default 'horizontal' */
      orientation?: 'horizontal' | 'vertical';
      /** The slider's size. @default 'medium' */
      size?: 'small' | 'medium' | 'large';
      /** The starting value from which to draw the slider's fill, which is based on its current value. */
      'indicator-offset'?: number;
      /** The minimum value allowed. @default 0 */
      min?: number;
      /** The maximum value allowed. @default 100 */
      max?: number;
      /** The granularity the value must adhere to when incrementing and decrementing. @default 1 */
      step?: number;
      /** Tells the browser to focus the slider when the page loads or a dialog is shown. */
      autofocus?: boolean;
      /** The distance of the tooltip from the slider's thumb. @default 8 */
      'tooltip-distance'?: number;
      /** The placement of the tooltip in reference to the slider's thumb. @default 'top' */
      'tooltip-placement'?: 'top' | 'right' | 'bottom' | 'left';
      /** Draws markers at each step along the slider. @default false */
      'with-markers'?: boolean;
      /** Draws a tooltip above the thumb when the control has focus or is dragged. @default false */
      'with-tooltip'?: boolean;
      /**
       * Only required for SSR. Set to `true` if you're slotting in a `label` element so the server-rendered markup includes the label before the component hydrates on the client.
       * @default false
       */
      'with-label'?: boolean;
      /**
       * Only required for SSR. Set to `true` if you're slotting in a `hint` element so the server-rendered markup includes the hint before the component hydrates on the client.
       * @default false
       */
      'with-hint'?: boolean;
      /** @default null */
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The switch's label.
     * @slot hint - Text that describes how to use the switch. Alternatively, you can use the `hint` attribute.
     * @csspart base - The component's base wrapper.
     * @csspart control - The control that houses the switch's thumb.
     * @csspart thumb - The switch's thumb.
     * @csspart label - The switch's label.
     * @csspart hint - The hint's wrapper.
     * @cssproperty --width - The width of the switch.
     * @cssproperty --height - The height of the switch.
     * @cssproperty --thumb-size - The size of the thumb.
     * @event wa-invalid - Emitted when the form control has been checked for validity and its constraints aren't satisfied. (React: `onWaInvalid`)
     */
    'beam-switch': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** @default '' */
      title?: string;
      /** The name of the switch, submitted as a name/value pair with form data. @default null */
      name?: string | null;
      /** The value of the switch, submitted as a name/value pair with form data. */
      value?: string | null;
      /** The switch's size. @default 'medium' */
      size?: 'small' | 'medium' | 'large';
      /** Disables the switch. @default false */
      disabled?: boolean;
      /** The default value of the form control. Primarily used for resetting the form control. */
      checked?: boolean;
      /** Makes the switch a required field. @default false */
      required?: boolean;
      /** The switch's hint. If you need to display HTML, use the `hint` slot instead. @default '' */
      hint?: string;
      /**
       * Only required for SSR. Set to `true` if you're slotting in a `hint` element so the server-rendered markup includes the hint before the component hydrates on the client.
       * @default false
       */
      'with-hint'?: boolean;
      /** @default null */
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The tab's label.
     * @csspart base - The component's base wrapper.
     */
    'beam-tab': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * The name of the tab panel this tab is associated with. The panel must be located in the same tab group.
       * @default ''
       */
      panel?: string;
      /** Disables the tab and prevents selection. @default false */
      disabled?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot start - Content to place in the start panel.
     * @slot end - Content to place in the end panel.
     * @slot divider - The divider. Useful for slotting in a custom icon that renders as a handle.
     * @csspart start - The start panel.
     * @csspart end - The end panel.
     * @csspart panel - Targets both the start and end panels.
     * @csspart divider - The divider that separates the start and end panels.
     * @cssproperty --divider-width - The width of the visible divider. [default: 4px]
     * @cssproperty --divider-hit-area - The invisible region around the divider where dragging can occur. This is usually wider than the divider to facilitate easier dragging. [default: 12px]
     * @cssproperty --min - The minimum allowed size of the primary panel. [default: 0]
     * @cssproperty --max - The maximum allowed size of the primary panel. [default: 100%]
     * @event wa-reposition - Emitted when the divider's position changes. (React: `onWaReposition`)
     */
    'beam-split-panel': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * The current position of the divider from the primary panel's edge as a percentage 0-100. Defaults to 50% of the container's initial size.
       * @default 50
       */
      position?: number;
      /** The current position of the divider from the primary panel's edge in pixels. */
      'position-in-pixels'?: number;
      /** Sets the split panel's orientation. @default 'horizontal' */
      orientation?: 'horizontal' | 'vertical';
      /**
       * Disables resizing. Note that the position may still change as a result of resizing the host element.
       * @default false
       */
      disabled?: boolean;
      /**
       * If no primary panel is designated, both panels will resize proportionally when the host element is resized. If a primary panel is designated, it will maintain its size and the other panel will grow or shrink as needed when the host element is resized.
       */
      primary?: 'start' | 'end' | undefined;
      /**
       * One or more space-separated values at which the divider should snap. Values can be in pixels or percentages, e.g. `"100px 50%"`.
       */
      snap?: string | undefined;
      /** How close the divider must be to a snap point until snapping occurs. @default 12 */
      'snap-threshold'?: number;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The tab panel's content.
     * @csspart base - The component's base wrapper.
     * @cssproperty --padding - The tab panel's padding.
     */
    'beam-tab-panel': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** The tab panel's name. @default '' */
      name?: string;
      /** When true, the tab panel will be shown. @default false */
      active?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - Used for grouping tab panels in the tab group. Must be `<wa-tab-panel>` elements.
     * @slot nav - Used for grouping tabs in the tab group. Must be `<wa-tab>` elements. Note that `<wa-tab>` will set this slot on itself automatically.
     * @csspart base - The component's base wrapper.
     * @csspart nav - The tab group's navigation container where tabs are slotted in.
     * @csspart tabs - The container that wraps the tabs.
     * @csspart body - The tab group's body where tab panels are slotted in.
     * @csspart scroll-button - The previous/next scroll buttons that show when tabs are scrollable, a `<wa-button>`.
     * @csspart scroll-button-start - The starting scroll button.
     * @csspart scroll-button-end - The ending scroll button.
     * @csspart scroll-button__base - The scroll button's exported `base` part.
     * @cssproperty --indicator-color - The color of the active tab indicator.
     * @cssproperty --track-color - The color of the indicator's track (the line that separates tabs from panels).
     * @cssproperty --track-width - The width of the indicator's track (the line that separates tabs from panels).
     * @event wa-tab-show - Emitted when a tab is shown. (React: `onWaTabShow`)
     * @event wa-tab-hide - Emitted when a tab is hidden. (React: `onWaTabHide`)
     */
    'beam-tab-group': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** Sets the active tab. @default '' */
      active?: string;
      /** The placement of the tabs. @default 'top' */
      placement?: 'top' | 'bottom' | 'start' | 'end';
      /**
       * When set to auto, navigating tabs with the arrow keys will instantly show the corresponding tab panel. When set to manual, the tab will receive focus but will not show until the user presses spacebar or enter.
       * @default 'auto'
       */
      activation?: 'auto' | 'manual';
      /** Disables the scroll arrows that appear when tabs overflow. @default false */
      'without-scroll-controls'?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot label - The textarea's label. Alternatively, you can use the `label` attribute.
     * @slot hint - Text that describes how to use the input. Alternatively, you can use the `hint` attribute.
     * @csspart label - The label
     * @csspart form-control-input - The input's wrapper.
     * @csspart hint - The hint's wrapper.
     * @csspart textarea - The internal `<textarea>` control.
     * @csspart base - The wrapper around the `<textarea>` control.
     * @csspart count - The character count element, rendered when the `with-count` attribute is present.
     * @event wa-invalid - Emitted when the form control has been checked for validity and its constraints aren't satisfied. (React: `onWaInvalid`)
     */
    'beam-textarea': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** @default '' */
      title?: string;
      /** The name of the textarea, submitted as a name/value pair with form data. @default null */
      name?: string | null;
      /** The default value of the form control. Primarily used for resetting the form control. */
      value?: string;
      /** The textarea's size. @default 'medium' */
      size?: 'small' | 'medium' | 'large';
      /** The textarea's visual appearance. @default 'outlined' */
      appearance?: 'filled' | 'outlined' | 'filled-outlined';
      /** The textarea's label. If you need to display HTML, use the `label` slot instead. @default '' */
      label?: string;
      /** The textarea's hint. If you need to display HTML, use the `hint` slot instead. @default '' */
      hint?: string;
      /** Placeholder text to show as a hint when the input is empty. @default '' */
      placeholder?: string;
      /** The number of rows to display by default. @default 4 */
      rows?: number;
      /** Controls how the textarea can be resized. @default 'vertical' */
      resize?: 'none' | 'vertical' | 'horizontal' | 'both' | 'auto';
      /** Disables the textarea. @default false */
      disabled?: boolean;
      /** Makes the textarea readonly. @default false */
      readonly?: boolean;
      /** Makes the textarea a required field. @default false */
      required?: boolean;
      /** The minimum length of input that will be considered valid. */
      minlength?: number;
      /** The maximum length of input that will be considered valid. */
      maxlength?: number;
      /** Controls whether and how text input is automatically capitalized as it is entered by the user. */
      autocapitalize?: 'off' | 'none' | 'on' | 'sentences' | 'words' | 'characters';
      /**
       * Indicates whether the browser's autocorrect feature is on or off. When set as an attribute, use `"off"` or `"on"`. When set as a property, use `true` or `false`.
       */
      autocorrect?: boolean;
      /**
       * Specifies what permission the browser has to provide assistance in filling out form field values. Refer to [this page on MDN](https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/autocomplete) for available values.
       */
      autocomplete?: string;
      /** Indicates that the input should receive focus on page load. */
      autofocus?: boolean;
      /** Used to customize the label or icon of the Enter key on virtual keyboards. */
      enterkeyhint?: 'enter' | 'done' | 'go' | 'next' | 'previous' | 'search' | 'send';
      /** Enables spell checking on the textarea. @default true */
      spellcheck?: boolean;
      /**
       * Tells the browser what type of data will be entered by the user, allowing it to display the appropriate virtual keyboard on supportive devices.
       */
      inputmode?: 'none' | 'text' | 'decimal' | 'numeric' | 'tel' | 'search' | 'email' | 'url';
      /**
       * Only required for SSR. Set to `true` if you're slotting in a `label` element so the server-rendered markup includes the label before the component hydrates on the client.
       * @default false
       */
      'with-label'?: boolean;
      /**
       * Only required for SSR. Set to `true` if you're slotting in a `hint` element so the server-rendered markup includes the hint before the component hydrates on the client.
       * @default false
       */
      'with-hint'?: boolean;
      /**
       * Shows a character count below the textarea. When `maxlength` is set, shows remaining characters instead.
       * @default false
       */
      'with-count'?: boolean;
      /** @default null */
      'custom-error'?: string | null;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The default slot.
     * @slot expand-icon - The icon to show when the tree item is expanded. Works best with `<wa-icon>`.
     * @slot collapse-icon - The icon to show when the tree item is collapsed. Works best with `<wa-icon>`.
     * @csspart base - The component's base wrapper.
     * @cssproperty --indent-size - The size of the indentation for nested items. [default: var(--wa-space-m)]
     * @cssproperty --indent-guide-color - The color of the indentation line. [default: var(--wa-color-surface-border)]
     * @cssproperty --indent-guide-offset - The amount of vertical spacing to leave between the top and bottom of the indentation line's starting position. [default: 0]
     * @cssproperty --indent-guide-style - The style of the indentation line, e.g. solid, dotted, dashed. [default: solid]
     * @cssproperty --indent-guide-width - The width of the indentation line. [default: 0]
     * @event wa-selection-change - Emitted when a tree item is selected or deselected. (React: `onWaSelectionChange`)
     */
    'beam-tree': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * The selection behavior of the tree. Single selection allows only one node to be selected at a time. Multiple displays checkboxes and allows more than one node to be selected. Leaf allows only leaf nodes to be selected.
       * @default 'single'
       */
      selection?: 'single' | 'multiple' | 'leaf';
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    'beam-markdown': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * The tab stop width used when converting leading tabs to spaces during whitespace normalization.
       * @default 4
       */
      'tab-size'?: number;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @slot (default) - The page's main content.
     * @slot banner - The banner that gets display above the header. The banner will not be shown if no content is provided.
     * @slot header - The header to display at the top of the page. If a banner is present, the header will appear below the banner. The header will not be shown if there is no content.
     * @slot subheader - A subheader to display below the `header`. This is a good place to put things like breadcrumbs.
     * @slot menu - The left side of the page. If you slot an element in here, you will override the default `navigation` slot and will be handling navigation on your own. This also will not disable the fallback behavior of the navigation button. This section "sticks" to the top as the page scrolls.
     * @slot navigation-header - The header for a navigation area. On mobile this will be the header for `<wa-drawer>`.
     * @slot navigation - The main content to display in the navigation area. This is displayed on the left side of the page, if `menu` is not used. This section "sticks" to the top as the page scrolls.
     * @slot navigation-footer - The footer for a navigation area. On mobile this will be the footer for `<wa-drawer>`.
     * @slot navigation-toggle - Use this slot to slot in your own button + icon for toggling the navigation drawer. By default it is a `<wa-button>` + a 3 bars `<wa-icon>`
     * @slot navigation-toggle-icon - Use this to slot in your own icon for toggling the navigation drawer. By default it is 3 bars `<wa-icon>`.
     * @slot main-header - Header to display inline above the main content.
     * @slot main-footer - Footer to display inline below the main content.
     * @slot aside - Content to be shown on the right side of the page. Typically contains a table of contents, ads, etc. This section "sticks" to the top as the page scrolls.
     * @slot skip-to-content - The "skip to content" slot. You can override this If you would like to override the `Skip to content` button and add additional "Skip to X", they can be inserted here.
     * @slot footer - The content to display in the footer. This is always displayed underneath the viewport so will always make the page "scrollable".
     * @csspart base - The component's base wrapper.
     * @csspart banner - The banner to show above header.
     * @csspart header - The header, usually for top level navigation / branding.
     * @csspart subheader - Shown below the header, usually intended for things like breadcrumbs and other page level navigation.
     * @csspart body - The wrapper around menu, main, and aside.
     * @csspart menu - The left hand side of the page. Generally intended for navigation.
     * @csspart navigation - The `<nav>` that wraps the navigation slots on desktop viewports.
     * @csspart navigation-header - The header for a navigation area. On mobile this will be the header for `<wa-drawer>`.
     * @csspart navigation-footer - The footer for a navigation area. On mobile this will be the footer for `<wa-drawer>`.
     * @csspart navigation-toggle - The default `<wa-button>` that will toggle the `<wa-drawer>` for mobile viewports.
     * @csspart navigation-toggle-icon - The default `<wa-icon>` displayed inside of the navigation-toggle button.
     * @csspart main-header - The header above main content.
     * @csspart main-content - The main content.
     * @csspart main-footer - The footer below main content.
     * @csspart aside - The right hand side of the page. Used for things like table of contents, ads, etc.
     * @csspart skip-links - Wrapper around skip-link
     * @csspart skip-link - The "skip to main content" link
     * @csspart footer - The footer of the page. This is always below the initial viewport size.
     * @csspart dialog-wrapper - A wrapper around elements such as dialogs or other modal-like elements.
     * @cssproperty --menu-width - The width of the page's "menu" section. [default: auto]
     * @cssproperty --main-width - The width of the page's "main" section. [default: 1fr]
     * @cssproperty --aside-width - The wide of the page's "aside" section. [default: auto]
     * @cssproperty --banner-height - The height of the banner. This gets calculated when the page initializes. If the height is known, you can set it here to prevent shifting when the page loads. [default: 0px]
     * @cssproperty --header-height - The height of the header. This gets calculated when the page initializes. If the height is known, you can set it here to prevent shifting when the page loads. [default: 0px]
     * @cssproperty --subheader-height - The height of the subheader. This gets calculated when the page initializes. If the height is known, you can set it here to prevent shifting when the page loads. [default: 0px]
     */
    'beam-page': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * The view is a reflection of the "mobileBreakpoint", when the page is larger than the `mobile-breakpoint` (768px by default), it is considered to be a "desktop" view. The view is merely a way to distinguish when to show/hide the navigation. You can use additional media queries to make other adjustments to content as necessary. The default is "desktop" because the "mobile navigation drawer" isn't accessible via SSR due to drawer requiring JS.
       * @default 'desktop'
       */
      view?: 'mobile' | 'desktop';
      /**
       * Whether or not the navigation drawer is open. Note, the navigation drawer is only "open" on mobile views.
       * @default false
       */
      'nav-open'?: boolean;
      /**
       * At what page width to hide the "navigation" slot and collapse into a hamburger button. Accepts both numbers (interpreted as px) and CSS lengths (e.g. `50em`), which are resolved based on the root element.
       * @default '768px'
       */
      'mobile-breakpoint'?: string;
      /** Where to place the navigation when in the mobile viewport. @default 'start' */
      'navigation-placement'?: 'start' | 'end';
      /**
       * Determines whether or not to hide the default hamburger button. This will automatically flip to "true" if you add an element with `data-toggle-nav` anywhere in the element light DOM. Generally this will be set for you and you don't need to do anything, unless you're using SSR, in which case you should set this manually for initial page loads.
       * @default false
       */
      'disable-navigation-toggle'?: boolean;
      dir?: string;
      lang?: string;
      'did-ssr'?: unknown;
    };

    /**
     * @cssproperty --wa-panel-border-radius - Outer corner radius of the table block. [default: 0.75rem]
     * @cssproperty --wa-color-surface-raised - Background when the `card` attribute is set.
     * @cssproperty --wa-color-surface-border - Border color of the card variant and row separators.
     * @cssproperty --wa-color-text-normal - Primary text color.
     * @cssproperty --wa-color-text-quiet - Header label and secondary text color.
     * @cssproperty --wa-font-size-s - Body cell font size. [default: 0.875rem]
     * @cssproperty --wa-font-size-xs - Header label font size. [default: 0.75rem]
     * @cssproperty --wa-shadow-s - Drop shadow for the card variant.
     * @cssproperty --cell-padding-y - Vertical padding for header, body, and title-bar cells. Default is the "compact" density that every dashboard mockup in this codebase reaches for. Use the `density` attribute below for the named presets; this variable is for fine-grained overrides. [default: 0.5rem]
     * @cssproperty --cell-padding-x - Horizontal padding for header, body, and title-bar cells. [default: 1rem]
     */
    'beam-table': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * Cell density preset — `compact` (default, 0.5rem vertical) or `comfortable` (0.75rem vertical). Setting `--cell-padding-y` / `--cell-padding-x` inline overrides this.
       * @default 'compact'
       */
      density?: 'compact' | 'comfortable';
      /** @default [] */
      columns?: unknown;
      /** @default [] */
      data?: any[];
      'group-by'?: string | undefined;
      'page-size'?: number | undefined;
      /** @default 'No data' */
      'empty-message'?: string;
      /** @default false */
      loading?: boolean;
      /** @default 'Loading...' */
      'loading-message'?: string;
      /** @default false */
      'default-collapsed'?: boolean;
      'table-title'?: string | undefined;
      /**
       * Slots for an external owner (e.g. the React wrapper) to portal content into. When set, the table emits a placeholder with `data-beam-portal-id` — `__top-row__` for the top row, `__group-header__:{groupKey}` for each group's header — and the owner is expected to mount its own DOM there. If unset, the table renders its built-in defaults.
       * @default false
       */
      'has-top-row'?: boolean;
      /** @default false */
      'has-group-header'?: boolean;
    };

    /**
     * @cssproperty --beam-json-max-height - Maximum height of the code block before scrolling kicks in. [default: 60vh]
     * @cssproperty --wa-color-text-normal - Primary text color.
     * @cssproperty --wa-color-text-quiet - Toolbar label color.
     * @cssproperty --wa-color-surface-default - Code block background.
     * @cssproperty --wa-color-surface-border - Toolbar separator color.
     * @cssproperty --wa-border-radius-m - Outer corner radius.
     */
    'beam-json': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** @default '' */
      label?: string;
      /** @default 2 */
      indent?: number;
      /** @default false */
      'no-toolbar'?: boolean;
    };

    /**
     * @slot (default) - Free-form content rendered between the title block and the actions slot. Useful for breadcrumbs, secondary metadata, or filters that should sit under the title.
     * @slot actions - Right-aligned action elements (typically `<beam-button>`s).
     * @csspart base - The component's outer flex wrapper.
     * @csspart text - The text block (title + description + default slot).
     * @csspart title - The H1 title element.
     * @csspart description - The description paragraph element.
     * @csspart actions - Container wrapping the `actions` slot.
     * @cssproperty --beam-page-header-title-size - Title font size. Default `1.25rem` (20px) matches the mockups every news-agent extension was rolling by hand. [default: 1.25rem]
     * @cssproperty --beam-page-header-spacing - Bottom margin reserved below the header. Defaults to `0` so the header composes naturally inside a flex/grid parent that already has `gap`. Set to `1.5rem` explicitly when used inside a legacy block-layout page. [default: 0]
     * @cssproperty --beam-page-header-gap - Gap between the text block and the actions slot. [default: 1rem]
     */
    'beam-page-header': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** Page title. Rendered as an H1. @default '' */
      label?: string;
      /** Optional description rendered below the title. @default '' */
      description?: string;
    };

    /**
     * @slot (default) - Free-form content rendered between the message and the footer (custom form fields, secondary text).
     * @slot footer - Replaces the default Cancel/Confirm button row entirely. Use when you need bespoke buttons.
     * @csspart base - The fixed-position overlay backdrop.
     * @csspart panel - The dialog panel.
     * @csspart title - The H2 title element.
     * @csspart message - The message paragraph element.
     * @csspart footer - The footer container holding the cancel/confirm buttons.
     * @csspart cancel-button - The cancel button.
     * @csspart confirm-button - The confirm button.
     * @cssproperty --beam-confirm-dialog-width - Maximum panel width. [default: 28rem]
     * @cssproperty --beam-confirm-dialog-radius - Panel corner radius. [default: 0.75rem]
     * @cssproperty --beam-confirm-dialog-z-index - Stacking context for the overlay. [default: 50]
     * @event wa-show - Fired when the dialog opens.
     * @event wa-hide - Fired when the dialog closes for any reason.
     * @event wa-cancel - Fired when the dialog is dismissed via the cancel button, overlay click, Escape, or `cancel()`. Bubbles, composed.
     * @event wa-confirm - Fired when the user clicks the confirm button. Bubbles, composed.
     */
    'beam-confirm-dialog': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** When true, the dialog is shown. Toggle from the consumer to open/close. @default false */
      open?: boolean;
      /** Title shown at the top of the dialog. @default 'Confirm' */
      label?: string;
      /** Body text rendered under the title. @default '' */
      message?: string;
      /** Confirm button label. @default 'Confirm' */
      'confirm-label'?: string;
      /** Cancel button label. @default 'Cancel' */
      'cancel-label'?: string;
      /** Visual variant of the confirm button. @default 'brand' */
      'confirm-variant'?: unknown;
    };

    /**
     * @csspart base - The component's outer wrapper.
     * @csspart info - The "Showing X–Y of Z" info block.
     * @csspart controls - The right-side controls block (page input + prev/next).
     * @csspart prev-button - The previous-page button.
     * @csspart next-button - The next-page button.
     * @csspart page-input - The page-number text input.
     * @csspart page-size-select - The page-size dropdown.
     * @cssproperty --beam-pagination-color - Color of the surrounding text. [default: var(--wa-color-text-quiet)]
     * @cssproperty --beam-pagination-active-color - Color of the page-number / button text on hover. [default: var(--wa-color-text-normal)]
     * @cssproperty --beam-pagination-border-color - Border color of buttons and inputs. [default: var(--wa-color-surface-border)]
     * @event wa-page-change - Fired when the user clicks Prev/Next, edits the page input, or any other navigation control. Bubbles, composed.
     * @event wa-page-size-change - Fired when the user picks a new page size from the dropdown. Bubbles, composed. The page is NOT auto-reset; consumers should clamp the page when responding.
     */
    'beam-pagination': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** 1-indexed current page number. @default 1 */
      page?: number;
      /** Items per page. @default 10 */
      'page-size'?: number;
      /** Total number of items across all pages. @default 0 */
      total?: number;
      /**
       * Optional comma-separated list of page-size choices, e.g. "10,25,50,100". When set, a `<select>` dropdown is rendered to the left of the info text.
       * @default ''
       */
      'page-size-options'?: string;
      /** Hide the "Showing X–Y of Z" info block. @default false */
      'hide-info'?: boolean;
      /** Hide the page input (Prev/Next only). @default false */
      'hide-page-input'?: boolean;
    };

    /**
     * @slot (default) - The toast body content. Replaces the `message` attribute when used.
     * @slot icon - Optional icon to render at the start of the toast.
     * @slot action - Optional inline action element (e.g. a `beam-button`) that replaces the auto-generated action button.
     * @csspart base - The card outer element.
     * @csspart icon - The icon slot wrapper.
     * @csspart message - The message body element.
     * @csspart action - The action slot wrapper.
     * @csspart close-button - The dismiss button.
     * @cssproperty --beam-toast-min-width - Minimum width of the toast card. [default: 18rem]
     * @cssproperty --beam-toast-max-width - Maximum width of the toast card. [default: 24rem]
     * @cssproperty --beam-toast-radius - Card corner radius. [default: 0.5rem]
     * @event wa-show - Fired after the toast is mounted and visible.
     * @event wa-hide - Fired when the toast dismisses for any reason. Bubbles, composed.
     * @event wa-action - Fired when the user clicks the action button (when one is provided). Bubbles, composed.
     */
    'beam-toast': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** Visual variant. @default 'info' */
      variant?: unknown;
      /** Plain-text message. Replaced by default-slot content when present. @default '' */
      message?: string;
      /** Auto-dismiss after N ms. 0 = sticky. @default 0 */
      duration?: number;
      /** Identifier supplied by the host stack. @default 0 */
      toastId?: number;
      /** Optional action label rendered as a default action button. @default '' */
      'action-label'?: string;
      /** Hide the close button. @default false */
      'no-close'?: boolean;
    };

    /**
     * @csspart base - The fixed-position container.
     * @cssproperty --beam-toast-stack-gap - Vertical gap between toasts. [default: 0.5rem]
     * @cssproperty --beam-toast-stack-offset - Distance from the screen edge. [default: 1rem]
     * @cssproperty --beam-toast-stack-z-index - Stacking context. [default: 70]
     * @event wa-show - Fired when a new toast is appended. Bubbles, composed.
     * @event wa-hide - Fired when a toast is removed. Bubbles, composed.
     */
    'beam-toast-stack': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** Where toasts appear on the screen. @default 'bottom-end' */
      placement?: 'top-start' | 'top-end' | 'bottom-start' | 'bottom-end';
      /** Maximum number of toasts visible at once. Older toasts beyond this limit are dropped. @default 5 */
      'max-toasts'?: number;
    };

    /**
     * @csspart base - The component's outer wrapper.
     * @csspart header - The header bar (language label + copy button).
     * @csspart lang - The language label inside the header.
     * @csspart copy-button - The copy button.
     * @csspart body - The scrollable code area that holds the Shiki output.
     * @cssproperty --beam-code-snippet-bg - Background of the code area. Defaults to the sidebar color in dark mode and the surface-hover color in light mode.
     * @cssproperty --beam-code-snippet-radius - Outer border radius. [default: 0.5rem]
     */
    'beam-code-snippet': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** Source code to render. @default '' */
      code?: string;
      /** Shiki language id (e.g. `tsx`, `bash`, `json`). Defaults to `tsx`. @default 'tsx' */
      lang?: string;
    };

    /**
     * @csspart bar - The fixed-position sticky bar at the bottom of the viewport.
     * @csspart bar-content - The max-width container inside the bar.
     * @csspart summary - The summary text and pulsing indicator block.
     * @csspart indicator - The pulsing colored dot.
     * @csspart actions - The right-side actions cluster (discard / review / save).
     * @csspart discard-button - The discard button.
     * @csspart review-button - The review button.
     * @csspart save-button - The primary save button rendered in the bar.
     * @csspart dialog - The dialog overlay backdrop.
     * @csspart dialog-panel - The dialog panel.
     * @csspart dialog-header - The dialog header row (title + summary count).
     * @csspart dialog-body - The scrollable dialog body containing the three change sections.
     * @csspart dialog-footer - The dialog footer row (close + save-all).
     * @csspart section - Each modified / added / deleted section wrapper inside the dialog.
     * @csspart entry - Each individual change entry card in the dialog.
     * @cssproperty --beam-change-bar-z-index - Stacking context for the bar. [default: 30]
     * @cssproperty --beam-change-bar-dialog-z-index - Stacking context for the review dialog. [default: 40]
     * @cssproperty --beam-change-bar-bg - Background of the sticky bar. [default: var(--color-beam-sidebar)]
     * @cssproperty --beam-change-bar-border - Top border color of the bar. [default: var(--color-beam-border)]
     * @cssproperty --beam-change-bar-max-width - Max width of the inner bar content. [default: 80rem]
     * @event wa-review-show - Fired when the review dialog opens.
     * @event wa-review-hide - Fired when the review dialog closes for any reason.
     * @event wa-state-change - Fired when the bar transitions between high-level states. Consumers can use this to drive per-field status UI (e.g., spinners, status dots) without polling. Bubbles, composed.
     * @event wa-save - Fired when the user clicks Save (either in the bar or the dialog footer). Also fired automatically when `auto-save` is enabled and the debounce window elapses. Bubbles, composed.
     * @event wa-discard - Fired when the user clicks Discard. Bubbles, composed.
     */
    'beam-change-bar': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * Reflects the current high-level state (`idle` | `pending` | `saving` | `paused-errors`). Useful as a CSS hook (e.g., `beam-change-bar[data-state="saving"] ~ .field { opacity: 0.7 }`).
       */
      'data-state'?: unknown;
      /** Show the Save button as "Saving…" and disable it while a save is in flight. @default false */
      saving?: boolean;
      /** Override the auto-derived summary text (e.g. "2 added, 1 modified"). @default '' */
      'summary-text'?: string;
      /** Label of the Save button. @default 'Save' */
      'save-label'?: string;
      /** Label of the Save button while saving. @default 'Saving...' */
      'saving-label'?: string;
      /** Label of the Discard button. @default 'Discard' */
      'discard-label'?: string;
      /** Label of the Review button. @default 'Review' */
      'review-label'?: string;
      /** Title of the review dialog. @default 'Review Changes' */
      'review-title'?: string;
      /** Hide the Review button + dialog entirely. @default false */
      'no-review'?: boolean;
      /**
       * Render the bar inline at the host's normal flow position instead of fixed to the bottom of the viewport. Useful for in-page docs/previews and for embedding the bar inside a settings panel.
       * @default false
       */
      inline?: boolean;
      /**
       * Auto-save mode. When enabled, the bar dispatches `wa-save` automatically (debounced by `auto-save-debounce`) whenever `changes` is dirty AND `errors` is empty, and the visible bar UI is hidden. If validation errors appear, the bar falls back to its normal interactive state so the user can fix them. The consumer still owns the actual save logic via the `wa-save` handler — the bar only schedules the dispatch.
       * @default false
       */
      'auto-save'?: boolean;
      /**
       * Debounce window in milliseconds before auto-save dispatches `wa-save`. Resets every time `changes` updates so rapid edits collapse into one save.
       * @default 800
       */
      'auto-save-debounce'?: number;
    };

    /**
     * @slot icon - Top-left icon. Pass a `<beam-icon>`, emoji, or any inline-sized element. Falls back to empty if no icon is provided. Shortcut: set the `icon` attribute to a Font Awesome name (e.g. `"bolt"`) and the card renders a softly-tinted `<beam-icon>` for you. Anything in the slot wins over the shortcut.
     * @slot (default) - Default slot rendered below the standard fields. Useful for sparklines or auxiliary content.
     * @csspart base - The card's outer container.
     * @csspart header - Row containing the icon and change indicator.
     * @csspart icon - Wrapper around the icon slot.
     * @csspart change - The delta indicator span.
     * @csspart value - The big value text.
     * @csspart label - The label text.
     * @csspart sub - The subtitle text.
     * @cssproperty --beam-kpi-card-padding - Inner padding of the card. [default: 1rem]
     * @cssproperty --beam-kpi-card-radius - Outer corner radius. [default: 0.5rem]
     * @cssproperty --beam-kpi-card-value-size - Font size of the value. [default: 1.5rem]
     * @cssproperty --beam-kpi-card-background - Card background. Defaults to `var(--wa-color-surface-raised)` so it matches the host's `.card` look.
     * @cssproperty --beam-kpi-card-border-color - Card border color. Defaults to `var(--color-beam-border)`.
     */
    'beam-kpi-card': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** The big primary value, e.g. "23" or "$306" or "892K". @default '' */
      value?: string;
      /** Short label rendered below the value. @default '' */
      label?: string;
      /** Optional secondary line below the label. @default '' */
      sub?: string;
      /** Delta indicator string, e.g. "+12%" or "-5.2%". Omit to hide. @default '' */
      change?: string;
      /**
       * Coloring for the change text. `positive` → success green, `negative` → error red, `neutral` → muted gray. Defaults to `neutral`.
       * @default 'neutral'
       */
      tone?: 'positive' | 'negative' | 'neutral';
      /**
       * Shortcut for the `icon` slot — set this to a Font Awesome name (e.g. `"bolt"`, `"file-lines"`) and the card renders a softly-tinted `<beam-icon>` for you. Slotted content always wins.
       * @default ''
       */
      icon?: string;
    };

    /**
     * @slot (default) - The label text. Plain string is typical; rich content works too.
     * @slot action - Right-aligned action element (button, link, etc).
     * @csspart base - The outer flex container.
     * @csspart label - The label text span.
     * @cssproperty --beam-section-label-margin-bottom - Bottom margin reserved below the label. Set to 0 if the label sits inline. [default: 1rem]
     */
    'beam-section-label': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      // No attributes defined in CEM.
    };

    /**
     * @slot (default) - The pill label (usually a short string like "Enabled" or "Live").
     * @csspart base - The pill element itself.
     * @cssproperty --beam-status-pill-padding - Inner padding. [default: 0.125rem 0.5rem]
     * @cssproperty --beam-status-pill-radius - Corner radius. [default: 4px]
     * @cssproperty --beam-status-pill-font-size - Font size. [default: 0.6875rem]
     */
    'beam-status-pill': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /**
       * Color treatment. `neutral` (default) uses the muted text color. `success` / `warning` / `error` / `info` / `accent` use the matching semantic palette.
       * @default 'neutral'
       */
      tone?: 'success' | 'warning' | 'error' | 'info' | 'accent' | 'neutral';
    };

    /**
     * @slot (default) - Anything inline-sized. Overrides the `icon` attribute shortcut.
     * @csspart base - The tile element.
     * @cssproperty --beam-icon-tile-size - Width and height of the tile. [default: 2rem]
     * @cssproperty --beam-icon-tile-radius - Corner radius (50% = circle). [default: 50%]
     * @cssproperty --beam-icon-tile-font-size - Icon font size. [default: 0.875rem]
     */
    'beam-icon-tile': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      /** Tone — defaults to `neutral` (muted gray surface). @default 'neutral' */
      tone?: 'success' | 'warning' | 'error' | 'info' | 'accent' | 'neutral';
      /** Shape — defaults to `circle`. Use `rounded` for a 6px-radius square. @default 'circle' */
      shape?: 'circle' | 'rounded';
      /** Size — defaults to `medium` (2rem). @default 'medium' */
      size?: 'small' | 'medium' | 'large';
      /**
       * Font Awesome icon-name shortcut. If set and nothing is slotted, renders a `<beam-icon name=...>` inside the tile. Slotted content wins.
       * @default ''
       */
      icon?: string;
    };

    /**
     * @slot (default) - Tile content. Typically `<beam-kpi-card>` children, but any block-level child works.
     * @csspart base - The grid container.
     * @cssproperty --beam-kpi-row-min - Minimum column width before the grid reflows. [default: 11rem]
     * @cssproperty --beam-kpi-row-gap - Gap between tiles. [default: 1rem]
     */
    'beam-kpi-row': import('svelte/elements').HTMLAttributes<HTMLElement> & {
      // No attributes defined in CEM.
    };
  }
}

export {};
