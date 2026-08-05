/**
 * The two log streams, hoisted above the tab navigator.
 *
 * Both used to be `useState` inside the single `Home()` screen, which meant the native-event
 * stream was scoped to one page. They live here so every tab writes to the same Activity log
 * and the debug console can render either stream from anywhere.
 *
 * The actions and the data are deliberately TWO contexts. Pages only ever need `append`,
 * which is stable — putting it in the same context value as the arrays would re-render every
 * page on every log line.
 */
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';

import { BeamNotifications } from '@beamable/notifications-react-native';
import type { BeamableEvent } from '@beamable/notifications-react-native';

/**
 * One line of free-text feedback from a button press.
 *
 * Each entry carries a stable id so prepending a new line MOUNTS one <Text> instead of
 * mutating every existing line's content (index keys would re-write them all on each append,
 * and heavy text re-serialization can trip a New-Architecture text-layout crash).
 */
export type ActivityEntry = { id: number; text: string };

/** One captured native-event firing, with its raw payload. */
export type EventEntry = {
  key: number;
  time: string;
  event: BeamableEvent;
  data: unknown;
};

const ACTIVITY_CAP = 40;
const EVENT_CAP = 100;

type LogActions = {
  append: (msg: string) => void;
  clearActivity: () => void;
  clearEvents: () => void;
};

type LogData = { activity: ActivityEntry[]; events: EventEntry[] };

const LogActionsContext = createContext<LogActions | null>(null);
const LogDataContext = createContext<LogData | null>(null);

export function LogProvider({ children }: { children: ReactNode }) {
  const [activity, setActivity] = useState<ActivityEntry[]>([]);
  const [events, setEvents] = useState<EventEntry[]>([]);
  const activityCounter = useRef(0);
  const eventCounter = useRef(0);

  const append = useCallback((msg: string) => {
    setActivity((prev) =>
      [
        { id: (activityCounter.current += 1), text: `${timestamp()}  ${msg}` },
        ...prev,
      ].slice(0, ACTIVITY_CAP),
    );
  }, []);

  const clearActivity = useCallback(() => setActivity([]), []);
  const clearEvents = useCallback(() => setEvents([]), []);

  // `isSupported` is static true on iOS/Android but DYNAMIC on web — it flips once a Unity
  // WebView host reports native support — so track it rather than reading it once.
  const [supported, setSupported] = useState(BeamNotifications.isSupported);
  useEffect(() => {
    const sub = BeamNotifications.addSupportListener(setSupported);
    return () => sub.remove();
  }, []);

  // ── Native events → the color-coded event stream ───────────────────────────
  // `addAllListeners` subscribes to the whole event vocabulary in one typed call and returns
  // a single subscription — no per-event loop, no `addListener` cast.
  useEffect(() => {
    if (!supported) return;
    const sub = BeamNotifications.addAllListeners((event, data) => {
      setEvents((prev) =>
        [
          { key: (eventCounter.current += 1), time: timestamp(), event, data },
          ...prev,
        ].slice(0, EVENT_CAP),
      );
    });
    return () => sub.remove();
  }, [supported]);

  // Stable for the lifetime of the provider: all three are `useCallback(…, [])`.
  const actions = useMemo<LogActions>(
    () => ({ append, clearActivity, clearEvents }),
    [append, clearActivity, clearEvents],
  );
  const data = useMemo<LogData>(() => ({ activity, events }), [activity, events]);

  return (
    <LogActionsContext.Provider value={actions}>
      <LogDataContext.Provider value={data}>{children}</LogDataContext.Provider>
    </LogActionsContext.Provider>
  );
}

/** Write to the logs. Safe to call from any page — the value never changes identity. */
export function useLogActions(): LogActions {
  const ctx = useContext(LogActionsContext);
  if (!ctx) throw new Error('useLogActions must be used inside <LogProvider>');
  return ctx;
}

/** Read the logs. Only the debug console should use this — it re-renders on every entry. */
export function useLogData(): LogData {
  const ctx = useContext(LogDataContext);
  if (!ctx) throw new Error('useLogData must be used inside <LogProvider>');
  return ctx;
}

export function timestamp(): string {
  return new Date().toLocaleTimeString();
}
