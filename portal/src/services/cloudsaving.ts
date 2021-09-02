import {BaseService} from "./base";
import {Readable, Writable, get} from "../lib/stores";
import {PlayerData} from "./players";
import { networkFallback, roleGuard } from '../lib/decorators';
import { writable } from "svelte/store";

export interface S3ObjectSummary {
  readonly bucketName: string;
  readonly key: string;
  readonly eTag: string;
  readonly size: number;
  readonly lastModified: Date;
  readonly storageClass: string;
  readonly owner: Owner;

}

export interface Manifest {
    readonly id: string;
    readonly manifest: Array<ManifestEntry>;
}

export interface ManifestEntry {
  readonly bucketName: string;
  readonly key: string;
  readonly eTag: string;
  readonly size: number;
  readonly lastModified: number;

}

export interface Owner {
  readonly displayName: string;
  readonly id: string;
}

export interface ListObjectsResponse {
  readonly result: Array<S3ObjectSummary>
}

export interface DownloadObjectsResponse {
  readonly response: Array<URLResponse>
}

export interface URLResponse {
  readonly url: string;
  readonly objectKey: string;
}

interface UploadRequest {
  readonly objectKey: string;
  readonly sizeInBytes: number;
  // TODO: include optional metadata?
  // TODO: add the forceReinit property
}

export interface UploadURLResponse {
  readonly url: string;
  readonly objectKey: string;
}

export interface FileUploadData {
  readonly newFileSize: number,
  readonly s3Object: S3ObjectSummary;
}

export class CloudSavingService extends BaseService {

  readonly forceRefresh: Writable<number> = writable(0);
  public readonly playerCloudData: Readable<Manifest> = this.derived(
    [this.app.players.playerData, this.forceRefresh],
    (args:[PlayerData, number], set:(arg: Manifest) => void) => {
      if (args[0]){
        this.fetchCloudSavingDataList(args[0]).then(set);
      }
    });

  @networkFallback()
  async fetchCloudSavingDataList(player: PlayerData): Promise<Manifest> {
      const { http } = this.app;
      const emptyResponse = {
      id: '',
      manifest: []
      };

      try {
        const response = await http.request(`basic/cloudsaving?playerId=${player.gamerTagForRealm()}`, void 0, 'get');
        return response.data as Manifest;
      } catch (exception) {
        if (exception.status == "404") {
          return emptyResponse as Manifest;
        } else {
          throw exception;
        }
      }
    }


  @roleGuard(['admin', 'developer'])
  public async copyDataFrom(srcPlayer: PlayerData, targetPlayer: PlayerData): Promise<void> {
    const { http, players } = this.app;
    const srcDbid = srcPlayer.gamerTagForRealm();
    const targetDbid = targetPlayer.gamerTagForRealm();
    await http.request(`basic/cloudsaving/data/replace?sourcePlayerId=${srcDbid}&targetPlayerId=${targetDbid}`, void 0, 'post');

    const existingPlayer = get(players.playerData);
    if (existingPlayer && existingPlayer.gamerTagForRealm() == targetDbid){
      this.forceRefresh.set(get(this.forceRefresh) + 1);
    }
  }

  public async fetchUploadUrl(player: PlayerData, files: Array<FileUploadData>): Promise<Array<UploadURLResponse>> {
    const { http } = this.app;

    const uploadRequests: Array<UploadRequest> = files.map(f => ({
      objectKey: f.s3Object.key,
      sizeInBytes: f.newFileSize
    }));
    const reqBody = {
      request: uploadRequests,
      playerId: player.gamerTagForRealm()
    };
    const result = await http.request(`basic/cloudsaving/data/uploadURLFromPortal`, reqBody, 'post');
    const urls = result.data.response as Array<UploadURLResponse>;
    return urls;
  }

  public async moveObjects(player: PlayerData): Promise<void> {
    const { http, players } = this.app;
    const reqBody = {
      playerId: player.gamerTagForRealm()
    };
    await http.request(`basic/cloudsaving/data/move`, reqBody, 'put');
    const existingPlayer = get(players.playerData);
    if (existingPlayer && existingPlayer.gamerTagForRealm() == player.gamerTagForRealm()){
      this.forceRefresh.set(get(this.forceRefresh) + 1);
    }
  }

  async fetchDownloadURLs(player: PlayerData, objectKey: string): Promise<URLResponse> {
    const { http } = this.app;
    const requestBody = {
      request : [ { objectKey } ], playerId: player.gamerTagForRealm()
    }
    const response = await http.request(`basic/cloudsaving/data/downloadURL`, requestBody, 'post');
    const downloadResponse = response.data as DownloadObjectsResponse;
    return downloadResponse.response[0];
  }
  /*public async fetchLeaderboardPage(leaderboardName: string, from: number, max: number): Promise<LeaderboardPage> {
    const { http } = this.app;
    const response = await http.request(`object/leaderboards/${leaderboardName}/details?from=${from}&max=${max}`, void 0, 'get');
    const page = response.data as LeaderboardPage;
    return page;
  }*/

}


