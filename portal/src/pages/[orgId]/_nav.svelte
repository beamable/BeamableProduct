<script>
  import Navbar from '../../components/Theme/Navbar';
  import Link from '../../components/Link';
  import Dropdown from '../../components/Dropdown';
  import FeatherIcon from '../../components/FeatherIcon';
  import { getServices } from '../../services';

  const services = getServices();

  const { isLoggedIn, logout, account } = services.auth;
  const { orgId } = services.router;
  const { realm, org } = services.realms;

  let accountLabel;
  account.subscribe(value => {
    if (value) {
      accountLabel = `(${value.roleString}) ${value.email}`;
    } else {
      accountLabel = '';
    }
  });

</script>

<Navbar>
  <div slot="navbar-menu" class="navbar-menu">
      <div class="navbar-start">
        {#if $org}
          <div class="navbar-item" class:is-icon={!!$realm}>
            <Link href="/:orgId" let:href>
              <a class={$realm ? 'icon-link is-accent' : 'button is-solid accent-button raised'} {href}>
                <span class="icon is-small">
                  <FeatherIcon icon="home" height="16" width="16"/>
                </span>
                <span>{$orgId}</span>
              </a>
            </Link>
          </div>
        {/if}

        {#if $realm}
          <div class="navbar-item">
            <Link href="/:orgId/games/:gameId" let:href>
              <a class="button is-solid accent-button raised" href="{href}" title="Games">
                <span class="icon is-small">
                  <FeatherIcon icon="server" />&nbsp;
                </span>

                <span>{$realm.displayName}</span>
              </a>
            </Link>
          </div>
        {/if}
      </div>

      <div class="navbar-end">
        <div class="navbar-item account-label"> {accountLabel} </div>
        <Dropdown class="navbar-item is-account drop-trigger has-caret" let:active>
          <div slot="trigger" class="user-image">
            <img src="//via.placeholder.com/400x400" alt="" />
            <span class="indicator" />
          </div>

          <div class="nav-drop is-account-dropdown" class:is-active={active}>
            <div class="inner">
              <div class="nav-drop-body account-items">
                <a class="account-item" on:click={logout}>
                  <div class="media">
                    <div class="icon-wrap">
                      <FeatherIcon icon="power" />
                    </div>
                    <div class="media-content">
                      <h3>Log out</h3>
                      <small>Log out from your account.</small>
                    </div>
                  </div>
                </a>
              </div>
            </div>
          </div>
        </Dropdown>
      </div>

  </div>
</Navbar>


<style>
  .account-label {
    color: white;
  }
</style>