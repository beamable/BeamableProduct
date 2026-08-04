/**
 * App-specific binding for account management.
 *
 * The sample connects as a guest (an anonymous player created by `Beam.init()`). These
 * helpers let that guest attach an email/password credential so the account can later be
 * recovered / logged into. `AccountService` is registered in `beamClient.ts`.
 */
import { getBeam } from './beamClient';

const NOT_CONNECTED =
  'Not connected — call initBeam() (Connect to Beamable) first.';

/**
 * Attaches an email + password login to the current (guest) account.
 *
 * Wraps `beam.account.addCredentials(...)`, which POSTs to `/basic/accounts/register` and
 * returns the updated account with its `email` populated. Throws if the account already has
 * an email credential.
 */
export function addEmail(email: string, password: string) {
  const beam = getBeam();
  if (!beam) throw new Error(NOT_CONNECTED);
  return beam.account.addCredentials({ email, password });
}
