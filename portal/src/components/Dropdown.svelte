<script>
  import {writable} from 'svelte/store';
  import Toggle from './Toggle';
  const toggle$ = writable();

  export let disabled = false;
</script>

<svelte:window on:mouseup={$toggle$} />

<Toggle active={$$props.active} let:toggle let:active>
  {(toggle$.set(toggle), '')}
  <div {...$$props} class:is-active={active} on:click|stopPropagation>
    {#if !disabled}
      <span on:click={toggle}>
        <slot name="trigger" {toggle} {active} />
      </span>

      <slot name="drop" {toggle} {active}>
        <slot {toggle} {active} />
      </slot>
    {:else}
      <span>
        <slot name="trigger" {toggle} {active} />
      </span>
    {/if}
  </div>
</Toggle>
