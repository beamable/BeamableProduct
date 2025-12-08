import type { TokenResponse } from '@/__generated__/schemas';
import { TokenStorage } from '@/platform/types/TokenStorage';
import { REFRESHABLE_SERVICES, type RefreshableServiceMap } from '@/core/types';
import { BeamJsonUtils } from '@/utils/BeamJsonUtils';
import { HttpRequester } from '@/network/http/types/HttpRequester';
import { BeamRequester } from '@/network/http/BeamRequester';
import { BaseRequester } from '@/network/http/BaseRequester';
import { isBrowserEnv } from '@/utils/isBrowserEnv';
import { defaultTokenStorage } from '@/defaults';
import { BeamEnvironmentName } from '@/configs/BeamEnvironmentConfig';
import { BeamEnvironment } from '@/core/BeamEnvironmentRegistry';
import { HEADERS } from '@/constants';
import packageJson from '../../package.json';
import type { Beam } from '@/core/Beam';
import type { BeamServer } from '@/core/BeamServer';
import {
  AccountService,
  AnnouncementsService,
  AuthService,
  ContentService,
  LeaderboardsService,
  StatsService,
} from '@/services';

/** Saves the access token, refresh token, and expiration time from a token response to the token storage. */
export async function saveToken(
  tokenStorage: TokenStorage,
  tokenResponse: TokenResponse,
): Promise<void> {
  await tokenStorage.setTokenData({
    accessToken: tokenResponse.access_token as string,
    refreshToken: tokenResponse.refresh_token as string,
    expiresIn: Date.now() + Number(tokenResponse.expires_in),
  });
}

/** Parses a raw socket message string into a structured object based on the expected `RefreshableServiceMap` type. */
export function parseSocketMessage<K extends keyof RefreshableServiceMap>(
  rawMessage: string,
): RefreshableServiceMap[K] {
  const parsed = JSON.parse(rawMessage, BeamJsonUtils.reviver);
  const { scopes, delay, ...rest } = parsed;
  // The unknown field will be the remaining key in `rest`
  const [unknownKey] = Object.keys(rest);
  const data = rest[unknownKey] as RefreshableServiceMap[K]['data'];

  return {
    data,
    ...parsed,
  };
}

export interface StandaloneRequesterProps {
  /** The Beamable Customer ID. */
  cid: string;
  /** The Beamable Project ID. */
  pid: string;
  /**
   * The Beamable environment to connect to.
   * Can be one of 'prod', 'stg', 'dev', or a custom environment name.
   * @default 'prod'
   */
  environment?: BeamEnvironmentName;
  /** The custom `HttpRequester` to use for the API requests. If not provided, a default one will be used. */
  requester?: HttpRequester;
  /** The custom `TokenStorage` to use for the API requests. If not provided, a default one will be used. */
  tokenStorage?: TokenStorage;
  /** Unique tag for instance-specific token storage synchronization. */
  tokenStorageTag?: string;
  /**
   * Enables signing outgoing requests with a signature header.
   * @remarks
   * This option is only supported in Node.js environments.
   * When running in a browser, this setting will be ignored.
   * @defaultValue false
   */
  useSignedRequest?: boolean;
}

/**
 * Creates a `HttpRequester` instance for standalone API requests.
 * This is useful when you need to make API calls without initializing the SDK.
 * @param props - Configuration properties for the standalone requester.
 * @returns A `HttpRequester` instance.
 */
export function createStandaloneRequester(
  props: StandaloneRequesterProps,
): HttpRequester {
  const baseRequester = props.requester ?? new BaseRequester();
  baseRequester.baseUrl = BeamEnvironment.get(
    props.environment ?? 'prod',
  ).apiUrl;
  baseRequester.defaultHeaders = {
    [HEADERS.ACCEPT]: 'application/json',
    [HEADERS.CONTENT_TYPE]: 'application/json',
    [HEADERS.BEAM_SCOPE]: `${props.cid}.${props.pid}`,
    [HEADERS.BEAM_SDK_VERSION]: packageJson.version,
  };

  return new BeamRequester({
    inner: baseRequester,
    tokenStorage:
      props.tokenStorage ??
      defaultTokenStorage({ pid: props.pid, tag: props.tokenStorageTag }),
    useSignedRequest: !isBrowserEnv() && (props.useSignedRequest ?? false),
    pid: props.pid,
  });
}

/** Creates services for the Beam Client SDK */
export function clientServices(beam: Beam) {
  const apiServiceProps = { beam, getPlayer: () => beam.player };
  const allApiServices = [
    new AccountService(apiServiceProps),
    new AnnouncementsService(apiServiceProps),
    new AuthService(apiServiceProps),
    new ContentService(apiServiceProps),
    new LeaderboardsService(apiServiceProps),
    new StatsService(apiServiceProps),
  ];

  allApiServices.forEach((svc) => {
    const svcName = svc.serviceName;
    (beam as any).clientServices[svcName] = svc;

    if (REFRESHABLE_SERVICES.includes(svcName)) {
      const refreshKey = `${svcName}.refresh` as keyof RefreshableServiceMap;
      (beam as any).refreshableRegistry[refreshKey] = svc;
    }
  });
}

/** Creates services for the Beam Server SDK */
export function serverServices(beamServer: BeamServer) {
  const apiServiceProps = { beam: beamServer };
  const allApiServices = [
    new AccountService(apiServiceProps),
    new AnnouncementsService(apiServiceProps),
    new AuthService(apiServiceProps),
    new ContentService(apiServiceProps),
    new LeaderboardsService(apiServiceProps),
    new StatsService(apiServiceProps),
  ];

  allApiServices.forEach((svc) => {
    const svcName = svc.serviceName;
    (beamServer as any).serverServices[svcName] = (userId: string) => {
      svc.userId = userId;
      return svc;
    };
  });
}
