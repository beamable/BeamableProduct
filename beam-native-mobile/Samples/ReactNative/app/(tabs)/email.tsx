import { useState } from 'react';

import {
  addEmail,
  confirmEmailChange,
  isEmailAvailable,
  startEmailChange,
} from '../../src/beam/account';
import { useBeam } from '../../src/state/beamContext';
import AsyncButton from '../../src/ui/AsyncButton';
import Field from '../../src/ui/Field';
import { Hint, Value } from '../../src/ui/Hint';
import RailActionNote from '../../src/ui/RailActionNote';
import Screen from '../../src/ui/Screen';
import Section from '../../src/ui/Section';

/**
 * Email tab: the account's login credential, and the `email` message rail.
 *
 * ADD and CHANGE are different backend flows and only one applies at a time, so the section
 * shown is driven by whether the account already carries an email — see src/beam/account.ts.
 */
export default function EmailTab() {
  const { account, isGuest, refreshAccount, setRailOptIn, isReady } = useBeam();
  const hasEmail = !!account?.email;

  // Add flow (no email attached yet).
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  // Change flow (an email is already attached).
  const [newEmail, setNewEmail] = useState('');
  const [code, setCode] = useState('');
  const [currentPassword, setCurrentPassword] = useState('');

  const requireConnected = () => {
    if (!isReady) throw new Error('Beamable is not connected yet');
  };

  const checkAvailability = async () => {
    requireConnected();
    const address = email.trim();
    if (!address) throw new Error('Enter an email address first');
    if (!(await isEmailAvailable(address)))
      throw new Error(`${address} is already registered to an account`);
    return `${address} is available`;
  };

  const addEmailToAccount = async () => {
    requireConnected();
    const address = email.trim();
    if (!address || !password) throw new Error('Enter an email and a password first');
    // addCredentials fails with a generic message when the address is taken, which is the
    // usual cause — check first so the button can name it.
    if (!(await isEmailAvailable(address)))
      throw new Error(
        `${address} is already registered to another account — use a different address.`,
      );
    const acct = await addEmail(address, password);
    // Re-read so the Account section (and the player id in the connection bar) reflect it.
    await refreshAccount();
    return `Email attached: ${acct.email ?? address}`;
  };

  const sendChangeCode = async () => {
    requireConnected();
    const address = newEmail.trim();
    if (!address) throw new Error('Enter the new email address first');
    if (!(await isEmailAvailable(address)))
      throw new Error(`${address} is already registered to another account`);
    await startEmailChange(address);
    return `Verification code sent to ${address} — enter it below`;
  };

  const confirmChange = async () => {
    requireConnected();
    if (!code.trim() || !currentPassword)
      throw new Error('Enter the code from the new address and your current password');
    await confirmEmailChange(code.trim(), currentPassword);
    await refreshAccount();
    setCode('');
    setCurrentPassword('');
    return `Email changed to ${newEmail.trim()}`;
  };

  return (
    <Screen>
      <Section
        title="Account"
        right={<AsyncButton label="Refresh" variant="secondary" run={refreshAccount} />}
      >
        <Hint>
          Reads beam.account.current() → GET /basic/accounts/me. The SDK has no observable for
          identity, so this is fetched on connect and after any credential change.
        </Hint>
        {account ? (
          <>
            <Value label="Player">{String(account.id)}</Value>
            <Value label="Email">{account.email || '— (none attached)'}</Value>
            <Value label="Type">{isGuest ? 'guest' : 'has credentials'}</Value>
            <Value label="Devices">{String(account.deviceIds.length)}</Value>
            {account.thirdPartyAppAssociations.length > 0 && (
              <Value label="Third party">
                {account.thirdPartyAppAssociations.join(', ')}
              </Value>
            )}
          </>
        ) : (
          <Hint>Not loaded yet — waiting for the connection.</Hint>
        )}
      </Section>

      {/* Distinct keys matter: without them React reconciles the two branches positionally when
          `hasEmail` flips after a successful add, and the AsyncButtons inherit each other's
          result — the availability ✓ reappears under "Send verification code". */}
      {hasEmail ? (
        <Section key="change-email" title="Change the account email">
          <Hint>
            This account already has an email, so addCredentials no longer applies — changing it
            is a two-step flow. Step 1 asks the backend to email a verification code to the NEW
            address (initiateEmailUpdate). Step 2 confirms that code with your current password
            (confirmEmailUpdate).
          </Hint>
          <Value label="Current">{account?.email ?? ''}</Value>
          <Field
            placeholder="New email address"
            keyboardType="email-address"
            value={newEmail}
            onChangeText={setNewEmail}
          />
          <AsyncButton label="1 · Send verification code" run={sendChangeCode} />
          <Field
            placeholder="Verification code (from the new address)"
            value={code}
            onChangeText={setCode}
          />
          <Field
            placeholder="Current account password"
            secureTextEntry
            value={currentPassword}
            onChangeText={setCurrentPassword}
          />
          <AsyncButton label="2 · Confirm new email" run={confirmChange} />
        </Section>
      ) : (
        <Section key="add-email" title="Add email to this account">
          <Hint>
            The app connects as a guest. Attach an email + password so the account can be
            recovered / logged into later. Calls beam.account.addCredentials → POST
            /basic/accounts/register. An address already used by another account is rejected —
            check it first if adding fails.
          </Hint>
          <Field
            placeholder="Email (e.g. rn-demo@example.com)"
            keyboardType="email-address"
            value={email}
            onChangeText={setEmail}
          />
          <Field
            placeholder="Password"
            secureTextEntry
            value={password}
            onChangeText={setPassword}
          />
          <AsyncButton label="Check availability" variant="secondary" run={checkAvailability} />
          <AsyncButton label="Add email to account" run={addEmailToAccount} />
        </Section>
      )}

      <Section title="Email rail">
        <Hint>
          Email delivery is opt-in. Attach an email above first — opting in without one is
          rejected — then opt in so the backend routes campaigns to the `email` rail (POST
          /api/message-rail/register), which resolves your address server-side at send time.
        </Hint>
        <AsyncButton label="Opt in to email" run={() => setRailOptIn('email', true)} />
        <AsyncButton
          label="Opt out of email"
          variant="secondary"
          run={() => setRailOptIn('email', false)}
        />
        <RailActionNote rail="email" />
      </Section>
    </Screen>
  );
}
