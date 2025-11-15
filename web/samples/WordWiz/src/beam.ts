import { AccountService, AuthService, Beam, StatsService } from 'beamable-sdk';
import { DAILY_STREAK, ENDLESS_STREAK } from '@app/game/constants.ts';
import { GameStore } from '@app/game/state/store.ts';

/**
 * Initializes Beamable SDK
 * - Configures Beam with environment values.
 * - Registers required Beam services.
 *
 * @returns {Promise<Beam>} A fully initialized and authenticated Beam instance.
 */
export async function setupBeam(): Promise<Beam> {
  // Initialize Beamable SDK with project configuration
  const beam: Beam = await Beam.init({
    cid: '1639786776798208',
    pid: 'DE_1912686680736768',
    environment: 'dev',
  });

  try {
    // Register required services with the Beam instance
    // beam.use(AuthService).use(AccountService).use(StatsService);
    beam.use([AuthService, AccountService, StatsService]);
    return beam;
  } catch (error) {
    console.error('Beam Error:', error);
    return beam;
  }
}

export async function getAndComputePlayerStats(
  beam: Beam,
  store: GameStore,
): Promise<void> {
  try {
    const stats = await beam.stats.get({
      accessType: 'private',
      stats: [DAILY_STREAK, ENDLESS_STREAK],
    });

    const dailyStreak = DAILY_STREAK in stats ? stats[DAILY_STREAK] : '0';
    const endlessStreak = ENDLESS_STREAK in stats ? stats[ENDLESS_STREAK] : '0';

    store.stats = {
      [DAILY_STREAK]: dailyStreak,
      [ENDLESS_STREAK]: endlessStreak,
    };

    dispatchEvent(new CustomEvent('stats_updated'));
  } catch (error) {
    console.error('Failed to fetch player stats:', error);
  }
}
