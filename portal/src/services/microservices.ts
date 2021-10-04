import {BaseService} from './base';
import { Readable, Writable, get } from '../lib/stores';

import { writable } from 'svelte/store';
import { roleGuard, networkFallback } from '../lib/decorators';

import * as AxiosTypes from 'axios';
import axios from 'axios';
import {HttpHeaders, HttpRequestConfig} from './http'

export class Microservices extends BaseService {

    private readonly forceUpdate : Writable = writable(0);

    public readonly status : Readable<MicroserviceStatus> = this.derived(
        [this.app.realms.cid, this.forceUpdate, this.app.realms.realm],
        (args: Array<any>, set: any) => {
            const realm = args[2];
            const customer = args[0]
            if (customer && realm) {
                this.fetchStatus().then(set)
            }
        }
    );

    public readonly templates: Readable<Array<MicroserviceTemplate>> = this.derived(
        [this.app.realms.cid],
        (args: Array<any>, set: any) => {
            const realm = args[0]
            if (realm){
                this.fetchTemplates().then(set);
            }
        }
    )

    public readonly manifests : Readable<Array<ServiceManifest>> = this.derived(
        [this.app.realms.cid, this.app.realms.realm, this.forceUpdate],
        (args: Array<any>, set: any) => {
            const customer = args[0];
            const realm = args[1]
            if (customer && realm) {
                this.fetchAllManifests().then(set)
            }
        }
    );

    public readonly currentManifest : Readable<ServiceManifest> = this.derived(
        [this.app.realms.cid, this.app.realms.realm,this.forceUpdate],
        (args: Array<any>, set: any) => {
            const customer = args[0];
            const realm = args[1]
            if (customer && realm) {
                this.fetchLatestManifest().then(set)
            }
        }
    );

    public createMetricStream(serviceName: string, metricName: string, period: Readable<number>, startTime: Readable<number>, endTime: Readable<number>): Readable<MetricResults> {
        var debouncedAction:any = undefined;
        
        return this.derived(
            [this.app.realms.cid, this.forceUpdate, period, startTime, endTime],
            (args: [any, any, number,number, number], set: any) => {
                clearTimeout(debouncedAction);
                debouncedAction = setTimeout(() => {
                    const cid = args[0]
                    const periodValue = args[2];
                    const startTimeValue = args[3];
                    const endTimeValue = args[4];

                    if (!cid) return;
                    set({
                        data: [],
                        label: 'Loading',
                        loading: true
                    });
                    this.fetchMetricProcess(serviceName, metricName, periodValue, startTimeValue, endTimeValue)
                        .then(result => {
                            set({
                                data: result.data,
                                label: result.label
                            });
                        }).catch(err => {
                            set({
                                label: 'Error',
                                data: [],
                                error: 'Invalid Metrics: ' + err.message
                            })
                        })
                }, 250);
            }
        )
    }

    public createLogStream(serviceName: string, queryStore: Writable<string>, next: Writable<any>, startTime: Writable<number>, endTime:Writable<number>): Readable<LogResults> {
        let nextToken:string|undefined = undefined;
        let lastFilter:string|undefined;
        let lastStartTime = -1;
        let lastEndTime = -1;
        let lastData: Array<LogMessage> = [];

        return this.derived(
            [this.app.realms.cid, this.forceUpdate, queryStore, next, startTime, endTime],
            (args: Array<any>, set: any) => {
                const realm = args[0];
                const rowLoad = args[3];
                const filter = args[2];
                const startTimeValue = args[4];
                const endTimeValue = args[5];
                if (realm && rowLoad >= 0 && filter){
                    let currFilter = filter;
                    let firstPage = false;
                    let filterDifferent = currFilter != lastFilter;
                    let startTimeDifferent = startTimeValue != lastStartTime;
                    let endTimeDifferent = endTimeValue != lastEndTime;
                    if (filterDifferent || startTimeDifferent || endTimeDifferent){
                        console.log('something changed', filterDifferent)
                        firstPage = true;
                        nextToken = undefined;
                    }
                    lastFilter = currFilter;
                    lastStartTime = startTimeValue;
                    lastEndTime = endTimeValue;

                    this.fetchLogProcess(serviceName, currFilter, nextToken, startTimeValue, endTimeValue)
                        .then(result => {
                            if (firstPage){
                                lastData = [];
                                nextToken = undefined;
                                console.log('clearing data')
                            }

                            nextToken = result.nextToken;
                            lastData = [...lastData, ...result.logs]
                           
                            set({
                                logs: lastData,
                                nextToken: result.nextToken,
                                firstPage
                            });
                        }).catch(err => {
                            console.error('log error', err);
                            nextToken = undefined;
                            set({
                                logs: [],
                                nextToken: undefined,
                                error: 'Invalid Search: ' + err.message
                            })
                        })
                }
            }
        )
    }

    public async createNewManifest(): Promise<ServiceManifest> {
        // fork from the latest manifest.
        const latest = await this.fetchLatestManifest();
        return {
            ...latest,
            // erase fields that don't make sense for a new manifest...
            comments: '',
            id: '',
            created: 0,
            createdByAccountId: 0
        };
    }

    async forceRefresh(): Promise<void> {
        setTimeout(() => {
            this.forceUpdate.update(n => n + 1);
        }, 2000) // force a little time here, to prevent mad refresh clicking.
    }

    @roleGuard(['admin', 'developer'])
    @networkFallback()
    async deployManifest(manifest: ServiceManifest) : Promise<void> {
        const { http } = this.app;

        const data = {
            manifest: manifest.manifest,
            comments: manifest.comments,
            autoDeploy: true,
        }
        const _ = await http.request(`basic/beamo/manifest`, data, 'post');
    }

    @roleGuard(['admin', 'developer'])
    @networkFallback()
    public async fetchTemplates(): Promise<Array<MicroserviceTemplate>> {
        const { http } = this.app;
        const response = await http.request(`basic/beamo/templates`, void 0, 'get');
        const data = response.data.templates as Array<MicroserviceTemplate>;
        return data;
    }

    async fetchLogProcess(serviceName: string, filter: string, token: string|undefined, startTime:number, endTime:number): Promise<LogResults> {
        const presignedData = await this.fetchLogUrl(serviceName, filter, token, startTime, endTime)
        const results = await this.fetchLogData(presignedData)

        return results;
    }

    async fetchMetricProcess(serviceName: string, metricName: string, period: number, startTime: number, endTime: number): Promise<MetricResults> {
        const presignedData = await this.fetchMetricUrl(serviceName, metricName, period, startTime, endTime);
        const results = await this.fetchMetricData(presignedData);
        return results;
    }

    async fetchMetricUrl(serviceName: string, metricName: string, period:number, startTime: number, endTime: number): Promise<LogUrlResponse> {
        const { http } = this.app;
        let req:any ={
            serviceName,
            period,
            metricName
        }
        if (startTime >= 0){
            req.startTime = startTime;
        }
        if (endTime >= 0){
            req.endTime = endTime;
        }

        const response = await http.request(`basic/beamo/metricsUrl`, req, 'post');
        const data = response.data as LogUrlResponse;

        return data;
    }

    async fetchLogUrl(serviceName: string, filter: string, token: string|undefined,startTime:number, endTime:number): Promise<LogUrlResponse> {

        const { http } = this.app;
        let req:any ={
            serviceName,
            filter,
        }

        if (token){
            req.nextToken = token;
        }
        if (startTime >= 0){
            req.startTime =startTime;
        }
        if (endTime >= 0){
            req.endTime =endTime;
        }

        console.log('sending beamo request', req);
        const response = await http.request(`basic/beamo/logsUrl`, req, 'post');
        const data = response.data as LogUrlResponse;

        return data;
    }


    createPresignedAxiosConfig(presignedData: LogUrlResponse): HttpRequestConfig {
        let headers: { [name: string]: string } = presignedData.headers.reduce( (agg, curr) => ({
            ...agg,
            [curr.key]: curr.value
        }), {})

        // filter out headers that the browser will override
        delete headers["host"]
        delete headers["connection"]
        delete headers["content-length"]

        let config: HttpRequestConfig = {
            method: 'POST',
            baseURL: presignedData.url,
            url: presignedData.url,
            data: presignedData.body,
            headers: headers
        };
        return config;
    }

    async fetchMetricData(presignedData: LogUrlResponse): Promise<MetricResults> {
        const config = this.createPresignedAxiosConfig(presignedData);
        try {
            const response = await axios(config);
            const data = response.data as CloudwatchMetricStatResponse
            return {
                label: data.Label,
                data: data.Datapoints.map(p => ({
                    unit: p.Unit,
                    timestamp: p.Timestamp,
                    min: p.Minimum,
                    max: p.Maximum,
                    average: p.Average
                }))
            }

        } catch (ex){
            console.error(ex)
            throw ex;
        }
    }

    async fetchLogData(presignedData: LogUrlResponse) : Promise<LogResults> {
        const config = this.createPresignedAxiosConfig(presignedData);
        try {
            const response = await axios(config);
            const data = response.data as CloudwatchResponse
            return {
                logs: data.events,
                nextToken: data.nextToken,
                error: undefined
            }

        } catch (ex){
            console.error(ex)
            throw ex;
        }

    }

    @roleGuard(['admin', 'developer'])
    @networkFallback('empty', status => status == 404)
    async fetchStatus(): Promise<MicroserviceStatus> {

        const { http } = this.app;

        const response = await http.request(`basic/beamo/status`, void 0, 'get');
        const data = response.data as MicroserviceStatus;
        return data;
    }

    @roleGuard(['admin', 'developer'])
    @networkFallback(null, status => status == 404)
    async fetchLatestManifest(): Promise<ServiceManifest> {
        const { http } = this.app;

        const response = await http.request(`basic/beamo/manifest/current`, void 0, 'get');
        const data = response.data.manifest as ServiceManifest;
        return data;
    }

    @roleGuard(['admin', 'developer'])
    @networkFallback([], status => status == 404)
    async fetchAllManifests(): Promise<Array<ServiceManifest>> {
        const { http } = this.app;

        const response = await http.request(`basic/beamo/manifests`, void 0, 'get');
        const data = response.data.manifests as Array<ServiceManifest>;

        return data;
    }

    public async serviceHealthCheck(cid: string, pid: string, serviceName: string, prefix: string): Promise<void> {
        const { http } = this.app;

        const uri = `basic/${cid}.${pid}.${prefix}micro_${serviceName}/admin/HealthCheck`;
        await http.request(uri, void 0, 'post');
    }

}

export interface MicroserviceTemplate {
    readonly id: string;
}

export interface MicroserviceStatus {
    readonly isCurrent: boolean;
    readonly services: Array<Status>
}

export interface Status {
    readonly serviceName: string;
    readonly imageId: string;
    readonly isRunning: boolean;
    readonly isCurrent: boolean;
}

export interface ServiceReference {
    readonly serviceName: string;
    readonly imageId: string;
    readonly templateId: string;
    readonly comments: string;
    readonly enabled: boolean;
}

export interface ServiceManifest {
    readonly manifest: Array<ServiceReference>;
    readonly id: string;
    readonly created: number;
    readonly createdByAccountId: number;
    readonly comments: string;
}

export interface MetricResults {
    readonly label: string;
    readonly data: Array<MetricDatapoint>;
}
export interface MetricDatapoint {
    readonly unit: string;
    readonly timestamp: number;
    readonly average: number;
    readonly max: number;
    readonly min: number;
}

export interface LogResults {
    readonly error: string|undefined;
    readonly logs: Array<LogMessage>;
    readonly nextToken?: string;
}
export interface LogMessage {
    readonly message: string;
    readonly timestamp: number;
    readonly eventId: string;
}
export interface CloudwatchMetricStatResponse {
    readonly Label: string;
    readonly Datapoints: Array<CloudwatchMetricDatapointResponse>;
}
export interface CloudwatchMetricDatapointResponse {
    readonly Unit: string;
    readonly Timestamp: number;
    readonly Average: number;
    readonly Minimum: number;
    readonly Maximum: number;
}
export interface CloudwatchResponse {
    readonly events: Array<LogMessage>;
    readonly nextToken?: string;
}
export interface LogUrlResponse {
    readonly url: string;
    readonly body: string;
    readonly method: string;
    readonly headers: Array<LogUrlHeader>;
}
export interface LogUrlHeader {
    readonly key: string;
    readonly value: string;
}