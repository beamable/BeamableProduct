<script>
  import Tabs from './_tabs';
  import Card from '../../../../../../components/Card';
  import PageList from '../../../../../../components/PageList';
  import FilterSearch from '../../../../../../components/FilterSearch';
  import Leaderboard from '../../../../../../components/Leaderboard';
  import RoleGuard from '../../../../../../components/RoleGuard';

  import { getServices } from '../../../../../../services';
  const { realms: { realm }, http, leaderboards } = getServices();

  let allLeaderboards;
  let filteredLeaderboards = [];
  let selectedLeaderboard;

  const unsubCurrent = leaderboards.currentLeaderboard.subscribe(value => {
    selectedLeaderboard = value;
  });

  const unsubList = leaderboards.leaderboardList.subscribe(value => {
    if (value){
      allLeaderboards = value.nameList.map(id => {
        const prefix = id.substr(0, id.indexOf('.') + 1)
        var displayType = prefix.substr(0, prefix.length - 1);
        if (displayType === 'event_events'){
            displayType = 'events';
        }
        return {
          prefix: displayType,
          canView: id.indexOf('#') < 0,
          name: id.substr(prefix.length),
          id,
        }
      });
    }
  });

  async function selectLeaderboard(leaderboard){
    leaderboards.currentLeaderboard.set(leaderboard);
  }

</script>

 {#if $realm}
  <Tabs activeTab="leaderboards">

  </Tabs>
{/if}

<RoleGuard roles={['admin', 'developer']}>

<Card title="Leaderboards" data={allLeaderboards} isPanel={false}>
  <FilterSearch
      placeholder="Search..."
      allElements={allLeaderboards}
      filterOn="name"
      filterFunctions={[
                {func: (m) => true, name: 'All'},
                {func: (m) => m.id.startsWith('leaderboards.'), name: 'Leaderboards'},
                {func: (m) => m.id.startsWith('tournaments.'), name: 'Tournaments'},
                {func: (m) => m.id.startsWith('event_events.'), name: 'Events'},
       ]}
      bind:value={filteredLeaderboards}
      >
  </FilterSearch>

  <PageList
    elements={filteredLeaderboards}
    >
      <div slot="element" let:element class="elem">
        <div class="leaderboard-name">
            <label>
                {element.prefix}
            </label>
            <div>
                {element.name}
            </div>
        </div>
        <div>
          <button class="button" title={element.canView ? undefined : 'cannot show scores for partitioned leaderboards'} disabled={!element.canView} on:click={evt => selectLeaderboard(element)}>
            View
          </button>
        </div>
      </div>
  </PageList>
</Card>

{#if !selectedLeaderboard}
  <div>
    Select a leaderboard to inspect its details
  </div>
{:else}
  <Card title={selectedLeaderboard.name || ''} link="Leaderboard" data={selectedLeaderboard} isPanel={false}>
    <Leaderboard leaderboard={selectedLeaderboard}/>
  </Card>

{/if}
</RoleGuard>


<style lang="scss">
  .panel-block {
    color: white;
  }

  .elem {
    .leaderboard-name {
      flex-grow: 1;
      label {
        opacity: .8;
        font-size: 12px;
      }
    }
  }

</style>
