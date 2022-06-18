<script lang="ts">
    import {x} from './lib';
    import {sqrt} from 'mathjs';

    let world: number = 32;
    function doop(){
        world = world + x();
        tuna();
    }
    function square(){
        world = sqrt(world);
    }

</script>
<div>
    Hello {world}
    <button on:click={doop}>
        Click it up!
    </button>
    <button on:click={square}>
        Sqrt
    </button>
</div>
