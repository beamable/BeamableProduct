<script lang="ts">
    import {x} from './lib';
    import {sqrt} from 'mathjs';
    import Doodad from './Doodad.svelte';

    let world: number = 32;
    let a: number = 0;
    let b: number = 0;
    function doop(){
        world = a + b;
    }
    function square(){
        world = sqrt(world);
    }

</script>
<div>
    Sum {world}
    <button on:click={doop}>
        Add It Up
    </button>
    <button on:click={square}>
        Sqrt
    </button>

    <Doodad name="test1" bind:value={a}/>
    <Doodad name="test2" bind:value={b}/>
</div>
