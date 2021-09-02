<script>
  export let active = false;

  function toggle(state) {
    if (state && state.currentTarget === window) {
      active = false;
    } else {
      active = typeof state === 'boolean' ? state : !active;
    }
  }
</script>

<slot {toggle} {active} />
