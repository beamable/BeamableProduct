<script>
  //import Toggle from './Toggle';
  import FeatherIcon from './FeatherIcon';

  export let active = false;
  export let onClose = () => {};

  let loading = false;

  function toggle(state) {
    if (state && state.currentTarget === window) {
      active = false;
    } else {
      active = typeof state === 'boolean' ? state : !active;
    }

    if (!active){
      onClose();
    }
  }
</script>

<style lang="scss">
  @import 'partials/_layout-colors.scss';
    .card {
      background: white;
      border: none;

      .card-heading {
        border:none;
        background: #191919;
        text-transform: uppercase;
        justify-content: center;
        position: relative;
        .close-wrap {
          position: absolute;
          right: 0;
          top:0;
          bottom: 0;
        }
      }

      .card-body {
        display: flex;
        flex-direction: column;
        text-align: center;
        padding-top: 24px;
        color: white;
        background: rgb(69, 69, 69);

        .button-container {
          display: flex;
          flex-direction: row;
          justify-content: center;
          width: 100%;
          :global(.button) {
            text-transform: uppercase;
            margin: 12px;
          }
        }
      }
    }
    :global(h3){
      color: white;
    }
</style>

<slot name="trigger-evt" toggle={toggle} active={active}>
  <span on:click={toggle}>
    <slot name="trigger" toggle={toggle} active={active} />
  </span>
</slot>

<div {...$$props} class="modal {$$props.class || ''}" class:is-active={active}>
  <slot name="background">
    <div class="modal-background" on:click|preventDefault|stopPropagation={toggle}></div>
  </slot>

  <slot name="content" toggle={toggle} active={active}>
    <div class="modal-content" on:click|stopPropagation={e => {}}>
      <div class="card">
        <div class="card-heading">
          <slot name="title">
            <h3> Confirm </h3>
          </slot>
          <div class="close-wrap" on:click|preventDefault|stopPropagation={toggle}>
            <span class="close-modal">
              <FeatherIcon icon="x" />
            </span>
          </div>
        </div>

        <div class="card-body">
          <slot name="body" toggle={toggle} active={active}/>
          <div class="button-container" >
            <slot name="buttons" toggle={toggle} active={active}>
                <button class="button is-success" class:is-loading={loading} on:click|preventDefault|stopPropagation={evt => handleClick()}>
                    <span>
                        <slot name="primary-button">
                            Remove
                        </slot>
                    </span>
                </button>
                <button class="button cancel is-outlined" disabled={loading} on:click|preventDefault|stopPropagation={toggle}>
                    <span>Cancel</span>
                </button>
            </slot>
          </div>
        </div>
        <slot />
      </div>
    </div>
  </slot>

</div>

