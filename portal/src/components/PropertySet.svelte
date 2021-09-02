<script>

    export let properties = []

    let newPropertyName, newPropertyValue;
    
    function addProperty(){
        properties = [...properties, {
            name: newPropertyName,
            value: newPropertyValue
        }];
        newPropertyName = '';
        newPropertyValue = '';

    }
    function removeNewProperty(prop, index){
        properties = properties.filter(p => p != prop);
    }
</script>

<div>
    {#each properties as property, index}
        <div class="property-input-group">
            <div class="field property-name">
                <div class="control">
                    <input class="input is-info" type="text" placeholder="[Property Name]" bind:value={property.name}>
                </div>
            </div>
            <div class="separator">
            :
            </div>
            <div class="field property-value">
                <div class="control">
                    <input class="input is-info" type="text" placeholder="[Value]" bind:value={property.value} >
                </div>
            </div>
            <a class="delete is-small" on:click={evt => removeNewProperty(property,index)}></a>
        </div>
    {/each}

    <div class="property-input-group"> 
    
        <div class="field property-name">
            <div class="control">
                <input class="input is-info" type="text" placeholder="[Property Name]" bind:value={newPropertyName} autofocus>
            </div>
        </div>
        <div class="separator">
            :
        </div>
        <div class="field property-value">
            <div class="control">
                <input class="input is-info" type="text" placeholder="[Value]" bind:value={newPropertyValue} on:blur={addProperty}>
            </div>
        </div>
    </div>
</div>

<style lang="scss">
.property-input-group {
    display: flex;
    justify-content: space-between;
    position: relative;
    margin-right: 15px;
    .property-name {
        input {
            border-top-right-radius: 0;
            border-bottom-right-radius: 0;
            border-right: none;
        }
        
    }
    .property-value {
        flex-grow: 1;
        input {
            border-top-left-radius: 0;
            border-bottom-left-radius: 0;
            border-left: none;
        }
    }
    a {
        position: absolute;
        right: -20px;
        top: 10px;
    }
    .separator {
        width: 12px;
        background-color: #333336;
        color: #fff;
        height: 2.5em;
        border-top: solid 1px #4497B7;
        border-bottom: solid 1px #4497B7;
        user-select: none;
        padding-top: 8px;
    }
}
</style>