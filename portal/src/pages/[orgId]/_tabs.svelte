<script>
  import Link from '../../components/Link';
  import FeatherIcon from '../../components/FeatherIcon';
  import TabPanel from '../../components/TabPanel';

  export let activeTab = '';
</script>

<TabPanel>
  <ul slot="tabs">
    <li class:is-active={activeTab === 'games'}>
      <Link href="/:orgId/games" let:href>
        <a {href}>
          <span class="icon is-small">
            <FeatherIcon icon="package" />
          </span>

          <span>Games</span>
        </a>
      </Link>
    </li>

    <li class:is-active={activeTab === 'contributors'}>
      <Link href="/:orgId/contributors" let:href>
        <a {href}>
          <span class="icon is-small">
            <FeatherIcon icon="users" />
          </span>

          <span>Team</span>
        </a>
      </Link>
    </li>
  </ul>

  <slot />
</TabPanel>
