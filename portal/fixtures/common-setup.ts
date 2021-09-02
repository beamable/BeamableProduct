import featherIconMock from './feather-icon-mock';
import { route } from "svelte-filerouter";
import { Config } from '../src/config';

jest.mock('../src/services');
import { getServices } from '../src/services';

export interface CommonSetupOptions {
    readonly icons: Array<string>
}

export interface CommonOutput {
    mockServices: (services: any) => void,
    config: (config: Partial<Config>) => void,
}

export default function(options: Partial<CommonSetupOptions> = {
    icons: ['test']
}): CommonOutput {


    window.router = {
        url: () => {}, 
        route,
        changeRoute: () => {}
      }
    window.config = {
        host: 'nowhere',
        dev: false
    }

    featherIconMock(options.icons);


    return {
        mockServices: services => {
            (getServices as jest.Mock).mockImplementation(() => services); 
        },
        config: config => {
            window.config = {...window.config, ...config}
        }
    }
}