<script>
  import Layout from '../../components/Theme/Layout';
  import Navigate from '../../components/Navigate';
  import Nav from './_nav.svelte';
  import { getServices } from '../../services';
  const services = getServices();

  export let route;

  const { isLoggedIn, isLoading, canLogIn, logout } = services.auth;
</script>

<style>
  .is-smaller {
    margin-top: 20px;
  }
</style>
{#if !$isLoading}
  {#if $isLoggedIn}
    <Nav />
    <div class="view-wrapper">
      <div class="is-smaller">
        <div class="question-content is-large">
          <slot />
        </div>
      </div>
    </div>
  {:else if $canLogIn}
    {#if route.name === 'orgId/login'}
      <slot />
    {:else}
      <Navigate replace href="/:orgId/login" />
    {/if}
  {:else}
    <Navigate replace href="/" />
  {/if}
{/if}
