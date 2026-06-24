// AUTO-GENERATED — do not edit manually.
// Run `pnpm sync-components` to regenerate from agentic-portal's custom-elements.json.
//
// React forwarder components for input-like `beam-*` elements that support
// controlled-component binding. Same pattern as `react-components.ts`, plus
// typed value callbacks:
//
//   <BeamInput     value={x} onValueChange={setX} />   // (value: string) => void
//   <BeamSlider    value={x} onValueChange={setX} />   // (value: number) => void
//   <BeamCheckbox  checked={x} onCheckedChange={setX} /> // (checked: boolean) => void
//   <BeamNumberInput value={x} onValueChange={setX} /> // empty → NaN
//
// The standard `onChange` / `onInput` still works and fires alongside —
// these callbacks are purely additive.

import { createElement, type DetailedHTMLProps, type HTMLAttributes, type ReactElement, type Ref } from 'react';
import { hostComponent } from '../react-host';

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
export interface BeamCheckboxProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
  /** Fired when the checked state toggles. Receives the new boolean. */
  onCheckedChange?: (checked: boolean) => void;
}

/** React forwarder for `<beam-checkbox>` with controlled-component bindings. */
export function BeamCheckbox(props: BeamCheckboxProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamCheckbox'), props);
}
BeamCheckbox.displayName = 'BeamCheckbox';

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
export interface BeamInputProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
  /** Fired on commit (change). Receives the typed value. */
  onValueChange?: (value: string) => void;
  /** Fired continuously while the user edits. Receives the typed value. */
  onValueInput?: (value: string) => void;
}

/** React forwarder for `<beam-input>` with controlled-component bindings. */
export function BeamInput(props: BeamInputProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamInput'), props);
}
BeamInput.displayName = 'BeamInput';

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
export interface BeamColorPickerProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
  /** Fired on commit (change). Receives the typed value. */
  onValueChange?: (value: string) => void;
  /** Fired continuously while the user edits. Receives the typed value. */
  onValueInput?: (value: string) => void;
}

/** React forwarder for `<beam-color-picker>` with controlled-component bindings. */
export function BeamColorPicker(props: BeamColorPickerProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamColorPicker'), props);
}
BeamColorPicker.displayName = 'BeamColorPicker';

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
export interface BeamNumberInputProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
  /** Fired on commit (change). Receives the typed value. */
  onValueChange?: (value: number) => void;
  /** Fired continuously while the user edits. Receives the typed value. */
  onValueInput?: (value: number) => void;
}

/** React forwarder for `<beam-number-input>` with controlled-component bindings. */
export function BeamNumberInput(props: BeamNumberInputProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamNumberInput'), props);
}
BeamNumberInput.displayName = 'BeamNumberInput';

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
export interface BeamSelectProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
  /** Fired on commit (change). Receives the typed value. */
  onValueChange?: (value: string) => void;
}

/** React forwarder for `<beam-select>` with controlled-component bindings. */
export function BeamSelect(props: BeamSelectProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamSelect'), props);
}
BeamSelect.displayName = 'BeamSelect';

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
export interface BeamRadioGroupProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
  /** Fired on commit (change). Receives the typed value. */
  onValueChange?: (value: string) => void;
}

/** React forwarder for `<beam-radio-group>` with controlled-component bindings. */
export function BeamRadioGroup(props: BeamRadioGroupProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamRadioGroup'), props);
}
BeamRadioGroup.displayName = 'BeamRadioGroup';

/**
 * @csspart base - The component's base wrapper.
 * @cssproperty --symbol-color - The inactive color for symbols.
 * @cssproperty --symbol-color-active - The active color for symbols.
 * @cssproperty --symbol-spacing - The spacing to use around symbols.
 * @event wa-hover - Emitted when the user hovers over a value. The `phase` property indicates when hovering starts, moves to a new value, or ends. The `value` property tells what the rating's value would be if the user were to commit to the hovered value. (React: `onWaHover`)
 * @event wa-invalid - Emitted when the form control has been checked for validity and its constraints aren't satisfied. (React: `onWaInvalid`)
 */
export interface BeamRatingProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
  /** Fired on commit (change). Receives the typed value. */
  onValueChange?: (value: number) => void;
}

/** React forwarder for `<beam-rating>` with controlled-component bindings. */
export function BeamRating(props: BeamRatingProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamRating'), props);
}
BeamRating.displayName = 'BeamRating';

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
export interface BeamSliderProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
  /** Fired on commit (change). Receives the typed value. */
  onValueChange?: (value: number) => void;
  /** Fired continuously while the user edits. Receives the typed value. */
  onValueInput?: (value: number) => void;
}

/** React forwarder for `<beam-slider>` with controlled-component bindings. */
export function BeamSlider(props: BeamSliderProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamSlider'), props);
}
BeamSlider.displayName = 'BeamSlider';

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
export interface BeamSwitchProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
  /** Fired when the checked state toggles. Receives the new boolean. */
  onCheckedChange?: (checked: boolean) => void;
}

/** React forwarder for `<beam-switch>` with controlled-component bindings. */
export function BeamSwitch(props: BeamSwitchProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamSwitch'), props);
}
BeamSwitch.displayName = 'BeamSwitch';

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
export interface BeamTextareaProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
  /** Fired on commit (change). Receives the typed value. */
  onValueChange?: (value: string) => void;
  /** Fired continuously while the user edits. Receives the typed value. */
  onValueInput?: (value: string) => void;
}

/** React forwarder for `<beam-textarea>` with controlled-component bindings. */
export function BeamTextarea(props: BeamTextareaProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamTextarea'), props);
}
BeamTextarea.displayName = 'BeamTextarea';
