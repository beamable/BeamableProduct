<style>
  .tabs {
    margin-bottom: 0;
  }

  .tabs :global(a) {
    padding-bottom: .25em;
  }

  .tabs :global(ul) {
    border-bottom-color: transparent;
  }

  .panel {
    background-color: #454545;
  }
</style>

<div class="tabs is-centered is-boxed">
  <slot name="tabs" />
</div>

<div class="panel">
  <slot name="content">
    <slot />
  </slot>
</div>
