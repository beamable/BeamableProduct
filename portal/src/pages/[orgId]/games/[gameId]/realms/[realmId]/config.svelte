<script>
  import Tabs from './_tabs';
  import { getServices } from '../../../../../../services';
  const { realms: { realm }, http } = getServices();

  function setTab(tab) {
    activeTab = tab || 'all';
  }

</script>

{#if $realm}
  <Tabs activeTab="config">
    <div class="panel-block">
      <div>
        Hello world
      </div>
    </div>

  </Tabs>
{/if}
