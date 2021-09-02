<script>
  import Link from '../../../components/Link';
  import FeatherIcon from '../../../components/FeatherIcon';
  import Tabs from '../_tabs';
  import { getServices } from '../../../services';
  import RawDataButton from '../../../components/RawDataButton';
  import CreateRealmButton from '../../../components/CreateRealmButton';
  const { realms, realm, cid } = getServices().realms;

  let customerId;

  cid.subscribe(value => {
      customerId = value;
  });

  function generateConfigDefaults(realm) {
    let host = window.config.host;

    return {
      cid: customerId,
      pid: realm.name,
      platform: location.protocol + host
    }
  }
</script>

<style>
  .realm-icon {
    display: block;
    margin: 0 auto;
    filter: grayscale(1);
    opacity: .6;
    margin-bottom: 15px;
    transition: all .3s;
  }

  .category-box:hover .realm-icon {
    filter: grayscale(0);
    opacity: 1;
  }

  .field .button {
    font-size: .75rem;
    padding: calc(.5em - 1px) calc(.75em - 1px);
  }

  .panel-block {
    justify-content: space-between;
  }
</style>


<Tabs activeTab="games">
  <div class="panel-block level">
    <div class="level-left">
      <div class="level-item field">
        <div class="control">
          <input class="input is-small" type="text" placeholder="Filter">
        </div>
      </div>
    </div>

    <div class="level-right">
      <div class="level-item field">
        <CreateRealmButton title="Create New Game" buttonText="Create New Game"/>
      </div>
    </div>
    <!-- TODO: Create a new game button -->
  </div>

  <div class="columns is-mobile is-multiline is-centered">
    {#if $realms }
    {#each $realms as realm, i (realm.name)}

      {#if !realm.parent}

        <div class="column is-full-mobile is-half-tablet is-one-third-desktop">
          <Link href="./{realm.name}" let:href>
            <a class="category-box is-accent has-background-white" {href}>
              <div class="realm-icon">
                <FeatherIcon icon="package" height="48" width="48" />
              </div>

              <div class="box-content has-text-centered">
                <h3 class="title is-6">{realm.displayName}</h3>
                <p>Project Id: {realm.name}</p>
                <RawDataButton
                  title="Config Defaults"
                  buttonName="Config"
                  resolveFunction={() => generateConfigDefaults(realm)}
                />
              </div>
            </a>
          </Link>
        </div>
      {/if}
    {/each}
    {/if}
  </div>
</Tabs>

