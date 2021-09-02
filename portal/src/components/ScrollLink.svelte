<script context="module">
    let tickFunctions = [];
    let navigateFunctions = [];

    function tickAll(){
        tickFunctions.forEach(f => f());
    }
    function register(tickFunction, navigateObj){
        tickFunctions = [...tickFunctions, tickFunction];
        navigateFunctions = [...navigateFunctions, navigateObj]
    }
    function unregister(tickFunction, link){
        tickFunctions = tickFunctions.filter(f => f != tickFunction);
        navigateFunctions = navigateFunctions.filter(f => f.link != link);
        
    }

    export function navigateTo(link){
        const matching = navigateFunctions.filter(f => f.link === link)
        const first = matching[0];
        first.navigate();
    }

</script>

<script>
    import { onMount, onDestroy } from 'svelte';


    export let link=''; // TODO: make link non case sensitive.
    export let offset=-110;
    export let onArrived=() => {};
    export let onTick=() => {};

    let updateRate = 16;

    function handleClick(){
        scrollTo(link);
    }
    function runTick(){
        onTick();
    }
    onMount(() => {
        // when the component wakes up, steal control.
        var hashId = location.hash.substr(1); // strip the preceding '#';
        if (hashId && hashId === link){
            scrollTo(hashId);
        }
        register(runTick, {link, navigate: () => scrollTo(link)})
    });
    onDestroy(() => {
        unregister(runTick, link);
    });

    function scrollTo(id) {

        if (!id || !id.length){
            // scroll to top of page.
            scrollToTop();
            return;

        }

        const element = document.getElementById(id);
        if (element) {
            scrollToResolver(element);
        }
    }

    function scrollToTop(){
        document.body.scrollTop *= .9;
        document.documentElement.scrollTop *= .9;
        tickAll();
        if (document.documentElement.scrollTop || document.body.scrollTop > 1){
            setTimeout(() => {
                scrollToTop()
            }, updateRate);
        } else {
            window.location.replace("#");
            onArrived();
        }
    }

    function scrollToResolver(elem) {
        var jump = (elem.getBoundingClientRect().top+offset) * .2;
        document.body.scrollTop += jump;
        document.documentElement.scrollTop += jump;
        tickAll();
        if (!elem.lastjump || elem.lastjump > Math.abs(jump) + 1) {
            elem.lastjump = Math.abs(jump);
            setTimeout(() => {
                scrollToResolver(elem);
            }, updateRate);
        } else {
            window.location.replace(`#${link}`);
            document.body.scrollTop += offset ;
            document.documentElement.scrollTop += offset;
            onArrived();
            elem.lastjump = 0;
        }
    }
</script>

<div class="scroll-link" id={link} on:click={evt => handleClick()} style="display:inline-block">
    <slot>
        <!-- ??? -->
    </slot>
</div>

<style lang="scss">
    .scroll-link {
        cursor: pointer;
    }
</style>