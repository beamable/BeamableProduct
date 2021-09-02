<script>
    import SwaggerUI from 'swagger-ui'
    import { onMount } from 'svelte';
    import { getServices } from '../../../../../../../../../../services';
    import { get } from 'svelte/store';

    export let route //current route

    const { 
        realms,
        microservices,
        auth
    } = getServices();

    const cid = get(realms.cid);
    const realm = get(realms.realm);
    const token = get(auth.token).access_token;
    const prefix = route.params.prefix == 'remote' ? '' : route.params.prefix;
    const serviceName = route.params.serviceName;
    const hasPrefix = prefix != '';

    // TODO: this should really be pulled from the manifest file.
    const host = `https:${window.config.host}/basic/${cid}.${realm.name}.${prefix}micro_${serviceName}/admin/Docs`;

    let swaggerError;
    let swaggerLoading;

    onMount(async () => {
        
        try {
            await microservices.serviceHealthCheck(cid, realm.name, serviceName, prefix)
            swaggerLoading = true;
            let swagger = SwaggerUI({
                dom_id: '#swaggerRoot',
                syntaxHighlight: {
                    activate: true,
                    theme: 'arta'
                    // theme: 'monokai'
                },
                requestSnippets: {
                    defaultExpanded: false,
                    generators: {
                        "curl_bash": {
                            title: "cURL (bash)",
                            syntax: "bash"
                        },
                    }
                },
                onComplete: () => {
                    console.log('pre-populating auth with cid.pid and bearer token');
                    swagger.preauthorizeApiKey('scope', `${cid}.${realm.name}`);
                    swagger.preauthorizeApiKey('user', token);
                    swaggerLoading = false;
                },
                url: host
            });
        } catch (ex) {
            console.log('Failed to load swagger docs.', ex);

            if (ex && ex.status == 504){
                swaggerError = 'Failed to load Microservice Documentation for this service. The service is not running.'
            
            } else {
                swaggerError = 'Failed to load Microservice Documentation for this service. Documentation is only supported on microservices built with Beamable 0.11.0 and beyond.'
            }
        }
    });
    
</script>

<div>
    {#if hasPrefix}
        <div class="local">
            <h2>|| LOCAL DOCUMENTATION ||</h2>
        </div>
    {/if}

    <div id="swaggerRoot" class:init={swaggerLoading == undefined} class:loading={swaggerLoading} class:error={swaggerError}>
    </div>

    {#if swaggerError}
        <div> {swaggerError} </div>
    {/if}
    
</div>

<style lang="scss">
    #swaggerRoot {
        &.error,
        &.init {
            display: none;
        }
    }
    .local {
        background: #0196f1;
        text-align: center;
        font-weight: bold;
        position: fixed;
        width: calc(80% - 20px);
        z-index: 1;
        top: 58px;
    }
</style>
