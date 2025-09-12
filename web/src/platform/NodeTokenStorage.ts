import { TokenStorage, type TokenData } from '@/platform/types/TokenStorage';
import * as fs from 'node:fs';
import * as os from 'node:os';
import * as path from 'node:path';
import { BEAM_NODE_DIR } from '@/constants';

/**
 * TokenStorage implementation for Node.js environments.
 * Persists tokens to disk between process runs and caches them in memory.
 */
export class NodeTokenStorage extends TokenStorage {
  private readonly prefix: string;
  private readonly filePath: string;

  constructor(pid: string, tag?: string) {
    super();
    this.prefix = tag ? `${tag}_${pid}_` : `${pid}_`;
    const directory = path.join(os.homedir(), BEAM_NODE_DIR);
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

  async getTokenData() {
    return {
      accessToken: this.accessToken,
      refreshToken: this.refreshToken,
      expiresIn: this.expiresIn,
    };
  }

  async setTokenData(data: Partial<TokenData>): Promise<this> {
    if ('accessToken' in data) this.accessToken = data.accessToken ?? null;
    if ('refreshToken' in data) this.refreshToken = data.refreshToken ?? null;
    if ('expiresIn' in data) this.expiresIn = data.expiresIn ?? null;
    await this.persist();
    return this;
  }

  async clear(): Promise<void> {
    // Remove all token files from disk regardless of prefix
    const dir = path.join(os.homedir(), BEAM_NODE_DIR);
    try {
      const files = await fs.promises.readdir(dir);
      await Promise.all(
        files
          .filter((file) => file.endsWith('beam_tokens.json'))
          .map((file) => fs.promises.unlink(path.join(dir, file))),
      );
    } catch {
      // Ignore errors (e.g., directory does not exist)
    }

    // Reset in-memory values
    this.accessToken = null;
    this.refreshToken = null;
    this.expiresIn = null;
  }

  dispose(): void {}
}
