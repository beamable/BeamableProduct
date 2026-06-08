// AUTO-GENERATED — do not edit manually.
// Run `pnpm sync-components` to regenerate from agentic-portal's custom-elements.json.
//
// React forwarder components for every `beam-*` web element. Each one
// resolves the host portal's real implementation at render time via
// `window.__beamPortal.react.<Name>` and renders it with the forwarded
// props + ref. The toolkit ships only the forwarder shell; the host owns
// the implementation, so a bugfix in the host doesn't require republishing
// the toolkit or rebuilding every extension.
//
// React 19's ref-as-prop semantics let us use plain function components
// instead of `forwardRef` — that keeps the dts bundler from collapsing
// each component to `any`.

import { createElement, type DetailedHTMLProps, type HTMLAttributes, type ReactElement, type Ref } from 'react';
import { hostComponent } from './react-host';

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
export interface BeamIconProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-icon>`. */
export function BeamIcon(props: BeamIconProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamIcon'), props);
}
BeamIcon.displayName = 'BeamIcon';

/**
 * @csspart base - The component's base wrapper.
 * @cssproperty --track-width - The width of the track.
 * @cssproperty --track-color - The color of the track.
 * @cssproperty --indicator-color - The color of the spinner's indicator.
 * @cssproperty --speed - The time it takes for the spinner to complete one animation cycle.
 */
export interface BeamSpinnerProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

/** React forwarder for `<beam-spinner>`. */
export function BeamSpinner(props: BeamSpinnerProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamSpinner'), props);
}
BeamSpinner.displayName = 'BeamSpinner';

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
export interface BeamTreeItemProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-tree-item>`. */
export function BeamTreeItem(props: BeamTreeItemProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamTreeItem'), props);
}
BeamTreeItem.displayName = 'BeamTreeItem';

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
export interface BeamButtonProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-button>`. */
export function BeamButton(props: BeamButtonProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamButton'), props);
}
BeamButton.displayName = 'BeamButton';

/**
 * @slot (default) - The element to animate. Avoid slotting in more than one element, as subsequent ones will be ignored. To animate multiple elements, either wrap them in a single container or use multiple `<wa-animation>` elements.
 * @event wa-cancel - Emitted when the animation is canceled. (React: `onWaCancel`)
 * @event wa-finish - Emitted when the animation finishes. (React: `onWaFinish`)
 * @event wa-start - Emitted when the animation starts or restarts. (React: `onWaStart`)
 */
export interface BeamAnimationProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-animation>`. */
export function BeamAnimation(props: BeamAnimationProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamAnimation'), props);
}
BeamAnimation.displayName = 'BeamAnimation';

/**
 * @slot icon - The default icon to use when no image or initials are present. Works best with `<wa-icon>`.
 * @csspart icon - The container that wraps the avatar's icon.
 * @csspart initials - The container that wraps the avatar's initials.
 * @csspart image - The avatar image. Only shown when the `image` attribute is set.
 * @cssproperty --size - The size of the avatar.
 * @event wa-error - The image could not be loaded. This may because of an invalid URL, a temporary network condition, or some unknown cause. (React: `onWaError`)
 */
export interface BeamAvatarProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-avatar>`. */
export function BeamAvatar(props: BeamAvatarProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamAvatar'), props);
}
BeamAvatar.displayName = 'BeamAvatar';

/**
 * @slot (default) - The badge's content.
 * @slot start - An element, such as `<wa-icon>`, placed before the label.
 * @slot end - An element, such as `<wa-icon>`, placed after the label.
 * @csspart base - The component's base wrapper.
 * @csspart start - The container that wraps the `start` slot.
 * @csspart end - The container that wraps the `end` slot.
 * @cssproperty --pulse-color - The color of the badge's pulse effect when using `attention="pulse"`.
 */
export interface BeamBadgeProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-badge>`. */
export function BeamBadge(props: BeamBadgeProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamBadge'), props);
}
BeamBadge.displayName = 'BeamBadge';

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
export interface BeamBreadcrumbItemProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-breadcrumb-item>`. */
export function BeamBreadcrumbItem(props: BeamBreadcrumbItemProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamBreadcrumbItem'), props);
}
BeamBreadcrumbItem.displayName = 'BeamBreadcrumbItem';

/**
 * @slot (default) - One or more breadcrumb items to display.
 * @slot separator - The separator to use between breadcrumb items. Works best with `<wa-icon>`.
 * @csspart base - The component's base wrapper.
 */
export interface BeamBreadcrumbProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  /**
   * The label to use for the breadcrumb control. This will not be shown on the screen, but it will be announced by screen readers and other assistive devices to provide more context for users.
   * @default ''
   */
  label?: string;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

/** React forwarder for `<beam-breadcrumb>`. */
export function BeamBreadcrumb(props: BeamBreadcrumbProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamBreadcrumb'), props);
}
BeamBreadcrumb.displayName = 'BeamBreadcrumb';

/**
 * @slot (default) - One or more `<wa-button>` elements to display in the button group.
 * @csspart base - The component's base wrapper.
 */
export interface BeamButtonGroupProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-button-group>`. */
export function BeamButtonGroup(props: BeamButtonGroupProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamButtonGroup'), props);
}
BeamButtonGroup.displayName = 'BeamButtonGroup';

/**
 * @slot (default) - The callout's main content.
 * @slot icon - An icon to show in the callout. Works best with `<wa-icon>`.
 * @csspart icon - The container that wraps the optional icon.
 * @csspart message - The container that wraps the callout's main content.
 */
export interface BeamCalloutProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-callout>`. */
export function BeamCallout(props: BeamCalloutProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamCallout'), props);
}
BeamCallout.displayName = 'BeamCallout';

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
export interface BeamCardProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-card>`. */
export function BeamCard(props: BeamCardProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamCard'), props);
}
BeamCard.displayName = 'BeamCard';

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
export interface BeamPopupProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-popup>`. */
export function BeamPopup(props: BeamPopupProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamPopup'), props);
}
BeamPopup.displayName = 'BeamPopup';

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
export interface BeamTooltipProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-tooltip>`. */
export function BeamTooltip(props: BeamTooltipProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamTooltip'), props);
}
BeamTooltip.displayName = 'BeamTooltip';

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
export interface BeamCopyButtonProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-copy-button>`. */
export function BeamCopyButton(props: BeamCopyButtonProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamCopyButton'), props);
}
BeamCopyButton.displayName = 'BeamCopyButton';

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
export interface BeamDetailsProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-details>`. */
export function BeamDetails(props: BeamDetailsProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamDetails'), props);
}
BeamDetails.displayName = 'BeamDetails';

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
export interface BeamDialogProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-dialog>`. */
export function BeamDialog(props: BeamDialogProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamDialog'), props);
}
BeamDialog.displayName = 'BeamDialog';

/**
 * @cssproperty --color - The color of the divider.
 * @cssproperty --width - The width of the divider.
 * @cssproperty --spacing - The spacing of the divider.
 */
export interface BeamDividerProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  /** Sets the divider's orientation. @default 'horizontal' */
  orientation?: 'horizontal' | 'vertical';
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

/** React forwarder for `<beam-divider>`. */
export function BeamDivider(props: BeamDividerProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamDivider'), props);
}
BeamDivider.displayName = 'BeamDivider';

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
export interface BeamDrawerProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-drawer>`. */
export function BeamDrawer(props: BeamDrawerProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamDrawer'), props);
}
BeamDrawer.displayName = 'BeamDrawer';

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
export interface BeamDropdownItemProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-dropdown-item>`. */
export function BeamDropdownItem(props: BeamDropdownItemProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamDropdownItem'), props);
}
BeamDropdownItem.displayName = 'BeamDropdownItem';

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
export interface BeamDropdownProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-dropdown>`. */
export function BeamDropdown(props: BeamDropdownProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamDropdown'), props);
}
BeamDropdown.displayName = 'BeamDropdown';

export interface BeamFormatBytesProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  /** The number to format in bytes. @default 0 */
  value?: number;
  /** The type of unit to display. @default 'byte' */
  unit?: 'byte' | 'bit';
  /** Determines how to display the result, e.g. "100 bytes", "100 b", or "100b". @default 'short' */
  display?: 'long' | 'short' | 'narrow';
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

/** React forwarder for `<beam-format-bytes>`. */
export function BeamFormatBytes(props: BeamFormatBytesProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamFormatBytes'), props);
}
BeamFormatBytes.displayName = 'BeamFormatBytes';

export interface BeamFormatDateProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-format-date>`. */
export function BeamFormatDate(props: BeamFormatDateProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamFormatDate'), props);
}
BeamFormatDate.displayName = 'BeamFormatDate';

export interface BeamFormatNumberProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-format-number>`. */
export function BeamFormatNumber(props: BeamFormatNumberProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamFormatNumber'), props);
}
BeamFormatNumber.displayName = 'BeamFormatNumber';

/**
 * @slot (default) - Elements to track. Only immediate children of the host are monitored.
 * @event wa-intersect - Fired when a tracked element begins or ceases intersecting. (React: `onWaIntersect`)
 */
export interface BeamIntersectionObserverProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-intersection-observer>`. */
export function BeamIntersectionObserver(props: BeamIntersectionObserverProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamIntersectionObserver'), props);
}
BeamIntersectionObserver.displayName = 'BeamIntersectionObserver';

/**
 * @slot (default) - The content to watch for mutations.
 * @event wa-mutation - Emitted when a mutation occurs. (React: `onWaMutation`)
 */
export interface BeamMutationObserverProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-mutation-observer>`. */
export function BeamMutationObserver(props: BeamMutationObserverProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamMutationObserver'), props);
}
BeamMutationObserver.displayName = 'BeamMutationObserver';

/**
 * @slot (default) - The tag's content.
 * @csspart base - The component's base wrapper.
 * @csspart content - The tag's content.
 * @csspart remove-button - The tag's remove button, a `<wa-button>`.
 * @csspart remove-button__base - The remove button's exported `base` part.
 * @event wa-remove - Emitted when the remove button is activated. (React: `onWaRemove`)
 */
export interface BeamTagProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-tag>`. */
export function BeamTag(props: BeamTagProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamTag'), props);
}
BeamTag.displayName = 'BeamTag';

/**
 * @slot (default) - The option's label.
 * @slot start - An element, such as `<wa-icon>`, placed before the label.
 * @slot end - An element, such as `<wa-icon>`, placed after the label.
 * @csspart checked-icon - The checked icon, a `<wa-icon>` element.
 * @csspart label - The option's label.
 * @csspart start - The container that wraps the `start` slot.
 * @csspart end - The container that wraps the `end` slot.
 */
export interface BeamOptionProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-option>`. */
export function BeamOption(props: BeamOptionProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamOption'), props);
}
BeamOption.displayName = 'BeamOption';

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
export interface BeamPopoverProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-popover>`. */
export function BeamPopover(props: BeamPopoverProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamPopover'), props);
}
BeamPopover.displayName = 'BeamPopover';

/**
 * @slot (default) - A label to show inside the progress indicator.
 * @csspart base - The component's base wrapper.
 * @csspart indicator - The progress bar's indicator.
 * @csspart label - The progress bar's label.
 * @cssproperty --track-height - The color of the track. [default: 1rem]
 * @cssproperty --track-color - The color of the track. [default: var(--wa-color-neutral-fill-normal)]
 * @cssproperty --indicator-color - The color of the indicator. [default: var(--wa-color-brand-fill-loud)]
 */
export interface BeamProgressBarProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-progress-bar>`. */
export function BeamProgressBar(props: BeamProgressBarProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamProgressBar'), props);
}
BeamProgressBar.displayName = 'BeamProgressBar';

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
export interface BeamProgressRingProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  /** The current progress as a percentage, 0 to 100. @default 0 */
  value?: number;
  /** A custom label for assistive devices. @default '' */
  label?: string;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

/** React forwarder for `<beam-progress-ring>`. */
export function BeamProgressRing(props: BeamProgressRingProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamProgressRing'), props);
}
BeamProgressRing.displayName = 'BeamProgressRing';

/**
 * @csspart base - The component's base wrapper.
 */
export interface BeamQrCodeProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-qr-code>`. */
export function BeamQrCode(props: BeamQrCodeProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamQrCode'), props);
}
BeamQrCode.displayName = 'BeamQrCode';

/**
 * @slot (default) - The radio's label.
 * @csspart control - The circular container that wraps the radio's checked state.
 * @csspart checked-icon - The checked icon.
 * @csspart label - The container that wraps the radio's label.
 * @cssproperty --checked-icon-color - The color of the checked icon.
 * @cssproperty --checked-icon-scale - The size of the checked icon relative to the radio.
 */
export interface BeamRadioProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-radio>`. */
export function BeamRadio(props: BeamRadioProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamRadio'), props);
}
BeamRadio.displayName = 'BeamRadio';

/**
 * @cssproperty --beam-rt-popover-bg - Background of the popover panel.
 * @cssproperty --beam-rt-popover-border - Border color of the popover panel.
 * @cssproperty --beam-rt-popover-color - Text color inside the popover.
 * @cssproperty --beam-rt-anchor-underline - Color of the dashed underline shown under the time when the popover is enabled. Defaults to a muted text color.
 * @cssproperty --beam-rt-anchor-underline-hover - Underline color on hover/focus.
 * @cssproperty --beam-rt-anchor-hover-bg - Background color on hover/focus.
 */
export interface BeamRelativeTimeProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-relative-time>`. */
export function BeamRelativeTime(props: BeamRelativeTimeProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamRelativeTime'), props);
}
BeamRelativeTime.displayName = 'BeamRelativeTime';

/**
 * @slot (default) - The content to show inside the scroller.
 * @csspart content - The container that wraps the slotted content.
 * @cssproperty --shadow-color - The base color of the shadow. [default: var(--wa-color-surface-default)]
 * @cssproperty --shadow-size - The size of the shadow. [default: 2rem]
 */
export interface BeamScrollerProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  /** The scroller's orientation. @default 'horizontal' */
  orientation?: 'horizontal' | 'vertical';
  /** Removes the visible scrollbar. @default false */
  'without-scrollbar'?: boolean;
  /** Removes the shadows. @default false */
  'without-shadow'?: boolean;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

/** React forwarder for `<beam-scroller>`. */
export function BeamScroller(props: BeamScrollerProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamScroller'), props);
}
BeamScroller.displayName = 'BeamScroller';

/**
 * @slot (default) - One or more elements to watch for resizing.
 * @event wa-resize - Emitted when the element is resized. (React: `onWaResize`)
 */
export interface BeamResizeObserverProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  /** Disables the observer. @default false */
  disabled?: boolean;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

/** React forwarder for `<beam-resize-observer>`. */
export function BeamResizeObserver(props: BeamResizeObserverProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamResizeObserver'), props);
}
BeamResizeObserver.displayName = 'BeamResizeObserver';

/**
 * @csspart indicator - The skeleton's indicator which is responsible for its color and animation.
 * @cssproperty --color - The color of the skeleton.
 * @cssproperty --sheen-color - The sheen color when the skeleton is in its loading state.
 */
export interface BeamSkeletonProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  /** Determines which effect the skeleton will use. @default 'none' */
  effect?: 'pulse' | 'sheen' | 'none';
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

/** React forwarder for `<beam-skeleton>`. */
export function BeamSkeleton(props: BeamSkeletonProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamSkeleton'), props);
}
BeamSkeleton.displayName = 'BeamSkeleton';

/**
 * @slot (default) - The tab's label.
 * @csspart base - The component's base wrapper.
 */
export interface BeamTabProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-tab>`. */
export function BeamTab(props: BeamTabProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamTab'), props);
}
BeamTab.displayName = 'BeamTab';

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
export interface BeamSplitPanelProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-split-panel>`. */
export function BeamSplitPanel(props: BeamSplitPanelProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamSplitPanel'), props);
}
BeamSplitPanel.displayName = 'BeamSplitPanel';

/**
 * @slot (default) - The tab panel's content.
 * @csspart base - The component's base wrapper.
 * @cssproperty --padding - The tab panel's padding.
 */
export interface BeamTabPanelProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  /** The tab panel's name. @default '' */
  name?: string;
  /** When true, the tab panel will be shown. @default false */
  active?: boolean;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

/** React forwarder for `<beam-tab-panel>`. */
export function BeamTabPanel(props: BeamTabPanelProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamTabPanel'), props);
}
BeamTabPanel.displayName = 'BeamTabPanel';

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
export interface BeamTabGroupProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-tab-group>`. */
export function BeamTabGroup(props: BeamTabGroupProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamTabGroup'), props);
}
BeamTabGroup.displayName = 'BeamTabGroup';

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
export interface BeamTreeProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  /**
   * The selection behavior of the tree. Single selection allows only one node to be selected at a time. Multiple displays checkboxes and allows more than one node to be selected. Leaf allows only leaf nodes to be selected.
   * @default 'single'
   */
  selection?: 'single' | 'multiple' | 'leaf';
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

/** React forwarder for `<beam-tree>`. */
export function BeamTree(props: BeamTreeProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamTree'), props);
}
BeamTree.displayName = 'BeamTree';

export interface BeamMarkdownProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  /**
   * The tab stop width used when converting leading tabs to spaces during whitespace normalization.
   * @default 4
   */
  'tab-size'?: number;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

/** React forwarder for `<beam-markdown>`. */
export function BeamMarkdown(props: BeamMarkdownProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamMarkdown'), props);
}
BeamMarkdown.displayName = 'BeamMarkdown';

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
export interface BeamPageProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-page>`. */
export function BeamPage(props: BeamPageProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamPage'), props);
}
BeamPage.displayName = 'BeamPage';

/**
 * @cssproperty --beam-json-max-height - Maximum height of the code block before scrolling kicks in. [default: 60vh]
 * @cssproperty --wa-color-text-normal - Primary text color.
 * @cssproperty --wa-color-text-quiet - Toolbar label color.
 * @cssproperty --wa-color-surface-default - Code block background.
 * @cssproperty --wa-color-surface-border - Toolbar separator color.
 * @cssproperty --wa-border-radius-m - Outer corner radius.
 */
export interface BeamJsonProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  /** @default '' */
  label?: string;
  /** @default 2 */
  indent?: number;
  /** @default false */
  'no-toolbar'?: boolean;
}

/** React forwarder for `<beam-json>`. */
export function BeamJson(props: BeamJsonProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamJson'), props);
}
BeamJson.displayName = 'BeamJson';

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
export interface BeamPageHeaderProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  /** Page title. Rendered as an H1. @default '' */
  label?: string;
  /** Optional description rendered below the title. @default '' */
  description?: string;
}

/** React forwarder for `<beam-page-header>`. */
export function BeamPageHeader(props: BeamPageHeaderProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamPageHeader'), props);
}
BeamPageHeader.displayName = 'BeamPageHeader';

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
export interface BeamConfirmDialogProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-confirm-dialog>`. */
export function BeamConfirmDialog(props: BeamConfirmDialogProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamConfirmDialog'), props);
}
BeamConfirmDialog.displayName = 'BeamConfirmDialog';

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
export interface BeamPaginationProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-pagination>`. */
export function BeamPagination(props: BeamPaginationProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamPagination'), props);
}
BeamPagination.displayName = 'BeamPagination';

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
export interface BeamToastProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-toast>`. */
export function BeamToast(props: BeamToastProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamToast'), props);
}
BeamToast.displayName = 'BeamToast';

/**
 * @csspart base - The fixed-position container.
 * @cssproperty --beam-toast-stack-gap - Vertical gap between toasts. [default: 0.5rem]
 * @cssproperty --beam-toast-stack-offset - Distance from the screen edge. [default: 1rem]
 * @cssproperty --beam-toast-stack-z-index - Stacking context. [default: 70]
 * @event wa-show - Fired when a new toast is appended. Bubbles, composed.
 * @event wa-hide - Fired when a toast is removed. Bubbles, composed.
 */
export interface BeamToastStackProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  /** Where toasts appear on the screen. @default 'bottom-end' */
  placement?: 'top-start' | 'top-end' | 'bottom-start' | 'bottom-end';
  /** Maximum number of toasts visible at once. Older toasts beyond this limit are dropped. @default 5 */
  'max-toasts'?: number;
}

/** React forwarder for `<beam-toast-stack>`. */
export function BeamToastStack(props: BeamToastStackProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamToastStack'), props);
}
BeamToastStack.displayName = 'BeamToastStack';

/**
 * @csspart base - The component's outer wrapper.
 * @csspart header - The header bar (language label + copy button).
 * @csspart lang - The language label inside the header.
 * @csspart copy-button - The copy button.
 * @csspart body - The scrollable code area that holds the Shiki output.
 * @cssproperty --beam-code-snippet-bg - Background of the code area. Defaults to the sidebar color in dark mode and the surface-hover color in light mode.
 * @cssproperty --beam-code-snippet-radius - Outer border radius. [default: 0.5rem]
 */
export interface BeamCodeSnippetProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  /** Source code to render. @default '' */
  code?: string;
  /** Shiki language id (e.g. `tsx`, `bash`, `json`). Defaults to `tsx`. @default 'tsx' */
  lang?: string;
}

/** React forwarder for `<beam-code-snippet>`. */
export function BeamCodeSnippet(props: BeamCodeSnippetProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamCodeSnippet'), props);
}
BeamCodeSnippet.displayName = 'BeamCodeSnippet';

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
export interface BeamChangeBarProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-change-bar>`. */
export function BeamChangeBar(props: BeamChangeBarProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamChangeBar'), props);
}
BeamChangeBar.displayName = 'BeamChangeBar';

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
export interface BeamKpiCardProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-kpi-card>`. */
export function BeamKpiCard(props: BeamKpiCardProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamKpiCard'), props);
}
BeamKpiCard.displayName = 'BeamKpiCard';

/**
 * @slot (default) - The label text. Plain string is typical; rich content works too.
 * @slot action - Right-aligned action element (button, link, etc).
 * @csspart base - The outer flex container.
 * @csspart label - The label text span.
 * @cssproperty --beam-section-label-margin-bottom - Bottom margin reserved below the label. Set to 0 if the label sits inline. [default: 1rem]
 */
export interface BeamSectionLabelProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  // No attributes defined in CEM.
}

/** React forwarder for `<beam-section-label>`. */
export function BeamSectionLabel(props: BeamSectionLabelProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamSectionLabel'), props);
}
BeamSectionLabel.displayName = 'BeamSectionLabel';

/**
 * @slot (default) - The pill label (usually a short string like "Enabled" or "Live").
 * @csspart base - The pill element itself.
 * @cssproperty --beam-status-pill-padding - Inner padding. [default: 0.125rem 0.5rem]
 * @cssproperty --beam-status-pill-radius - Corner radius. [default: 4px]
 * @cssproperty --beam-status-pill-font-size - Font size. [default: 0.6875rem]
 */
export interface BeamStatusPillProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  /**
   * Color treatment. `neutral` (default) uses the muted text color. `success` / `warning` / `error` / `info` / `accent` use the matching semantic palette.
   * @default 'neutral'
   */
  tone?: 'success' | 'warning' | 'error' | 'info' | 'accent' | 'neutral';
}

/** React forwarder for `<beam-status-pill>`. */
export function BeamStatusPill(props: BeamStatusPillProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamStatusPill'), props);
}
BeamStatusPill.displayName = 'BeamStatusPill';

/**
 * @slot (default) - Anything inline-sized. Overrides the `icon` attribute shortcut.
 * @csspart base - The tile element.
 * @cssproperty --beam-icon-tile-size - Width and height of the tile. [default: 2rem]
 * @cssproperty --beam-icon-tile-radius - Corner radius (50% = circle). [default: 50%]
 * @cssproperty --beam-icon-tile-font-size - Icon font size. [default: 0.875rem]
 */
export interface BeamIconTileProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

/** React forwarder for `<beam-icon-tile>`. */
export function BeamIconTile(props: BeamIconTileProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamIconTile'), props);
}
BeamIconTile.displayName = 'BeamIconTile';

/**
 * @slot (default) - Tile content. Typically `<beam-kpi-card>` children, but any block-level child works.
 * @csspart base - The grid container.
 * @cssproperty --beam-kpi-row-min - Minimum column width before the grid reflows. [default: 11rem]
 * @cssproperty --beam-kpi-row-gap - Gap between tiles. [default: 1rem]
 */
export interface BeamKpiRowProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  // No attributes defined in CEM.
}

/** React forwarder for `<beam-kpi-row>`. */
export function BeamKpiRow(props: BeamKpiRowProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamKpiRow'), props);
}
BeamKpiRow.displayName = 'BeamKpiRow';
