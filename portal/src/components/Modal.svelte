<script>
  import Toggle from './Toggle';
</script>

<Toggle active={$$props.active} let:toggle let:active>
  <span on:click={toggle}>
    <slot name="trigger" {toggle} {active} />
  </span>

  <div {...$$props} class="modal {$$props.class || ''}" class:is-active={active}>
    <slot name="background">
      <div class="modal-background" on:click={toggle}></div>
    </slot>
    <slot name="content" {toggle} {active}>
      <div class="modal-content">
        <slot {toggle} {active} />
      </div>
    </slot>
  </div>
</Toggle>
