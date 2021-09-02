<script>
    import { onMount } from 'svelte';
    import { getServices } from '../services';
    import ScrollLink from './ScrollLink';

    export let title = "Information";
    export let loadingHeight = 88;
    export let isPanel = true;
    export let link = undefined;
    export let data = undefined;

    const { http: {isResponseUnavailable} } = getServices();

    let self;
    $: isLoading = !data;
    $: unavailable = isResponseUnavailable(data);

    $: className = isLoading ? 'de-loader' : '';
    $: display = isLoading ? 'none': 'block';
    $: forcedHeight = (isLoading && !unavailable) ? loadingHeight: 0;

</script>

<style>
    .panel.has-background {
        padding: 12px;
        
    }
    
    .panel:not(.panel.de-loader){
        background: none;
    }

    .panel.has-background:not(.de-loader){
        background: #4d4d4d;
    }
    .panel.de-loader {
        background: repeating-linear-gradient( 33.2deg, #cecece, #cecece 10px, #dbdbdb 10px, #dbdbdb 20px );
        animation: mymove 3s infinite linear;
    }
    @keyframes mymove {
        0% {
            background-position: 0px 0px;
            opacity: .4;
        }
        25% {
            opacity: .6;

        }
        50% {
            opacity: .4;
        }
        75% {
            opacity: .6;

        }
        100% {
            background-position: 78px 0px;
            opacity: .4;
        }
    }
</style>

<br>

{#if title && title.length}
    
    <ScrollLink link={link || title}>
        <h3 class="title" style="margin-bottom: 20px;"> {title} </h3>
    </ScrollLink>
    
{/if}

<div class="panel svelte-ssueq4"
    class:de-loader={isLoading && !unavailable}
    class:has-background={isPanel}
    class:unavailable={unavailable}
    style="min-height: {forcedHeight}px" >
    {#if unavailable}
        <slot name="unavailable">
            <article class="message is-danger">
                <div class="message-body">
                    <strong> Resource Unavailable </strong>
                    <br>
                    The {title} resources are not available.
                </div>
            </article>
        </slot>
    {:else}
        <div style="display: {display}" >
            <slot>
                <span> - </span>
            </slot>
        </div>
    {/if}
</div>
