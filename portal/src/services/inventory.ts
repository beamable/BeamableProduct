import {BaseService} from './base';
import { Readable, Writable, get } from '../lib/stores';

import { writable } from 'svelte/store';
import { PlayerData } from './players';
import { networkFallback } from '../lib/decorators';


export interface PlayerInventory {
    readonly items: Array<InventoryGroup>;
    readonly currencies: Array<InventoryCurrency>;
}

export interface InventoryCurrency {
    readonly id: string;
    readonly amount: number;
}

export interface InventoryGroup {
    readonly id: string;
    readonly items: Array<InventoryItem>;
}

export interface InventoryItem {
    readonly id: number;
    readonly properties: Array<InventoryProperty>;
}

export interface InventoryProperty {
    readonly name: string;
    readonly value: any;
}

export class InventoryService extends BaseService {

    private readonly forceInventoryUpdate : Writable = writable(0);

    public readonly playerInventory : Readable<PlayerInventory> = this.derived(
        [this.app.players.playerData, this.forceInventoryUpdate],
        (args: Array<any>, set: any) => {
            const player = args[0];
            if (player) {
                this.fetchPlayerInventory(player).then(set)
            }
        }
    );

    public async addCurrency(currency: InventoryCurrency): Promise<void> {
        const player = get(this.app.players.playerData);
        await this.putCurrency(player, currency);
        this.refreshInventory();
    }

    public async addItem(id: string, properties: Array<InventoryProperty>): Promise<void> {
        const player = get(this.app.players.playerData);
        await this.putItem(player, id, properties);
        this.refreshInventory();
    }

    public async updateItem(itemGroup: string, id: number, properties: Array<InventoryProperty>): Promise<void> {
        const player = get(this.app.players.playerData);
        await this.putItemUpdate(player, itemGroup, id, properties);
        this.refreshInventory(); 
    }

    public async removeItem(contentId: string, itemId: number): Promise<void> {
        const player = get(this.app.players.playerData);
        await this.deleteItem(player, contentId, itemId);
        this.refreshInventory();
    }

    refreshInventory(): void {
        // svelte doesn't allow manual store refreshes, so you need to force it via write
        this.forceInventoryUpdate.update(n => n + 1);
    }

    @networkFallback()
    async fetchPlayerInventory(player: PlayerData) : Promise<PlayerInventory> {
        const { http } = this.app;
        const response = await http.request(`/object/inventory/${player.gamerTagForRealm()}`, void 0, 'get');
        const inventory = response.data as PlayerInventory;

        return inventory;
    }

    async putCurrency(player: PlayerData, currency: InventoryCurrency) : Promise<void> {
        const { http } = this.app;
        const payload = {
            currencies: {
                [currency.id]: currency.amount
            }
        };
        await http.request(`/object/inventory/${player.gamerTagForRealm()}`, payload, 'put');
    }

    async putItem(player: PlayerData, id: string, properties: Array<InventoryProperty>) : Promise<void> {
        const { http } = this.app;
        const payload = {
            newItems: [{
                contentId: id,
                properties,
            }]
        };
        await http.request(`/object/inventory/${player.gamerTagForRealm()}`, payload, 'put');
    }

    async putItemUpdate(player: PlayerData, itemGroup: string, itemId: number, properties: Array<InventoryProperty>):Promise<void> {
        const { http } = this.app;
        const payload = {
            updateItems: [{
                contentId: itemGroup,
                id: itemId,
                properties,
            }]
        };
        await http.request(`/object/inventory/${player.gamerTagForRealm()}`, payload, 'put');
    }

    async deleteItem(player: PlayerData, contentId: string, itemId: number): Promise<void> {
        const { http } = this.app;
        const payload = {
            deleteItems: [{
                contentId,
                id: itemId,
            }]
        };
        await http.request(`/object/inventory/${player.gamerTagForRealm()}`, payload, 'put');
    }
}