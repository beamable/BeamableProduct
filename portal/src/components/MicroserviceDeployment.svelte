<script>
    import AsyncInput from './AsyncInput';
    import PageList from './PageList';

    // ServiceManifest
    export let deployment = {
        comments: '',
        manifest: []
    };
    export let readonly = false;
    export let availableTemplates = [];
</script>

<style lang="scss">
    label, p {
        text-align: left;
    }
    p {
        opacity: .9;
        font-size: 12px;
    }
    .big-row {
        flex-direction: column;

        > * {
            align-self: stretch;
        }

        .data-title {
            padding-left: 0px;
        }
        .data-title h4 {
            text-align: left;
        }
        .data-fields {
            flex-direction: row;
            display:flex;
            padding:0px;
            .field:not(:first-child) {
                margin-left: 4px;
            }
            .field:not(:last-child) {
                margin-right: 4px;
            }
            
            :last-child {
                flex-grow: 1;
            }
        }
    }
</style>    

<div>
    <div>
        <slot>
            <p>
                You can select the running state, size, and comments for a new deployment. 
                This deployment will use the latest copy of the microservices. You cannot change the version of the microservices.
            </p>
        </slot>
    </div>
    <PageList elements={deployment.manifest}>
        <div slot="element" let:element let:index class="big-row">        
            <div class="data-title">
                <h4>
                    {element.serviceName}
                </h4>
            </div>
            <div class="data-fields">

                <div class="field">
                    <label class="label">Enabled</label>
                    <div class="control">
                        <div class="select">
                        <select bind:value={deployment.manifest[index].enabled} disabled={readonly} >
                            <option value={true}>
                                Enabled
                            </option>
                            <option value={false}>
                                Disabled
                            </option>
                        </select>
                        </div>
                    </div>
                </div>

                <div class="field">
                    <label class="label">Template</label>
                    <div class="control">
                        <div class="select">
                        <select bind:value={deployment.manifest[index].templateId} disabled={readonly} >
                            {#each availableTemplates as template}
                                <option value={template.id}>
                                    {template.id}
                                </option>
                            {/each}
                        </select>
                        </div>
                    </div>
                </div>

                <div class="field">
                    <label class="label">Comments</label>
                    <div class="control">
                        <input class="input" type="text" placeholder="Comments" bind:value={deployment.manifest[index].comments} disabled={readonly} >
                    </div>
                </div>
            </div>

        </div>
    </PageList>

    <form>
        <div class="field">
            <label class="label">Deployment Comments</label>
            <div class="control">
                <input class="input" type="text" placeholder="Describe this deployment" bind:value={deployment.comments} disabled={readonly} >
            </div>
        </div>
    </form>

</div>