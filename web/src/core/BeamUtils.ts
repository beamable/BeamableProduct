import { TokenResponse } from '@/__generated__/schemas';
import { TokenStorage } from '@/platform/types/TokenStorage';
import { isBrowserEnv } from '@/utils/isBrowserEnv';
import path from 'node:path';
import os from 'node:os';
import fs from 'node:fs';

/** A collection of utility functions for the Beam SDK. */
export class BeamUtils {
  /**
   * Saves the access token, refresh token, and expiration time from a token response to the token storage.
   * @param {TokenStorage} tokenStorage - The token storage instance to use.
   * @param {TokenResponse} tokenResponse - The token response object containing the tokens and expiration time.
   * @returns {Promise<void>} A promise that resolves when the tokens are saved.
   */
  static async saveToken(
    tokenStorage: TokenStorage,
    tokenResponse: TokenResponse,
  ): Promise<void> {
    await tokenStorage.setAccessToken(tokenResponse.access_token as string);
    await tokenStorage.setRefreshToken(tokenResponse.refresh_token as string);
    await tokenStorage.setExpiresIn(
      Date.now() + Number(tokenResponse.expires_in),
    );
  }

  /**
   * Saves the provided cid and pid to a local config file or local storage.
   * @param {string} cid - The customer ID to save.
   * @param {string} pid - The project ID to save.
   * @returns {Promise<void>} A promise that resolves when the config is saved.
   */
  static async saveConfig(cid: string, pid: string): Promise<void> {
    if (isBrowserEnv()) {
      localStorage.setItem('beam_cid', cid);
      localStorage.setItem('beam_pid', pid);
      return;
    }

    const directory = path.join(os.homedir(), '.beamable');
    const filePath = path.join(directory, 'beam_config.json');

    try {
      // Read existing config if it exists
      let current: Record<string, string> = {};
      try {
        const raw = await fs.promises.readFile(filePath, 'utf8');
        current = JSON.parse(raw);
      } catch {
        // If the file doesn't exist or isn't valid JSON, ignore
      }

      // Merge and write new config
      const data = { ...current, cid, pid };
      await fs.promises.mkdir(directory, { recursive: true });
      await fs.promises.writeFile(
        filePath,
        JSON.stringify(data, null, 2) + '\n',
        'utf8',
      );
    } catch (err) {
      console.error("Couldn't save beam config:", err);
    }
  }

  /**
   * Reads the saved config from a local config file or local storage.
   * @returns {Promise<{ cid: string; pid: string }>} A promise that resolves with the saved cid and pid.
   */
  static async readConfig(): Promise<{ cid: string; pid: string }> {
    if (isBrowserEnv()) {
      return {
        cid: localStorage.getItem('beam_cid') ?? '',
        pid: localStorage.getItem('beam_pid') ?? '',
      };
    }

    const directory = path.join(os.homedir(), '.beamable');
    const filePath = path.join(directory, 'beam_config.json');

    try {
      if (fs.existsSync(filePath)) {
        const raw = await fs.promises.readFile(filePath, 'utf8');
        const data = JSON.parse(raw) as Partial<Record<string, string>>;
        return {
          cid: data.cid ?? '',
          pid: data.pid ?? '',
        };
      }
      return { cid: '', pid: '' };
    } catch (err) {
      // If the file doesn't exist or isn't valid JSON, return default values
      return { cid: '', pid: '' };
    }
  }
}
