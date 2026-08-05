/**
 * App-specific bindings for account management.
 *
 * The sample connects as a guest (an anonymous player created by `Beam.init()`). These helpers
 * cover the two credential flows, which are NOT interchangeable:
 *
 *   - ADD    (`addCredentials`) — attaches an email/password to an account that has none.
 *             Fails once the account already has an email credential.
 *   - CHANGE (`initiateEmailUpdate` → `confirmEmailUpdate`) — replaces the email on an account
 *             that already has one. Two steps: the backend emails a verification code, which is
 *             then confirmed together with the account password.
 *
 * `AccountService` is registered in `beamClient.ts`.
 */
import { CredentialStatus } from '@beamable/sdk';
import { getBeam } from './beamClient';

const NOT_CONNECTED =
  'Not connected — Beamable connects automatically on launch; wait for it, or use Retry connection.';

function requireBeam() {
  const beam = getBeam();
  if (!beam) throw new Error(NOT_CONNECTED);
  return beam;
}

/**
 * Whether an address is free to register.
 *
 * `addCredentials` fails with a generic error when the address already belongs to another
 * account — by far the most common reason "add email" fails when reusing a test address across
 * realms — so the sample checks first and reports that specific cause.
 */
export async function isEmailAvailable(email: string): Promise<boolean> {
  const status = await requireBeam().account.getEmailCredentialStatus({ email });
  return status === CredentialStatus.NotAssigned;
}

/**
 * Attaches an email + password login to the current (guest) account.
 *
 * Wraps `beam.account.addCredentials(...)`, which POSTs to `/basic/accounts/register` and
 * returns the updated account with its `email` populated.
 */
export function addEmail(email: string, password: string) {
  return requireBeam().account.addCredentials({ email, password });
}

/**
 * Step 1 of changing the email on an account that already has one: asks the backend to send a
 * verification code to `newEmail`.
 */
export function startEmailChange(newEmail: string) {
  return requireBeam().account.initiateEmailUpdate({ newEmail });
}

/**
 * Step 2 of changing the email: confirms the code delivered to the new address, authorised by
 * the account's current password.
 */
export function confirmEmailChange(code: string, password: string) {
  return requireBeam().account.confirmEmailUpdate({ code, password });
}
