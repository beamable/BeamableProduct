<script>
    import { onDestroy, onMount } from 'svelte';
    import { getServices } from '../services';

    const {
        auth: {
            currentRole,
            isLoggedIn
        }
    } = getServices();

    export let roles=['admin'];
    export let emptyNoAuth;

    let hasAccess = false;
    let isLoading = false;

    $: hasAccess = $isLoggedIn && roles.indexOf($currentRole) > -1;
    $: isLoading = $isLoggedIn && $currentRole === undefined;

</script>

{#if isLoading}
    <slot>
        <!-- TODO: Maybe we should show some loading spinner here? -->
        <!-- PROTECTED CONTENT. -->
    </slot>
{:else if hasAccess}
    <slot>
        <!-- PROTECTED CONTENT. -->
    </slot>
{:else}
    {#if !emptyNoAuth}
        <slot name="unauthorized">
            <div>
                Sorry, your account doesn't have the required privledges to see this content.
            </div>
        </slot>
    {/if}
{/if}