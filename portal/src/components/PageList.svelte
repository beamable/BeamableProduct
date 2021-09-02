<script>

    export let elements=[];
    export let headers=[]; // {name: "", width: ""}
    export let leftFlagClass=(elem)=>null;
    export let rightFlagClass=(elem)=>null;
    export let onFlagCreation=(node, element)=>null;
    export let flags;

    function configureRow(node, element) {
        onFlagCreation(node, element);
        return { destroy() {} };
    }

</script>

<style lang="scss">
    .flags {
        border-left: solid 12px transparent;
        border-right: solid 12px transparent;
        border-top-left-radius: 4px;
        border-bottom-left-radius: 4px;
        position: absolute;
        top:0;
        bottom:0;
    }

    .list-row {
        margin: 14px 0px;
        background:#454545;
        border-radius: 4px;

        position: relative;
        display: flex;
        flex-direction: row;
        align-items: stretch;
        min-height: 60px;


        > * {
            display: inline-block;
            &.vertical-center {
                align-self: center;
            }
            &.vertical-top {
                padding-top: 15px;
            }
        }


        .flag-spacer {
            &.has-left, &.has-right {
                padding-left: 12px;
            }
            &.has-left.has-right {
                padding-left: 24px;
            }
        }
    }


    :global(*[slot="element"]) {
        display: flex;
        flex-grow: 1;
    }
    :global(*[slot="element"] > *) {
        display: inline-block;
        padding: 12px;
        padding-right: 0px;

        &:last-child {
            padding-right: 12px;

        }

        text-overflow: ellipsis;
        white-space: nowrap;
        overflow: hidden;

        align-self: center;
        &.vertical-top {
            padding-top: 15px;
        }


    }

    :global(.underline) {
        border-bottom: solid 3px;
        border-color: inherit;
        padding: 2px 4px;
        text-transform: capitalize;
        vertical-align: top;
    }


    :global(input){
        text-overflow: ellipsis;
        white-space: nowrap;
        overflow: hidden;
    }
    .page-list-header {
        display: flex;
        margin-bottom: -10px;
        padding-top: 10px;

        .page-list-header-item {
            color: grey;
            text-align: center;
        }
    }


</style>

<div class="page-list">

    {#if headers.length > 0 && elements.length > 0}
        <div class="page-list-header">
            {#each headers as header}
                <div class="page-list-header-item" style="width: {header.width || '10%'};">
                    {header.name}
                </div>
            {/each}
        </div>
    {/if}

    {#each elements as element, index }
        <div class="list-row">
            <div use:configureRow={element} class="flags {flags} {leftFlagClass(element)} {rightFlagClass(element)}"></div>
            <div class="flag-spacer"
                class:has-left={![null, undefined].includes(leftFlagClass(element))}
                class:has-right={![null, undefined].includes(rightFlagClass(element))}>
            </div>
            <slot name="element" {element} {index}>
                {element}
            </slot>
        </div>
    {/each}

</div>
