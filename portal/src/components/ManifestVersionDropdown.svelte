<script>

    import { ContentService } from 'services/content';
    import FeatherIcon from './FeatherIcon';
    import { getServices } from '../services';
    import { get } from 'lib/stores';

    
    const {
        content
    } = getServices();

    let availableManifests = [];
    let showDropdown = false;

    function fetchManifestList(){
        content.fetchManifestList().then(m => availableManifests = m);
    }

    function onClick(){
        fetchManifestList();
        showDropdown = !showDropdown;
    }

    function onManifestClick(id){
        content.manifestId.set(id);
        showDropdown = false;
    }

    fetchManifestList();

    let manifestId = get(content.manifestId);
    content.manifestId.subscribe(v => manifestId = v);

    function clickOutside(node) {
  
        const handleClick = event => {
            if (node && !node.contains(event.target) && !event.defaultPrevented) {
                node.dispatchEvent(
                new CustomEvent('click_outside', node)
                )
            }
        }

        document.addEventListener('click', handleClick, true);
        
        return {
            destroy() {
                document.removeEventListener('click', handleClick, true);
            }
	    }
}
</script>

<style lang="scss">
.dropdown{
    display: flex;
    &.hidden {
        display: none;
    }
}

.dropdown-trigger {
        display: flex;
        width: 100%;
    }
    .page-button {
        &.fake-hover, &:hover {
            border-color: #b5b5b5;
        }
        &.fake-active, &:active {
            border-color: #3273dc;
        }
    }

    .page-button {
        &:focus {
            outline: 0;
        }
        width: 100%;
        height: 40px;
        margin: 8px;
        margin-left: 0px;
        margin-right: 0px;
        display: flex;
        background: #333236;
        flex-direction: row;
        justify-content: space-between;
        border-color: #dbdbdb;
        color: white;
        font-weight: bold;
        padding-left: 0;
    }
    .manifest-name{
        min-width: -webkit-fill-available;
        margin: 4px;
        margin-left: 16px;
    }
    .dropdown-menu {
        min-width: -webkit-fill-available;
        display: flex;
        width: 100%;
        padding-top: 0px;
        margin-top: 0px;
    }
    .dropdown-content {
        margin-left: 116px;
        background: #333236;
        border: solid 1px #5d5d5d;
        padding:0;
        max-height: 220px;
        overflow-y: scroll;
        width: 100%;
        a {
            padding-left: 20px;
            width: 100%;
            text-align: center;
        }
        a:hover {
            background: lighten(#424242, 5%);
        }
        a.is-active {
            background: #424242;
        }
    }
    
    a {
        color: grey;
        background:#1f1f1f;
        border-radius: 0px;
        border: none;
        border-bottom: solid 2px transparent;
        &:hover {
            color: #0095f1;
            border-bottom-color: grey;
        }
        &.disabled {
            opacity: .5;
            cursor: not-allowed;
        }
        &.is-current:hover,
        &.is-current {
            border-color: transparent;
            background-color: #454545;
            color:white;
        }
    }

    .manifest-id-label{
        min-width: fit-content;
        margin: 15px;
        color: #808080;
    }
</style>

<div class="dropdown bounded" class:hidden={availableManifests.length <= 1}>
    <span class="manifest-id-label">Namespace</span>
    <div class="dropdown-trigger">
        <button class="button page-button" on:click={onClick}>
            <span class="manifest-name">{manifestId}</span>
            <span class="is-capitalized">&nbsp;</span>
            <span class="icon is-small is-triangle" >
            <FeatherIcon icon="triangle" width="8" height="8" style="transform:rotate({showDropdown?0:180}deg)"/>
        </span>
        </button>
        {#if showDropdown}
        <div class="dropdown-menu" id="dropdown-menu" use:clickOutside on:click_outside={() => showDropdown = false}>
            <div class="dropdown-content">
                {#each availableManifests as manifest}
                    <a  class="dropdown-item" 
                    class:is-current={manifest.id == manifestId}
                    on:click={onManifestClick(manifest.id) 
                    }>
                        {manifest.id}
                    </a>
                {/each}
            </div>
        </div>
        {/if}
    </div>
</div>