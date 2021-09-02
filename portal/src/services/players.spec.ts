
import common from '../../fixtures/common-setup';
const {mockServices} = common();

import PlayersService from './players';
import Services, {getServices} from './index';

// TODO: This test is a jerk. We need to fix it in the future.
// describe("PlayerService -- playerData", () => {
//     test("should call findPlayer when emailQuery is changed", () => {
//         mockServices({
//             router: {
//                 realmId: () => "test"
//             }
//         });

//         const services = new Services();
//         services.router = {...services.router};

//         const services = {...(new Services()), ...getServices()}
//         const playerSrvc = new PlayersService(services);
//         playerSrvc.findPlayer = jest.fn(() => new Promise(() => {}));
//         playerSrvc.playerData.subscribe(player => {
//             // no op. only exists so that svelte runs the store logic.
//         });

//         playerSrvc.emailOrDbid.set('test');
//         expect(playerSrvc.findPlayer).toBeCalledWith('test', true);
//     });
// });

describe("PlayerService -- findPlayer", () => {
    test("should now throw unhandled promise when getPlayer 404s", async () => {
        mockServices({
            auth: {
                checkAccess: () => true
            },
            router: {
                realmId: () => "test"
            }
        });

        console.error = jest.fn();
        const playerSrvc = new PlayersService(getServices());
        playerSrvc.getPlayer = jest.fn(() => new Promise(() => {
            throw 'forced failure 404';
        }));

        await playerSrvc.findPlayer('test');
        expect(console.error).toBeCalled();
    });
  });