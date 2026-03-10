<script>
  import { Beam } from '../store.js'

  let loading = false
  let error = null
  export let playerData = null

  function init(){
    loading = true
    error = null

    try{

      playerData = {
        cid: $Beam.cid,
        pid: $Beam.pid,
        playerId: $Beam.player.id
      }
    } catch (err) {
      console.error('Failed to use Beamable SDK:', err)
      error = err.message
    } finally {
      loading = false
    }
  }

</script>

<v-card>

  <v-card-actions >
    <v-btn          
      on:click={init}
      disabled={loading || playerData ? true : null}
      loading={loading ? true : null}
    >
      {#if loading}
        Initializing...
      {:else if playerData}
        Player Loaded
      {:else}
        Initialize Player
      {/if}
    </v-btn>

    {#if error}
      <p class="error">{error}</p>
    {/if}
  </v-card-actions>

  {#if playerData}
    <portal-extension-widget 
      cid={playerData.cid}
      pid={playerData.pid}
      playerId={playerData.playerId}>
      
    </portal-extension-widget>
  {:else}
    <p class="placeholder">Click the button to load player data.</p>
  {/if}
</v-card>

<style>
  .error {
    color: red;
    margin-top: 0.5rem;
    font-size: 0.9rem;
  }

  .placeholder {
    color: #666;
    font-style: italic;
  }
</style>