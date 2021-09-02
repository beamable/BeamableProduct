
export interface Config {
    readonly dev: boolean;
    readonly host: string;
}

const dev: Config = {
    host: '//dev.api.beamable.com',
    dev: true,
}

const staging: Config = {
    host: '//staging.api.disruptorengine.com',
    dev: false,
}

const prod: Config = {
    host: '//api.beamable.com',
    dev: false,
}

const allConfigs:any = {
    dev,
    staging,
    prod
}

const config = allConfigs['__buildEnv__'] as Config;
export default config;
