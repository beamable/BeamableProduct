<script>
  import Link from '../../../../../../components/Link';
  import FeatherIcon from '../../../../../../components/FeatherIcon';
  import TabPanel from '../../../../../../components/TabPanel';

  export let activeTab = '';
</script>

<TabPanel>
  <ul slot="tabs" class="dark-tabs">
    <li class:is-active={activeTab === 'players'}>
      <Link href="/:orgId/games/:gameId/realms/:realmId/players" let:href>
        <a {href}>
          <span class="icon is-small">
            <FeatherIcon icon="users" />
          </span>

          <span>Players</span>
        </a>
      </Link>
    </li>
    <li class:is-active={activeTab === 'leaderboards'}>
      <Link href="/:orgId/games/:gameId/realms/:realmId/leaderboards" let:href>
        <a {href}>
          <span class="icon is-small">
            <FeatherIcon icon="award" />
          </span>

          <span>Leaderboards</span>
        </a>
      </Link>
    </li>
    <li class:is-active={activeTab === 'content'}>
      <Link href="/:orgId/games/:gameId/realms/:realmId/content" let:href>
        <a {href}>
          <span class="icon is-small">
            <FeatherIcon icon="book-open" />
          </span>

          <span>Content</span>
        </a>
      </Link>
    </li>
    <li class:is-active={activeTab === 'microservices'}>
      <Link href="/:orgId/games/:gameId/realms/:realmId/microservices" let:href>
        <a {href}>
          <span class="icon is-small">
            <FeatherIcon icon="cloud" />
          </span>

          <span>Microservices</span>
        </a>
      </Link>
    </li>
  </ul>

  <slot />
</TabPanel>
