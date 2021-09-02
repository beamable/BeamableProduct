<!-- 1208935505932288 -->
<script>
    import Tabs from './_tabs';
    import { onDestroy } from 'svelte';
    import { getServices } from '../../../../../../services';
    import FilterSearch from '../../../../../../components/FilterSearch';
    import FeatherIcon from '../../../../../../components/FeatherIcon';
    import PageList from '../../../../../../components/PageList';
    import ModalCard from '../../../../../../components/ModalCard';
    import RawDataButton from '../../../../../../components/RawDataButton';
    import PaginateArray from '../../../../../../components/PaginateArray';
    import RoleGuard from '../../../../../../components/RoleGuard';
    import ManifestVersionDropdown from '../../../../../../components/ManifestVersionDropdown';

    const { realms: { realm }, http, content } = getServices();

    var manifest;
    var allReferences = [];
    var filteredReferences = [];
    var filterFunctions = [];
    var pagedReferences = [];
    var manifestDate;

    function setTab(tab) {
        activeTab = tab || 'all';
    }
    function onlyUnique(value, index, self) {
        return self.indexOf(value) === index;
    }

    const unsubscribe = content.manifest.subscribe(nextManifest => {
        manifest = nextManifest;
        manifestDate = new Date(manifest.created)
        allReferences = manifest.references.map(upgradeReference)
        allReferences.sort((a, b) => a.visibility < b.visibility ? 1 : -1);
        var typeFilterFunctions = allReferences
            .map(e => e.id.split('.')[0])
            .filter(onlyUnique)
            .map(type => ({
                func: m => m.id.startsWith(type),
                name: type
            }))
        filterFunctions = [
            { func: m => true, name: 'All' },
            ...typeFilterFunctions,
        ]
    });

    function upgradeReference(reference){
        var lastDotIndex = reference.id.lastIndexOf('.');
        return {
            ...reference,
            typeString: reference.id.substr(0, lastDotIndex),
            nameString: reference.id.substr(lastDotIndex + 1),
            tagString: reference.tags.join(' ')
        }
    }

    async function fetchContent(contentDescriptor){
        const data = await content.fetchContent(contentDescriptor);
        return data;
    }

    function downloadFile(toggle) {
        const type = "application/json";
        const data = contentJson;
        const fileName = `${selectedContent.id}.json`;
        // Create an invisible A element
        const a = document.createElement("a");
        a.style.display = "none";
        document.body.appendChild(a);

        // Set the HREF to a Blob representation of the data to be downloaded
        a.href = window.URL.createObjectURL(
            new Blob([data], { type })
        );

        // Use download attribute to set set desired file name
        a.setAttribute("download", fileName);

        // Trigger the download by simulating click
        a.click();

        // Cleanup
        window.URL.revokeObjectURL(a.href);
        document.body.removeChild(a);
    }

    onDestroy(() => {
        unsubscribe();
    });

</script>

<style lang="scss">
    $red: #FF5c5c;
    $green: #77B769;
    $blue: #4497B7;
    $yellow: #EBC65F;
    .labeled {
        position: relative;
        top: 8px;
        overflow: visible;
        p {
            color: #a5a5a5;
            position: absolute;
            top: -10px;
        }
    }
    .content-container :global(.flags.private) {
        border-left-color: $red;
    }
    .content-container :global(.flags.public) {
        border-left-color: $blue;
    }
    .content-container h3 {
        color: white;
    }
    .visibility.public {
        border-color: $blue;
    }
    .visibility.private {
        border-color: $red;
    }

    :global(.filter-wrap div.filter-search){
        flex-direction: column-reverse !important;
    }
    :global(.filter-wrap div.filter-search .buttons) {
        justify-content: center;
    }
    :global(.filter-wrap div.filter-search .control) {
        max-width: initial !important;
    }
    :global(.filter-wrap div.filter-search .field) {
        max-width: initial !important;
        margin-bottom: 14px;
    }

    .last-updated-time-container {
        text-align: right;
        .last-updated-time {
            opacity: .7;
            font-size: 14px;
        }
    }

    .id-wrapper {
        display: flex;
        flex-direction: column;

        .id-string {
            opacity: .7;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
            max-width: 280px;
        }
        .name-string {
            font-weight: bold;
            max-width: 280px;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
        }
    }



</style>

{#if $realm}
    <Tabs activeTab="content">
        <!-- <div class="panel-block">
             <button class="button trigger-button" on:click={evt => content.forceManifestReload()}>
                <span class="icon is-small">
                    <FeatherIcon icon="refresh-ccw"/>
                </span>
            </button>
        </div> -->
    </Tabs>
{/if}
<RoleGuard roles={['admin', 'developer']}>
    <div class="filter-wrap">

        <div class="last-updated-time-container">
            {#if manifestDate}
                <span class="last-updated-time">
                    Last Updated {manifestDate}
                </span>
            {/if}
        </div>
        <ManifestVersionDropdown/>
        <FilterSearch
            placeholder="Search Content"
            allElements={allReferences}
            filterOn="id"
            filterFunctions={filterFunctions}
            bind:value={filteredReferences}
            >
        </FilterSearch>

        <div class="content-container">

            <PaginateArray
                paginationKey="content"
                position="top"
                elements={filteredReferences}
                bind:pagedElements={pagedReferences}/>
            <PageList elements={pagedReferences}
                leftFlagClass={elem => elem.visibility}>
                <div slot="element" let:element class="elem">


                    <div class="id-wrapper" style="flex-grow: 1;">
                        <span class="id-string" title={element.id}>
                            <!-- XXX. Leave this as one horrible line. To support the copy/paste ux we want, we can't let newlines be treated as spaces. -->
                            {element.typeString}<span class="tiny-dot">.</span></span><span class="name-string" title={element.id}>{element.nameString}
                        </span>

                    </div>
                    <div class="visibility {element.visibility}" style="min-width: 120px;">
                        <span class="icon is-medium">
                            <FeatherIcon icon="{element.visibility === 'public' ? 'users' : 'eye-off'}"/>
                        </span>
                        <span class="underline {element.visibility}">
                            {element.visibility}
                        </span>
                    </div>
                    <div class="labeled" style=" max-width: 120px; min-width: 120px">
                        <p class="help is-primary" >Checksum</p>
                        <input class="input is-static" type="text" value={element.checksum} readonly>
                    </div>

                    <div class="labeled" style=" min-width: 200px;">
                        <p class="help is-primary" >Tags</p>
                        <input class="input is-static" type="text" value={element.tagString} readonly>
                    </div>

                    <div class="labeled">
                        <RawDataButton
                            resolveFunction={() => fetchContent(element)}
                            title={element.id}
                            downloadName={element.id}
                            />
                    </div>

                </div>
            </PageList>
            <PaginateArray
                paginationKey="content"
                position="bottom"
                elements={filteredReferences}
                bind:pagedElements={pagedReferences}/>
        </div>

    </div>
</RoleGuard>