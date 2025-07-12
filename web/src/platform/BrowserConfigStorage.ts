interface Config {
  cid: string;
  pid: string;
}

/**
 * Reads the Beamable configuration from storage.
 * In browser environments, it reads from localStorage.
 */
export async function readConfigBrowser(): Promise<Config> {
  return {
    cid: localStorage.getItem('beam_cid') ?? '',
    pid: localStorage.getItem('beam_pid') ?? '',
  };
}

/**
 * Saves the Beamable configuration to storage.
 * In browser environments, it saves to localStorage.
 */
export async function saveConfigBrowser({ cid, pid }: Config): Promise<void> {
  localStorage.setItem('beam_cid', cid);
  localStorage.setItem('beam_pid', pid);
}
