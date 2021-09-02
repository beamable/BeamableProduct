
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

    document.addEventListener('click', closeAll);

</script>

<script>
    import { onMount, onDestroy, afterUpdate} from 'svelte';
    import FeatherIcon from './FeatherIcon';

    export let active = false;
    export let onConfirmFunction = () => { };
    export let left=85;
    export let top=-75;
    export let headerClass=""
    export let header="WARNING"
    export let message="This operation cannot be undone."

    let loading = false;

    function toggle(){
        const wasActive = active;
        closeAll();
        active = !wasActive;
    }

    function handleDocClick() {
        closeAll();
    }

    function handleClose(){
        active = false;
    }

    async function handleClick(){
        loading = true;
        try {
            await onConfirmFunction();
        } finally {
            loading = false;
            active = false;
        }
    }

    onMount((x,y,z) => {
        register(handleClose);
    });
    onDestroy(() => {
        unregister(handleClose);
    });


    let hookNode;
    let dropNode;
    let triangleClass = 'tri-low';
    function createHook(node){
        hookNode = node;
        return {
            destroy() {
                // clean up.
            }
        };
    }

    function createDropdown(node){
        dropNode = node;
        return {
            destroy() {
                // clean up.
            }
        };
    }

    afterUpdate(() => {
        var hookRect = hookNode.getBoundingClientRect();
        var dropRect = dropNode.firstElementChild.getBoundingClientRect();

        var verticalOffset = 10;
        var triangleHeight = 20;
        var x = (hookRect.width * .5) -(dropRect.width * .5);
        var y = -(dropRect.height + hookRect.height + verticalOffset);

        triangleClass = 'tri-low';
        if (hookRect.y - (dropRect.height + verticalOffset) < 0){
            y = hookRect.height + verticalOffset + triangleHeight;
            triangleClass = 'tri-top';
        }
        
        left = x;
        top = y;
	});

</script>

<style lang="scss">
    .dropdown-content {
        display: flex;
        flex-direction: column;
    }

    .dropdown-menu .dropdown-content {
        background: white;
        padding: 0px;
        min-width: 300px;
        /* min-height: 200px; */
    }
    .dropdown-content .dropdown-title {
        background: #191919;
        text-transform: uppercase;
        text-align: center;
        padding: 8px 0px;

        &.light-header {
            background: #333236;
        }
    }
    .dropdown-content .dropdown-main {
        flex-grow: 1;
        display: flex;
        flex-direction: column;
    }
    .dropdown-main .warning {
        margin-top: 18px;
        padding: 0px 12px;        
    }

    .dropdown-content button {
        text-transform: uppercase;
        margin: 12px;
    }

    .dropdown-content button.x-button {
        position: absolute;
        right: 8px;
        padding: 0;
        margin: 0;
        background: none;
        border: none;
        color: white;
        cursor: pointer;
    }

    .dropdown-menu {
        padding-top: 0px;
        z-index: 100;
    }
    .dropdown-menu:before {
        content: ' ';
        position: absolute;
        z-index: 100;
    }

    .dropdown-menu.tri-top:before {
        left: calc(50% - 10px);
        top: -20px;
        border-left: solid 8px transparent;
        border-bottom: solid 20px #333236;
        border-top: solid 0px transparent;
        border-right: solid 8px transparent;
    }

    .dropdown-menu.tri-low:before{
        left: calc(50% - 10px);
        bottom: -20px;
        border-left: solid 8px transparent;
        border-top: solid 20px white;
        border-bottom: solid 0px transparent;
        border-right: solid 8px transparent;
    }

    .button.cancel:hover {
        background: #eaeaea;
    }

    :global(.warning-message) {
        color: red;
    }
    .warning-message {
        
        :global(strong) {
            color: red;
        }
    }

</style>
<span style="position: relative;">
<span use:createHook> 
    <slot name="trigger" {toggle}>
        <button class="button no-select" >
            Toggle
        </button>
    </slot>
</span>

<slot name="popup">
    <div class="dropdown " class:is-active={active} style="position: absolute; left:{left}px; top:{top}px;" use:createDropdown>
        <div class="dropdown-menu {triangleClass}" role="menu" on:click|stopPropagation|preventDefault={evt => {}} >
            <div class="dropdown-content">
                <div class="dropdown-title {headerClass}" style="position:relative;">
                    <strong style="color: white;"> 
                        <slot name="title">
                            {header}
                        </slot>
                    </strong>

                    <button class="x-button" on:click|preventDefault={handleDocClick}>
                        <FeatherIcon icon="x"/>
                    </button>
                </div>
                <div class="dropdown-main">
                    <div style="text-align: center" class="warning is-danger">
                        <span class="warning-message">
                            <slot name="message">
                                <strong class="is-danger">WARNING</strong> {message}
                            </slot>
                        </span>
                    </div>

                    <div style="display: flex; flex-direction: row; justify-content: center">
                        <slot name="buttons">
                            <button class="button is-danger" class:is-loading={loading} on:click|preventDefault|stopPropagation={evt => handleClick()}>
                                <span>
                                    <slot name="primary-button">
                                        Remove
                                    </slot>
                                </span>
                            </button>
                            <button class="button cancel is-outlined" disabled={loading} on:click|preventDefault={handleDocClick}>
                                <span>Cancel</span>
                            </button>
                        </slot>
                    </div>
                </div>
            </div>
        </div>
    </div>
</slot>
</span>
