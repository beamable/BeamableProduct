import path from 'node:path';
import os from 'node:os';
import fs from 'node:fs';
import { BEAM_NODE_DIR } from '@/constants';

/**
 * Reads the Beamable configuration from storage.
 * In Node.js environments, it reads from a JSON file in the user's home directory.
 */
export async function readConfigNode(): Promise<{
  cid: string;
  pid: string;
}> {
  const directory = path.join(os.homedir(), BEAM_NODE_DIR);
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
  } catch {
    // If the file doesn't exist or isn't valid JSON, return default values
    return { cid: '', pid: '' };
  }
}

/**
 * Saves the Beamable configuration to storage.
 * In Node.js environments, it saves it to a JSON file in the user's home directory.
 */
export async function saveConfigNode(cfg: {
  cid: string;
  pid: string;
}): Promise<void> {
  const directory = path.join(os.homedir(), BEAM_NODE_DIR);
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
    const data = { ...current, cid: cfg.cid, pid: cfg.pid };
    await fs.promises.mkdir(directory, { recursive: true });
    await fs.promises.writeFile(
      filePath,
      JSON.stringify(data, null, 2) + '\n',
      'utf8',
    );
  } catch (error) {
    console.error("Couldn't save beam config:", error);
  }
}
