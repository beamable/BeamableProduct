<script context="module">

    import { writable } from 'svelte/store';

    let allPaginations = {};
    let paginationCounts = {};

    function createPaginationData(paginationKey){
        return {
            pagedElements: [],
            pageSizeStore: writable(10),
            activePageStore: writable({index: 0, number: 1}),
            pageDescriptorStore: writable([]),
            hasInitiated: false,
            totalElements: 0,
            pageSizes: [5, 10, 15],
            isFetchingData: false,
            fetchPage: (pageNumber, pageSize) => [],
            forceRefreshStore: writable( () => console.warn('refresh function not set') )
        }
    }

    function registerPagination(key){
        if (!key || !key.length){
            throw {
                message: 'cannot create a pagination element without a paginationKey'
            }
        }
        var data = allPaginations[key] || createPaginationData();
        allPaginations[key] = data;
        var oldCount = paginationCounts[key] || 0;
        paginationCounts[key] = oldCount + 1;

        

        return data;
    }

    function unRegisterPagination(key){
        var oldCount = paginationCounts[key] || 0;
        paginationCounts[key] = oldCount - 1;
        if (paginationCounts[key] <= 0){
            delete paginationCounts[key];
            delete allPaginations[key];
        }
    }

</script>

<script>
    import FeatherIcon from './FeatherIcon';
    import { onMount, onDestroy } from 'svelte';
    
    export let paginationKey = '';
    export let pagedElements = [];
    export let totalElements = 0;
    export let pageSizes = [5,10,15];
    export let position = "top";
    export let isFetchingData = false;
    export let unbounded = false;
    export let forceRefresh = () => {};
    export let fetchPage = (pageNumber, pageSize) => [];
    export let currentPage = 0;
    let data = createPaginationData();

    let pageSize = 10;

    let unsubPageSizeSubscription;
    let unsubActivatePageSubscription;
    let unsubPageDescriptorsSubscription;
    let unsubForceRefreshSubscription;

    let pageOpen = false;
    let textEntryPage = 0;
    let fakeHighlight = false;
    let fakeActivate = false;
    let activePage = { number: 1, index: 0}
    let pageDescriptors = [
        /*
        {
            number: 1,
            index: 0
            ???
        }
        */
    ]
    let isTop = true;
    $: isTop = position === "top";
    $: totalElements = unbounded == true ? Number.POSITIVE_INFINITY : totalElements;
    $: totalElements = Math.max(0, totalElements);
    $: totalPages = Math.ceil(totalElements / pageSize);
    $: currentPage = activePage == undefined ? 0 : activePage.number;
    
    $: if (totalPages > 0){
        refresh();
    }

    let oldPaginationKey;
    $: if (paginationKey) {
        if (oldPaginationKey){
            handleDestroy(oldPaginationKey);
        }
        handleMount(paginationKey);
        oldPaginationKey = paginationKey;
    }
    onDestroy(() => handleDestroy(paginationKey));

    function handleMount(key){
        data = registerPagination(key);
        data.fetchPage = fetchPage;
        
        unsubPageSizeSubscription = data.pageSizeStore.subscribe(value => {
            pageSize = value;
            if (pageSize){
                refresh();

            }
        });
        unsubActivatePageSubscription = data.activePageStore.subscribe(value => {
            activePage = value;
            textEntryPage = activePage.number
        });
        unsubPageDescriptorsSubscription = data.pageDescriptorStore.subscribe(value => {
            pageDescriptors = value;
        });
        unsubForceRefreshSubscription = data.forceRefreshStore.subscribe(value => {
            forceRefresh = value;
        });
                
        if (!data.hasInitiated){
            data.hasInitiated = true;

            const firstPage = {number: 1, index: 0};
            data.pageDescriptorStore.set([
                firstPage
            ]);
            data.activePageStore.set(firstPage);
            gotoPage(firstPage);
            data.forceRefreshStore.set(() => {
                gotoPage(activePage);
            });
        }
    }

    function handleDestroy(key){
        unsubPageSizeSubscription();
        unsubActivatePageSubscription();
        unsubPageDescriptorsSubscription();
        unsubForceRefreshSubscription();
        unRegisterPagination(key);
    }
    
    function refresh(){
        pageDescriptors = []; // clear.
        if (unbounded){
            // do nothing, because there is no concept of real page descriptors
            return;
        }
        for (let i = 0; i < totalPages; i ++){
            pageDescriptors.push({
                number: i + 1,
                index: i
            });
        }
        data.pageDescriptorStore.set([...pageDescriptors]);
        if (pageDescriptors.length == 0){
            // do nothing?
        } else {
            if (!activePage){
                data.activePageStore.set(pageDescriptors[0]);
            } else if (activePage.index >= pageDescriptors.length){
                data.activePageStore.set(pageDescriptors[pageDescriptors.length - 1]);
            }
        }
    }

    async function gotoPage(pageDescriptor){
        data.activePageStore.set(pageDescriptor);
        activePage = pageDescriptor;
        isFetchingData = true;
        textEntryPage = activePage.number;
        try {
            const pageData = await fetchPage(pageDescriptor.index, pageSize);
            pagedElements = pageData;
        } finally {
            isFetchingData = false;
            pageOpen = false;
        }
    }

    function handlePageChange(){
        textEntryPage = Number(textEntryPage) || activePage.number;
        if (unbounded){
            const index = textEntryPage - 1;
            if (index !== activePage.index){
                gotoPage({
                    number: textEntryPage,
                    index: index
                });
            }
        } else {
            const index = Math.max(0, Math.min(pageDescriptors.length - 1, textEntryPage - 1));
            textEntryPage = pageDescriptors[index].number;
            if (index !== activePage.index){
                gotoPage(pageDescriptors[index]);
            }
        }
    }

    function deltaPage(delta){
        if (!unbounded){
            const index = Math.max(0, Math.min(pageDescriptors.length - 1, activePage.index + delta));
            gotoPage(pageDescriptors[index]);
        } else {
            const index = activePage.index + delta;
            gotoPage({
                number: index + 1,
                index,
            });
        }
    
    }

    function handlePageSizeChange(nextPageSize){
        data.pageSizeStore.set(nextPageSize); // notify other components of the change.
        gotoPage(activePage); // recalculate the page.
    }

</script>

{#if (totalElements > data.pageSizes[0]) || unbounded }
<div class="paginate-wrap" class:is-top={isTop}>
    <nav class="navigation-sizes">
        <ul class="page-sizes">
            {#each data.pageSizes as size}
                <li>
                    <a class="pagination-link" 
                        class:is-current={pageSize == size}
                        on:click={evt => handlePageSizeChange(size) }>
                        {size}
                    </a>
                </li>
            {/each}
        </ul>
    </nav>
    <div class="navigation-buttons">
        <nav>
            <ul class="inc-pages">
                <li>
                    <a class="pagination-link" class:disabled={ activePage.index == 0} on:click={evt => activePage.index == 0 ? undefined : deltaPage(-1)}>
                        Prev Page
                    </a>
                </li>
            </ul>
        </nav>
        {#if !unbounded}
            <div class="dropdown bounded" class:is-up={!isTop} class:is-active={pageOpen}>
                <div class="dropdown-trigger">
            
                    <form on:submit|preventDefault={handlePageChange}>
                        <input class="input page-input" 
                            class:fake-hover={fakeHighlight} 
                            class:fake-active={fakeActivate} 
                            on:mouseenter={evt => fakeHighlight = true}
                            on:mouseleave={evt => fakeHighlight = false}
                            on:blur={evt => fakeActivate = false}
                            on:blur={handlePageChange}
                            on:mousedown={evt => fakeActivate = true}
                            type="text" 
                            bind:value={textEntryPage}>
                    </form>

                    <button class="button page-button" class:is-loading={isFetchingData} aria-haspopup="true" aria-controls="dropdown-menu6" 
                        on:click={evt => pageOpen = !pageOpen}
                        class:fake-hover={fakeHighlight} 
                        class:fake-active={fakeActivate} 
                        on:mouseenter={evt => fakeHighlight = true}
                        on:mouseleave={evt => fakeHighlight = false}
                        on:mousedown={evt => fakeActivate = true}
                        on:mouseup={evt => fakeActivate = false}
                    >

                    <span class="is-capitalized">&nbsp;</span>
                    <span class="icon is-small is-triangle" >
                        <FeatherIcon icon="triangle" width="8" height="8" />
                    </span>
                    </button>
                </div>
                <div class="dropdown-menu" id="dropdown-menu" role="menu">
                    <div class="dropdown-content">
                        {#each pageDescriptors as page}

                            <a  class="dropdown-item"
                                class:is-active={page.index === activePage.index}
                                on:click={evt => gotoPage(page)}
                                >
                                Page {page.number}
                            </a>
                        {/each}
                    </div>
                    
                </div>
            </div>
        {:else}
            <div class="unbounded">
                <form on:submit|preventDefault={handlePageChange}>
                    <input class="input page-input" 
                        class:fake-hover={fakeHighlight} 
                        class:fake-active={fakeActivate} 
                        on:mouseenter={evt => fakeHighlight = true}
                        on:mouseleave={evt => fakeHighlight = false}
                        on:blur={evt => fakeActivate = false}
                        on:blur={handlePageChange}
                        on:mousedown={evt => fakeActivate = true}
                        type="text" 
                        bind:value={textEntryPage}>
                </form>
            </div>
        {/if}
        <nav>
            <ul class="inc-pages">
                <li>
                    <a class="pagination-link" class:disabled={!unbounded && activePage.index == pageDescriptors.length - 1} on:click={evt => !unbounded && activePage.index == pageDescriptors.length - 1 ? undefined : deltaPage(1)}>
                        Next Page
                    </a>
                </li>
            </ul>
        </nav>
    </div>
</div>
{/if}

<style lang="scss">

    .dropdown-trigger {
        display: flex;
    }
    .page-input, .page-button {
        &.fake-hover, &:hover {
            border-color: #b5b5b5;
        }
        &.fake-active, &:active {
            border-color: #3273dc;
        }
    }

    .unbounded .page-input {
        width: 90px;        
    }
    .bounded .page-input {
        border-right: none;
        border-top-right-radius: 0;
        border-bottom-right-radius: 0;
        width: 67px;
        
    }
    .page-button {
        &:focus {
            outline: 0;
        }
        width: 0px;
        height: 40px;
        display: flex;
        background: #333236;
        flex-direction: row;
        justify-content: space-between;
        border-left: none;
        border-top-left-radius: 0;
        border-bottom-left-radius: 0;
        border-color: #dbdbdb;
        color: white;
        font-weight: bold;
        padding-left: 0;
    }
    .paginate-wrap {
        display: flex;

        &.is-top {
            margin-top: 14px;
            padding-top: 12px;
            border-top: solid 1px grey;
        }
        .navigation-buttons {
            display: flex;
        }
        .navigation-sizes {
            flex-grow: 1;
        }
    }
    .dropdown-menu {
        min-width: auto;
    }
    .dropdown-content {
       
        background: #333236;
        border: solid 1px #5d5d5d;
        padding:0;
        max-height: 220px;
        overflow-y: scroll;
        a {
            padding-left: 20px;
            width: 88px;
            
        }
        a:hover {
            background: lighten(#424242, 5%);
        }
        a.is-active {
            background: #424242;
        }
    }

    .page-sizes{
        display:flex;
        flex-direction: row;
    }
    a {
        color: grey;
        background:#1f1f1f;
        border-radius: 0px;
        border: none;
        border-bottom: solid 2px transparent;
        &:hover {
            color: #0095f1;
            border-bottom-color: grey;
        }
        &.disabled {
            opacity: .5;
            cursor: not-allowed;
        }
        &.is-current:hover,
        &.is-current {
            border-color: transparent;
            background-color: #454545;
            color:white;
        }
    }

</style>
