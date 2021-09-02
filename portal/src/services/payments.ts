import {BaseService} from './base';
import { PlayerData } from './players';
import { networkFallback } from '../lib/decorators';

export interface PaymentAudit {
    readonly created: number;
    readonly details: Array<PaymentAuditDetail>;
    readonly entitlements: Array<any>;
    readonly gt: number;
    readonly history: Array<PaymentAuditHistory>;
    readonly obtainCurrency: Array<PaymentAuditObtainCurrency>;
    readonly obtainItems: Array<PaymentAuditObtainItem>;
    readonly providerid: string;
    readonly providername: string;
    readonly txid: number;
    readonly txstate: string;
    readonly updated: number;
    readonly version: string;
}

export interface PaymentAuditDetail {
    readonly gameplace: string;
    readonly localCurrency: string;
    readonly localPrice: string;
    readonly name: string;
    readonly providerProductId: string;
    readonly price: number;
    readonly quantity: number;
    readonly reference: string;
    readonly sku: string;
}

export interface PaymentAuditHistory {
    readonly change: string;
    readonly data: string;
    readonly timestamp: string;
    readonly MAX_FIELD_SIZE: number;
}

export interface PaymentAuditObtainCurrency {
    readonly symbol: string;
    readonly amount: number;
}

export interface PaymentAuditObtainItem {

}

export interface PaymentAuditResponse {
    readonly audits: Array<PaymentAudit>
}


export class PaymentsService extends BaseService {

    @networkFallback()
    async fetchPlayerPaymentPage(player: PlayerData, start: number, limit: number) : Promise<Array<PaymentAudit>> {
        const { http } = this.app;
        const gamerTag = player.gamerTagForRealm();
        const response = await http.request(`/basic/payments/audits?limit=${limit}&start=${start}&player=${gamerTag}`, void 0, 'get');
        const paymentAuditResponse = response.data as PaymentAuditResponse;
        return paymentAuditResponse.audits;
    }

}