<script>

    import FeatherIcon from './FeatherIcon';
    import Card from './Card';
    import ComponentButtons from './ComponentButtons';
    import ModalCard from './ModalCard';
    import Link from './Link';
    import AsyncInput from './AsyncInput';
    import RawDataButton from './RawDataButton';
    import Dropdown from './Dropdown';
    import { onMount, onDestroy, afterUpdate } from 'svelte';
    import { writable } from 'svelte/store';
    import { getServices } from '../services';

    const {
        realms
    } = getServices();

    export let gameId;
    export let orgId;

    export let realmMap=undefined;
    export let realm=undefined;

    let allData;
    $:allData = realm && orgId;

    onDestroy( () => {
        realmMap = undefined;
        realm = undefined;
        allRealms = [];
    })

    let allRealms = [];
    $:allRealms = realmMap ? Object.values(realmMap) : [];

    let promotables = [];
    let contentPromotable;
    let microservicePromotable;
    let isContentPromotable;
    let isMicroservicePromotable;
    let contentPromotionText = 'Unknown...'
    let microservicePromotionText = 'Unknown...'
    $: if (realm && realm.parent) {
        refreshRealmPromotionStatus()
    }
    $: contentPromotable = promotables.find(p => p.name == 'content');
    $: microservicePromotable = promotables.find(p => p.name == 'microservices') || {name: 'microservices'};

    $: isContentPromotable = contentPromotable && contentPromotable.isPromotionAvailable;
    $: isMicroservicePromotable = microservicePromotable && microservicePromotable.isPromotionAvailable;

    $: contentPromotionText = (realm && realm.parent) 
        ? (isContentPromotable ? 'Pending Changes' : `In sync with ${realmMap[realm.parent].projectName}`)
        : 'Production';
    $: microservicePromotionText = (realm && realm.parent) 
        ? (isMicroservicePromotable ? 'Pending Changes' : `In sync with ${realmMap[realm.parent].projectName}`)
        : 'Production';

    let isNetworking = false;
    let createChildModalActive = false;
    let reparentChildModalActive = false;
    let archiveRealmModalActive = false;
    let promoteRealmModalActive = false;
    let realmTitle = 'Realm';

    let childRealmName = '';
    let childRealmParent;

    let realmToReparent;
    let realmNewParent;
    let realmToArchive;
    let realmToArchiveConfirmation;
    let realmToPromoteInto;
    let realmToPromoteFrom;
    let availableRealmParents = [];
    let possibleRealmsToReparent = [];
    let possibleRealmsToArchive = [];
    let unarchivedRealms = [];

    $:realmTitle = realm ? realm.projectName : 'Realm';

    $: availableRealmParents = allRealms.filter(r => isRealmPossibleParent(realmToReparent, r));
    $: possibleRealmsToReparent = allRealms.filter(r => allRealms.filter(rr => isRealmPossibleParent(r, rr)).length > 0 )
    $: possibleRealmsToArchive = allRealms.filter(r => isRealmArchivable(r))
    $: unarchivedRealms = allRealms.filter(r => !r.archived)

    let availablePromotables = [];
    let desiredPromotables = [];
    let desiredPromotableNames = '';
    let isRealmPromotionValid = false;
    $: if (realmToPromoteFrom && realmToPromoteInto) {
        getRealmPromotables();
    }
    $: desiredPromotables = availablePromotables.filter(p => p.shouldPromote)
    $: desiredPromotableNames = desiredPromotables.map(p => p.name).join(', ')

    $: isRealmPromotionValid = realmToPromoteFrom 
        && realmToPromoteInto 
        && !realmToPromoteInto.archived 
        && !realmToPromoteFrom.archived 
        && realmToPromoteFrom.pid != realmToPromoteInto.pid 
        && desiredPromotables.length > 0;

    function openReparentChild(){
        reparentChildModalActive = true;
        realmToReparent = realm;
        realmNewParent = undefined;
    }
    function openCreateChild(){
        childRealmParent = realm;
        childRealmName=undefined;
        createChildModalActive = true;
    }
    function openArchiveRealm(){
        archiveRealmModalActive = true;
        realmToArchive = realm;
        realmToArchiveConfirmation = undefined;
    }
    function openPromoteRealm(){
        promoteRealmModalActive = true;

        realmToPromoteInto = realmMap[realm.parent];
        realmToPromoteFrom = realm;

    }
    function isRealmNameValid(name){
        return name && name.length && allRealms.filter(r => r.projectName == name).length == 0;
    }

    function refreshRealmPromotionStatus(){
        realms.getRealmPromotables(realm.parent, realm.pid)
            .then(data => {
                promotables = data.scopes;
            })
    }

    async function getRealmPromotables(){
        isNetworking = true;
        try {
            const promotables = await realms.getRealmPromotables(realmToPromoteFrom.pid, realmToPromoteInto.pid)
            availablePromotables = promotables.scopes.map(p => ({
                ...p,
                shouldPromote: false
            }));
        } finally {
            isNetworking = false;
        }
    }
    
    function generateConfigDefaults(realm) {
        let host = window.config.host;

        return {
            cid: orgId,
            pid: realm.pid,
            platform: location.protocol + host
        }
    }

    function isRealmArchivable(realm){
        if (!realm) return false;
        if (realm.archived) return false;
        if (realm.children){
            // TODO: Refactor to use .filter(<pred>).length > 0
            for (var i = 0 ; i < realm.children.length ; i ++){
                if (!realmMap[realm.children[i]].archived){
                
                    return false;
                } 
            }
        }
        return true;
    }

    function isRealmPossibleParent(realm, possibleParent){
        if (!realm){
            return false;
        }
        if (!possibleParent){
            return false;
        }
        if (realm.archived || possibleParent.archived){
            return false;
        }

        // cannot reparent to anything that is a child.
        var children = [];
        var toExplore = [];
        toExplore.push(realm);
        while (toExplore.length > 0) {
            var curr = toExplore.pop();
            if (curr.children){
                curr.children.forEach(c => {
                    children.push(c);
                    toExplore.push(realmMap[c])
                });
            }
        }
        var isChild = children.indexOf(possibleParent.pid) > -1;
        var isSelf = realm.pid == possibleParent.pid;
        var isCurrentParent = realm.parent == possibleParent.pid;

        return !isChild && !isSelf && !isCurrentParent;
    }

    async function createChildRealm(toggle){
        if (!childRealmName){
            console.error('cannot create child realm with empty realm name')
            return;
        }
        if (!childRealmParent || !childRealmParent.pid){
            console.error('cannot create child realm without parent with pid')
            return;
        }

        try {
            isNetworking = true;
            await realms.createRealm(orgId, childRealmName, childRealmParent.pid)
        } finally {
            isNetworking = false;
            childRealmName=undefined;
            toggle();
        }

    }

    async function promoteRealm(toggle){
        try {
            isNetworking = true;
            var promotionNames = desiredPromotables.map(p => p.name);
            const resultPromotables = await realms.performRealmPromotion(realmToPromoteFrom.pid, realmToPromoteInto.pid, promotionNames)
            await refreshRealmPromotionStatus();
        } finally {
            isNetworking = false;

            promoteRealmModalActive = false;
            toggle();
        }
    }

    async function reparentChildRealm(toggle){
        createChildModalActive = false;

        realmToReparent.parent = realmNewParent.pid;
        try {
            isNetworking = true;
            await realms.setProjectHierarchy(gameId, allRealms);

        } finally {
            isNetworking = false;
            toggle();
        }

    }

    async function archiveRealm(toggle){
        if (!realmToArchive || realmToArchive.projectName != realmToArchiveConfirmation || !realmToArchive.pid) {
            console.error('no realm to archive, or confirmation doest match', realmToArchive, realmToArchiveConfirmation)
            return;
        }
        try {
            isNetworking = true;
            await realms.archiveProject(orgId, realmToArchive.pid)
        } finally {
            isNetworking = false;
            toggle();
        }
    }

    let foobar = true;

</script>



<Card title={realmTitle} data={allData} loadingHeight={252}>

    {#if realm}
        <ModalCard class="is-xsmall has-light-bg replace-data" active={createChildModalActive} onClose={() => createChildModalActive = false}>
            <div slot="trigger-evt" let:toggle={modalToggle} let:active >
                <!-- Left intentionally blank to "hide" the popup. -->
            </div>
            <h3 slot="title">
                Create Child Realm
            </h3>
            <span slot="body" class="create-realm-body">
                <span >
                    <!-- Warning Message -->
                    Creating a new realm will create a new environment for your game. 
                </span>

                <!-- Pick what realm to fork from (disabled field) -->
                <div class="field" style="margin-top: 24px;">
                    <label class="label">Parent Realm</label>
                    <div class="control">
                        <div class="select">
                            <select bind:value={childRealmParent}>
                                {#each allRealms as otherRealm}
                                    <option value={otherRealm}> {otherRealm.projectName} </option>
                                {/each}
                            </select>
                        </div>
                    </div>
                </div>

                <!-- Pick a realm name (required) -->
                <div class="field">
                    <label class="label">Child Realm Name</label>
                    <div class="control">
                        <input class="input" type="text" placeholder="enter realm name" bind:value={childRealmName}>
                    </div>
                </div>
                
            </span>

            <span slot="buttons" let:toggle>
                
                <button class="button cancel" on:click|preventDefault={toggle}>
                    <span>Cancel</span>
                </button>
                <button class="button is-primary" class:is-loading={isNetworking} disabled={!isRealmNameValid(childRealmName)} on:click|preventDefault={evt => createChildRealm(toggle)}>
                    <span>Create Child Realm</span>
                </button>
            </span>
        </ModalCard>

        <ModalCard class="is-xsmall has-light-bg replace-data" active={reparentChildModalActive} onClose={() => reparentChildModalActive = false}>
            <div slot="trigger-evt" let:toggle={modalToggle} let:active>
                <!-- Left intentionally blank to "hide" the popup. -->
            </div>
            <h3 slot="title">
                Reparent Realm
            </h3>
            <span slot="body" class="create-realm-body">
                <span >
                    <!-- Warning Message -->
                    Changing the parent of a realm will change how data promotes to production. <br> Please note that you cannot change a realm's parent such that any realm would be orphaned, or such that the new realm tree would contain a cycle. If you don't see a realm in the dropdowns below, it is because it would result in one of these error conditions.
                </span>

                <!-- Pick what realm to reparent-->
                <div class="field" style="margin-top: 24px;">
                    <label class="label">Realm to reparent</label>
                    <div class="control">
                        <div class="select">
                            <select bind:value={realmToReparent}>
                                {#each possibleRealmsToReparent as otherRealm}
                                    {#if otherRealm && otherRealm.parent}
                                        <option value={otherRealm}> {otherRealm.projectName} </option>
                                    {:else}
                                        <option value={otherRealm} disabled> {otherRealm.projectName} (game root)</option>
                                    {/if}
                                {/each}
                            </select>
                        </div>
                        <span>
                            {#if realmToReparent}
                                Current parent is {realmMap[realmToReparent.parent].projectName}
                            {:else}
                                No realm selected
                            {/if}
                        </span>
                    </div>
                </div>

                <!-- Pick what realm to reparent-->
                <div class="field" style="margin-top: 24px;">
                    <label class="label">New Parent</label>
                    <div class="control">
                        <div class="select">
                            <select bind:value={realmNewParent}>
                                {#if !realmNewParent}
                                    <option value={undefined} disabled> (select a new parent) </option>
                                {/if}
                                {#each availableRealmParents as otherRealm2}
                                    {#if otherRealm2 == realmToReparent}
                                        <option value={otherRealm2} disabled> {otherRealm2.projectName} (cannot reparent to self)</option>
                                    {:else if realmToReparent && otherRealm2 == realmMap[realmToReparent.parent]}
                                        <option value={otherRealm2} disabled> {otherRealm2.projectName} (current parent)</option>
                                    {:else}
                                        <option value={otherRealm2}> {otherRealm2.projectName} </option>
                                    {/if}
                                {/each}
                            </select>
                        </div>
                    </div>
                </div>

                {#if realmToReparent && realmNewParent}
                    <span >
                        <!-- Clarity -->
                        You will reparent <b>{realmToReparent.projectName}</b> from <b>{realmMap[realmToReparent.parent].projectName}</b> to <b>{realmNewParent.projectName}</b>. Are you sure?
                    </span>
                {/if}


            </span>

            <span slot="buttons" let:toggle>
                
                <button class="button cancel" on:click|preventDefault={toggle}>
                    <span>Cancel</span>
                </button>
                <button class="button is-primary" class:is-loading={isNetworking} disabled={!realmNewParent || !realmToReparent} on:click|preventDefault={evt => reparentChildRealm(toggle)}>
                    <span>Reparent Realm</span>
                </button>
            </span>
        </ModalCard>

        <ModalCard class="is-xsmall has-light-bg replace-data" active={archiveRealmModalActive} onClose={() => archiveRealmModalActive = false}>
            <div slot="trigger-evt" let:toggle={modalToggle} let:active >
                <!-- Left intentionally blank to "hide" the popup. -->
            </div>
            <h3 slot="title">
                Archive Realm
            </h3>
            <span slot="body" class="create-realm-body">
                <span >
                    <!-- Warning Message -->
                    <b> DANGER </b> Archiving a realm will permanently remove this realm from your realm tree. 
                </span>

                <!-- Pick what realm to fork from (disabled field) -->
                <div class="field" style="margin-top: 24px;">
                    <label class="label">Realm to Archive</label>
                    <div class="control">
                        <div class="select">
                            <select bind:value={realmToArchive}>
                                {#each possibleRealmsToArchive as otherRealm}
                                    <option value={otherRealm}> {otherRealm.projectName} </option>
                                {/each}
                            </select>
                        </div>
                    </div>
                </div>

                <!-- Pick a realm name (required) -->
                <div class="field">
                    <label class="label">Enter the realm name to confirm archival.</label>
                    <div class="control">
                        <input class="input is-danger" type="text" placeholder="confirm realm name" bind:value={realmToArchiveConfirmation}>
                    </div>
                </div>
                
            </span>

            <span slot="buttons" let:toggle>
                
                <button class="button cancel" on:click|preventDefault={toggle}>
                    <span>Cancel</span>
                </button>
                <button class="button is-danger" class:is-loading={isNetworking} disabled={!realmToArchive || realmToArchiveConfirmation != realmToArchive.projectName} on:click|preventDefault={evt => archiveRealm(toggle)}>
                    <span>Archive </span>
                </button>
            </span>
        </ModalCard>

        <ModalCard class="is-medium has-light-bg replace-data" active={promoteRealmModalActive} onClose={() => promoteRealmModalActive = false}>
            <div slot="trigger-evt" let:toggle={modalToggle} let:active >
                <!-- Left intentionally blank to "hide" the popup. -->
            </div>
            <h3 slot="title">
                Promote Realm
            </h3>
            <span slot="body" class="create-realm-body">
                <span >
                    <!-- Warning Message -->
                    Realm promotion allows you to send Beamable data from one realm to another realm. 
                </span>

                <div class="field" style="margin-top: 24px;">
                    <label class="label">Source Realm</label>
                    <div class="control">
                        <div class="select">
                            <select bind:value={realmToPromoteFrom}>
                                {#each unarchivedRealms as otherRealm}
                                    <option value={otherRealm}> {otherRealm.projectName}</option>
                                {/each}
                            </select>
                        </div>
                    </div>
                </div>

                <div class="field" style="margin-top: 24px;">
                    <label class="label">Destination Realm</label>
                    <div class="control">
                        <div class="select">
                            <select bind:value={realmToPromoteInto}>
                                {#each unarchivedRealms as otherRealm}
                                    <option value={otherRealm}> {otherRealm.projectName}</option>
                                {/each}
                            </select>
                        </div>
                    </div>
                </div>

                {#each availablePromotables as scope, scopeIndex}
                    
                    <div class="promotable-container">
                        <div class="promotable-top-row">
                            <div class="promotable-name">
                                {scope.name}
                            </div>
                            <div class="promotable-include" class:disabled={!scope.isPromotionAvailable}>
                                <label class="checkbox">
                                    {#if scope.isPromotionAvailable}
                                        Promote {scope.name}
                                    {:else}
                                        No Changes
                                    {/if}
                                    <input type="checkbox" disabled={!scope.isPromotionAvailable} bind:checked={scope.shouldPromote}>
                                </label>
                            </div>
                        </div>
                        {#each scope.promotions as promotable, index}
                        <div class="promotable-checksums" class:diff={promotable.source.checksum != promotable.destination.checksum}>
                         
                            <div class="field">
                                {#if index == 0}
                                    <label class="label">{realmToPromoteFrom.projectName} Checksum</label>
                                {/if}
                                <div class="control">
                                    {#if scope.promotions.length > 1}
                                        <label class="label">{promotable.name}</label>
                                    {/if}
                                    <input class="input" type="text" readonly placeholder="No {promotable.name} has been promoted yet." bind:value={promotable.source.checksum}>
                                </div>
                                <label class="label right-aligned">
                                    {promotable.source.createdAtDisplay}
                                </label>
                            </div>
                            <div class="field">
                                {#if index == 0}
                                    <label class="label">{realmToPromoteInto.projectName} Checksum </label>
                                {/if}
                                <div class="control">
                                   {#if scope.promotions.length > 1}
                                        <label class="label">{promotable.name}</label>
                                    {/if}
                                    <input class="input" type="text" readonly placeholder="No {promotable.name} has been promoted yet." bind:value={promotable.destination.checksum}>
                                </div>
                                <label class="label right-aligned">
                                    {promotable.destination.createdAtDisplay}
                                </label>
                            </div>
                        </div>
                        {/each}
                    </div>
                {/each}

                {#if realmToPromoteInto && realmToPromoteFrom && desiredPromotables.length > 0}
                    <span>
                        You will be promoting <b>{desiredPromotableNames}</b> from <b>{realmToPromoteFrom.projectName}</b> to <b> <b>{realmToPromoteInto.projectName}</b></b>
                    </span>
                {:else if realmToPromoteInto}
                    <span>
                        Select the features you'd like to promote to {realmToPromoteInto.projectName}
                    </span>
                {:else}
                    <span>
                        Select the realms to promote
                    </span>
                {/if}

                
            </span>

            <span slot="buttons" let:toggle>
                
                <button class="button cancel" on:click|preventDefault={toggle}>
                    <span>Cancel</span>
                </button>
                <button class="button is-primary" class:is-loading={isNetworking} disabled={!isRealmPromotionValid} on:click|preventDefault={evt => promoteRealm(toggle)}>
                    <span>Promote </span>
                </button>
            </span>
        </ModalCard>

    
        <div class="realm-container">
            <ComponentButtons right={-12}>
                {#if realm.parent}
                <p class="control">

                    <button class="button trigger-button" style="" on:click={evt => openPromoteRealm()}>
                        <span class="icon is-small">
                            <FeatherIcon icon="chevrons-up"/>
                        </span>
                        <span>
                            Promote Realm
                        </span>
                    </button>

                </p>
                {/if}

                <p class="control">
                    <Link href="/{orgId}/games/{gameId}/realms/{realm.pid}" let:href>
                        <a class="button trigger-button" {href}>
                            <span class="icon is-small">
                                <FeatherIcon icon="corner-up-right"/>
                            </span>
                            <span>
                               Goto Realm
                            </span>
                        </a>
                    </Link>

                </p>

                <p class="control">
                    <RawDataButton
                        title="Config Defaults"
                        buttonName=""
                        iconName="settings"
                        resolveFunction={() => generateConfigDefaults(realm)}
                    />
                </p>


                <p class="control" style="width: 48px">
                    <Dropdown class="dropdown" style="display: block;">
                        <div slot="trigger" class="dropdown-trigger button" style="height: 32px;padding-top: 0px;padding-bottom: 0px; border: none; background: rgb(69, 69, 69); color: rgb(197, 197, 197);">
                            <span class="icon is-small">
                                <FeatherIcon icon="list"/>
                            </span>
                        </div>
                        
                        <div class="dropdown-menu is-edit-role" id="dropdown-menu" role="menu" style="right: 0px; left: auto;">
                            <div class="dropdown-content">
  
                                <button class="button trigger-button" style="width: 100%;" on:click={evt => openCreateChild()}>
                                    <span class="icon is-small">
                                        <FeatherIcon icon="git-branch"/>
                                    </span>
                                    <span>
                                        Create Child
                                    </span>
                                </button>

                                {#if realm.parent && possibleRealmsToReparent.indexOf(realm) > -1}
                                    <button class="button trigger-button" style="width: 100%;" on:click={evt => openReparentChild()}>
                                        <span class="icon is-small">
                                            <FeatherIcon icon="git-pull-request"/>
                                        </span>
                                        <span>
                                            Change Parent
                                        </span>
                                    </button>
                                {/if}

                                {#if possibleRealmsToArchive.indexOf(realm) > -1}
                                    <button class="button trigger-button" style="width: 100%;" on:click={evt => openArchiveRealm()}>
                                        <span class="icon is-small">
                                            <FeatherIcon icon="trash"/>
                                        </span>
                                        <span>
                                            Archive Realm
                                        </span>
                                    </button>
                                {/if}
                            </div>
                        </div>
                    </Dropdown>
                </p>
            </ComponentButtons>

            <div class="data">

                <div>
                    <AsyncInput 
                        title="Project Id (pid)"
                        value={realm.pid}
                        editable={false}
                    />

                    <AsyncInput 
                        title="Customer Id (cid)"
                        value={orgId}
                        editable={false}
                    />

                    <AsyncInput 
                        title="Parent Realm"
                        value={realm.parent ? realmMap[realm.parent].projectName : '<none>'}
                        editable={false}
                    />
                </div>

                
                <div>
                    <AsyncInput 
                        title="Microservice Status"
                        value="{microservicePromotionText}"
                        editable={false}
                    />

                    <AsyncInput 
                        title="Content Status"
                        value={contentPromotionText}
                        editable={false}
                    />
                </div>
            </div>
        </div>
    {/if}
</Card>



<style lang="scss">
    .realm-container {
        position: relative
    }

    .promotable-container {

        .promotable-top-row {
            display:flex;
            margin-top: 40px;

            .promotable-name {
                flex-grow: 1;
                font-weight: bold;
                text-transform: capitalize;
                margin-bottom: 4px;
            }
        
            .promotable-include {
                &.disabled {
                    opacity: .3;
                }
                .checkbox {
                    display: flex;
                    align-items: center;
                    &:hover {
                        color:white;
                    }
                    input {
                        width: 20px;
                        margin-left: 10px;
                        height: 20px;
                        border: 2px black;
                        border-radius: 10px;
                        &:hover {
                            color:white;
                        }
                    }
                }
            }
        }
        .promotable-checksums {
            display: flex;
            flex-direction: row;
            &:not(.diff) .field .control{
                opacity: .3;
            }
            .field {
                .label.right-aligned {
                    text-align:right;
                }
                width: 50%;
                flex-grow: 1;
                &:first-child {
                    margin-right: 8px;
                }
                &:last-child {
                    margin-left: 8px;
                }
            }
        }
    }


    .data {
        display: flex;
        flex-direction: row;
        align-items: flex-start;
        > div {
            &:first-child{
                margin-right: 12px;
            }
            &:last-child {
                margin-left: 12px;
            }
            flex-grow: 1;
            display: flex;
            flex-direction: column;
        }
    }
    .control :global(.modal-wrap button.trigger-button){
        width: 48px;
        display: flex;
        text-align: center;
        align-content: center;
        justify-content: center;
    }

    .control :global(.modal-wrap){
        padding: 0;
    }



    .dropdown-content {
        background: #333236;
        border: solid 1px #5d5d5d;
        padding: 0;
        padding-right: 4px;

        button {
            margin: 4px 2px;
            display: flex;
            .icon {
                margin-left: 12px;
                margin-right: 12px;
            }
            span:not(:first-child){
                flex-grow: 1;
                margin-left: 20px;
                text-align: left;
            }
            &:hover {
                background: #606060;
            }
        }
    }

    label.label {
        color: rgba(255, 255, 255, .7);
        font-size: 12px;
        font-weight: 700;
    }
    .create-realm-body {
        text-align: left;
        .select {
            width: 100%;
            select {
                background: #333336;
                width: 100%;
                color: white;
            }
        }
    }
</style>