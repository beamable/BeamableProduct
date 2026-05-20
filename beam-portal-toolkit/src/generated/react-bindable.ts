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
import { hostComponent } from './react-host';

export interface BeamCheckboxProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
  /** Fired when the checked state toggles. Receives the new boolean. */
  onCheckedChange?: (checked: boolean) => void;
}

export function BeamCheckbox(props: BeamCheckboxProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamCheckbox'), props);
}
BeamCheckbox.displayName = 'BeamCheckbox';

export interface BeamInputProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
  /** Fired on commit (change). Receives the typed value. */
  onValueChange?: (value: string) => void;
  /** Fired continuously while the user edits. Receives the typed value. */
  onValueInput?: (value: string) => void;
}

export function BeamInput(props: BeamInputProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamInput'), props);
}
BeamInput.displayName = 'BeamInput';

export interface BeamColorPickerProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
  /** Fired on commit (change). Receives the typed value. */
  onValueChange?: (value: string) => void;
  /** Fired continuously while the user edits. Receives the typed value. */
  onValueInput?: (value: string) => void;
}

export function BeamColorPicker(props: BeamColorPickerProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamColorPicker'), props);
}
BeamColorPicker.displayName = 'BeamColorPicker';

export interface BeamNumberInputProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
  /** Fired on commit (change). Receives the typed value. */
  onValueChange?: (value: number) => void;
  /** Fired continuously while the user edits. Receives the typed value. */
  onValueInput?: (value: number) => void;
}

export function BeamNumberInput(props: BeamNumberInputProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamNumberInput'), props);
}
BeamNumberInput.displayName = 'BeamNumberInput';

export interface BeamSelectProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
  /** Fired on commit (change). Receives the typed value. */
  onValueChange?: (value: string) => void;
}

export function BeamSelect(props: BeamSelectProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamSelect'), props);
}
BeamSelect.displayName = 'BeamSelect';

export interface BeamRadioGroupProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
  /** Fired on commit (change). Receives the typed value. */
  onValueChange?: (value: string) => void;
}

export function BeamRadioGroup(props: BeamRadioGroupProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamRadioGroup'), props);
}
BeamRadioGroup.displayName = 'BeamRadioGroup';

export interface BeamRatingProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
  /** Fired on commit (change). Receives the typed value. */
  onValueChange?: (value: number) => void;
}

export function BeamRating(props: BeamRatingProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamRating'), props);
}
BeamRating.displayName = 'BeamRating';

export interface BeamSliderProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
  /** Fired on commit (change). Receives the typed value. */
  onValueChange?: (value: number) => void;
  /** Fired continuously while the user edits. Receives the typed value. */
  onValueInput?: (value: number) => void;
}

export function BeamSlider(props: BeamSliderProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamSlider'), props);
}
BeamSlider.displayName = 'BeamSlider';

export interface BeamSwitchProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
  /** Fired when the checked state toggles. Receives the new boolean. */
  onCheckedChange?: (checked: boolean) => void;
}

export function BeamSwitch(props: BeamSwitchProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamSwitch'), props);
}
BeamSwitch.displayName = 'BeamSwitch';

export interface BeamTextareaProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
  /** Fired on commit (change). Receives the typed value. */
  onValueChange?: (value: string) => void;
  /** Fired continuously while the user edits. Receives the typed value. */
  onValueInput?: (value: string) => void;
}

export function BeamTextarea(props: BeamTextareaProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamTextarea'), props);
}
BeamTextarea.displayName = 'BeamTextarea';
