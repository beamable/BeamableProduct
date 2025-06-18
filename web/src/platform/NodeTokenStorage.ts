import { TokenStorage } from '@/platform/types/TokenStorage';
import * as fs from 'node:fs';
import * as os from 'node:os';
import * as path from 'node:path';

/**
 * TokenStorage implementation for Node.js environments.
 * Persists tokens to disk between process runs and caches them in memory.
 */
export class NodeTokenStorage extends TokenStorage {
  private readonly prefix: string;
  private readonly filePath: string;

  /**
   * @param {string} tag - Optional tag used to distinguish tokens that belong to different Beam instances.
   */
  constructor(tag?: string) {
    super();
    this.prefix = tag ? `${tag}_` : '';
    const directory = path.join(os.homedir(), '.beamable');
    this.filePath = path.join(directory, `${this.prefix}beam_tokens.json`);
    try {
      if (fs.existsSync(this.filePath)) {
        const content = fs.readFileSync(this.filePath, 'utf8');
        const data = JSON.parse(content);
        this.accessToken = data.accessToken ?? null;
        this.refreshToken = data.refreshToken ?? null;
        this.expiresIn =
          typeof data.expiresIn === 'number' ? data.expiresIn : null;
      }
    } catch {
      // Ignore errors during initial read and fallback to in‑memory behavior
    }
  }

  private async persist(): Promise<void> {
    try {
      const dir = path.dirname(this.filePath);
      await fs.promises.mkdir(dir, { recursive: true });
      const data = {
        accessToken: this.accessToken,
        refreshToken: this.refreshToken,
        expiresIn: this.expiresIn,
      };
      await fs.promises.writeFile(
        this.filePath,
        JSON.stringify(data, null, 2) + '\n',
        'utf8',
      );
    } catch {
      // Ignore errors during persistence and fallback to in‑memory behavior
    }
  }

  async getAccessToken(): Promise<string | null> {
    return this.accessToken;
  }

  async setAccessToken(token: string): Promise<void> {
    this.accessToken = token;
    await this.persist();
  }

  async removeAccessToken(): Promise<void> {
    this.accessToken = null;
    await this.persist();
  }

  async getRefreshToken(): Promise<string | null> {
    return this.refreshToken;
  }

  async setRefreshToken(token: string): Promise<void> {
    this.refreshToken = token;
    await this.persist();
  }

  async removeRefreshToken(): Promise<void> {
    this.refreshToken = null;
    await this.persist();
  }

  async getExpiresIn(): Promise<number | null> {
    return this.expiresIn;
  }

  async setExpiresIn(expiresIn: number): Promise<void> {
    this.expiresIn = expiresIn;
    await this.persist();
  }

  async removeExpiresIn(): Promise<void> {
    this.expiresIn = null;
    await this.persist();
  }

  async clear(): Promise<void> {
    await Promise.all([
      this.removeAccessToken(),
      this.removeRefreshToken(),
      this.removeExpiresIn(),
    ]);
  }

  dispose(): void {}
}
