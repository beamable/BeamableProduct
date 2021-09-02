<svelte:head>
  <title>Login - Beamable</title>
</svelte:head>

<style lang="scss">
  .hero.is-success {
    background: rgb(31,31,31);
  }

  .hero .nav,
  .hero.is-success .nav {
    box-shadow: none;
  }

  .org-id {
    font-weight: 700;
  }

  p.subtitle {
    padding-top: 1rem;
    &.is-danger {
      color: red;
    }
  }

  .login-hr {
    border-bottom: 1px solid white;
  }

  .has-text-white {
    color: white;
  }

  input {
    font-weight: 300;
  }

  .field {
    padding-bottom: 10px;
  }

  .fa {
    margin-left: 5px;
  }

  .login-container {
    max-width: 400px;
    margin: auto;
  }
  
  .button.forgot {
    background: none;
    color: white;
    outline: none;
    border: none;
    text-align: right;
    padding: 0px;
    display: flex;
    margin-top: 12px;
    margin-bottom: -12px;
    text-decoration: underline;
    color: lightgray;
    &:hover {
      color: white;
      
    }
  }
  .forgot-finish,
  .forgot-start {
    .text {
      color: white;
      text-align: left;
      margin-bottom: 12px;
    }
  }

</style>

<script>
  import Navigate from '../../components/Navigate';

  import { getServices } from '../../services';

  const services = getServices();

  const {
    router: {
      orgId
    },

    auth: {
      isLoading,
      isLoggedIn,

      username,
      password,
      code,
      error,

      login,
      forgotPassword,
      finishForgotPassword
    }
  } = services;

  let didForgetPassword;
  let hasCode;
  let isNetworking = false;
  
  async function sendForgotPassword(){
    didForgetPassword = true;
    hasCode = false;
    isNetworking = true;
    try {
      await forgotPassword();
      hasCode = true;
    } finally {
      isNetworking = false;
    }
  }
  async function sendFinishForgotPassword(){
    isNetworking = true;
    try {
      await finishForgotPassword();
      didForgetPassword = false;
    } catch (ex){
      didForgetPassword = true;
    } finally {
      isNetworking = false;
    }
  }

</script>

{#if $isLoggedIn}

  <Navigate href="/:orgId" />

{:else}

  <section class="hero is-success is-fullheight">
    <div class="hero-body">
      <div class="container has-text-centered">
        <div class="login-container">

          <h3 class="title has-text-white">Disruptor Engine</h3>
          <hr class="login-hr">
          <p class="subtitle has-text-white">
            Signing in as <span class="org-id">{ $orgId }</span>
          </p>

          {#if $error}
            <p class="subtitle is-danger">
              {$error}
            </p>
          {/if}

          {#if !didForgetPassword}
          <div class="box">
            <form on:submit|preventDefault={ login }>
              <fieldset disabled={ $isLoading }>
                <div class="field">
                  <div class="control">
                    <input
                      bind:value={ $username }
                      class="input is-large"
                      type="email"
                      placeholder="Your Email"
                      autofocus=""
                    />
                  </div>
                </div>

                <div class="field">
                  <div class="control">
                    <input
                      bind:value={ $password }
                      class="input is-large"
                      type="password"
                      placeholder="Your Password"
                    />
                  </div>
                </div>

                <button type="submit" class="button is-block is-info is-large is-fullwidth" class:is-loading={$isLoading} disabled={!$username || !$password}>
                  Login <i class="fa fa-sign-in" aria-hidden="true" />
                </button>
                <button class="button forgot" on:click|preventDefault={() => didForgetPassword = true}>
                    Forgot Password
                </button>
              </fieldset>
            </form>

          </div>
          {:else if !hasCode}
          <div class="box forgot-start">
            <div class="text">
                We'll send your email address a one-time use password reset code.
              
            </div>
              
            <form on:submit|preventDefault={ sendForgotPassword }>
              <fieldset disabled={ $isLoading }>
                <div class="field">
                  <div class="control">
                    <input
                      bind:value={ $username }
                      class="input is-large"
                      type="email"
                      placeholder="Your Email"
                      autofocus=""
                    />
                  </div>
                </div>

                <button type="submit" class="button is-block is-info is-large is-fullwidth" class:is-loading={isNetworking} disabled={!$username}>
                  Send Code
                </button>
                <button class="button forgot" on:click|preventDefault={() => didForgetPassword = false}>
                  Back
                </button>
              </fieldset>
            </form>
          </div>

          {:else if hasCode}
          <div class="box forgot-finish">
            <div class="text">
                Enter the code we sent to {$username}
            </div>
              
            <form on:submit|preventDefault={ sendFinishForgotPassword }>
              <fieldset disabled={ $isLoading }>
                <div class="field">
                  <div class="control">
                    <input
                      bind:value={ $code }
                      class="input is-large"
                      type="text"
                      placeholder="Your Code"
                      autofocus=""
                    />
                  </div>
                </div>
                <div class="field">
                  <div class="control">
                    <input
                      bind:value={ $password }
                      class="input is-large"
                      type="password"
                      placeholder="Your New Password"
                    />
                  </div>
                </div>

                <button type="submit" class="button is-block is-info is-large is-fullwidth" class:is-loading={isNetworking} disabled={!$code || !$password}>
                  Update Password & Login
                </button>
                <button class="button forgot" on:click|preventDefault={() => hasCode = false}>
                  Back
                </button>
              </fieldset>
            </form>
          </div>

          {/if}

        </div>
      </div>
    </div>
  </section>

{/if}
