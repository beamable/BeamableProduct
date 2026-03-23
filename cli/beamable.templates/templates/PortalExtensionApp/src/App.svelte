<script>
  import BeamableInit from './lib/BeamableInit.svelte'
  import { Context } from './store'

  export let context

  $: Context.set(context)
</script>

<BeamableInit />