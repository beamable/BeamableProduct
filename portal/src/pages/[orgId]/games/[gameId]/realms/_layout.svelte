<script>
  import { getServices } from '../../../../../services';
  const { realms } = getServices().realms;
</script>

{#if $realms}
  <slot />
{/if}

