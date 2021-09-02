<script context="module">
    let closeFunctions = [];

    function closeAll(){
        closeFunctions.forEach(f => f());
    }
    function register(closingFunction){
        closeFunctions = [...closeFunctions, closingFunction];
    }
    function unregister(closingFunction){
        closeFunctions = closeFunctions.filter(f => f != closingFunction);
    }
    
</script>

<script>
    import FeatherIcon from './FeatherIcon';
    import { onMount, onDestroy } from 'svelte';

    export let value;
    export let title;
    export let inputType = 'text';
    export let onWrite = (next, old) => next;
    export let placeholder="Enter value";
    export let editable = true;
    export let disabled = false;
    export let buttonTitle = undefined;
    export let onValidate = (val) => null;
    export let floatError=false;
    export let buttonTopPadding=12; // TODO: eventually, we should factor this out and support alignment through classes, or just have a generalized css solution

    let isNetworking = false;
    let nextValue;
    let inputLine;
    let errorMessage;

    $: if (inputLine) {
        inputLine.focus();
        inputLine.select();
    }

    $: errorMessage = nextValue ? onValidate(nextValue) : null;

    onMount(() => {
        register(endEdit);
    });
    onDestroy(() => {
        unregister(endEdit);
    });

    function startEdit(){
        closeAll();
        nextValue = value; 
    }

    function endEdit(){
        nextValue = undefined;
    }

    async function saveEdit(){
        isNetworking = true;
        if (errorMessage) return;

        try {
            errorMessage = undefined;
            value = await onWrite(nextValue, value)
            closeAll();

        } catch (err){
            if (err && err.message){
                errorMessage = err.message;
            }
            else if (err){
                errorMessage = err;
            } else {
                errorMessage = 'unknown error';
            }
        } finally {
            isNetworking = false;
        }
    }

</script>

<style lang="scss">
    .network-input {
        flex-grow: 1;
    }
    .vertical-top {
       padding: 10px 0px;
       align-self: flex-start;
    }
    .network-buttons {
        min-width: 92px;
        padding-left: 12px;
        padding-right: 12px;
        align-self: flex-start;
        .button {
            padding: 0px;
            min-width: 32px;
            margin: 0px;
        }
    }
    .help.is-danger.above {
        position: absolute;
        z-index: 10;
        top: -26px;
        left: 4px;
        font-size: 12px;
    }
    label.label {
        color: rgba(255, 255, 255, .7);
        font-size: 12px;
    }

</style>


<div class="network-input {$$props.class || ''}">
    {#if title}
        <label class="label">{title}</label>
    {/if}
    {#if nextValue !== undefined}
        <form on:submit|preventDefault={evt => saveEdit()}>
            <div class="control has-icons-right" class:is-loading={isNetworking} class:is-danger={errorMessage}>
                {#if inputType === 'number'}
                    <input class="input" type="number"
                        class:is-danger={errorMessage}
                        placeholder={placeholder} 
                        bind:value={nextValue} 
                        bind:this={inputLine} 
                        readonly={isNetworking}
                    >
                {:else if inputType === 'email'}
                    <input class="input" type="email"
                        class:is-danger={errorMessage}
                        placeholder={placeholder} 
                        bind:value={nextValue} 
                        bind:this={inputLine} 
                        readonly={isNetworking}
                    >
                {:else}
                    <input class="input" type="text" 
                        class:is-danger={errorMessage}
                        placeholder={placeholder} 
                        bind:value={nextValue} 
                        bind:this={inputLine} 
                        readonly={isNetworking}
                    >
                {/if}
                {#if errorMessage}
                    <span class="icon is-small is-right">
                        <FeatherIcon class="icon is-small is-right" icon="alert-circle" stroke="#ff5c5c" style="padding: 8px"/>
                    </span>
                    <p class="help is-danger" class:above={floatError}>{errorMessage}</p>
                {/if}
            </div>
        </form>
    {:else}
        <div class="control">
            <input class="input" class:is-static={editable !== true} type="text" placeholder={placeholder} readonly bind:value={value} >
        </div>
    {/if}

</div>
<div class="network-buttons" style="padding-top: {buttonTopPadding}px">

    {#if nextValue !== undefined}
        <button class="button network-button" disabled={isNetworking} on:click={evt => endEdit()}>
            <FeatherIcon icon="x"/>
        </button>
        <button class="button network-button {isNetworking ? 'is-loading': ''}" disabled={errorMessage} on:click={evt => saveEdit()}>
            <FeatherIcon icon="check"/>
        </button>
    {:else if (editable === true || disabled)}
        <button class="button network-button" title={buttonTitle} disabled={disabled} on:click={evt => !disabled && startEdit()}>
            <FeatherIcon icon="edit-2"/>
        </button>
    {/if}

</div>
