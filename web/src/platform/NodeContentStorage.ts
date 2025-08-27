import * as fs from 'node:fs';
import * as os from 'node:os';
import * as path from 'node:path';
import { BEAM_NODE_DIR } from '@/constants';
import { ContentStorage } from '@/platform/types/ContentStorage';
import { BeamJsonUtils } from '@/utils/BeamJsonUtils';

/** Content storage implementation for Node.js, using the file system. */
export class NodeContentStorage extends ContentStorage {
  private readonly storeDir: string;

  private constructor(storeDir: string) {
    super();
    this.storeDir = storeDir;
  }

  static async open(): Promise<NodeContentStorage> {
    const directory = path.join(os.homedir(), BEAM_NODE_DIR);
    const storeDir = path.join(directory, ContentStorage.storeName);

    await fs.promises.mkdir(storeDir, { recursive: true });
    return new NodeContentStorage(storeDir);
  }

  async set<T>(key: string, value: T): Promise<void> {
    this.ensureOpen();

    if (value === undefined) {
      await this.del(key); // undefined deletes the key
      return;
    }

    const file = this.fileForKey(key);
    const tmpFile = file + '.tmp-' + Math.random().toString(36).slice(2);
    const data = JSON.stringify(value, BeamJsonUtils.replacer);

    await fs.promises.mkdir(path.dirname(file), { recursive: true });
    // atomic write
    await fs.promises.writeFile(tmpFile, data, 'utf8');
    await fs.promises.rename(tmpFile, file);
  }

  async get<T = unknown>(key: string): Promise<T | undefined> {
    this.ensureOpen();
    const file = this.fileForKey(key);
    try {
      const data = await fs.promises.readFile(file, 'utf8');
      return JSON.parse(data, BeamJsonUtils.reviver) as T;
    } catch (err: any) {
      if (err?.code === 'ENOENT') return undefined; // the file or directory does not exist
      throw err;
    }
  }

  async has(key: string): Promise<boolean> {
    this.ensureOpen();
    const file = this.fileForKey(key);
    try {
      await fs.promises.access(file);
      return true;
    } catch (err: any) {
      if (err?.code === 'ENOENT') return false; // the file or directory does not exist
      throw err;
    }
  }

  async del(key: string): Promise<void> {
    this.ensureOpen();
    const file = this.fileForKey(key);
    try {
      await fs.promises.unlink(file);
    } catch (err: any) {
      if (err?.code === 'ENOENT') return; // deleting a non-existent key is fine
      throw err;
    }
  }

  async clear(): Promise<void> {
    this.ensureOpen();
    // Remove only files in the store directory
    const entries = await fs.promises.readdir(this.storeDir, {
      withFileTypes: true,
    });
    await Promise.all(
      entries.map(async (entry) => {
        if (entry.isFile()) {
          await fs.promises.unlink(path.join(this.storeDir, entry.name));
        }
      }),
    );
  }

  // Helper to get safe file names for keys
  private fileForKey(key: string): string {
    // encode file name to avoid slashes or odd chars
    const safe = encodeURIComponent(key) + '.json';
    return path.join(this.storeDir, safe);
  }
}
