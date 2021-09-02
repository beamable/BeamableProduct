<script>
    import FeatherIcon from './FeatherIcon';

    export let placeholder = "Search";
    export let filterFunctions = []; // {func: (e) => true, name: 'name', type: 'default'}; TODO: change type to 'group'
    export let allElements = []; // some array of objects to filter on
    export let filterOn = 'name'; // name of property to do text search over

    export let value = [];

    let searchQuery ;
    let activeFilters = { };

    let filterTimer;
    let filteredContent = [];

    let groupedFilterFunctions = {}; // [string group type => array of functions]
 
    let content;

    $: if (allElements){
        performFilter();
    }
    $: if (searchQuery != undefined){
        debounceQuery();
    }
    $: if (filterFunctions){
        groupedFilterFunctions = filterFunctions.reduce((agg, curr) => {
            const key = curr.type || 'default';
            let existingGroup = agg[key];
            if (!existingGroup){
                existingGroup = [];
            }
            return {
                ...agg, 
                [key]: [...existingGroup, curr]
            };
        }, {});
    }
    $: activeFilters = Object.keys(groupedFilterFunctions).reduce( (agg, curr) => {
        return {
            ...agg,
            [curr]: groupedFilterFunctions[curr][0]
        }
    }, {});

	function debounceQuery() {
        clearTimeout(filterTimer);
		filterTimer = setTimeout(() => {
			performFilter()
		}, 750);
	}

    function performFilter(){
        clearTimeout(filterTimer);
        filterTimer = undefined;

        const filters = Object.keys(activeFilters).map(k => activeFilters[k]);
        const availableElements = filters.reduce( (agg, curr) => {
            return agg.filter(curr.func);
        }, allElements);

        if (!searchQuery) {
            filteredContent = availableElements;

            updateValue();
            return;
        }

        const query = searchQuery.toLowerCase();
        filteredContent = availableElements.filter(s => {
            const filteringOn = s[filterOn];
            return filteringOn == null ? false : filteringOn.toLowerCase().indexOf(query) > -1
        })

        updateValue();
    }

    function updateValue(){
        value = [...filteredContent]
    }

    function handleFilterClick(group, filter){
        activeFilters[group] = filter;
        
        performFilter();
    }

</script>

<style lang="scss">
    .filter-search {
        display: flex;
        flex-direction: row;
        .search-box {
            flex-grow: 1;
            display: flex;
            flex-direction: row-reverse;
            .field {
                flex-grow: 1;
                max-width: 360px;
            }
        }
        >.control {
            max-width: calc(100% - 300px);
        }
        .buttons {
            flex-direction: column;
            align-items: flex-start;
            .button-group {
                align-items: center;
                display: flex;
                flex-wrap: wrap;
                justify-content: flex-start;
            }
        }
        .filter-button {
            color: grey;
            background:#1f1f1f;
            border: none;
            min-width: 80px;
            border-bottom: solid 2px transparent;
            &:hover {
                color: #0095f1;
                border-bottom-color: grey;
            }
            &.active:hover,
            &.active {
                background-color: #454545;
                color:white;
            }
        }
    }
</style>

<div class="filter-search">
    <div class="control">
        <div class="buttons has-addons">
            {#each Object.keys(groupedFilterFunctions) as filterGroup}
                <div class="button-group">
                    {#each (groupedFilterFunctions[filterGroup]) as filterFunction}
                        <button class="button filter-button" class:active={filterFunction === activeFilters[filterGroup]} on:click={evt => handleFilterClick(filterGroup, filterFunction)}>
                            {filterFunction.name}
                        </button>
                    {/each}
                </div>
            {/each}
        </div>
    </div>

    <form on:submit|preventDefault={performFilter} class="search-box">
        <div class="field">
            <div class="control has-icon" class:is-loading={filterTimer}>
                <input type="text" class="input" placeholder={placeholder} bind:value={searchQuery} >
                <div class="form-icon">
                    <FeatherIcon icon="search"/>
                </div>
            </div>
        </div>
    </form>
</div>
