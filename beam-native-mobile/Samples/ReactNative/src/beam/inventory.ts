/**
 * App-specific bindings for the player's **inventory** — currency balances and what a purchase
 * moved.
 *
 * There is no `beam.inventory` service in the Web SDK, so these are raw generated REST bindings
 * from `@beamable/sdk/api` called with `beam.requester` — the same pattern `segments.ts` and
 * `ingameMessages.ts` use for endpoints the SDK has no high-level service for.
 *
 * Three things here are deliberate and easy to get wrong:
 *
 *  - **Money is `bigint`, not `number`.** Currency amounts are C# `long`s, and the SDK's JSON
 *    reviver widens long numeric strings to BigInt. The rest of this sample normalises with
 *    `Number(...)`, which is right for timestamps and stat values and wrong here: a balance is
 *    money and a receipt is a subtraction of two balances. `formatAmount` groups digits by hand
 *    rather than using `toLocaleString`, because Hermes ships a partial `Intl` and
 *    `BigInt.prototype.toLocaleString` is not something to bet a money display on.
 *  - **The realm's currencies are read from content, not from the wallet.** `inventoryGetByObjectId`
 *    returns only what the player holds, so a currency they have none of is simply absent — and a
 *    purchase that grants 500 Gems then has nothing to move from. Reading the published currency
 *    content gives the full set, so a `0` row exists to change.
 *  - **This wallet can be read by the client but not written by it.** Reading is a player-token
 *    route; crediting is not, because `CurrencyContent.clientPermission.write_self` gates
 *    inventory writes and is off for any currency worth having. So `grantCurrency` calls a
 *    microservice — see that function.
 */
import { inventoryGetByObjectId } from '@beamable/sdk/api';
import { getBeam, getDebugWalletService } from './beamClient';

const NOT_CONNECTED =
  'Not connected — Beamable connects automatically on launch; wait for it, or use Retry connection.';

function requireBeam() {
  const beam = getBeam();
  if (!beam) throw new Error(NOT_CONNECTED);
  return beam;
}

/** One currency the player holds, or is known to be able to hold. */
export type Balance = {
  /** The currency content id, e.g. `currency.gems`. */
  id: string;
  /** Display label — the id's last dot-segment. Content carries no display name; see below. */
  label: string;
  amount: bigint;
};

/** A signed change in one currency, as shown on a receipt or a wallet chip. */
export type BalanceDelta = {
  id: string;
  label: string;
  /** Negative for the price paid, positive for what was granted. Never zero. */
  change: bigint;
};

/**
 * The player's currency balances.
 *
 * `scope: 'currency'` keeps the response to currencies; dropping it also returns `items`, which
 * this screen has no use for and which can be large for an established player.
 */
export async function readBalances(): Promise<Balance[]> {
  const beam = requireBeam();
  const { body } = await inventoryGetByObjectId(beam.requester, beam.player.id, 'currency');

  return (body.currencies ?? [])
    .filter((currency) => !!currency?.id)
    .map((currency) => ({
      id: currency.id,
      label: currencyLabel(currency.id),
      amount: toBigInt(currency.amount),
    }))
    .sort((a, b) => a.id.localeCompare(b.id));
}

/**
 * Every currency the realm publishes, so the wallet can show a `0` row for one the player has
 * never held.
 *
 * Note this is NOT about nicer names: `CurrencyContent` is `{ startingAmount, icon,
 * clientPermission, external? }` over `ContentBase`'s `id`/`version`/`uri`/`tags` — there is no
 * title field to read. It is about completeness, and about being able to say "this listing pays
 * out a currency that is not in the published manifest", which is the `UnknownCurrencyException`
 * failure caught before it fires rather than after.
 *
 * Best-effort: content is a separate read and a realm can legitimately have none published.
 */
export async function knownCurrencyIds(): Promise<string[]> {
  try {
    const contents = await requireBeam().content.getByType({ type: 'currency' });
    return contents.map((content) => content.id).filter(Boolean);
  } catch {
    // The wallet still renders from what the player holds; it just cannot show empty rows.
    return [];
  }
}

/**
 * The wallet as it should be shown: every published currency, with the player's amount, plus
 * anything they hold that content does not know about (a currency published after this session
 * synced, or granted by a service).
 */
export function mergeKnownCurrencies(held: Balance[], knownIds: string[]): Balance[] {
  const byId = new Map(held.map((balance) => [balance.id, balance]));

  for (const id of knownIds) {
    if (!byId.has(id)) {
      byId.set(id, { id, label: currencyLabel(id), amount: 0n });
    }
  }

  return [...byId.values()].sort((a, b) => a.id.localeCompare(b.id));
}

/**
 * Grants currency to the player, through the **`DebugWalletService`** microservice.
 *
 * **Why this is not a client call.** The obvious version of this — `PUT /object/inventory/{id}/`
 * with a `currencies` delta, straight from here — does not work, and not because of the token: it
 * is the *currency content* that decides. `CurrencyContent.clientPermission.write_self` gates
 * inventory writes, and it is off for any currency worth having, since a player who can credit
 * their own wallet has no reason to buy anything. The platform refuses, and no client change can
 * fix it.
 *
 * So the grant goes through a service, which runs with the privileged identity the `write_self`
 * check does not apply to. This is the same shape as `segments.ts` writing `game.private` stats
 * through `PlayerStatsService`, one domain over: the read is client-side, the write is not.
 *
 * **The service is a debug fixture, and its name says so.** It is `[ClientCallable]`, so anyone
 * with a player token for that realm can grant themselves anything — fine for a sample pointed at
 * a dev realm, never something to deploy anywhere else. A real grant is a server-authoritative
 * consequence of something the player did, not an amount the client names.
 *
 * `amount` crosses as a decimal string: it is a C# `long`, which the generated client types
 * `bigint | string`, and a string is what the SDK would put on the wire either way. The response
 * carries the resulting balance, so this returns it — but the screen still re-reads the wallet,
 * because one grant is not the only thing that can have moved.
 *
 * A rejection arrives as **200 with `success: false`** (unknown currency, non-positive amount), so
 * a resolved promise is not a success — the service's own sentence is thrown instead.
 */
export async function grantCurrency(id: string, amount: bigint): Promise<bigint> {
  const wallet = getDebugWalletService();
  if (!wallet) throw new Error(NOT_CONNECTED);

  let result;
  try {
    result = await wallet.addCurrency({ currencyId: id, amount: amount.toString() });
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    // `BindingNotFoundException` in a 500 means the PLATFORM has no route for
    // `micro_DebugWalletService` in this realm — it reads like a bug in the call. Same
    // translation `segments.ts` does for its own service, for the same reason.
    if (msg.includes('BindingNotFoundException')) {
      throw new Error(
        'DebugWalletService is not reachable in this realm — the platform has no binding for it.' +
          ' Either it is not deployed here (`beam deploy release`), or it is running locally' +
          " (`beam project run`), in which case the call needs that machine's routing key in" +
          ' `env.local` as BEAM_ROUTING_KEY.',
      );
    }
    throw e instanceof Error ? e : new Error(msg);
  }

  if (!result.success) {
    throw new Error(result.message || `The service refused to grant ${id}`);
  }

  return toBigInt(result.balance);
}

/**
 * A typed amount → `bigint`, or a thrown message the player can act on.
 *
 * Deliberately not `toBigInt` below: that one answers "whatever a provider sent me, keep the wallet
 * up", which is right for a response and wrong for a text field, where a typo silently becoming
 * `0` is how you press Add three times and conclude the button is broken.
 */
export function parseAmount(text: string): bigint {
  const trimmed = text.trim();
  if (!/^\d+$/.test(trimmed)) throw new Error(`"${text}" is not a whole number of currency`);

  const amount = BigInt(trimmed);
  if (amount === 0n) throw new Error('Enter an amount greater than zero');
  return amount;
}

/**
 * What changed between two wallet reads. This is how the sample answers "what did I just
 * receive" — see `payments.ts` for why the purchase response cannot.
 */
export function diffBalances(before: Balance[], after: Balance[]): BalanceDelta[] {
  const beforeById = new Map(before.map((balance) => [balance.id, balance.amount]));
  const deltas: BalanceDelta[] = [];

  for (const balance of after) {
    const change = balance.amount - (beforeById.get(balance.id) ?? 0n);
    if (change !== 0n) {
      deltas.push({ id: balance.id, label: balance.label, change });
    }
  }

  // A currency spent down to nothing disappears from the wallet entirely, so it would be missed
  // by the loop above — and that is exactly the row a player most wants to see after paying.
  const afterIds = new Set(after.map((balance) => balance.id));
  for (const balance of before) {
    if (!afterIds.has(balance.id) && balance.amount !== 0n) {
      deltas.push({ id: balance.id, label: balance.label, change: -balance.amount });
    }
  }

  // Gains first, then losses — "you got X, it cost Y" reads better than the reverse.
  return deltas.sort((a, b) => (b.change > a.change ? 1 : b.change < a.change ? -1 : 0));
}

/** `currency.gems` → `gems`. Content has no display name, so the id's tail is the best available. */
export function currencyLabel(id: string): string {
  if (!id) return '';
  const i = id.lastIndexOf('.');
  return i >= 0 && i < id.length - 1 ? id.slice(i + 1) : id;
}

/** `1234567n` → `1,234,567`. Sign is rendered by the caller, which knows if it wants a `+`. */
export function formatAmount(amount: bigint): string {
  const negative = amount < 0n;
  const digits = (negative ? -amount : amount).toString();

  let grouped = '';
  for (let i = 0; i < digits.length; i++) {
    if (i > 0 && (digits.length - i) % 3 === 0) grouped += ',';
    grouped += digits[i];
  }

  return negative ? `-${grouped}` : grouped;
}

/** Signed, for a receipt line or a wallet chip: `+500`, `-1,200`. */
export function formatDelta(change: bigint): string {
  return change > 0n ? `+${formatAmount(change)}` : formatAmount(change);
}

/**
 * `bigint | string` → `bigint`, without throwing on anything a provider might send.
 *
 * `BigInt('')` throws, and so does `BigInt('1.5')` — a store sending a float would take the whole
 * wallet down with it, which is not a trade the wallet should make.
 */
function toBigInt(value: bigint | string | undefined): bigint {
  if (typeof value === 'bigint') return value;
  try {
    return BigInt(String(value ?? 0).trim() || 0);
  } catch {
    return 0n;
  }
}
