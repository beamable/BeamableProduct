<script lang="ts">
    export let name: String="unset";
    export let value: number = 0;
</script>
<div>
    <form>
        <label>{name}
        </label>
        <input type="number" bind:value={value}>
    </form>
</div>