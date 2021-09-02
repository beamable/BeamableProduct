<script>
    import PlayerData from '../services/players';
    import FeatherIcon from './FeatherIcon';
    import FilterSearch from './FilterSearch';
    import PaginateArray from './PaginateArray';
    import { getServices } from '../services';
    import AsyncInput from './AsyncInput';

    export let player;
    export let playerStats;

    const { players } = getServices();

    let stats = [];
    let filteredStats = [];
    let pagedStats = [];
    let activeTab = 'all';

    $: if (playerStats){
        stats = playerStats
    } else stats = []


    function setTab(tab) {
        activeTab = tab || 'all';
    }

    async function handleStatWrite(stat, next, old){
        const nextStat = {
            ...stat,
            value: next
        };
        const savedStat = await players.updateStats(player, nextStat)
        stat.value = savedStat.value;
        return stat.value;
    }

</script>


<FilterSearch 
    placeholder="Search by stat name" 
    allElements={stats}
    filterOn="name"
    filterFunctions={[
        {func: (m) => true, name: 'All'},
        {func: (m) => m.origin === 'game', name: 'Game'},
        {func: (m) => m.origin === 'client', name: 'Client'},
        {func: (m) => m.visibility === 'public', name: 'Public'},
        {func: (m) => m.visibility === 'private', name: 'Private'},
    ]}
    bind:value={filteredStats}
    >
</FilterSearch>


<div >
    {#if filteredStats.length == 0}
        <div class="no-stat-message">
            No stats found
        </div>
    {:else}
    <PaginateArray 
        paginationKey="stats"
        position="top"
        elements={filteredStats}
        bind:pagedElements={pagedStats}/>
    {#each pagedStats as stat}
    <div class="stat panel">
        <div class="stat-flags origin-{stat.origin} visibility-{stat.visibility}">
        </div>

        <div class="stat-name">
            {stat.name}
        </div>
        <div class="stat-origin">
            <span class="underline {stat.origin}">
                {stat.origin}
            </span>
        </div>
        <div class="stat-visibility">
            <span class="icon is-medium">
                <FeatherIcon icon="{stat.visibility === 'public' ? 'users' : 'eye-off'}"/>
            </span>
            <span class="underline {stat.visibility}"> 
                {stat.visibility}
            </span>

        </div>
        <AsyncInput 
            value={stat.value}
            editable={stat.origin === 'client'}
            onWrite={(next, old) => handleStatWrite(stat, next, old)}
        />
    </div>
    {/each}
    <PaginateArray 
        paginationKey="stats"
        position="bottom"
        elements={filteredStats}
        bind:pagedElements={pagedStats}/>
    {/if}


</div>



<style lang="scss">

    $red: #FF5c5c;
    $green: #77B769;
    $blue: #4497B7;
    $yellow: #EBC65F;

    .stat {
        margin: 14px 0px;
        background:#454545;
        border-radius: 4px;

        position: relative;
        display: flex;
        flex-direction: row;
        align-items: center;
        min-height: 60px;
    }

    .stat > * {
        display: inline-block;
    }

    .stat .stat-name {
        width: 33%;
        padding-left: 40px;
        overflow-wrap: anywhere;
    }

    .stat .stat-origin {
        width: 10%;
    }
    .stat .stat-visibility {
        width: 120px;
    }

    .stat .stat-value {
        flex-grow: 1;
    }

    .stat .stat-buttons {
        width: 92px;
        padding: 12px;
    }

    .stat .stat-flags {
        border-left: solid 12px grey;
        border-right: solid 12px black;
        border-top-left-radius: 4px;
        border-bottom-left-radius: 4px;
        position: absolute;
        top:0;
        bottom:0;
    }

    .stat-visibility .icon {
        vertical-align: middle;
    }

    .stat .stat-flags.origin-game {
        border-left: solid 12px $green;
    }
    .stat .stat-flags.origin-client {
        border-left: solid 12px $yellow;
    }
    .stat .stat-flags.visibility-public {
        border-right: solid 12px $blue;
    }
    .stat .stat-flags.visibility-private  {
        border-right: solid 12px $red;
    }

    .stat .underline {
        border-bottom: solid 3px;
        padding: 2px 4px;
        text-transform: capitalize;
    }
    .stat .underline.game {
        border-bottom-color: $green;
    }
    .stat .underline.client {
        border-bottom-color: $yellow;
    }
    .stat .underline.public {
        border-bottom-color: $blue;
    }
    .stat .underline.private {
        border-bottom-color: $red;
    }

    .stat-buttons .button {
        padding: 0px;
        min-width: 32px;
    }

    .no-stat-message {
        text-align: center;
        border-bottom: solid 1px white;
        padding-bottom: 200px;
        margin-top: 120px;
    }

</style>
