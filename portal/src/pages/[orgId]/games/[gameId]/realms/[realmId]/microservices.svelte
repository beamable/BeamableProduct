<script>
    import Tabs from './_tabs';
    import { onDestroy } from 'svelte';
    import { getServices } from '../../../../../../services';
    import FilterSearch from '../../../../../../components/FilterSearch';
    import Card from '../../../../../../components/Card';
    import AsyncInput from '../../../../../../components/AsyncInput';
    import FeatherIcon from '../../../../../../components/FeatherIcon';
    import PageList from '../../../../../../components/PageList';
    import WarningPopup from '../../../../../../components/WarningPopup';
    import ModalCard from '../../../../../../components/ModalCard';
    import ComponentButtons from '../../../../../../components/ComponentButtons';
    import RawDataButton from '../../../../../../components/RawDataButton';
    import PaginateArray from '../../../../../../components/PaginateArray';
    import RoleGuard from '../../../../../../components/RoleGuard';
    import MicroserviceDeployment from '../../../../../../components/MicroserviceDeployment';

    const { realms: { realm }, http, microservices } = getServices();

    const statusData = {
        stopped: {
            text: 'Stopped',
            icon: 'pause-circle'
        },
        running: {
            text: 'Running',
            icon: 'cloud'
        },
        deploying: {
            text: 'Deploying',
            icon: 'upload-cloud'
        },
        tearingDown: {
            text: 'Stopping',
            icon: 'download-cloud'
        }
    }

    let status;
    microservices.status.subscribe(next => {
        status = next;
    });

    let currentManifest;
    microservices.currentManifest.subscribe(next => {
        currentManifest = next;
    });

    let deployments;
    microservices.manifests.subscribe(next => {
        deployments = next;
        isRefreshing = false;
    });

    let newDeployment;
    let availableTemplates;
    let isRefreshing;

    let microserviceEntries = [];
    let filteredMicroservieEntries = [];
    let pagedMicroserviceEntries = [];
    let allData;

    let deploymentEntries = [];
    let filteredDeploymentEntries = [];
    let pagedDeploymentEntries = [];

    let noManifests = false;

    $:microserviceEntries = (currentManifest && status) ? currentManifest.manifest.map(upgradeMicroservice): [];
    $:deploymentEntries = (deployments && currentManifest && status) ? deployments.map(upgradeDeployment) : [];

    $:allData = currentManifest && status && deployments;
    $:noManifests = currentManifest == null && status == 'empty';




    function upgradeDeployment(deployment) {
        const date = new Date(deployment.created * 1000);
        return {
            ...deployment,
            dateText: `${date.toLocaleDateString()} ${date.toLocaleTimeString()}`,
            isCurrentDeployment: currentManifest.id == deployment.id,
            searchTerm: deployment.manifest.map(s => s.imageId + ',' + s.comments).join(' ')
        }
    }

    function downgradeDeployment(deployment) {
        return {
            manifest: deployment.manifest,
            id: deployment.id,
            comments: deployment.comments,
            date: deployment.date,
            dateText: deployment.dateText
        }
    }

    function upgradeMicroservice(microservice){
        const serviceStatus = getServiceStatus(microservice);
        return {
            ...microservice,
            serviceStatus,
            statusViewData: getStatusData(microservice, serviceStatus)
        }
    }

    function getServiceStatus(microservice) {
        var matchingServices = status.services.filter(service => service.serviceName == microservice.serviceName);
        if (matchingServices && matchingServices.length == 1){
            return matchingServices[0];
        } else {
            console.warn('No service status available for service', microservice);
            return {
                isCurrent: false,
                isRunning: false,
                imageId: '',
                serviceName: microservice.serviceName
            }
        }
    }

    function getStatusData(microservice, status){

        if (status.isCurrent && microservice.enabled) {
            return statusData.running;
        }

        if (status.isCurrent && !microservice.enabled) {
            return statusData.stopped;
        }

        if (!status.isCurrent && microservice.enabled) {
            return statusData.deploying; // Could separate this out into re-deploying, or first deploy
        }

        if (!status.isCurrent && !microservice.enabled && status.isRunning) {
            return statusData.tearingDown;
        }

        return statusData.stopped;

    }

    async function startRedeploy(manifest, toggle){
        availableTemplates = await microservices.fetchTemplates();
        toggle();
    }

    async function createDeployment(toggle){
        newDeployment = await microservices.createNewManifest();
        availableTemplates = await microservices.fetchTemplates();
        toggle();
    }

    async function deploy(deployment, toggle){
        await microservices.deployManifest(deployment);
        
        if (toggle) toggle();
        refresh();
    }

    function openDocs(service){
        let logUri = `${location.origin}${location.pathname}/${service.serviceName}/docs/remote/`;
        window.open(logUri, '_blank');
    }

    function openLogs(service){
        let logUri = `${location.origin}${location.pathname}/${service.serviceName}/logs`;
        window.open(logUri, '_blank');
    }

    function openMetrics(service){
        let metricUri = `${location.origin}${location.pathname}/${service.serviceName}/metrics`;
        window.open(metricUri, '_blank');
    }
    

    function refresh(){

        isRefreshing = true;

        microservices.forceRefresh();
    }


</script>

<style lang="scss">
    $red: #FF5c5c;
    $green: #77B769;
    $blue: #4497B7;
    $yellow: #EBC65F;

    .no-data-message {
        text-align: left;
        max-width: 500px;
        margin: auto;
        h2 {
            font-weight: bold;
            font-size: 20px;
        }
        p {
            margin-bottom: 4px;
            margin-top: 8px;
        }
    }

    .microservices :global(.flags.current) {
        border-left-color: $green;
    }
    .microservices :global(.flags.not-current) {
        border-left-color: $yellow;
    }

    .microservices :global(.flags.current-deploy) {
        border-left-color: $green;
    }
    .microservices :global(.flags.old-deploy) {
        border-left-color: $blue;
    }

    .deployment-service-entry  .buttons {
        display: flex;
        flex-direction: row;
        justify-content: flex-start;
        align-self: center;
        padding-top: 0;
    }
    

    .big-row {
        flex-direction: column;

        > * {
            align-self: stretch;
        }
        .deployment-top {
            display: flex;
            flex-direction: row;
            > *:not(:first-child){
                margin-left: 5px;
            }
            > *:not(:last-child){
                margin-right: 5px;
            }
            .date {
                width: 200px;
                max-width: 200px;
            }
            .comments {
                flex-grow: 1;
            }
            .buttons {
                display: flex;
                align-items: baseline;
                margin-right: 10px;
                > *:not(:first-child){
                    margin-left: 5px;
                }
                > *:not(:last-child){
                    margin-right: 5px;
                }
                .button {
                    padding: 6px 12px;

                }
                .latest {
                    width: 100px;
                    text-align: center;
                }
            }
        }

        .deployment-service-headers > div {
            flex-grow: 1;
            font-size: 14px;
            text-align: center;
            color: rgba(255, 255, 255, .8);
            margin-bottom: 4px;
        }
        .deployment-service-headers,
        .deployment-service-entry {
            display: flex;
            flex-direction: row;

            > *:not(:first-child){
                margin-left: 5px;
            }
            > *:not(:last-child){
                margin-right: 5px;
            }


        }
    }

    .serviceName {
        width: 150px;
        max-width: 150px;
    }
    .enabled {
        max-width: 105px;
        width: 105px;
    }
    .status {
        max-width: 130px;
        width: 130px;
    }
    .templateId {
        max-width: 100px;
    }
    .imageId {
        flex-grow: 1;
    }
    .comments {
        flex-grow: 1;
    }


</style>

{#if $realm}
    <Tabs activeTab="microservices">

    </Tabs>
{/if}


<div class="microservices">

{#if !noManifests}
<Card title="Status" data={allData} isPanel={false}>
    <div style="position: relative">
        <ComponentButtons>
            <p class="control">
                <button class="button trigger-button" class:is-loading={isRefreshing} on:click|preventDefault|stopPropagation={e => refresh()}>
                    Refresh
                </button>
            </p>
        </ComponentButtons>
        <FilterSearch
            placeholder="Search by Service Name"
            allElements={microserviceEntries}
            filterOn="serviceName"
            filterFunctions={[
                {func: (m) => true, name: 'All'},
                {func: (m) => m.serviceStatus.isCurrent, name: 'Current'},
                {func: (m) => !m.serviceStatus.isCurrent, name: 'Deploying'},
            ]}
            bind:value={filteredMicroservieEntries}
            />

        {#if filteredMicroservieEntries.length == 0}
            <div class="no-message">
                No Microservices Found
            </div>
        {:else}
            <PaginateArray
                paginationKey="microservices"
                position="top"
                elements={filteredMicroservieEntries}
                bind:pagedElements={pagedMicroserviceEntries}/>

            <PageList elements={pagedMicroserviceEntries}
                leftFlagClass={e => e.serviceStatus.isCurrent == true ? 'current' : 'not-current'}
                headers={[
                    {name: 'serviceName', width: '15%'},
                    {name: 'status', width: '120px'},
                    {name: 'template', width: '100px'},
                    {name: 'imageId', width: '20%'},
                    {name: 'comments', width: '20%'},
                ]}
                >
                <div slot="element" let:element class="deployment-service-entry">
                    <div class="serviceName">
                        <AsyncInput
                            value={element.serviceName}
                            editable={false}
                        />
                    </div>
                    <div class="status">
                        <span class="icon is-medium">
                            <FeatherIcon icon="{element.statusViewData.icon}"/>
                        </span>
                        <span class="underline">
                            {element.statusViewData.text}
                        </span>
                    </div>
                    <div class="templateId">
                        <AsyncInput
                            value={element.templateId}
                            editable={false}
                        />
                    </div>

                    <div class="imageId">
                        <AsyncInput
                            value={element.imageId}
                            editable={false}
                        />
                    </div>

                    <div class="comments">
                        <AsyncInput
                            value={element.comments}
                            editable={false}
                        />
                    </div>
                     <div class="buttons ">
                        <button class="button" on:click|preventDefault|stopPropagation={evt => {openMetrics(element)}} >
                            <span class="icon is-small">
                                <FeatherIcon icon="activity" class="is-small"/>
                            </span>
                            <span>
                                Metrics
                            </span>
                        </button>
                        <button class="button" on:click|preventDefault|stopPropagation={evt => {openLogs(element)}} >
                            <span class="icon is-small">
                                <FeatherIcon icon="file-text" class="is-small"/>
                            </span>
                            <span>
                                Logs
                            </span>
                        </button>
                        <button class="button" on:click|preventDefault|stopPropagation={evt => {openDocs(element)}} >
                            <span class="icon is-small">
                                <FeatherIcon icon="book" class="is-small"/>
                            </span>
                            <span>
                                Docs
                            </span>
                        </button>
                    </div>


                </div>
            </PageList>

        {/if}

    </div>
</Card>

<Card title="Deployments" data={allData} isPanel={false}>
    <div style="position: relative">
        <ComponentButtons>
            <ModalCard class="is-xsmall has-light-bg redeploy" let:toggle>
                <div slot="trigger-evt" let:active>
                    <button class="button trigger-button" on:click|preventDefault|stopPropagation={evt => createDeployment(toggle)}>
                        <span class="icon is-small">
                            <FeatherIcon icon="plus-square"/>
                        </span>
                        <span>
                            New Deployment
                        </span>
                    </button>
                </div>
                <h3 slot="title">
                    New Deployment
                </h3>
                <span slot="body">
                    <MicroserviceDeployment deployment={newDeployment} availableTemplates={availableTemplates}/>
                </span>

                <span slot="buttons">
                    <button class="button is-success" on:click|preventDefault|stopPropagation={evt => deploy(newDeployment, toggle)} >
                        <span>
                            <slot name="primary-button">
                                Deploy Microservices
                            </slot>
                        </span>
                    </button>
                    <button class="button cancel" on:click|preventDefault={toggle}>
                        <span>Cancel</span>
                    </button>
                </span>
            </ModalCard>
        </ComponentButtons>
        <FilterSearch
            placeholder="Search by imageId or comment"
            allElements={deploymentEntries}
            filterOn="searchTerm"
            bind:value={filteredDeploymentEntries}
            />

        {#if filteredDeploymentEntries.length == 0}
            <div class="no-message">
                No Deployments Found
            </div>
        {:else}
            <PaginateArray
                paginationKey="deployments"
                position="top"
                elements={filteredDeploymentEntries}
                bind:pagedElements={pagedDeploymentEntries}/>

            <PageList elements={pagedDeploymentEntries}
                leftFlagClass={e => e.isCurrentDeployment == true ? 'current-deploy' : 'old-deploy'}>
                <div slot="element" let:element class="big-row">

                    <div class="deployment-top">
                        <div class="date">
                            {element.dateText}
                        </div>

                        <div class="comments">
                            <AsyncInput
                                value={element.comments}
                                editable={false}
                            />
                        </div>

                        <div class="buttons">

                            {#if element.isCurrentDeployment}
                                {#if status.isCurrent}
                                    <!-- latest deployment -->
                                    <div class="latest">Latest</div>

                                {:else}
                                    <!-- Deploying... -->
                                    <div class="latest is-loading">Deploying...</div>

                                {/if}
                            {:else}
                                <!-- Rollback -->
                                <ModalCard class="is-xsmall has-light-bg redeploy">
                                    <div slot="trigger-evt" let:toggle let:active>
                                        <button class="button trigger-button" on:click|preventDefault|stopPropagation={evt => startRedeploy(element, toggle)}>
                                            <span class="icon is-small">
                                                <FeatherIcon icon="skip-back"/>
                                            </span>
                                            <span>
                                                Redeploy
                                            </span>
                                        </button>
                                    </div>
                                    <h3 slot="title">
                                        Redeploy
                                    </h3>
                                    <span slot="body">
                                        <MicroserviceDeployment deployment={element} availableTemplates={availableTemplates} readonly={true}>
                                            <p style="white-space: normal;">
                                                Redploying this manifest will change the running state to match this manifest. Are you sure you want to redeploy this manifest?
                                            </p>
                                        </MicroserviceDeployment>
                                    </span>

                                    <span slot="buttons" let:toggle>
                                        <button class="button is-success" on:click|preventDefault|stopPropagation={evt => deploy(element, toggle)} >
                                            <span>
                                                <slot name="primary-button">
                                                    Redeploy Microservices
                                                </slot>
                                            </span>
                                        </button>
                                        <button class="button cancel" on:click|preventDefault={toggle}>
                                            <span>Cancel</span>
                                        </button>
                                    </span>
                                </ModalCard>
                            {/if}

                            <!-- Raw Data -->
                            <RawDataButton
                                resolveFunction={() => downgradeDeployment(element)}
                                title={element.id}
                                downloadName={element.id}
                                />
                        </div>
                    </div>

                    <div class="deployment-content">
                        <div class="deployment-service-headers">
                            <div class="serviceName">
                                Service Name
                            </div>
                            <div class="enabled">
                                Status
                            </div>
                            <div class="templateId">
                                Template
                            </div>
                            <div class="imageId">
                                Image Id
                            </div>
                            <div class="comments">
                                Comments
                            </div>
                        </div>
                        {#each element.manifest as service}
                            <div class="deployment-service-entry">
                                <div class="serviceName">
                                    <AsyncInput
                                        value={service.serviceName}
                                        editable={false}
                                    />
                                </div>

                                <div class="enabled">
                                    <span class="icon is-medium">
                                        <FeatherIcon icon="{service.enabled ? 'cloud' : 'cloud-off'}"/>
                                    </span>
                                    <span class="underline">
                                        {service.enabled ? 'Enabled': 'Disabled'}
                                    </span>
                                </div>

                                <div class="templateId">
                                    <AsyncInput
                                        value={service.templateId}
                                        editable={false}
                                    />
                                </div>

                                <div class="imageId">
                                    <AsyncInput
                                        value={service.imageId}
                                        editable={false}
                                    />
                                </div>

                                <div class="comments">
                                    <AsyncInput
                                        value={service.comments}
                                        editable={false}
                                        placeholder=""
                                    />
                                </div>
                            </div>

                        {/each}
                    </div>

                </div>
            </PageList>
        {/if}
    </div>
</Card>
{:else}
    <div class="no-data-message">
        <h2>
            You have no deployed Microservices yet.
        </h2>
        <p>
            You'll need to create and deploy a Microservice using the Beamable Unity plugin before any information appears on this page. Once you deploy a microservice from Unity, you'll be able to manage it from here.
        </p>
        <p>
            Check the <a href="https://docs.beamable.com/docs/microservices"> Docs </a> for information on Beamable Microervices.
        </p>
    </div>
{/if}

</div>
