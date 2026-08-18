/**
 * Beamable connection state, hoisted above the tab navigator.
 *
 * The sample used to require a "Connect to Beamable" tap before anything worked. Init now
 * fires once on mount; on failure the connection bar offers a manual retry. There is no
 * automatic retry — a demo that silently reconnects hides the failure you wanted to see.
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

import type { Beam } from '@beamable/sdk';
import type { Subscription } from '@beamable/notifications-react-native';

import { getBeam, initBeam, type BeamStatus } from '../beam/beamClient';
import { startLiveActivityTokenForwarding } from '../beam/liveActivity';
import { useLogActions } from './logContext';

/**
 * The account view `beam.account.current()` resolves to. Derived from the service rather
 * than imported by name so it tracks the SDK's generated schema automatically.
 */
export type Account = Awaited<ReturnType<Beam['account']['current']>>;

/** Rails the client can opt into through `beam.messageRail`. */
export type RailId = 'email' | 'ingame';

/**
 * The outcome of the last opt-in/opt-out we performed for a rail, tracked purely client-side.
 *
 * `MessageRailService` exposes only `optIn` / `optOut` — there is no endpoint to READ a
 * player's current registration back, and the response doesn't echo it. So this is "what this
 * app last did", not authoritative server state, and the UI labels it as such.
 */
export type RailAction = { optIn: boolean; ok: boolean; at: string };

type BeamContextValue = {
  status: BeamStatus;
  isReady: boolean;
  playerId: string | null;
  /** Retry a failed connection. Also the initial connect, called once on mount. */
  retry: () => void;
  account: Account | null;
  /**
   * Re-reads the account. Resolves with a human-readable summary and REJECTS on failure, so
   * `AsyncButton` can render the outcome inline; callers that must not fail (connect) catch.
   */
  refreshAccount: () => Promise<string>;
  /** True when the account has no email, third-party, or external identity attached. */
  isGuest: boolean;
  railStatus: Partial<Record<RailId, RailAction>>;
  /** Resolves with a summary, rejects on failure — same contract as `refreshAccount`. */
  setRailOptIn: (rail: RailId, optIn: boolean) => Promise<string>;
};

const BeamContext = createContext<BeamContextValue | null>(null);

export function BeamProvider({ children }: { children: ReactNode }) {
  const { append } = useLogActions();
  const [status, setStatus] = useState<BeamStatus>({ state: 'idle' });
  const [account, setAccount] = useState<Account | null>(null);
  const [railStatus, setRailStatus] = useState<Partial<Record<RailId, RailAction>>>({});

  // Live Activity push-token forwarding (started on connect; iOS 17.2+ device only).
  const liveActivitySub = useRef<Subscription | null>(null);
  // `initBeam()` is promise-memoized, but the subscription restart below is not — so guard
  // against a second connect landing while the first is still in flight.
  const connecting = useRef(false);

  /**
   * Read the player's account. The SDK has no observable for identity — `Beam.on` only
   * supports `announcements.refresh` / `content.refresh` — so this is called explicitly:
   * once after init, and again after attaching an email. `current()` also refreshes
   * `beam.player.account`, which is where the player id displayed in the bar comes from.
   */
  const refreshAccount = useCallback(async () => {
    const beam = getBeam();
    if (!beam) throw new Error('Beamable is not connected yet');
    const acct = await beam.account.current();
    setAccount(acct);
    // Keep the connection bar's player id in step with whatever the account now resolves to.
    setStatus((prev) =>
      prev.state === 'ready' && prev.playerId !== beam.player.id
        ? { state: 'ready', playerId: beam.player.id }
        : prev,
    );
    return `Account loaded — ${acct.email || 'guest'} · player ${beam.player.id}`;
  }, []);

  const connect = useCallback(async () => {
    if (connecting.current) return;
    connecting.current = true;
    setStatus({ state: 'connecting' });
    append('Beam.init() …');
    try {
      const beam = await initBeam();
      setStatus({ state: 'ready', playerId: beam.player.id });
      append(`Beam ready. player.id = ${beam.player.id}`);
      // Forward any Live Activity push-to-start / update tokens to the `push` rail so the
      // backend can drive Live Activities via APNs. Tokens only arrive on a physical
      // iOS 17.2+ device.
      liveActivitySub.current?.remove();
      liveActivitySub.current = startLiveActivityTokenForwarding(append);
      // Best-effort: a failed account read shouldn't flip the whole connection to `error`.
      try {
        append(await refreshAccount());
      } catch (e) {
        append(`Account read error: ${message(e)}`);
      }
    } catch (e) {
      setStatus({ state: 'error', message: message(e) });
      append(`Beam error: ${message(e)}`);
    } finally {
      connecting.current = false;
    }
  }, [append, refreshAccount]);

  // Auto-init, once. This is the whole point of dropping the connect button.
  useEffect(() => {
    void connect();
    return () => liveActivitySub.current?.remove();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  /** Opt in / out of a message rail (`email` / `ingame`) via `beam.messageRail`. */
  const setRailOptIn = useCallback(async (rail: RailId, optIn: boolean) => {
    const beam = getBeam();
    if (!beam) throw new Error('Beamable is not connected yet');
    const verb = optIn ? 'opt-in' : 'opt-out';
    const at = new Date().toLocaleTimeString();
    const record = (ok: boolean) =>
      setRailStatus((prev) => ({ ...prev, [rail]: { optIn, ok, at } }));
    try {
      const res = optIn
        ? await beam.messageRail.optIn(rail)
        : await beam.messageRail.optOut(rail);
      // The endpoint answers 200 with `success: false` for a rejected registration, so a
      // resolved promise is not on its own a success.
      if (!res.success) {
        throw new Error(res.message || `${rail} ${verb} was rejected by the backend`);
      }
      record(true);
      return `${rail} ${verb} ok${res.message ? ` — ${res.message}` : ''}`;
    } catch (e) {
      record(false);
      throw e;
    }
  }, []);

  const value = useMemo<BeamContextValue>(
    () => ({
      status,
      isReady: status.state === 'ready',
      playerId: status.state === 'ready' ? status.playerId : null,
      retry: () => void connect(),
      account,
      refreshAccount,
      isGuest:
        !!account &&
        !account.email &&
        account.thirdPartyAppAssociations.length === 0 &&
        (account.external?.length ?? 0) === 0,
      railStatus,
      setRailOptIn,
    }),
    [status, account, refreshAccount, railStatus, setRailOptIn, connect],
  );

  return <BeamContext.Provider value={value}>{children}</BeamContext.Provider>;
}

export function useBeam(): BeamContextValue {
  const ctx = useContext(BeamContext);
  if (!ctx) throw new Error('useBeam must be used inside <BeamProvider>');
  return ctx;
}

function message(e: unknown): string {
  return e instanceof Error ? e.message : String(e);
}
