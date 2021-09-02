<script>
    import FilterSearch from './FilterSearch';
    import FeatherIcon from './FeatherIcon';
    import Paginate from './Paginate';
    import PageList from './PageList';
    import WarningPopup from './WarningPopup';
    import { getServices } from '../services';

    export let leaderboard = undefined;
    const { changeRoute } = window.router;

    let totalEntries = 0;
    let pagedEntries = [];
    let expandedItem;
    let playerCountString = 'No Players'

    const {leaderboards, players, router} = getServices();

    $: if (totalEntries !== undefined) {
        if (totalEntries == 0){
            playerCountString = 'No Players'
        } else if (totalEntries == 1){
            playerCountString = '1 Player';
        } else {
            playerCountString = `${totalEntries} Players`;
        }
    }

    async function fetchPage(pageNumber, pageSize){
        const start = pageNumber * pageSize;
        const data = await leaderboards.fetchLeaderboardPage(leaderboard.id, start, pageSize);
        totalEntries = data.numberOfEntries;
        return data.view.rankings.map(transformEntry);
    }

    function transformEntry(entry){
        const statObject = entry.stats.reduce( (agg, curr) => ({
            ...agg,
            [curr.name]: curr.value
        }), {});

        return {
            ...entry,
            statPreview: JSON.stringify(statObject),
            statPretty: JSON.stringify(statObject, null, 2)
        }
    }


    function jumpToPlayer(dbid){
        changeRoute(`/:orgId/games/:gameId/realms/:realmId/players?playerQuery=${dbid}`);
    }

    async function clearEntry(entry){
        const res = await leaderboards.clearPlayer(entry.gt, leaderboard)
        pagedEntries = pagedEntries.filter(e => e.gt != entry.gt);
    }

    async function clearLeaderboard(){
        await leaderboards.clearLeaderboard(leaderboard);
        pagedEntries = []; // there is now no data in the leaderboard...
        totalEntries = 0;
    }

    async function deleteLeaderboard(){ 
        await leaderboards.deleteLeaderboard(leaderboard);
    }

</script>

{#if leaderboard}
    <div class="total-player-label">
        {playerCountString}
    </div>

    <div class="leaderboard">
        <div class="field is-grouped leaderboard-action-buttons">
            {#if totalEntries > 0} 
                <p class="control">
                    <WarningPopup 
                        header="Clear Leadboard"
                        message="Clearing a leaderboard will erase all players' scores. This operation cannot be undone."
                        headerClass="light-header"
                        left={25}
                        top={-75}
                        onConfirmFunction={clearLeaderboard}>
                        <button class="button trigger-button" slot="trigger" let:toggle on:click|preventDefault|stopPropagation={toggle}>
                            <span class="icon is-small">
                                <FeatherIcon icon="alert-triangle"/>
                            </span>
                            <span>
                                Clear
                            </span>
                        </button>
                        <span slot="primary-button">
                            Clear All
                        </span>
                    </WarningPopup>
                </p>
            {/if}


            <p class="control">
                <WarningPopup 
                    header="Delete Leaderboard"
                    message="Deleting a leaderboard removes the board and scores. This operation cannot be undone."
                    headerClass="light-header"
                    left={25}
                    top={-75}
                    onConfirmFunction={deleteLeaderboard}>
                    <button class="button trigger-button" slot="trigger" let:toggle on:click|preventDefault|stopPropagation={toggle}>
                        <span class="icon is-small">
                            <FeatherIcon icon="trash"/>
                        </span>
                        <span>
                            Delete Board
                        </span>
                    </button>
                    <span slot="primary-button">
                        Delete
                    </span>
                </WarningPopup>
            </p>
        </div>

        <Paginate 
            paginationKey={leaderboard.name}
            position="top"
            totalElements={totalEntries}
            bind:pagedElements={pagedEntries}
            fetchPage={fetchPage}/>

        <PageList
            headers={[
                {name: 'rank', width: '7%'},
                {name: 'dbid', width: '28%'},
                {name: 'score', width: '20%'},
                {name: 'stats', width: '30%'},
                {name: 'actions', width: '10%'},
            ]}
            elements={pagedEntries}
            >
            <div slot="element" let:element class="elem">
                <div class="rank">
                    {element.rank}
                </div>

                <div class="labeled" style="flex-grow: 1;">
                    <input class="input is-static" type="text" value={element.gt} readonly>
                </div>

                <div class="labeled" style="flex-grow: 1; max-width: 200px">
                    <input class="input is-static" type="text" value={element.score} readonly>
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

                <div class="buttons">
                    {#if expandedItem === element}
                        <button class="button" on:click={evt => expandedItem = undefined}>
                            <FeatherIcon icon="arrow-up"/>
                        </button>
                    {:else}
                        <button class="button" on:click={evt => expandedItem = element}>
                            <FeatherIcon icon="arrow-right"/>
                        </button>
                    {/if}

                    <p>
                        <button class="button" on:click={evt => jumpToPlayer(element.gt)}>
                            <FeatherIcon icon="user"/>
                        </button>
                    </p>

                    <p class="clear-wrap">
                        <WarningPopup 
                            header="Clear Score"
                            message="Clearing a player's score cannot be undone."
                            headerClass="light-header"
                            left={25}
                            top={-68}
                            onConfirmFunction={() => clearEntry(element)}>
                            <button class="button clear-leaderboard-button" slot="trigger" let:toggle on:click|preventDefault|stopPropagation={toggle}>
                                <span class="icon is-small">
                                    <FeatherIcon icon="trash"/>
                                </span>
                            </button>
                            <span slot="primary-button">
                                Clear
                            </span>
                        </WarningPopup>
                    </p>
                </div>

            </div>
        </PageList>

        <Paginate 
            paginationKey={leaderboard.name}
            position="bottom"
            totalElements={totalEntries}
            bind:pagedElements={pagedEntries}
            fetchPage={fetchPage}/>
    </div>
{/if}

<style lang="scss">

    .leaderboard {
        position: relative;
        .leaderboard-action-buttons {
            position: absolute;
            right: 0px;
            top: -50px;
            p.control {
                display:flex;
            }
            span.icon {
                margin-right: 12px;
            }
            
            .button.trigger-button {
                padding-top: 6px;
                background: #454545;
                color: #c5c5c5;
                border: gray;
                width: 156px;
            }
        
        }
    }

    .button-wrapper {
        padding-bottom:0px;
        .button {
            padding-top: 10px;
            height: 38px;
        }
    }
    .buttons {
        overflow: visible;
        white-space: normal;
    }
    :global(.clear-wrap .button.clear-leaderboard-button) {
        padding-top: 0px;
        padding-bottom: 0px;
    }
    .total-player-label {
        margin-top: -14px;
    }
    .rank, .labeled, .value {
        align-self: flex-start;
    }
    .rank {
        min-width: 50px;
        text-align: right;
        padding-top: 20px;
    }

    .value {
        flex-grow: 1;
        width: 200px;
        input.input {
            text-overflow: ellipsis;
            white-space: nowrap;
            overflow: hidden;
        }
        textarea {
            max-height: 500px;
        }
    }
    .buttons {
        align-self: flex-start;
        
        width: 152px;
        padding: 12px;
        display: flex;
        .button {
            padding: 0px;
            min-width: 32px;
            margin: 4px;
        }
    }

</style>