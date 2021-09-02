<script>
    import { onDestroy, onMount } from 'svelte';

    let self;
    export let right=0;

    onMount(() => {
        if (window.getComputedStyle(self.parentElement).position !== 'relative'){
            if (window.config.dev){
                throw 'CommandButtons must exist under a relative component';
            } else {
                console.error('command buttons must exist under a relative component');
            }
        }
    });

</script>

<style lang="scss">
    :global(.action-buttons .button.trigger-button) { 
        padding: 0;
        background: #454545;
        color: #c5c5c5;
        border: gray;
        width: 156px;
    }

    :global(.action-buttons span.icon:first-child:not(:last-child)){
        margin-right: 12px;
    }
    
    .action-buttons {
        position: absolute;
        top: -50px;
    }
</style>

<div class="field is-grouped action-buttons" bind:this={self} style="right: {right}px;">
    <slot>
        <!-- Plug in buttons here... -->
    </slot>
</div>