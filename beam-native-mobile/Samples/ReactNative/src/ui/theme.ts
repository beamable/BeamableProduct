/**
 * Design tokens for the sample's UI kit.
 *
 * Every colour here was previously a hardcoded hex literal repeated across `app/index.tsx`
 * and `src/unity/UnityBridgeSection.tsx`. There is deliberately no dark-mode variant: the
 * sample commits to the light palette (`app.json` still declares `userInterfaceStyle:
 * "automatic"`, which only affects native chrome).
 */
export const colors = {
  /** Card / section background and its hairline border. */
  surface: '#f9fafb',
  surfaceBorder: '#e5e7eb',
  /** Inbox message cards sit on white to lift them off the section. */
  card: '#ffffff',

  ink: '#111827',
  inkSoft: '#374151',
  muted: '#6b7280',
  mutedSoft: '#9ca3af',

  primary: '#5A31F4',
  inputBorder: '#d1d5db',

  warn: '#b45309',
  danger: '#dc2626',

  /** Inline request outcomes rendered under a button (see AsyncButton). */
  okInk: '#166534',
  okBg: '#f0fdf4',
  okBorder: '#bbf7d0',
  errorInk: '#b91c1c',
  errorBg: '#fef2f2',
  errorBorder: '#fecaca',

  /** Connection-status dots, keyed by `BeamStatus['state']`. */
  statusIdle: '#9ca3af',
  statusConnecting: '#f59e0b',
  statusReady: '#22c55e',
  statusError: '#ef4444',

  /** The dark console + native-event cards. */
  console: '#0b1021',
  consoleBar: '#111827',
  consoleBorder: '#1f2937',
  consoleBarBorder: '#374151',
  consoleInk: '#e5e7eb',
  consoleMuted: '#6b7280',
  consoleClear: '#f87171',
} as const;

export const radius = { sm: 6, md: 8, lg: 10, xl: 12 } as const;

export const space = { xs: 4, sm: 8, md: 10, lg: 14, xl: 20 } as const;

/**
 * Hints, log lines and JSON payloads are monospaced so token prefixes and event
 * payloads line up. 'Courier' is what the sample has always used.
 */
export const mono = 'Courier';
