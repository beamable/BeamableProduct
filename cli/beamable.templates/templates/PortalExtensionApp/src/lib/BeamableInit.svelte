<script>
  import { Beam } from 'beamable-sdk';

  let loading = false
  let error = null
  let playerData = null

  async function init(){
    loading = true;
    error = null;

    try{
      const beam = await Beam.init({
        cid: 'a-real-cid',
        pid: 'a-real-pid',
        environment: 'dev', //or prod, or empty
      });

      playerData = {
        cid: beam.cid,
        pid: beam.pid,
        playerId: beam.player.id
      }
    } catch (err) {
      console.error('Failed to init player:', err);
      error = err.message;
    } finally {
      loading = false;
    }
  }

</script>

<div class="container">
  <div class="actions">
    <button on:click={init} disabled={loading || playerData}>
      {#if loading}
        Initializing...
      {:else if playerData}
        Player Loaded
      {:else}
        Initialize Player
      {/if}
    </button>

    {#if error}
      <p class="error">{error}</p>
    {/if}
  </div>

  {#if playerData}
    <table class="data-table">
      <thead>
      <tr>
        <th>Key</th>
        <th>Value</th>
      </tr>
      </thead>
      <tbody>
      <tr>
        <td><strong>CID</strong></td>
        <td>{playerData.cid}</td>
      </tr>
      <tr>
        <td><strong>PID</strong></td>
        <td>{playerData.pid}</td>
      </tr>
      <tr>
        <td><strong>Player ID</strong></td>
        <td>{playerData.playerId}</td>
      </tr>
      </tbody>
    </table>
  {:else}
    <p class="placeholder">Click the button to load player data.</p>
  {/if}
</div>

<style>
  .container {
    font-family: sans-serif;
    padding: 1rem;
    max-width: 500px;
    border: 1px solid #ccc;
    border-radius: 8px;
  }

  .actions {
    margin-bottom: 1.5rem;
  }

  button {
    background-color: #007bff;
    color: white;
    border: none;
    padding: 0.5rem 1rem;
    font-size: 1rem;
    border-radius: 4px;
    cursor: pointer;
  }

  button:disabled {
    background-color: #ccc;
    cursor: not-allowed;
  }

  .error {
    color: red;
    margin-top: 0.5rem;
    font-size: 0.9rem;
  }

  .data-table {
    width: 100%;
    border-collapse: collapse;
    margin-top: 10px;
  }

  .data-table th, .data-table td {
    text-align: left;
    padding: 8px;
    border-bottom: 1px solid #ddd;
  }

  .data-table th {
    background-color: #f4f4f4;
  }

  .placeholder {
    color: #666;
    font-style: italic;
  }
</style>