<script>
    import FilterSearch from './FilterSearch';
    import FeatherIcon from './FeatherIcon';
    import PaginateArray from './PaginateArray';
    import RawDataButton from './RawDataButton';
    import PageList from './PageList';
    import AsyncInput from './AsyncInput';
    import ModalCard from './ModalCard';
    import ComponentButtons from './ComponentButtons';
    import { getServices } from '../services';
    import { onDestroy, onMount } from 'svelte';

    const {
        leaderboards,
        leaderboards: {
            leaderboardList
        }
    } = getServices();

    export let player;
    export let playerLeaderboards;

    let filteredLeaderboards = [];
    let pagedLeaderboards = [];
    let searchableLeaderboards = [];
    let expandedItem;
    let isNetworking;
    let addAsScore;
    let leaderBoardToAdd;

    $: leaderBoardNames = $leaderboardList ? $leaderboardList.nameList.filter(x => x.startsWith('leaderboards.')) : [];

    //$: leaderBoardToAdd = (searchableLeaderboards && searchableLeaderboards.length > 0) ? searchableLeaderboards[0].id : '';
    $: noLeaderboardsAvailable = !$leaderboardList || !$leaderboardList.nameList || !$leaderboardList.nameList.length;
    $: if (playerLeaderboards) {
        searchableLeaderboards = playerLeaderboards.map(transformEntry);
    }

    onMount(() => {
        // leaderBoardToAdd = (searchableLeaderboards && searchableLeaderboards.length > 0) ? searchableLeaderboards[0].id : '';
        addAsScore = 0;
    })

    function transformEntry(leaderboardRank){
        const statObject = leaderboardRank.rank.stats.reduce( (agg, curr) => ({
            ...agg,
            [curr.name]: curr.value
        }), {});
        const prefix = leaderboardRank.id.substr(0, leaderboardRank.id.indexOf('.') + 1)
        var displayType = prefix.substr(0, prefix.length - 1);
        if (displayType === 'event_events'){
            displayType = 'events';
        }
        return {
            ...leaderboardRank,
            canEdit: prefix === 'leaderboards.',
            prefix: displayType,
            searchTerm: leaderboardRank.id.substr(prefix.length),
            statPreview: JSON.stringify(statObject),
            statPretty: JSON.stringify(statObject, null, 2)
        }
    }

    async function addPlayer(toggle){
        try {
            console.log('adding player', leaderBoardToAdd)
            await handleSaveScore(leaderBoardToAdd, addAsScore);
        } finally {
            toggle();
        }
    }

    async function handleSaveScore(leaderboardId, score){
        isNetworking = true;
        try {
            await leaderboards.editPlayerScore(leaderboardId, player, score);
            return score;
        } finally {
            isNetworking = false;
        }
    }


</script>

<div class="player-leaderboard">
    <ComponentButtons>
        <p class="control">
            <ModalCard class="is-xsmall has-light-bg add-leaderboard">
              <div slot="trigger-evt" let:toggle let:active>
                <button class="button trigger-button" on:click|preventDefault|stopPropagation={toggle}>
                    <span class="icon is-small">
                        <FeatherIcon icon="plus-square"/>
                    </span>
                    <span>
                        Add Leaderboard
                    </span>
                </button>
              </div>
              <h3 slot="title">
                {#if noLeaderboardsAvailable}
                    No Leaderboards Available
                {:else}
                    Add Leaderboard
                {/if}
              </h3>
              
              <span slot="body">
                {#if noLeaderboardsAvailable}
                    <div>
                        There are no leaderboards available in this realm.
                    </div>
                {:else}
                <div class="field">
                    <div class="control">
                        <div class="select is-info">
                            <select bind:value={leaderBoardToAdd}>
                                {#each leaderBoardNames as symbol}
                                    <option value={symbol}> {symbol} </option>
                                {/each}
                            </select>
                        </div>
                    </div>
                </div>
                <div class="field">
                    <div class="control">
                        <input class="input is-primary" type="number" placeholder="Score?" bind:value={addAsScore}>
                    </div>
                </div>
                {/if}
              </span>
            
              <span slot="buttons" let:toggle>
                {#if noLeaderboardsAvailable}
                    <button class="button cancel" on:click|preventDefault={toggle}>
                        <span>Back</span>
                    </button>
                {:else}
                    <button class="button is-success" disabled={!leaderBoardToAdd} on:click|preventDefault|stopPropagation={evt => addPlayer(toggle)} class:is-loading={isNetworking}>
                        <span>
                            <slot name="primary-button">
                                Add
                            </slot>
                        </span>
                    </button>
                    <button class="button cancel" on:click|preventDefault={toggle}>
                        <span>Cancel</span>
                    </button>
                {/if}
              </span>
            </ModalCard>
        </p>
    </ComponentButtons>
{#if searchableLeaderboards.length > 0}


    <FilterSearch
        placeholder="Search..."
        allElements={searchableLeaderboards}
        filterOn="searchTerm"
        filterFunctions={[
                {func: (m) => true, name: 'All'},
                {func: (m) => m.id.startsWith('leaderboards.'), name: 'Leaderboards'},
                {func: (m) => m.id.startsWith('tournaments.'), name: 'Tournaments'},
                {func: (m) => m.id.startsWith('event_events.'), name: 'Events'},
        ]}
        bind:value={filteredLeaderboards}/>

    <PaginateArray 
        paginationKey="player-leaderboards"
        position="top"
        elements={filteredLeaderboards}
        bind:pagedElements={pagedLeaderboards}/>

    <PageList elements={pagedLeaderboards}
            leftFlagClass={e => 'space'}
            rightFlagClass={e => 'space'}
            >

      <div slot="element" let:element class="elem">
        
        <div class="text leaderboard-title" style="min-width: 100px">
            <label>
                {element.prefix}
            </label>
            <div>
                {element.searchTerm}
            </div>
        </div>
        <div class="text rank" style="width: 20%; min-width: 100px">
            <span class="top"> {element.rank.rank} </span>
            <div>
            <span class="bottom"> {element.boardSize} </span>
            </div>
        </div>
        
        <div class="value">
            {#if expandedItem === element}
                <div class="control" >
                    <textarea class="textarea is-static has-fixed-size" 
                        type="text" 
                        placeholder="value..." 
                        readonly
                        value={element.statPretty}/>
                </div>
                {:else}
                <div class="control">
                    <input class="input is-static" type="text" placeholder="value..." readonly value={element.statPreview} >
                </div>
            {/if}
        </div>
        
        <div class="expand-buttons">
            {#if expandedItem === element}
                <button class="button" on:click={evt => expandedItem = undefined}>
                    <FeatherIcon icon="arrow-up"/>
                </button>
            {:else}
                <button class="button" on:click={evt => expandedItem = element}>
                    <FeatherIcon icon="arrow-right"/>
                </button>
            {/if}
        </div>
        
        <AsyncInput 
            class="vertical-top score-input"
            value={element.rank.score}
            inputType="number"
            disabled={!element.canEdit}
            buttonTitle={element.canEdit ? undefined : `cannot edit ${element.prefix} scores`}
            onWrite={(next, old) => handleSaveScore(element.id, next)}
        />

      </div>

    </PageList>


    <PaginateArray 
        paginationKey="player-leaderboards"
        position="bottom"
        elements={filteredLeaderboards}
        bind:pagedElements={pagedLeaderboards}/>
{:else}
This player has no leaderboard entries
{/if}
</div>

<style lang="scss">

    .leaderboard-title {
         padding-top: 8px;
        label {
            opacity: .8;
            font-size: 12px;
        }
        flex-grow: 1;
    }
    .rank {
        display: flex;
        flex-direction: column;
        text-align: center;
        
        span.bottom {
            border-top: solid 1px white;
            padding: 3px 6px;
        }
        
    }

    .player-leaderboard {
        position: relative;
       
    }
    .modal.add-leaderboard {
        *[slot="body"] {
            display: flex;
            .field:nth-child(2){
                flex-grow: 1;
            }
        }
    }
    
    .elem > * {
        align-self: flex-start;
    }
    :global(.player-leaderboard .elem .score-input) {
        width: 100px;
        flex-grow: 0 !important;
    }
    .value {
       
        input.input {
            text-overflow: ellipsis;
            white-space: nowrap;
            overflow: hidden;
        }
        textarea {
            max-height: 500px;
            padding-left: 12px;
        }
        input, textarea {
            margin: 0;
            max-width: 140px;
        }
    }
    .expand-buttons {
        align-self: flex-start;
        
        // width: 152px;
        padding: 12px;
        display: flex;
        .button {
            padding: 0px;
            min-width: 32px;
            margin: 4px;
        }
    }
</style>