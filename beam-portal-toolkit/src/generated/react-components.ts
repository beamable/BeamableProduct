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

export interface BeamIconProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

export function BeamIcon(props: BeamIconProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamIcon'), props);
}
BeamIcon.displayName = 'BeamIcon';

export interface BeamSpinnerProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamSpinner(props: BeamSpinnerProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamSpinner'), props);
}
BeamSpinner.displayName = 'BeamSpinner';

export interface BeamTreeItemProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  expanded?: boolean;
  selected?: boolean;
  disabled?: boolean;
  lazy?: boolean;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamTreeItem(props: BeamTreeItemProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamTreeItem'), props);
}
BeamTreeItem.displayName = 'BeamTreeItem';

export interface BeamButtonProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

export function BeamButton(props: BeamButtonProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamButton'), props);
}
BeamButton.displayName = 'BeamButton';

export interface BeamAnimationProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

export function BeamAnimation(props: BeamAnimationProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamAnimation'), props);
}
BeamAnimation.displayName = 'BeamAnimation';

export interface BeamAvatarProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  image?: string;
  label?: string;
  initials?: string;
  loading?: 'eager' | 'lazy';
  shape?: 'circle' | 'square' | 'rounded';
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamAvatar(props: BeamAvatarProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamAvatar'), props);
}
BeamAvatar.displayName = 'BeamAvatar';

export interface BeamBadgeProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  variant?: 'brand' | 'neutral' | 'success' | 'warning' | 'danger';
  appearance?: 'accent' | 'filled' | 'outlined' | 'filled-outlined';
  pill?: boolean;
  attention?: 'none' | 'pulse' | 'bounce';
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamBadge(props: BeamBadgeProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamBadge'), props);
}
BeamBadge.displayName = 'BeamBadge';

export interface BeamBreadcrumbItemProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  href?: string | undefined;
  target?: '_blank' | '_parent' | '_self' | '_top' | undefined;
  rel?: string;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamBreadcrumbItem(props: BeamBreadcrumbItemProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamBreadcrumbItem'), props);
}
BeamBreadcrumbItem.displayName = 'BeamBreadcrumbItem';

export interface BeamBreadcrumbProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  label?: string;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamBreadcrumb(props: BeamBreadcrumbProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamBreadcrumb'), props);
}
BeamBreadcrumb.displayName = 'BeamBreadcrumb';

export interface BeamButtonGroupProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  label?: string;
  orientation?: 'horizontal' | 'vertical';
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamButtonGroup(props: BeamButtonGroupProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamButtonGroup'), props);
}
BeamButtonGroup.displayName = 'BeamButtonGroup';

export interface BeamCalloutProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  variant?: 'brand' | 'neutral' | 'success' | 'warning' | 'danger';
  appearance?: 'accent' | 'filled' | 'outlined' | 'plain' | 'filled-outlined';
  size?: 'small' | 'medium' | 'large';
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamCallout(props: BeamCalloutProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamCallout'), props);
}
BeamCallout.displayName = 'BeamCallout';

export interface BeamCardProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  appearance?: 'accent' | 'filled' | 'outlined' | 'filled-outlined' | 'plain';
  'with-header'?: boolean;
  'with-media'?: boolean;
  'with-footer'?: boolean;
  orientation?: 'horizontal' | 'vertical';
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamCard(props: BeamCardProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamCard'), props);
}
BeamCard.displayName = 'BeamCard';

export interface BeamPopupProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

export function BeamPopup(props: BeamPopupProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamPopup'), props);
}
BeamPopup.displayName = 'BeamPopup';

export interface BeamTooltipProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

export function BeamTooltip(props: BeamTooltipProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamTooltip'), props);
}
BeamTooltip.displayName = 'BeamTooltip';

export interface BeamCopyButtonProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

export function BeamCopyButton(props: BeamCopyButtonProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamCopyButton'), props);
}
BeamCopyButton.displayName = 'BeamCopyButton';

export interface BeamDetailsProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  open?: boolean;
  summary?: string;
  name?: string;
  disabled?: boolean;
  appearance?: 'filled' | 'outlined' | 'filled-outlined' | 'plain';
  'icon-placement'?: 'start' | 'end';
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamDetails(props: BeamDetailsProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamDetails'), props);
}
BeamDetails.displayName = 'BeamDetails';

export interface BeamDialogProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  open?: boolean;
  label?: string;
  'without-header'?: boolean;
  'light-dismiss'?: boolean;
  'with-footer'?: boolean;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamDialog(props: BeamDialogProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamDialog'), props);
}
BeamDialog.displayName = 'BeamDialog';

export interface BeamDividerProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  orientation?: 'horizontal' | 'vertical';
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamDivider(props: BeamDividerProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamDivider'), props);
}
BeamDivider.displayName = 'BeamDivider';

export interface BeamDrawerProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  open?: boolean;
  label?: string;
  placement?: 'top' | 'end' | 'bottom' | 'start';
  'without-header'?: boolean;
  'light-dismiss'?: boolean;
  'with-footer'?: boolean;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamDrawer(props: BeamDrawerProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamDrawer'), props);
}
BeamDrawer.displayName = 'BeamDrawer';

export interface BeamDropdownItemProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  variant?: 'danger' | 'default';
  value?: string;
  type?: 'normal' | 'checkbox';
  checked?: boolean;
  disabled?: boolean;
  submenuOpen?: boolean;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamDropdownItem(props: BeamDropdownItemProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamDropdownItem'), props);
}
BeamDropdownItem.displayName = 'BeamDropdownItem';

export interface BeamDropdownProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

export function BeamDropdown(props: BeamDropdownProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamDropdown'), props);
}
BeamDropdown.displayName = 'BeamDropdown';

export interface BeamFormatBytesProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  value?: number;
  unit?: 'byte' | 'bit';
  display?: 'long' | 'short' | 'narrow';
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamFormatBytes(props: BeamFormatBytesProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamFormatBytes'), props);
}
BeamFormatBytes.displayName = 'BeamFormatBytes';

export interface BeamFormatDateProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

export function BeamFormatDate(props: BeamFormatDateProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamFormatDate'), props);
}
BeamFormatDate.displayName = 'BeamFormatDate';

export interface BeamFormatNumberProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

export function BeamFormatNumber(props: BeamFormatNumberProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamFormatNumber'), props);
}
BeamFormatNumber.displayName = 'BeamFormatNumber';

export interface BeamIntersectionObserverProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  root?: string | null;
  'root-margin'?: string;
  threshold?: string;
  'intersect-class'?: string;
  once?: boolean;
  disabled?: boolean;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamIntersectionObserver(props: BeamIntersectionObserverProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamIntersectionObserver'), props);
}
BeamIntersectionObserver.displayName = 'BeamIntersectionObserver';

export interface BeamMutationObserverProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  attr?: string;
  'attr-old-value'?: boolean;
  'char-data'?: boolean;
  'char-data-old-value'?: boolean;
  'child-list'?: boolean;
  disabled?: boolean;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamMutationObserver(props: BeamMutationObserverProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamMutationObserver'), props);
}
BeamMutationObserver.displayName = 'BeamMutationObserver';

export interface BeamTagProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  variant?: 'brand' | 'neutral' | 'success' | 'warning' | 'danger';
  appearance?: 'accent' | 'filled' | 'outlined' | 'filled-outlined';
  size?: 'small' | 'medium' | 'large';
  pill?: boolean;
  'with-remove'?: boolean;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamTag(props: BeamTagProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamTag'), props);
}
BeamTag.displayName = 'BeamTag';

export interface BeamOptionProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  value?: string;
  disabled?: boolean;
  selected?: boolean;
  label?: string;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamOption(props: BeamOptionProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamOption'), props);
}
BeamOption.displayName = 'BeamOption';

export interface BeamPopoverProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

export function BeamPopover(props: BeamPopoverProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamPopover'), props);
}
BeamPopover.displayName = 'BeamPopover';

export interface BeamProgressBarProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  value?: number;
  indeterminate?: boolean;
  label?: string;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamProgressBar(props: BeamProgressBarProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamProgressBar'), props);
}
BeamProgressBar.displayName = 'BeamProgressBar';

export interface BeamProgressRingProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  value?: number;
  label?: string;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamProgressRing(props: BeamProgressRingProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamProgressRing'), props);
}
BeamProgressRing.displayName = 'BeamProgressRing';

export interface BeamQrCodeProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

export function BeamQrCode(props: BeamQrCodeProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamQrCode'), props);
}
BeamQrCode.displayName = 'BeamQrCode';

export interface BeamRadioProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  value?: string;
  appearance?: 'default' | 'button';
  size?: 'small' | 'medium' | 'large';
  disabled?: boolean;
  name?: string | null;
  'custom-error'?: string | null;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamRadio(props: BeamRadioProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamRadio'), props);
}
BeamRadio.displayName = 'BeamRadio';

export interface BeamRelativeTimeProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  'with-utc-popover'?: boolean;
  date?: Date | string;
  format?: 'long' | 'short' | 'narrow';
  numeric?: 'always' | 'auto';
  sync?: boolean;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamRelativeTime(props: BeamRelativeTimeProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamRelativeTime'), props);
}
BeamRelativeTime.displayName = 'BeamRelativeTime';

export interface BeamScrollerProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  orientation?: 'horizontal' | 'vertical';
  'without-scrollbar'?: boolean;
  'without-shadow'?: boolean;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamScroller(props: BeamScrollerProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamScroller'), props);
}
BeamScroller.displayName = 'BeamScroller';

export interface BeamResizeObserverProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  disabled?: boolean;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamResizeObserver(props: BeamResizeObserverProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamResizeObserver'), props);
}
BeamResizeObserver.displayName = 'BeamResizeObserver';

export interface BeamSkeletonProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  effect?: 'pulse' | 'sheen' | 'none';
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamSkeleton(props: BeamSkeletonProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamSkeleton'), props);
}
BeamSkeleton.displayName = 'BeamSkeleton';

export interface BeamTabProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  panel?: string;
  disabled?: boolean;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamTab(props: BeamTabProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamTab'), props);
}
BeamTab.displayName = 'BeamTab';

export interface BeamSplitPanelProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
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
}

export function BeamSplitPanel(props: BeamSplitPanelProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamSplitPanel'), props);
}
BeamSplitPanel.displayName = 'BeamSplitPanel';

export interface BeamTabPanelProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  name?: string;
  active?: boolean;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamTabPanel(props: BeamTabPanelProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamTabPanel'), props);
}
BeamTabPanel.displayName = 'BeamTabPanel';

export interface BeamTabGroupProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  active?: string;
  placement?: 'top' | 'bottom' | 'start' | 'end';
  activation?: 'auto' | 'manual';
  'without-scroll-controls'?: boolean;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamTabGroup(props: BeamTabGroupProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamTabGroup'), props);
}
BeamTabGroup.displayName = 'BeamTabGroup';

export interface BeamTreeProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  selection?: 'single' | 'multiple' | 'leaf';
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamTree(props: BeamTreeProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamTree'), props);
}
BeamTree.displayName = 'BeamTree';

export interface BeamMarkdownProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  'tab-size'?: number;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamMarkdown(props: BeamMarkdownProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamMarkdown'), props);
}
BeamMarkdown.displayName = 'BeamMarkdown';

export interface BeamPageProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  view?: 'mobile' | 'desktop';
  'nav-open'?: boolean;
  'mobile-breakpoint'?: string;
  'navigation-placement'?: 'start' | 'end';
  'disable-navigation-toggle'?: boolean;
  dir?: string;
  lang?: string;
  'did-ssr'?: unknown;
}

export function BeamPage(props: BeamPageProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamPage'), props);
}
BeamPage.displayName = 'BeamPage';

export interface BeamJsonProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  label?: string;
  indent?: number;
  'no-toolbar'?: boolean;
}

export function BeamJson(props: BeamJsonProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamJson'), props);
}
BeamJson.displayName = 'BeamJson';

export interface BeamPageHeaderProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  label?: string;
  description?: string;
}

export function BeamPageHeader(props: BeamPageHeaderProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamPageHeader'), props);
}
BeamPageHeader.displayName = 'BeamPageHeader';

export interface BeamConfirmDialogProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  open?: boolean;
  label?: string;
  message?: string;
  'confirm-label'?: string;
  'cancel-label'?: string;
  'confirm-variant'?: unknown;
}

export function BeamConfirmDialog(props: BeamConfirmDialogProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamConfirmDialog'), props);
}
BeamConfirmDialog.displayName = 'BeamConfirmDialog';

export interface BeamPaginationProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  page?: number;
  'page-size'?: number;
  total?: number;
  'page-size-options'?: string;
  'hide-info'?: boolean;
  'hide-page-input'?: boolean;
}

export function BeamPagination(props: BeamPaginationProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamPagination'), props);
}
BeamPagination.displayName = 'BeamPagination';

export interface BeamToastProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  variant?: unknown;
  message?: string;
  duration?: number;
  toastId?: number;
  'action-label'?: string;
  'no-close'?: boolean;
}

export function BeamToast(props: BeamToastProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamToast'), props);
}
BeamToast.displayName = 'BeamToast';

export interface BeamToastStackProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  placement?: 'top-start' | 'top-end' | 'bottom-start' | 'bottom-end';
  'max-toasts'?: number;
}

export function BeamToastStack(props: BeamToastStackProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamToastStack'), props);
}
BeamToastStack.displayName = 'BeamToastStack';

export interface BeamCodeSnippetProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  code?: string;
  lang?: string;
}

export function BeamCodeSnippet(props: BeamCodeSnippetProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamCodeSnippet'), props);
}
BeamCodeSnippet.displayName = 'BeamCodeSnippet';

export interface BeamChangeBarProps extends DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement> {
  'data-state'?: unknown;
  saving?: boolean;
  'summary-text'?: string;
  'save-label'?: string;
  'saving-label'?: string;
  'discard-label'?: string;
  'review-label'?: string;
  'review-title'?: string;
  'no-review'?: boolean;
  inline?: boolean;
  'auto-save'?: boolean;
  'auto-save-debounce'?: number;
}

export function BeamChangeBar(props: BeamChangeBarProps & { ref?: Ref<HTMLElement> }): ReactElement {
  return createElement(hostComponent('BeamChangeBar'), props);
}
BeamChangeBar.displayName = 'BeamChangeBar';
