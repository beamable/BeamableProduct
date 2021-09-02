import { render } from "@testing-library/svelte";

import * as svelteStores from 'svelte/store';

import common from '../../fixtures/common-setup';

import {LeaderboardList} from '../services/leaderboards';
import {anonymousPlayer} from '../../fixtures/sample-players';
import {emptyLeaderboards} from '../../fixtures/sample-player-leaderboards';

const { mockServices } = common();
import PlayerLeaderboards from './PlayerLeaderboards.svelte';

describe("PlayerLeaderboards Component", () => {

    test("should default the add leaderboard to the first available one, when there is data available", () => {
        mockServices({
            leaderboards: {
                leaderboardList: svelteStores.writable<LeaderboardList>({
                    nameList: ['leaderboards.testboard'],
                    offset: 0,
                    total: 1
                })
            }
        })

        const comp = render(PlayerLeaderboards, {
            player: anonymousPlayer,
            playerLeaderboards: emptyLeaderboards
        });

        const state:any = comp.component['$capture_state']();
        expect(state.leaderBoardToAdd).toEqual('leaderboards.testboard');
    });

    test("should not throw an error if no leaderboards exist in the realm", () => {
        mockServices({
            leaderboards: {
                leaderboardList: svelteStores.writable<LeaderboardList>({
                    nameList: [],
                    offset: 0,
                    total: 0
                })
            }
        });

        const svelteComponent = render(PlayerLeaderboards, {
            player: anonymousPlayer,
            playerLeaderboards: emptyLeaderboards
        });

        const state:any = svelteComponent.component.$capture_state();
        expect(state.leaderBoardToAdd).toEqual(undefined);
    });
});
