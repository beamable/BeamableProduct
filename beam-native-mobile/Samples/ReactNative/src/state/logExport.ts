/**
 * Export a log stream to a `.log` file and hand it to the OS share sheet.
 *
 * Writing to the cache directory (rather than external storage) keeps this permission-free on
 * both platforms: the share sheet grants the receiving app read access to that one file, and
 * Android reclaims the space on its own. The user picks the destination — Drive, Gmail, Files,
 * a chat — so nothing is written anywhere durable without an explicit choice.
 */
import { Platform } from 'react-native';
import { File, Paths } from 'expo-file-system';
import * as Sharing from 'expo-sharing';

import { BEAM_CONFIG } from '../beam/config';
import type { ActivityEntry, EventEntry } from './logContext';

/** Context header so an exported log is diagnosable without the conversation around it. */
function header(title: string, count: number, playerId: string | null): string {
  return [
    `# Beamable RN Sample — ${title}`,
    `# Exported:  ${new Date().toISOString()}`,
    `# Platform:  ${Platform.OS} ${String(Platform.Version)}`,
    `# Realm:     cid=${BEAM_CONFIG.cid} pid=${BEAM_CONFIG.pid}`,
    `# Host:      ${String(BEAM_CONFIG.host)}`,
    `# Player:    ${playerId ?? '(not connected)'}`,
    `# Entries:   ${count}`,
    '',
  ].join('\n');
}

/**
 * Both streams are held newest-first for display; a file reads better oldest-first, so each
 * builder reverses a copy.
 */
export function formatActivity(entries: ActivityEntry[], playerId: string | null): string {
  return (
    header('Activity log', entries.length, playerId) +
    [...entries]
      .reverse()
      .map((e) => e.text)
      .join('\n') +
    '\n'
  );
}

export function formatEvents(entries: EventEntry[], playerId: string | null): string {
  return (
    header('Native events', entries.length, playerId) +
    [...entries]
      .reverse()
      .map((e) => `[${e.time}] ${e.event}\n${indent(pretty(e.data))}`)
      .join('\n\n') +
    '\n'
  );
}

function pretty(data: unknown): string {
  try {
    return JSON.stringify(data, null, 2) ?? String(data);
  } catch {
    // Payloads come straight off the native bridge; a cycle or a BigInt would throw.
    return String(data);
  }
}

function indent(text: string): string {
  return text
    .split('\n')
    .map((line) => `  ${line}`)
    .join('\n');
}

/** `beam-activity-2026-08-05T15-30-00.log` — colons are illegal in filenames on some targets. */
export function logFileName(kind: string): string {
  const stamp = new Date().toISOString().replace(/[:.]/g, '-').replace(/Z$/, '');
  return `beam-${kind}-${stamp}.log`;
}

/**
 * Writes `contents` to a cache-dir `.log` and opens the share sheet.
 *
 * Returns a human-readable outcome for the Activity log. Note the exported file is written
 * BEFORE the sheet opens and is left in the cache afterwards — there is no completion callback
 * that distinguishes "shared" from "dismissed", so the caller shouldn't claim it was sent.
 */
export async function exportLog(fileName: string, contents: string): Promise<string> {
  const file = new File(Paths.cache, fileName);
  file.create({ overwrite: true, intermediates: true });
  file.write(contents);

  if (!(await Sharing.isAvailableAsync())) {
    // Sharing is unavailable on web and on some restricted Android profiles. The file still
    // exists, so surface where it landed rather than failing silently.
    return `Sharing unavailable — log written to ${file.uri}`;
  }

  await Sharing.shareAsync(file.uri, {
    mimeType: 'text/plain',
    dialogTitle: 'Export log',
    UTI: 'public.plain-text',
  });
  return `Log exported: ${fileName}`;
}
