<script>
    import PageList from './PageList';
    import FeatherIcon from './FeatherIcon';
    import ModalCard from './ModalCard';
    import FilterSearch from './FilterSearch';
    import RawDataButton from './RawDataButton';
    import PaginateArray from './PaginateArray';

    export let player;
    export let announcements;

    let searchableAnnouncments = [];
    $: if(announcements) {
            searchableAnnouncments = announcements.map(announcement => ({
            ...announcement,
            searchTerm: announcement.announcement.title
        }));
    }

    let filteredAnnouncements = [];
    let pagedAnnouncements = [];

    function isAnnouncementComplete(announcement){
        return announcement.isRead === true && announcement.isClaimed == true && announcement.isDeleted == true;
    }

</script>

<div class="player-announcements">

    <FilterSearch 
        placeholder="Search..." 
        allElements={searchableAnnouncments}
        filterOn="searchTerm"
        filterFunctions={[
            {func: (m) => true, name: 'All'},
            {func: (m) => isAnnouncementComplete(m), name: 'Completed'},
            {func: (m) => !isAnnouncementComplete(m), name: 'Uncompleted'},
            {func: (m) => true, type: 'status', name: 'All'},
            {func: (m) => m.isRead, type: 'status', name: 'Read'},
            {func: (m) => m.isClaimed, type: 'status', name: 'Claimed'},
            {func: (m) => m.isDeleted, type: 'status', name: 'Deleted'},
        ]}
        bind:value={filteredAnnouncements}
        >
    </FilterSearch>

    {#if filteredAnnouncements.length == 0}
        <div class="no-announcement-message">
            No announcements found
        </div>
    {:else}
        <PaginateArray 
            paginationKey="announcements"
            position="top"
            elements={filteredAnnouncements}
            bind:pagedElements={pagedAnnouncements}/>
        <PageList elements={pagedAnnouncements}
            leftFlagClass={e => e.isRead ? 'read': 'unread'}
            rightFlagClass={e => e.isClaimed ? 'claimed': 'unclaimed'}>
            <div slot="element" let:element class="elem">

                <div style="flex-grow: 1;">
                    <div class="control">
                        <input class="input is-static" type="text" placeholder="value..." readonly value={element.announcement.title} >
                    </div>
                </div>
                
                <div class="has-read" class:read={element.isRead}>
                    <span class="icon is-medium">
                        <FeatherIcon icon="{element.isRead? 'eye' : 'eye-off'}"/>
                    </span>
                    <span class="underline"> 
                         {element.isRead ? 'Seen' : 'Unread'}
                    </span>
                </div>

                <div class="has-claim" class:claimed={element.isClaimed}>
                    <span class="icon is-medium">
                        <FeatherIcon icon="{element.isClaimed? 'gift' : 'inbox'}"/>
                    </span>
                    <span class="underline"> 
                         {element.isClaimed ? 'Claimed' : 'Unclaimed'}
                    </span>
                </div>

                <div class="has-delete" class:deleted={element.isDeleted}>
                    <span class="icon is-medium">
                        <FeatherIcon icon="{element.isDeleted? 'trash' : 'mail'}"/>
                    </span>
                    <span class="underline"> 
                         {element.isDeleted ? 'Deleted' : 'Available'}
                    </span>
                </div>

                <RawDataButton dataObject={element} downloadName={'announcement_' + element.announcement.id}/>
                
            </div>
        </PageList>
        <PaginateArray 
            paginationKey="announcements"
            position="bottom"
            elements={filteredAnnouncements}
            bind:pagedElements={pagedAnnouncements}/>
    {/if}
</div>

<style lang="scss">
    $red: #FF5c5c;
    $green: #77B769;
    $blue: #4497B7;
    $yellow: #EBC65F;
    .player-announcements :global(.flags.read) {
        border-left-color: $blue;
    }
    .player-announcements :global(.flags.unread) {
        border-left-color: $red;
    }
    .player-announcements :global(.flags.unclaimed) {
        border-right-color: $yellow;
    }
    .player-announcements :global(.flags.claimed) {
        border-right-color: $green;
    }


    .no-announcement-message {
        text-align: center;
        border-bottom: solid 1px white;
        padding-bottom: 100px;
        margin-top: 120px;
    }
    .player-announcements {

        .has-read {
            width: 120px;

            &:not(.read) .underline {
                border-bottom-color: $red;
            }
            &.read .underline {
                border-bottom-color: $blue;
            }
        }
        .has-claim {
            width: 150px;

            &:not(.claimed) .underline {
                border-bottom-color: $yellow;
            }
            &.claimed .underline {
                border-bottom-color: $green;
            }
        }

        .has-delete {
            width: 140px;

            .underline {
                border-bottom-color: transparent;
            }
        }
    }


</style>