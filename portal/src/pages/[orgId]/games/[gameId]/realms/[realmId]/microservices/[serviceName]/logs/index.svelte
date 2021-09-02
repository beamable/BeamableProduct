<script>

    import VirtualList from '@sveltejs/svelte-virtual-list';
    import JSONFormatter from 'json-formatter-js'
    import { writable, get } from 'svelte/store';
    import LogMessage from '../../../../../../../../../components/LogMessage';
    import FeatherIcon from '../../../../../../../../../components/FeatherIcon';
    import { getServices } from '../../../../../../../../../services';

    const { microservices } = getServices();

    export let route //current route

    let filterTimer;
    let queryStr = writable('')
    let serviceName;
    let combinedFilter='';
    let queryStore = writable('');
    let startTimeStore = writable(-1);
    let endTimeStore = writable(-1);
    let loadMoreStore = writable(0);
    let hasMore = false;
    let loadingMore = false;
    let logStore;
    let logData = [];
    let isLoading = true;
    let isDrawerOpen = false;
    let error;
    let selectedLog = undefined;
    let selectedTimeBound;
    let prettyObj = {a: 1}
    let jsonTree;
    let filterableLogLevels = [
        {
            name: 'Debug',
            value: false
        },{
            name: 'Info',
            value: true
        },{
            name: 'Warning',
            value: true
        },{
            name: 'Error',
            value: true
        },{
            name: 'Fatal',
            value: true
        }
    ]
    let timeBounds = [
        {
            name: '1m',
            getEndTime: now,
            getStartTime: () => getSecondsEarlier(now(), 1 * 60)
        },
        {
            name: '5m',
            getEndTime: now,
            getStartTime: () => getSecondsEarlier(now(), 5 * 60)
        },
        {
            name: '30m',
            getEndTime: now,
            getStartTime: () => getSecondsEarlier(now(), 30 * 60)
        },
        {
            name: '1h',
            getEndTime: now,
            getStartTime: () => getSecondsEarlier(now(), 2 * 60 * 60)
        },
        {
            name: '12h',
            getEndTime: now,
            getStartTime: () => getSecondsEarlier(now(), 24 * 60 * 60)
        },
        {
            name: 'clear',
            getEndTime: () => -1,
            getStartTime: () => -1
        }
    ]
    let computedFilter;

    $:serviceName = route.params.serviceName
    $: {
        if (serviceName){
            logStore = microservices.createLogStream(serviceName, queryStore, loadMoreStore, startTimeStore, endTimeStore)
            logStore.subscribe(value => {
                console.log('got log value', value);
                isLoading = false;
                hasMore = false;
                loadingMore = false;
                if (value == undefined) return; 

                if (value.firstPage){
                    jumpToTop()
                }

                if (value.error){
                    error = value.error;
                    logData = [];
                    return;
                } else {
                    if (value.nextToken){
                        hasMore = true;
                    }
                    error = undefined;
                    logData = value.logs.map((l, index) => ({...l, index}));
                }
            })
        }
    }

    queryStr.subscribe(value => {
        debounceQuery(value)
    })

    function forceLoad(){
        loadMoreStore.update(n => n + 1);
    }

    function now() {
        return new Date()
    }
    function getSecondsEarlier(endDate, secondsPrior){
        let date = new Date(endDate - secondsPrior * 1000)
        return date;
    }

    function computeFilterString(){
        let query = $queryStr;
        computedFilter = filterableLogLevels.filter(x => x.value == true)
        let levelQueries = computedFilter.map(level => `($.__l="${level.name}")`);
        let levelQuery = levelQueries.join('||');

        let filterParts = query.split(';')
        let parsedParts = filterParts.map(part => {
            let subParts = part.split('=')
            if (subParts.length == 2) {
                return `($.${subParts[0].trim()}="${subParts[1].trim()}")`
            }
            if (subParts.length == 1){
                return `($.__m="*${subParts[0].trim()}*")`
            }
        })
        let filterQuery = parsedParts.join('&&');

        combinedFilter = `{(${levelQuery})&&(${filterQuery})}`
    }

	function debounceQuery() {
        clearTimeout(filterTimer);
		filterTimer = setTimeout(() => {
			performFilter()
		}, 750);
    }
    
    function performFilter(){
        clearTimeout(filterTimer);
        filterTimer = undefined;
        isLoading = true;
        computeFilterString()
        if (selectedTimeBound){
            let endTime = selectedTimeBound.getEndTime().getTime()
            let startTime = selectedTimeBound.getStartTime().getTime()
            startTimeStore.set(startTime)
            endTimeStore.set(endTime)
        } else {
            startTimeStore.set(-1)
            endTimeStore.set(-1)
        }
        queryStore.set(combinedFilter)
        loadMoreStore.set(0);
    }

    function jumpToTop(){
        document.querySelector('svelte-virtual-list-viewport').scrollTop = 0;
    }

    function handleLowRowSelected(logData){

        try {
            var obj = JSON.parse(logData.raw.message);
            prettyObj = obj;
        } catch {
            prettyObj = logData.raw;
        }

        selectedLog = {
            ...logData,
            prettyObj
        }
        isDrawerOpen = true;

        removeAllChildNodes(jsonTree)

        const formatter = new JSONFormatter(prettyObj, 1, {
            hoverPreviewEnabled: false,
            hoverPreviewArrayCount: 100,
            hoverPreviewFieldCount: 5,
            theme: 'dark',
            animateOpen: true,
            animateClose: true,
            useToJSON: true
        });
        jsonTree.appendChild(formatter.render());


    }

    function logRowCreated(node, logMessage) {
        let rowLoad = logData.length;
        if (logMessage.index > logData.length - 20 && hasMore){
            let existingLoad = get(loadMoreStore);
            loadMoreStore.set(rowLoad);
            if (existingLoad != rowLoad){
                loadingMore = true;
            }
        }

		return {
			destroy() {
				// the node has been removed from the DOM
			}
		};
	}

    function removeAllChildNodes(parent) {
        while (parent.firstChild) {
            parent.removeChild(parent.firstChild);
        }
    }

    function selectTimeBound(timeBound){
        selectedTimeBound = timeBound;
        if (selectedTimeBound == timeBounds[timeBounds.length - 1]){
            selectedTimeBound = undefined;
        }
        debounceQuery()
    }

</script>

<style lang="scss">
    .log-view {
        $left: 150px;
        $top: 140px;

        .log-header {
            position: fixed;
            top: 70px;
            left: $left;
            right: 12px;
            h1 {
                font-size: 22px;
                font-weight: bold;
                margin-bottom: 12px;
            }

            .search-box {
                display: flex;
                .filter-field {
                    flex-grow: 1;
                    margin-right: 12px;
                }
            }
        }

        .filter-wrapper {
            position: absolute;
            left: 24px;
            top: calc(#{$top} + 80px);
            right: calc(100% - #{$left} + 12px);
            flex-direction: column;
            display: flex;

            .log-level {
                flex-direction: column;
                p{
                    label {
                        opacity: .8;
                        color: white;
                        &:hover {
                            opacity:.9;
                            color: white;
                        }
                    }
                }
            }
        }
        .main-loading {
            position: absolute;
            text-align: center;
            left: calc(50%-100px);
            top: 50%;
            progress {
                width: 200px;
                height: 8px;
                &.progress.is-info:indeterminate{
                    background-color: #655f5f;
                    background-image: linear-gradient(90deg,#3698d3b5 30%,#655f5f 0);
                }
            }
        }
        .floater {
            text-align: center;
            opacity: .9;
            .loading-more-data{
                progress {
                    width: 200px;
                    height: 8px;
                    margin: auto;
                    &.progress.is-info:indeterminate{
                        background-color: #655f5f;
                        background-image: linear-gradient(90deg,#3698d3b5 30%,#655f5f 0);
                    }
                }
            }
        }

        .vlist-wrapper {
            margin-top: $top;
            height: calc(100vh - 250px);
            position: absolute;
            left: $left;
            right: 12px;
        }

        .vlist-wrapper :global(svelte-virtual-list-row:nth-child(odd)) {
            background-color:rgba(0,0,0,.2);
        }

        .drawer {
            $drawer-size: 40%;
            position: absolute;
            width: $drawer-size;
            left: 100%;
            top: 60px;
            bottom: 0;
            background-color: #1f1f1f;
            box-shadow: -9px 0px 40px -8px rgba(0,0,0,0);

            transition: all .35s 0s ease-in-out;
            &.open {
                left: calc(100% -  #{$drawer-size});
                box-shadow: -9px 0px 40px -8px rgba(0,0,0,.7);
            }

            .button-strip {
                display: flex;
                flex-direction: row-reverse;
                button {
                    border: none;
                    background: none;
                    color: #e6e6e6;
                    &:hover {
                        color: white;
                    }
                }
            }
            .content {
                padding: 0px 12px;
                .top {
                    display: flex;
                    flex-direction: row;
                    .level {
                        min-width: 100px;
                        opacity: .8;
                        text-transform: uppercase;
                        font-weight: bold;
                        &.faded {
                            color: lightgreen;
                        }
                        &.danger {
                            color: red;
                            font-weight: bold;
                        }
                        &.warn {
                            color: orange;
                        }
                        &.normal {
                            color: cornflowerblue;
                        }
                    }
                    .dateText {
                        opacity: .9;
                        flex-grow: 1;
                    }
                }

                pre {
                    white-space: pre-wrap;       /* css-3 */
                    white-space: -moz-pre-wrap;  /* Mozilla, since 1999 */
                    white-space: -pre-wrap;      /* Opera 4-6 */
                    white-space: -o-pre-wrap;    /* Opera 7 */
                    word-wrap: break-word;       /* Internet Explorer 5.5+ */
                }
                .json-wrapper, .raw-wrapper {
                    background-color: rgba(0,0,0,.3);
                    border-radius: 8px;
                    padding: 12px 10px;
                    margin-bottom: 12px;
                }
            }
        }
    }
    .drawer :global(.json-formatter-string){
        white-space: pre-line;
    }

</style>

<div class="log-view">
<button on:click={forceLoad}>
    Force Load
</button>
    <div class="log-header" on:click={evt => isDrawerOpen = false}>
        <h2>Logs</h2>
        <h1>{serviceName}</h1>

        <form on:submit|preventDefault={performFilter} class="search-box">
            <div class="field filter-field">
                <div class="control has-icon" class:is-loading={isLoading || filterTimer} class:is-danger={error}>
                    <input class="input is-primary" type="text" placeholder="Filter Logs" bind:value={$queryStr}>
                    <div class="form-icon">
                        <FeatherIcon icon="search"/>
                    </div>
                </div>
                {#if error}
                    <p class="help is-danger">{error}</p>
                {/if}
            </div>
            <div class="field has-addons">
                {#each timeBounds as timeBound}
                    <p class="control">
                        <button class="button" class:is-info={selectedTimeBound == timeBound} on:click={evt => selectTimeBound(timeBound)}>
                            <span>{timeBound.name}</span>
                        </button>
                    </p>
                {/each}
            </div>
        </form>
    </div>

    <div class="filter-wrapper">
        <div class="log-level field is-grouped">
            <label>
                Log Level
            </label>
            {#each filterableLogLevels as logLevel}
                <p class="control">
                    <label class="checkbox">
                        <input type="checkbox" bind:checked={logLevel.value} on:input={evt => debounceQuery()}>
                        {logLevel.name}
                    </label>
                </p>
            {/each}
        </div>
    </div>

    {#if isLoading && logData.length == 0}
        <div class="main-loading">
            Fetching Logs...
            <progress class="progress is-large is-info" max="100">60%</progress>
        </div>
    {/if}

    <div class="vlist-wrapper" >
        <VirtualList itemHeight={48} items={logData} let:item>
            <div use:logRowCreated={item}>
            </div>
            <LogMessage logRow={item} onSelected={handleLowRowSelected} />
            {#if item.index == logData.length -1}
                <div class="floater">
                    {#if loadingMore}
                        <div class="loading-more-data">
                            loading more data...
                            <progress class="progress is-large is-info" max="100">60%</progress>
                        </div>
                    {:else}
                        <span> no more data </span>
                    {/if}
                </div>
            {/if}
        </VirtualList>
    </div>

    <div class="drawer" class:open={isDrawerOpen}>
        <div class="button-strip">
            <p class="buttons">
                <button class="button is-small" on:click={evt => isDrawerOpen = false}>
                    <span class="icon is-small">
                        <FeatherIcon icon="x"/>
                    </span>
                </button>
            </p>
        </div>

        <div class="content">
            {#if selectedLog}
                <div class="top">
                    <p class="level {selectedLog.levelClass}">
                        {selectedLog.level}
                    </p>
                    <p class="dateText">
                        {selectedLog.dateText}
                    </p>
                </div>
                <div class="raw-wrapper">
                    <pre class="raw">
                        {selectedLog.message}
                    </pre>
                </div>
            {/if}

            <div bind:this={jsonTree} class="json-wrapper">
            </div>
        </div>


    </div>
</div>
