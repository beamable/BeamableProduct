import {PlayerData, PlayerDataInterface} from '../src/services/players';

export const playerWithFacebook: PlayerData = new PlayerData({
    email: 'test@beamable.com',
    createdTimeMillis: 400000,
    id: 1,
    deviceId: 'abc',
    updatedTimeMillis: 400001,
    gamerTags: [{
        gamerTag: 123,
        projectId: 'test'
    }],
    thirdParties: [{
        appId: 'facebook',
        name: 'facebook',
        userAppId: 'facebook',
        userBusinessId: 'facebook',
        meta: {}
    }]
}, "test")

export const anonymousPlayer: PlayerData = new PlayerData({
    email: 'test@beamable.com',
    createdTimeMillis: 400000,
    id: 1,
    deviceId: '',
    updatedTimeMillis: 400001,
    gamerTags: [{
        gamerTag: 123,
        projectId: 'test'
    }],
    thirdParties: []
}, "test")