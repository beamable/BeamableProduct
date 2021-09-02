<script>
  import { prompt } from '../lib/browser-utils';
  import Navigate from '../components/Navigate';

  import { getServices } from '../services';

  const services = getServices();

  const { orgId } = services.router;

  let cid = '';

  function commitCid(){
    console.log('commiting', cid);
    orgId.set(cid);
  }
  
</script>

<!-- <style src="./index.scss"></style> -->

{#if $orgId}
  <Navigate replace href="/{$orgId}" />
{:else}

  <div>
    <div>
      you need a CID. Please find it in your config-defaults.txt file.
    </div>
    <input class="input text" type="text" placeholder="CID" bind:value={cid}>
    <button class="button primary" on:click={evt => commitCid()}>
      Okay
    </button>
  </div>

{/if}

