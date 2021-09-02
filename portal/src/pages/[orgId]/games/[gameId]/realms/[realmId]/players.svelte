<script>
  import Tabs from './_tabs';
  import {getServices} from '../../../../../../services';
  import FeatherIcon from '../../../../../../components/FeatherIcon';
  import Card from '../../../../../../components/Card';
  import PlayerData from '../../../../../../services/players';
  import PlayerStats from '../../../../../../components/PlayerStats';
  import PlayerInventory from '../../../../../../components/PlayerInventory';
  import PlayerLeaderboards from '../../../../../../components/PlayerLeaderboards';
  import PlayerProfile from '../../../../../../components/PlayerProfile';
  import PlayerPurchaseList from '../../../../../../components/PlayerPurchaseList';
  import PlayerAnnouncements from '../../../../../../components/PlayerAnnouncements';
  import ScrollLink, {navigateTo} from '../../../../../../components/ScrollLink';
  import RoleGuard from '../../../../../../components/RoleGuard';
  import TabPanel from '../../../../../../components/TabPanel';
  import {get} from 'svelte/store';
  import {beforeUpdate} from 'svelte';
  import PlayerCloudData from "../../../../../../components/PlayerCloudData.svelte";
  import PlayerTournaments from '../../../../../../components/PlayerTournaments.svelte';

  const {
    realms: {realm},
    http,
    players,
    inventory,
    announcements,
    cloudsaving,
    leaderboards,
    tournaments
  } = getServices();

  let playerIdOrEmail;
  let playerStats;
  let timer;
  let player;
  let playerInventory;
  let playerAnnouncements;
  let floatingButton;
  let playerCloudData;
  let playerLeaderboards;
  let playerPayments;
  let playerTournamentStatuses;

  let queryError;

  let profileTitle = 'Profile';
  let inventoryTitle = 'Inventory';
  let announcementTitle = 'Announcement';
  let statTitle = 'Stats';
  let cloudDataTitle = "Cloud Saving";
  let purchasesTitle = "Purchases";
  let leaderboardsTitle = 'Leaderboards';
  let tournamentsTitle = 'Tournaments';

  let isLoading;
  let wasLoading;
  let buttonOpacity = 0;
  let buttonRight = 20;
  let isNavbarLocked = false;

  $: isLoading = !(player) && playerIdOrEmail && !queryError;

  $: {
    playerIdOrEmail = playerIdOrEmail; // causes reaction
    debounce(queryPlayer)
  }

  announcements.playerAnnouncements.subscribe(value => {
    playerAnnouncements = value;
  });

  inventory.playerInventory.subscribe(value => {
    playerInventory = value;
  });

  leaderboards.playerLeaderboards.subscribe(value => {
    playerLeaderboards = value;
  });

  players.emailOrDbid.subscribe(value => {
    playerIdOrEmail = value;
  })

  players.playerData.subscribe(value => {
    player = value;
  });

  players.playerStats.subscribe(value => {
    playerStats = value;
  });

  players.playerError.subscribe(value => {
    queryError = value;
    if (queryError) {
      reset();
    }
  });

  cloudsaving.playerCloudData.subscribe(value => {
    playerCloudData = value;
  });

  tournaments.playerStatus.subscribe(value => {
    playerTournamentStatuses = value
  })

  function reset() {
    playerStats = undefined;
    player = undefined;
  }

  const debounce = action => {
    clearTimeout(timer);
    timer = setTimeout(() => {
      action()
    }, 1000);
  }

  async function queryPlayer() {
    clearTimeout(timer);

    players.emailOrDbid.update(_ => {
      if (get(players.emailOrDbid) !== playerIdOrEmail) {
        reset()
      }
      return playerIdOrEmail
    });
  }

  $: if (floatingButton) {
    getFloatingRightPosition();
  }

  function getFloatingRightPosition() {
    /*
      The floating button should be anchored to the middle of the page dead space on the right.
      But, if there is no dead space (because the page is at or less than 860px), then the button should be anchored 20px from the right.

      This could be achieved through css with the rule,
        right: calc(max(20px, .25 * (100vw - 860px) - 50px));
      But Unity/Chrome 37 doesn't support max(). Instead, we replicate the max() functionality in javascript.
      The 100vw - 860px bit is still done in css, but hi-jacking the padding-top property.
    */
    if (!floatingButton) {
      buttonRight = 20;
      return;
    }

    let width = parseInt(window.getComputedStyle(floatingButton).paddingTop.slice(0, -2), 10);
    buttonRight = Math.max(width, 20);
  }

  function refreshFloatingButton() {
    // when we interface with the scroll value, different browsers store it in different places. If one is empty, try the other.
    const value = document.documentElement.scrollTop || document.body.scrollTop;
    isNavbarLocked = value > 137; // XXX: a handpicked value to get the nav locking to "feel" right.

    // smooth step the opacity.
    const zeroAtScroll = 30; // handpicked values, we can replace with screenHeight * coef.
    const oneAtScroll = 100;
    var x = Math.max(0, Math.min(1, (value - zeroAtScroll) / (oneAtScroll - zeroAtScroll)));
    buttonOpacity = x * x * (3 - 2 * x);
  }

</script>

<style lang="scss">
  .search-box {
    width: 100%;
    text-align: right;
  }
  .floating-box {
    position: fixed;
    bottom: 20px;
    // padding-top has no effect when position is fixed. We use as a hackthrough to get width.
    padding-top: calc(.25 * (100vw - 860px) - 50px);
    box-shadow: 0px 9px 12px -8px black;
  }
  .lock-space-filler.locked {
    height: 60px;
  }
  .player-tabs {
    border-bottom: none;

    &.locked {
      position: fixed;
      top: 58px;
      width: 840px;
      background: #1e1e1e;
      z-index: 100;
      border-bottom: solid 1px gray;
    }

    ul {
      border-bottom: none;
    }
    li {
      flex-grow:1;
      a {
        color: grey;
        background:#1f1f1f;
        border: none;
        min-width: 80px;
        border-bottom: solid 2px transparent;
        &:hover {
            color: #0095f1;
            border-bottom-color: grey;
        }
        &.active:hover,
        &.active {
            background-color: #454545;
            color:white;
        }
      }

    }
  }
</style>

<svelte:window on:wheel={refreshFloatingButton} on:resize={getFloatingRightPosition} />

{#if $realm}
  <Tabs activeTab="players">
    <RoleGuard roles={['admin', 'developer']} emptyNoAuth>
      <div class="panel-block">
        <form on:submit|preventDefault={queryPlayer} class="search-box">
          <div class="field">
            <div class="control has-icon {(isLoading) ? 'is-loading': ''} {queryError ? 'has-error': ''}  ">
              <input type="text" class="input" placeholder="Search for player with email, or dbid" bind:value={playerIdOrEmail} >
              <div class="form-icon">
                <FeatherIcon icon="search"/>
              </div>
            </div>
          </div>
        </form>
      </div>
    </RoleGuard>
  </Tabs>
{/if}


<RoleGuard roles={['admin', 'developer']}>

{#if player}

  <div class="lock-space-filler" class:locked={isNavbarLocked}></div>
  <div class="tabs player-tabs" class:locked={isNavbarLocked}  >
    <ul>
      <li on:click={evt => navigateTo(profileTitle)}>
        <a>
          {profileTitle}
        </a>
      </li>
      <li class="" on:click={evt => navigateTo(inventoryTitle)}>
        <a>
          {inventoryTitle}
        </a>
      </li>
      <li class="" on:click={evt => navigateTo(leaderboardsTitle)}>
        <a>
          {leaderboardsTitle}
        </a>
      </li>
      <li class="" on:click={evt => navigateTo(announcementTitle)}>
        <a>
          {announcementTitle}
        </a>
      </li>
      <li class="" on:click={evt => navigateTo(statTitle)}>
        <a>
          {statTitle}
        </a>
      </li>
      <li class="" on:click={evt => navigateTo(purchasesTitle)}>
        <a>
          {purchasesTitle}
        </a>
      </li>
      <li class="" on:click={evt => navigateTo(cloudDataTitle)}>
        <a>
          {cloudDataTitle}
        </a>
      </li>
      <li class="" on:click={evt => navigateTo(tournamentsTitle)}>
        <a>
          {tournamentsTitle}
        </a>
      </li>
    </ul>
  </div>
{/if}


{#if player}
  <Card title={profileTitle} data={player} loadingHeight={206}>
    <PlayerProfile player={player} stats={playerStats}> </PlayerProfile>
  </Card>

  <Card title={inventoryTitle} data={playerInventory} loadingHeight={100} isPanel={false}>
    <PlayerInventory player={player} inventory={playerInventory}> </PlayerInventory>
  </Card>

  <Card title={leaderboardsTitle} data={playerLeaderboards} loadingHeight={100} isPanel={false}>
    <PlayerLeaderboards player={player} playerLeaderboards={playerLeaderboards}/>
  </Card>

  <Card title={announcementTitle} data={playerAnnouncements} loadingHeight={100} isPanel={false}>
    <PlayerAnnouncements player={player} announcements={playerAnnouncements}> </PlayerAnnouncements>
  </Card>

  <Card title={statTitle} data={playerStats} isPanel={false}>
    <PlayerStats player={player} playerStats={playerStats} > </PlayerStats>
  </Card>

  <Card title={purchasesTitle} data={playerPayments} isPanel={false}>
    <PlayerPurchaseList player={player} bind:pagedPayments={playerPayments}/>
  </Card>

  <Card title={cloudDataTitle} data={playerCloudData} isPanel={false}>
    <PlayerCloudData player={player} cloudData={playerCloudData} > </PlayerCloudData>
  </Card>

  <Card title={tournamentsTitle} data={playerTournamentStatuses} isPanel={false}>
    <PlayerTournaments player={player} statuses={playerTournamentStatuses}/>
  </Card>

  <div class="control floating-box" bind:this={floatingButton} style="opacity: {buttonOpacity}; right: {buttonRight}px">
    <ScrollLink link="" onTick={refreshFloatingButton}>
      <button class="button"> Back to top</button>
    </ScrollLink>
  </div>
{/if}

{#if queryError}
<article class="message is-danger">
  <div class="message-body">
    <strong> {queryError.error} </strong>
    <br>
    {queryError.message}
  </div>
</article>
{/if}
</RoleGuard>
