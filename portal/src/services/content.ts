import {BaseService} from './base';
import { Readable, Writable } from '../lib/stores';

import { get, writable } from 'svelte/store';
import { roleGuard } from '../lib/decorators';

interface ContentManifest {
    readonly id: string;
    readonly created: number;
    readonly references: Array<ContentEntry>;
}

interface ContentEntry {
    readonly id: string;
    readonly tags: Array<string>;
    readonly version: string;
    readonly uri: string;
    readonly checksum: string;
    readonly visibility: string;
    readonly type: string;
}

interface ManifestChecksum{
    readonly id: string;
    readonly checksum: string;
    readonly createdAt: number;
}

export class ContentService extends BaseService {

    private readonly manifestRefresh : Writable = writable(0);
    private _lastRealm: string = "";
    private _lastManualRefresh: number = 0;

    /* 
        The manifest store is configured to auto-reload anytime the realm changes, since content is persistent to each realm. 
        Before any manifest is loaded, start the store with an empty manifest to prevent null reference bugs.
    */
    
    
    public readonly manifestId : Writable<string> = this.writable('manifest.id', 'global');
    public readonly manifest : Readable<ContentManifest> = this.derived(['realm.id', this.manifestId, this.manifestRefresh], (args: any, set: any) => {
        const realmId = args[0];
        const manualRefresh = args[1];
        if (realmId !== this._lastRealm || manualRefresh != this._lastManualRefresh) {
            this._lastRealm = realmId;
            this._lastManualRefresh = manualRefresh;
            this.refreshManifest().then(set);
        }
    }, {
        id: null,
        created: 0,
        references: []
    });

    public readonly currencies : Readable<Array<ContentEntry>> = this.CreateContentStore('currency');
    public readonly items : Readable<Array<ContentEntry>> = this.CreateContentStore('items');
    public readonly announcements : Readable<Array<ContentEntry>> = this.CreateContentStore('accouncements');
    public readonly emails : Readable<Array<ContentEntry>> = this.CreateContentStore('emails');
    public readonly events : Readable<Array<ContentEntry>> = this.CreateContentStore('events');
    public readonly leaderboards : Readable<Array<ContentEntry>> = this.CreateContentStore('leaderboards');
    public readonly listings : Readable<Array<ContentEntry>> = this.CreateContentStore('listings');
    public readonly skus : Readable<Array<ContentEntry>> = this.CreateContentStore('skus');
    public readonly stores : Readable<Array<ContentEntry>> = this.CreateContentStore('stores');
    
    public forceManifestReload() : void {
        this.manifestRefresh.update(n => n + 1);
    }

    public async fetchContent(content: ContentEntry): Promise<string> {
        const { http } = this.app;
        const response = await http.requestWorld(content.uri, 'get');
        return response.data;
    }

    public async fetchManifestList() : Promise<ManifestChecksum[]>{
        const { http } = this.app;
        const response = await http.request(`/basic/content/manifest/checksums`, void 0, 'get');
        let result: ManifestChecksum[] = response.data.manifests;
        let global = result.find(c => c.id == "global")
        if(global == undefined){
            global = {
                id: "global",
                checksum: "",
                createdAt: 0
            };
        }
        result = result.filter(c => c.id != "global").sort((a, b) => a.id > b.id && 1 || -1);
        result.unshift(global);
        return result;
    }

    CreateContentStore<T>(contentPrefix: string) : Readable<T> {
        // derive a readonly store from the manifest, and filter for public content matching the given prefix
        return this.derived(this.manifest, (m: ContentManifest) => {
            return m.references.filter(entry => entry.visibility == 'public' && entry.id.startsWith(contentPrefix))
        });
    }

    @roleGuard(['admin', 'developer'])
    async refreshManifest() : Promise<ContentManifest> { 
        const { http } = this.app;
        const response = await http.request(`/basic/content/manifest?id=` + get(this.manifestId), void 0, 'get');
        const manifest = response.data as ContentManifest;
        return manifest;
    }
}