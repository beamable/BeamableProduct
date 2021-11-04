<script>
  import Tabs from './_tabs';
  import FeatherIcon from '../../components/FeatherIcon';
  import ModalCard from '../../components/ModalCard';
  import WarningPopup from '../../components/WarningPopup';
  import FilterSearch from '../../components/FilterSearch';
  import PaginateArray from '../../components/PaginateArray';
  import RoleGuard from '../../components/RoleGuard';
  import PermissionsModal from '../../components/PermissionsModal';

  import { getServices } from '../../services';
  const { members, setRole } = getServices().realms;
  const { players, realms } = getServices()

  function sortMembers(members = []) {
    
    return members.sort((a, b) => (a.id > b.id ? 1 : -1))
  }

  $: sortedMembers = sortMembers($members);

  let newMemberQuery;
  let newMember;
  let createMember;
  let memberError;
  let isLoading;

  let filteredMembers = [];
  let pagedMembers = [];

  let permissionsModalActive = false;
  let permissionsModalAccountId = undefined;

  async function addNewMember(toggle){
    try {
      isLoading = true;
      await realms.setRole(newMember.id, 'tester');
    } finally {
      isLoading = false;
      toggle();
    } 
  }

  async function createNewMember(toggle){
    try {
      isLoading = true;
      await realms.addUser(newMemberQuery);
    } finally {
      isLoading = false;
      toggle();
    }
  }

  async function lookUpMember(toggle){
    try {
      newMember = undefined;
      memberError = undefined;
      createMember = undefined;
      toggle();

      newMember = await players.findPlayer(newMemberQuery, false);
    } catch(err) {

      if (err && err.status === 404) {
        // ah, the account doesn't exist. We may want to add it.
        createMember = newMemberQuery;
      } else {
        memberError = err.message;
      }
    }
  }

  function openPermissionsModal(accountId){
      permissionsModalAccountId = accountId;
      permissionsModalActive = true;
  }

</script>

<Tabs activeTab="contributors">
  <RoleGuard roles={['admin']} emptyNoAuth>
    <div class="panel-block">

      <div class="new-member-bar">
        <form on:submit|preventDefault={() => {}}>
        
          <div class="field">
            <div class="control has-icon">
              <input type="text" class="input" placeholder="Enter team member's email or dbid" bind:value={newMemberQuery}/>
              <div class="form-icon">
                <FeatherIcon icon="user-plus"/>
              </div>
            </div>
          </div>
          <div class="field">
            <div class="control">
              <ModalCard class="is-xsmall has-light-bg">
                <div slot="trigger-evt" let:toggle let:active>
                  <button class="button" disabled={!newMemberQuery || active} on:click={() => lookUpMember(toggle)} >
                    Add
                  </button>
                </div>
                <h3 slot="title">
                  {#if newMember}
                    Confirm New Member
                  {:else if createMember}
                    Add New Member
                  {:else}
                    {#if memberError}
                      No Member Found
                    {:else}
                      Searching...
                    {/if}
                  {/if}
                </h3>
                <span slot="body">
                  {#if newMember}
                    Please confirm you want to add <strong> {newMemberQuery} </strong>
                  {:else if createMember}
                    Would you like to create an account for <strong> {newMemberQuery} </strong>?
                    <br>
                    The user will need to use the forgot-password flow at the login page to sign in for the first time.
                  {:else}
                    {#if memberError}
                      There is no account for <strong> {newMemberQuery} </strong>
                    {:else}
                      Searching...
                    {/if}
                  {/if}
                </span>
              
                <span slot="buttons" let:toggle>
                  {#if newMember}
                    <button class="button is-success" on:click|preventDefault|stopPropagation={evt => addNewMember(toggle)} class:is-loading={isLoading}>
                        <span>
                            <slot name="primary-button">
                                Add Member
                            </slot>
                        </span>
                    </button>
                  {/if}
                  {#if createMember}
                    <button class="button is-success" on:click|preventDefault|stopPropagation={evt => createNewMember(toggle)} class:is-loading={isLoading}>
                        <span>
                            <slot name="primary-button">
                                Create Member
                            </slot>
                        </span>
                    </button>
                  {/if}
                  <button class="button cancel" on:click|preventDefault={toggle}>
                      <span>Cancel</span>
                  </button>
                </span>
              </ModalCard>

            </div>
          </div>
        </form>
      </div>

    </div>
  </RoleGuard>
</Tabs>

<RoleGuard roles={['admin']}>
  <FilterSearch 
    placeholder="Search Members" 
    allElements={sortedMembers}
    filterOn="email"
    filterFunctions={[
      {func: (m) => true, name: 'All'},
      {func: (m) => ['developer', 'tester'].indexOf(m.roleString) === -1, name: 'Admins'},
      {func: (m) => m.roleString === 'developer', name: 'Developers'},
      {func: (m) => m.roleString === 'tester', name: 'Testers'},
    ]}
    bind:value={filteredMembers}
    >
  </FilterSearch>

  <PaginateArray 
    paginationKey="contributors"
    position="top"
    elements={filteredMembers}
    bind:pagedElements={pagedMembers}/>
    {#each pagedMembers as { id, email, availableRoles, roleString }, i(id)}
    <div class="panel is-member">

      <div class="flags {roleString}">
      </div>

      <div class="player-email">
        <div class="field">
            <div class="control">
                <input type="text" class="input is-static" placeholder="No Email Provided" readonly bind:value={email} >
            </div>
        </div>
      </div>

      <div class="player-id ">
          <div class="field">
            <div class="control">
                <input type="text" class="input is-static" readonly bind:value={id}>
            </div>
        </div>
      </div>

      <div class="player-role ">
        <div class="field">
          <div class="control">
              <input type="text" class="input is-static {roleString}" readonly bind:value={roleString}>
          </div>
        </div>
      </div>

      <div>
        <button disabled={!(availableRoles && availableRoles.length)} class="button is-small" on:click={evt => openPermissionsModal(id)}>
          Permissions
        </button>
      </div>
      <div class="player-delete ">
        <WarningPopup 
          left={25} 
          headerClass="light-header" 
          header="Remove Team Member"
          message="The team member will lose all game privileges."
          onConfirmFunction={() => setRole(id, null) }>
          <div slot="trigger" let:toggle>
            <button disabled={!(availableRoles && availableRoles.length)} class="button is-small" on:click|preventDefault|stopPropagation={toggle} >
              <span class="icon is-small">
                <FeatherIcon icon="trash-2" />
              </span>
            </button>
          </div>
        </WarningPopup>

      </div>
    </div>
  {/each}
  <PaginateArray 
    paginationKey="contributors"
    position="bottom"
    elements={filteredMembers}
    bind:pagedElements={pagedMembers}/>

  <PermissionsModal bind:modalActive={permissionsModalActive} bind:accountId={permissionsModalAccountId}/>
</RoleGuard>

<style lang="scss">
  
  $adminColor: #EBC65F;
  $developerColor: #4497B7;
  $testerColor: #77B769;
  .username {
    flex: 1;
  }

  .is-capitalized {
    text-transform: capitalize;
  }

  .is-member > div {
    margin: 4px;
  }

  .is-member {
    $flagWidth: 5px;

    margin: 14px 0;
    background: #454545;
    border-radius: 4px;
    position: relative;
    align-items: center;
    min-height: 60px;
    padding-left: calc(#{$flagWidth} * 3);
    padding-right: $flagWidth;
    display:flex;
    flex-direction:row;

    .flags {
      position: absolute;
      top: 0;
      bottom: 0;
      left: 0;
      border: solid $flagWidth $adminColor;
      margin: 0;
      border-top-left-radius: 6px;
      border-bottom-left-radius: 6px;
      &.admin {
        border-color: $adminColor;
      }
      &.developer {
        border-color: $developerColor;
      }
      &.tester {
        border-color: $testerColor;
      }
    }

    .player-email {
      flex-grow: 1;
    }
    .player-role {
      width: 120px;
    }

    .button {
      font-size: .75rem;
      padding: calc(.5em - 1px) calc(1.75em - 1px);
    }
  }

  .player-icon {
    text-align: center;
    
  }

  .card-body,
  .card-heading,
  .card-footer {
    border:none;
  }

  .player-delete {
    display: inline-flex;
  }
  .is-invisible {
    opacity: 0;
    pointer-events: none;
  }

  .button[disabled] {
    pointer-events: none;
  }

  .is-triangle :global(svg) {
    fill: currentColor;
    transform: rotate(180deg);

  }
  :global(.is-triangle){
    position: absolute;
    right: 4px;
    top: 10px;
    padding-left: 2px;
    border-left: solid 1px grey;
    margin: 0px !important;
  }

  .new-member-bar {
    flex-grow: 1;
    form {
      display: flex;
      flex-direction: row;
      .field {
        margin-bottom: 0px;
        &:first-child {
          flex-grow: 1;
        }
      }
      strong {
        color: white;
      }
      .card-body button.button {
        border-color: transparent;
      }
      button.button {
        border: solid 2px white;
        border-bottom-left-radius: 0px;
        border-top-left-radius: 0px;
        border-left: none;
      }
      input {
        border-top-left-radius: 0px;
        border-bottom-right-radius: 0px;
        border-right: none;
      }

    }
  }

  .player-role {
    width: 120px;
    button {
      background: #333236;
      height: 38px;
      border-color: grey;
      position: relative;
    }
    .button,
    .dropdown-item,
    input {
      &.admin {
        color: $adminColor!important;
      }
      &.developer {
        color: $developerColor!important;
      }
      &.tester {        
        color: $testerColor!important;
      }
    }
  }
</style>