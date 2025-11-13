<script>
  import beamLogo from './assets/beam_icon_small_color.png'
  import BeamableInit from './lib/BeamableInit.svelte'
</script>

<main>
  <div class="title">
    <a href="https://beamable.com/" target="_blank" rel="noreferrer">
      <img src={beamLogo} class="logo beam" alt="Beam Logo" />
    </a>
    <h1>Beamable</h1>
  </div>
  

  <div class="card">
    <BeamableInit />
  </div>

  <p class="read-the-docs">
    Click on the Beamable logo to learn more!
  </p>
</main>

<style>

  .title {
    display: flex;
    align-items: center;
    gap: 1rem;
  }

  .card {
    align-self: center;
  }

  .logo {
    height: 6em;
    padding: 1.5em;
    will-change: filter;
    transition: filter 300ms;
  }
  .logo.beam:hover {
    filter: drop-shadow(0 0 2em #ff3e00aa);
  }
  .read-the-docs {
    color: #888;
    align-self: center;
  }
</style>
