<script>
  import FilterSearch from './FilterSearch';
  import PaginateArray from './PaginateArray';
  import PageList from './PageList';
  import AsyncInput from './AsyncInput';
  import { getServices } from '../services';

  export let statuses;

  let entries = [];
  let filteredEntries = [];
  let pagedEntries = [];

  $: entries = statuses == null ? [] : [...statuses]

  let isNetworking = false;

  const { tournaments } = getServices();

  async function updatePlayerStatus(status, updates, next) {
    await tournaments.updatePlayerStatus(status, updates);
    return next;
  }

  function onFlagCreate(node, element) {
    // TODO: We need to change the color based on the tournament rank change schema.
    // Tier on left, rank on right.
  }

</script>

<div class="player-tournaments">
  <FilterSearch
    placeholder="Search by contentId"
    allElements={entries}
    filterOn="contentId"
    bind:value={filteredEntries}
  />

  {#if filteredEntries.length == 0}
    <div class="no-tournaments-message">
      No tournaments found
    </div>
  {:else}
    <PaginateArray
      paginationKey="tournaments"
      position="top"
      elements={filteredEntries}
      bind:pagedElements={pagedEntries}/>

    <PageList elements={pagedEntries}
      leftFlagClass={e => 'space'}
      rightFlagClass={e => 'space'}
      onFlagCreation={onFlagCreate}>

      <div slot="element" let:element class="big-row">
        <div class="header">
          <div class="field">
            <label class="label">Tournament</label>
            {element.contentId}
          </div>

          <div class="field">
            <label class="label">Next Cycle Start Time</label>
            {new Date(element.nextCycleStartMs).toString()}
          </div>
        </div>

        <div class="content">
          <div class="left">

            <div class="field">
              <label class="label">Tier</label>
              <div class="async-input">
                <AsyncInput
                  value={element.tier}
                  inputType="number"
                  onWrite={ (next, old) => updatePlayerStatus(element, {tier: next}, next) }
                  buttonTopPadding={4}
                />
              </div>
            </div>

            <div class="field">
              <label class="label">Stage</label>
              <div class="async-input">
                <AsyncInput
                  value={element.stage}
                  inputType="number"
                  onWrite={ (next, old) => updatePlayerStatus(element, {stage: next}, next) }
                  buttonTopPadding={4}
                />
              </div>
            </div>

          </div>
          <div class="right">

            <div class="field">
              <label class="label">Rank</label>
              <div class="async-input">
                <AsyncInput
                  value={element.rank}
                  inputType="number"
                  editable={false}
                  buttonTopPadding={4}
                />
              </div>
            </div>

            <div class="field">
              <label class="label">Score</label>
              <div class="async-input">
                <AsyncInput
                  value={element.score}
                  inputType="number"
                  onWrite={ (next, old) => updatePlayerStatus(element, {score: next}, next) }
                  buttonTopPadding={4}
                />
              </div>
            </div>

          </div>
        </div>
    </PageList>

    <PaginateArray
      paginationKey="tournaments"
      position="bottom"
      elements={filteredEntries}
      bind:pagedElements={pagedEntries}/>
  {/if}
</div>

<style lang="scss">

  .big-row {
    min-height: 200px;
    flex-direction: column;

    > * {
      align-self: stretch;
    }

    .header {
      display: flex;
      flex-direction: row;
      justify-content: space-between;

      .field {
        align-self: flex-start;
      }

      > :last-child {
        text-align: right;
        padding-right: 12px;
      }
    }

    .content {
      display: flex;
      flex-direction: row;
      .left, .right {
        flex-direction: column;
        flex-grow: 1;
      }
    }

    .field {
      align-self: stretch;
    }
  }

  .no-tournaments-message {
    text-align: center;
    border-bottom: solid 1px white;
    padding-bottom: 200px;
    margin-top: 120px;
  }

  .async-input {
    display: flex;
    flex-direction: row;
  }

  .player-tournaments {
    position: relative;
  }
</style>