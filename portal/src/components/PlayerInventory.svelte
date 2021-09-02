<script>
    import FilterSearch from './FilterSearch';
    import FeatherIcon from './FeatherIcon';
    import ModalCard from './ModalCard';
    import PaginateArray from './PaginateArray';
    import AsyncInput from './AsyncInput';
    import ComponentButtons from './ComponentButtons';
    import WarningPopup from './WarningPopup';
    import PropertySet from './PropertySet';
    import { onDestroy, onMount } from 'svelte';

    import { getServices } from '../services';

    export let player;
    export let inventory;

    const services = getServices();
    const content = services.content;
    const inventoryService = services.inventory;
    const currencyStore = content.currencies;
    const itemStore = content.items;

    let currencies = [];
    let items = [];

    let entries = [];
    let filteredEntries = [];
    let pagedEntries = [];
    $: entries = inventory == null ? [] : [
        ...inventory.currencies.map(mapCurrencyToEntry),
        ...inventory.items.flatMap(mapItemToEntry)];
    $: if (entries){
        entries.sort((a, b) => a.sortingKey > b.sortingKey ? -1 : 1);
    }

    let isNetworking = false;
    let expandedItem;
    let jsontext;
    let currencyToAdd, itemToAdd;
    let currencyAmountToAdd;

    let updateItem;

    let newProperties = []
    let updateProperties = []
    let newPropertyName, newPropertyValue;
    
    onMount( () => {
        currencyAmountToAdd = 0;
    })


    $: if(jsontext) {
        jsontext.style.height = jsontext.scrollHeight + 'px';
    }

    const unsubCurrency = content.currencies.subscribe(value => {
        currencies = value;
        if (currencies.length > 0) {
            currencyToAdd = currencies[0].id;
        }
    });
    const unsubItem = content.items.subscribe(value => {
        items = value;
        if (items.length > 0){
            itemToAdd = items[0].id
        }
    });

    function mapCurrencyToEntry(currency){
        let title = currency.id.substr('currency.'.length);
        return {
            type: 'currency',
            subtype: 'currency',
            id: currency.id,
            title,
            searchTerm: title,
            value: Number(currency.amount)
        }
    }


    function timeSince(date){

        var nowDate = new Date()
        var seconds = Math.floor((nowDate.getTime() - date.getTime()) / 1000);
    
        var interval = seconds / 31536000;
        
        if (interval > 1) {
        return Math.floor(interval) + " years";
        }
        interval = seconds / 2592000;
        if (interval > 1) {
        return Math.floor(interval) + " months";
        }
        interval = seconds / 86400;
        if (interval > 1) {
        return Math.floor(interval) + " days";
        }
        interval = seconds / 3600;
        if (interval > 1) {
        return Math.floor(interval) + " hours";
        }
        interval = seconds / 60;
        if (interval > 1) {
        return Math.floor(interval) + " minutes";
        }
        return Math.floor(seconds) + " seconds";
    }

    function mapItemToEntry(itemGroup) {

        return itemGroup.items.map(item => {
            
            var names = item.properties.map(p => p.name);
            /*
             finding a display name is hard, because we don't know what data is doing to be in there....
             however, for _now_, lets assume its either id, or name, or the only thing in the set.
            */
            var index = names.indexOf('id');
            index = index != -1 ? index : names.indexOf('name');
            index = index != -1 ? index : names.indexOf('title');
            index = index != -1 ? index : names.indexOf('key');
            index = index != -1 ? index : 0;

            var nameProperty = item.properties[index];
            var title = '';
            if (nameProperty){
                title = nameProperty.value;
            }
            var subType = itemGroup.id.substr(itemGroup.id.lastIndexOf('.') + 1)
            var searchTerm = `${title} ${subType} ${item.properties.map(p => `${p.name};${p.value}`).join(',')}`;

            var valueObject = item.properties.reduce( (agg, curr) => ({
                ...agg,
                [curr['name']]: curr.value
            }), { });
            var createdAtDate = new Date(item.createdAt);
            var udpatedAtDate = new Date(item.updatedAt);
            return {
                type: 'item',
                itemId: item.id,
                contentId: itemGroup.id,
                subtype: subType,
                title: title,
                searchTerm,
                createdAt: `${createdAtDate.toUTCString()} (${timeSince(createdAtDate)} ago)`,
                updatedAt: `${udpatedAtDate.toUTCString()} (${timeSince(udpatedAtDate)} ago)`,
                sortingKey: item.createdAt || Number.MAX_VALUE,
                properties: item.properties,
                json: JSON.stringify(valueObject),
                prettyJson: JSON.stringify(valueObject, null, 2)
            }
        })
    }

    async function handleSaveCurrency(entry, next, old){
        var deltaAmount = next - old;
        await inventoryService.addCurrency({
            id: entry.id,
            amount: deltaAmount
        });
        return next;
    }

    function openNewItem(toggle){
        newProperties = [];
        newPropertyName = '';
        newPropertyValue = '';
        toggle();
    }
    function addProperty(){
        newProperties = [...newProperties, {
            name: newPropertyName,
            value: newPropertyValue
        }];
        newPropertyName = '';
        newPropertyValue = '';

    }
    function removeNewProperty(prop, index){
        newProperties = newProperties.filter(p => p != prop);
    }

    function startUpdatingItem(item){
        updateItem = item;
        updateProperties = [...item.properties];
    }
    async function finishUpdateItem(toggle){
        var applicableProperties = updateProperties.filter(prop => prop.name && prop.value);
        try {
            isNetworking = true;
            await inventoryService.updateItem(updateItem.contentId, updateItem.itemId, applicableProperties)
        } finally {
            isNetworking = false;
            toggle()
        }
    }

    async function addItem(toggle){
        var applicableProperties = newProperties.filter(prop => prop.name && prop.value);

        try {
            isNetworking = true;
            await inventoryService.addItem(itemToAdd, applicableProperties)
        } finally {
            isNetworking = false;
            toggle()
        }
    }

    async function removeItem(item){
        await inventoryService.removeItem(item.contentId, item.itemId);
    }

    async function addCurrency(toggle){
        try {
            isNetworking = true;
            await inventoryService.addCurrency({
                id: currencyToAdd,
                amount: currencyAmountToAdd
            });
        } finally {
            isNetworking = false;
            toggle()
        }
    }

    onDestroy(() => {
        unsubCurrency();
        unsubItem();
    });

</script>

<div class="player-inventory">

    <ModalCard class="is-xsmall has-light-bg update-item" active={updateItem} onClose={() => updateItem = undefined}>
        <div slot="trigger-evt"  let:toggle let:active >
            <!-- Left intentionally blank -->
        </div>
        <h3 slot="title">
            Update {#if updateItem} {updateItem.contentId} {/if}
        </h3>
        <span slot="body">
            <PropertySet bind:properties={updateProperties}/>
        </span>

        <span slot="buttons" let:toggle>
            <button class="button is-success" on:click|preventDefault|stopPropagation={evt => finishUpdateItem(toggle)} class:is-loading={isNetworking}>
                <span>
                    <slot name="primary-button">
                        Save
                    </slot>
                </span>
            </button>
            <button class="button cancel" on:click|preventDefault={toggle}>
                <span>Cancel</span>
            </button>
        </span>
    </ModalCard>

    <ComponentButtons>
        <p class="control">
            <ModalCard class="is-xsmall has-light-bg add-currency">
              <div slot="trigger-evt" let:toggle let:active>
                <button class="button trigger-button" on:click|preventDefault|stopPropagation={toggle}>
                    <span class="icon is-small">
                        <FeatherIcon icon="dollar-sign"/>
                    </span>
                    <span>
                        Add Currency
                    </span>
                </button>
              </div>
              <h3 slot="title">
                Add Currency
              </h3>
              <span slot="body">
                <div class="field">
                    <div class="control">
                        <div class="select is-info">
                            <select bind:value={currencyToAdd}>
                                {#each currencies as symbol}
                                    <option value={symbol.id}> {symbol.id} </option>
                                {/each}
                            </select>
                        </div>
                    </div>
                </div>
                <div class="field">
                    <div class="control">
                        <input class="input is-primary" type="number" placeholder="How much?" bind:value={currencyAmountToAdd}>
                    </div>
                </div>
              </span>
            
              <span slot="buttons" let:toggle>
                <button class="button is-success" on:click|preventDefault|stopPropagation={evt => addCurrency(toggle)} class:is-loading={isNetworking}>
                    <span>
                        <slot name="primary-button">
                            Add
                        </slot>
                    </span>
                </button>
                <button class="button cancel" on:click|preventDefault={toggle}>
                    <span>Cancel</span>
                </button>
              </span>
            </ModalCard>
        </p>

        <p class="control">
            <ModalCard class="is-xsmall has-light-bg add-item">
              <div slot="trigger-evt" let:toggle let:active>
                <button class="button trigger-button" on:click|preventDefault|stopPropagation={evt => openNewItem(toggle)}>
                    <span class="icon is-small">
                        <FeatherIcon icon="gift"/>
                    </span>
                    <span>
                        Add Item
                    </span>
                </button>
              </div>
              <h3 slot="title">
                Add Item
              </h3>
              <span slot="body">
                <div class="field">
                    <div class="control" style="">
                        <div class="select is-info" style="display: flex; flex-grow: 1">
                            <select bind:value={itemToAdd} style="flex-grow: 1">
                                {#each items as item}
                                    <option value={item.id}> {item.id} </option>
                                {/each}
                            </select>
                        </div>
                    </div>
                </div>
                
                <PropertySet bind:properties={newProperties}/>
              </span>
            
              <span slot="buttons" let:toggle>
                <button class="button is-success" on:click|preventDefault|stopPropagation={evt => addItem(toggle)} class:is-loading={isNetworking}>
                    <span>
                        <slot name="primary-button">
                            Add
                        </slot>
                    </span>
                </button>
                <button class="button cancel" on:click|preventDefault={toggle}>
                    <span>Cancel</span>
                </button>
              </span>
            </ModalCard>
        </p>
    </ComponentButtons>

    <FilterSearch 
        placeholder="Search by name or property" 
        allElements={entries}
        filterOn="searchTerm"
        filterFunctions={[
            {func: (m) => true, name: 'All'},
            {func: (m) => m.type === 'currency', name: 'Currency'},
            {func: (m) => m.type === 'item', name: 'Items'}
        ]}
        bind:value={filteredEntries}
        >
    </FilterSearch>


    {#if filteredEntries.length == 0}
        <div class="no-inventory-message">
            No items found
        </div>
    {:else}
        <PaginateArray 
            paginationKey="inventory"
            position="top"
            elements={filteredEntries}
            bind:pagedElements={pagedEntries}/>
        {#each pagedEntries as entry}
        <div class="entry panel" class:currency={entry.type === 'currency'}>
            <div class="entry-flags type-{entry.type}"> </div>
            <div class="entry-subtype vertical-top"> {entry.subtype} </div>

            {#if entry.type === 'item'}
                <div class="entry-title vertical-top times">
                    <label>Created At</label>
                    <div>
                        {entry.createdAt}
                    </div>
                    <label>Updated At</label>
                    <div>
                        {entry.updatedAt}
                    </div>
                </div>
            {:else}
                <div class="entry-title vertical-top"> {entry.title} </div>
            {/if}

            {#if entry.type === 'currency'}
                <AsyncInput 
                    class="vertical-top"
                    value={entry.value}
                    inputType="number"
                    onWrite={(next, old) => handleSaveCurrency(entry, next, old)}
                />
            {:else if entry.type === 'item'}

                <div class="value" style="padding: 12px 0px;">
                    {#if expandedItem === entry}
                        <div class="control" >
                            <textarea class="textarea is-static has-fixed-size" 
                                type="text" 
                                bind:this={jsontext}
                                placeholder="value..." 
                                readonly bind:value={entry.prettyJson} />
                        </div>
                        {:else}
                        <div class="control">
                            <input class="input is-static" type="text" placeholder="value..." readonly bind:value={entry.json} >
                        </div>
                    {/if}
                </div>

                <div class="buttons">

                    <button class="button" on:click={evt => startUpdatingItem(entry)}>
                        <FeatherIcon icon="edit-2"/>
                    </button>

                    {#if expandedItem === entry}
                        <button class="button" on:click={evt => expandedItem = undefined}>
                            <FeatherIcon icon="arrow-up"/>
                        </button>
                    {:else}
                        <button class="button" on:click={evt => expandedItem = entry}>
                            <FeatherIcon icon="arrow-right"/>
                        </button>
                    {/if}

                    <WarningPopup 
                        left={88}
                        top={-95} 
                        headerClass="light-header" 
                        header="Remove Item"
                        message="The item will be removed from the player's inventory."
                        onConfirmFunction={() => removeItem(entry) }>
                        <div slot="trigger" let:toggle style="display: inline;">
                            <button class="button" on:click|preventDefault|stopPropagation={toggle}>
                                <FeatherIcon icon="trash"/>
                            </button>
                        </div>
                    </WarningPopup>

                </div>
            {/if}

        </div>
        {/each}
        <PaginateArray 
            paginationKey="inventory"
            position="bottom"
            elements={filteredEntries}
            bind:pagedElements={pagedEntries}/>
     {/if}
</div>


<style lang="scss">
    $green: #77B769;
    $blue: #4497B7;

    .no-inventory-message {
        text-align: center;
        border-bottom: solid 1px white;
        padding-bottom: 200px;
        margin-top: 120px;
    }

    :global(.player-inventory .entry .network-buttons) { 
        min-width: 136px;
    }

    .player-inventory {
        position: relative;

        .modal.add-currency {
            *[slot="body"] {
                display: flex;
                .field:nth-child(2){
                    flex-grow: 1;
                }
            }
            .field:first-child {
                max-width: calc(100% - 180px);
                margin-right: 12px;
            }
        }

        // .modal.add-item {
            
        // }

        .entry {
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
               // padding-top: 15px;
               &.vertical-center {
                   align-self: center;
               }
               &.vertical-top {
                   padding-top: 15px;
               }
            }

            .times {
                padding-top: 5px;
                padding-bottom: 5px;
                label {
                    font-weight: bold;
                }
                font-size: 10px;
            }
            .entry-subtype {
                padding-left: 40px;
                width: 33%;
            }
            .entry-title {
                width: calc(10% + 130px);
            }

            .entry-flags {
                border-left: solid 12px grey;
                border-right: solid 12px transparent;
                border-top-left-radius: 4px;
                border-bottom-left-radius: 4px;
                position: absolute;
                top:0;
                bottom:0;

                &.type-currency {
                    border-left-color: $green;
                }
                &.type-item {
                    border-left-color: $blue;
                }
            }
            .network-input {
                padding: 10px 0px;
            }
            .value {
                flex-grow: 1;
                //max-width: 266px;
                input.input {
                    text-overflow: ellipsis;
                    white-space: nowrap;
                    overflow: hidden;
                }
                textarea {
                    max-height: 500px;
                }
            }
            .buttons {
                min-width: 136px;
                padding: 12px;
                .button {
                    padding: 0px;
                    min-width: 32px;
                    margin: 0px;
                }
            }
        } 
    }


</style>
