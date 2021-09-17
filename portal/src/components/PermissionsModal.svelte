<script>
  import ModalCard from './ModalCard';
  import FeatherIcon from './FeatherIcon';
  import RawDataButton from './RawDataButton';
  import { getServices } from '../services';

  const realmsService = getServices().realms;
  const { realms, members } = realmsService;

  export let modalActive = false;
  export let accountId = undefined;

  $: account = accountId ? ($members || []).find(m => m.id == accountId) : undefined;
  $: availableRoles = account ? account.availableRoles.sort() : [];
  $: realmsMap = new Map(($realms || []).map(r => [r.name, r]));
  $: sortedRealms = ($realms || []).filter(r => !r.archived).sort((a, b) => (a.displayName > b.displayName ? 1 : -1));

  let projectRoles = [];
  $: {
    if (account && account.roles && account.roles.length > 0) {
      projectRoles = account.roles
      .filter(roleMapping => realmsMap.has(roleMapping.projectId))
      .map(roleMapping => {
        return { 
          project: realmsMap.get(roleMapping.projectId),
          role: roleMapping.role 
        }
      })
      .sort((a, b) => (`${a.project.displayName}.${a.role}` > `${b.project.displayName}.${b.role}` ? 1 : -1));
    }
    else {
      projectRoles = [];
    }
  }
  
  let globalRoleLoading = {};
  let scopedRoleLoading = {};
  let deleteScopedRoleLoading = {};
  let addingScopedRole = false;
  let newScopedRole = undefined;

  function close() {
    modalActive = false;
    addingScopedRole = false;
    accountId = undefined;
  }

  async function setGlobalRole(accountId, role) {
    globalRoleLoading[accountId] = true;
    try {
      await realmsService.setRole(accountId, role);
    } finally {
      globalRoleLoading[accountId] = false;
    }
  }

  async function removeProjectRole(accountId, role, projectId) {
    if (confirm("Are you sure you want to delete this mapping?")) {
      deleteScopedRoleLoading[`${accountId}.${projectId}.${role}`] = true;
      try {
        await realmsService.deleteProjectRole(accountId, role, projectId);
      } finally {
        deleteScopedRoleLoading[`${accountId}.${projectId}.${role}`] = false;
      }
    }
  }

  function showAddScopedRole() {
    addingScopedRole= true;
    newScopedRole = { role: undefined, project: undefined };
  }

  async function setScopedRole(accountId, role, projectId) {
    scopedRoleLoading[accountId] = true;
    try {
      await realmsService.setRole(accountId, role, projectId);
    } finally {
      addingScopedRole = false;
      newScopedRole = undefined;
      scopedRoleLoading[accountId] = false;
    }
  }

  async function generateExport(accountId) {
    return await realmsService.getAccountRolesReport(accountId);
  }
</script>

<ModalCard class="has-light-bg replace-data" active={modalActive} onClose={() => close()}>
  <h3 slot="title">Permissions</h3>
  <span slot="body" class="perm-body">  
    {#if account}
    <h3>{account.email}</h3>
    <p>Every role has a specific set of predefined permissions. The global role is assigned to all games and realms.<br />
       Realm-scoped roles are assigned to a realm and all its children.</p>
    <hr>
    <h3>Global</h3>
      <div class="field w12">
        <div class="control">
            <div class="select" class:is-loading={globalRoleLoading[account.id]}>
              <select class="{account.roleString}" disabled={globalRoleLoading[account.id]} bind:value={account.roleString} on:change="{(evt) => setGlobalRole(account.id, account.roleString)}">
                {#each availableRoles as role}
                    <option class="{role}" value={role}> {role} </option>
                {/each}
              </select>
            </div>
        </div>
      </div>
    <hr>
    <h3>Realm-scoped</h3>
    <div class="container">
      {#if projectRoles.length > 0}
        {#each projectRoles as projectRole}
          <div class="row">
            <div class="destination">
              <input class="input" type="text" readonly value="{projectRole.project.displayName}"/>
            </div>
            <div class="role">
              <input class="input {projectRole.role}" type="text" readonly value="{projectRole.role}" />
            </div>
            <div class="actions">
              <button class="button is-small" class:is-loading={deleteScopedRoleLoading[`${account.id}.${projectRole.project.name}.${projectRole.role}`]} 
                on:click|preventDefault|stopPropagation={ () => removeProjectRole(account.id, projectRole.role, projectRole.project.name) }>
                <span class="icon is-small">
                  <FeatherIcon icon="trash-2" />
                </span>
              </button>
            </div>
          </div>
        {/each}
      {:else if addingScopedRole === false}
      <div class="row"><em>No realm-scoped roles defined.</em></div>
      {/if}
      {#if addingScopedRole === true}
      <div class="row">        
        <div class="destination">
          <div class="field">
            <div class="control">
                <div class="select">
                  <select bind:value={newScopedRole.project} disabled={scopedRoleLoading[account.id]}>
                    {#if !newScopedRole.project}
                      <option value={undefined} disabled> (select a realm) </option>
                    {/if}
                    {#each sortedRealms as realm}
                      <option value={realm}> {realm.displayName} </option>
                    {/each}
                  </select>
                </div>
            </div>
          </div>
        </div>
        <div class="role">
          <div class="field">
            <div class="control">
                <div class="select">
                  <select class="{newScopedRole.role}" disabled={scopedRoleLoading[account.id]} bind:value={newScopedRole.role}>
                    {#if !newScopedRole.role}
                      <option value={undefined} disabled> (select a role) </option>
                    {/if}
                    {#each availableRoles as role}
                        <option class="{role}" value={role}> {role} </option>
                    {/each}
                  </select>
                </div>
            </div>
          </div>
        </div>
        <div class="actions">
          <button class="button is-small" class:is-loading={scopedRoleLoading[account.id]} disabled={!newScopedRole || !newScopedRole.role || !newScopedRole.project || scopedRoleLoading[account.id]} on:click|preventDefault|stopPropagation={ setScopedRole(account.id, newScopedRole.role, newScopedRole.project.name) }>
            <span class="icon is-small">
              <FeatherIcon icon="save" />
            </span>
          </button>
        </div>
      </div>      
      {/if}
      <div class="row">
        <button class="button is-primary" disabled={addingScopedRole} on:click|preventDefault|stopPropagation={ showAddScopedRole }>
          <span>Add a realm-scoped role</span>
        </button>
      </div>
    </div>
    {/if}
  </span>
  <span slot="buttons" style="display:flex" let:toggle>
    <button class="button cancel" on:click|preventDefault={toggle}>
      <span>Close</span>
    </button>
    <p class="control">
      <RawDataButton
        title="Export roles"
        buttonName="Export roles"
        iconName="user"
        downloadName="roles.{account ? account.id : "x"}"
        resolveFunction={() => generateExport(account.id)}
      />
    </p>
  </span>
</ModalCard>

<style lang="scss">
  $adminColor: #EBC65F;
  $developerColor: #4497B7;
  $testerColor: #77B769;
  
  .perm-body {
    text-align: left;
    p {
        margin-bottom: 6px;
    }
    hr {
      height: 1px;    
      margin: 1rem 0;  
    }
    strong {
      color: white;
    }
    h3 {
      font-weight: bold;
      margin-bottom: 6px;
    }
    .container {
      width: 100%;
      margin-bottom: 24px;
      >:not(:last-child) {
        margin-bottom: 10px;
      }

      .row {
        display: flex;
        .destination {
          margin-right: 8px;
          flex-grow: 1;
        }
        .role {
          margin-right: 8px;
          width: 12rem;
        }
        .actions {
          display: flex;
          align-items: center;
        }
      }
    }

    .field.w12 .select {
      width: 12rem;
    }

    input {
      &.admin { color: $adminColor!important; }
      &.developer { color: $developerColor!important; }
      &.tester { color: $testerColor!important; }
    }

    .select {      
      width: 100%;
      select {
        background-color: #333236;
        color: white;
        width: 100%;
        &.admin { color: $adminColor!important; }
        &.developer { color: $developerColor!important; }
        &.tester { color: $testerColor!important; }
      }
      &:not(.is-multiple):not(.is-loading):hover:after {
        border-color: #3273dc;        
      }
      option {
        &.admin { color: $adminColor!important; }
        &.developer { color: $developerColor!important; }
        &.tester { color: $testerColor!important; }
      }
    }
  }

  .control :global(.modal-wrap){
    padding: 0;
  }

  .control :global(.modal-wrap .trigger-button){
    padding: 18px 22px;
  }
</style>
