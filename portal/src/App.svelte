<script>
  import './styles/index.scss';

  import { Router, url, route } from "svelte-filerouter";
  
  window.router = {
    url, 
    route,
    changeRoute
  }
  
  let errors = []
  window.postError = function(title, message){
    addError({
      title, message
    });
  }
  window.addEventListener("error", function (e) {
    console.error('error', e);

    addError({
      title: 'error',
      message: e.error
    });
    return false;
  })
  window.addEventListener('unhandledrejection', function (e) {
    if (e.reason.error === 'invalidRole'){
      return;
    }
    
    console.error('promise error', e);
    addError({
      title: e.reason.error,
      message: e.reason.message
    });
  })

  function addError(err){
    errors = [...errors, err];
  }

  function removeError(errorIndex){
    errors = errors.filter((e, i) => i != errorIndex);
  }

  function changeRoute(path){
    const a = document.createElement("a");
    a.style.display = "none";
    document.body.appendChild(a);
    a.href = url(path);

    // Trigger the download by simulating click
    a.click();

    // Cleanup
    window.URL.revokeObjectURL(a.href);
    document.body.removeChild(a);
  }


</script>

<style>
  #notification-container {
    position: fixed;
    bottom: 24px;
    right: 24px;
    left: calc(100% - 300px);
  }
</style>

<Router />

<div id="notification-container">
  {#each errors as error, index}
    <div class="notification is-danger">
      <button class="delete" on:click|preventDefault={evt => removeError(index)}></button>
      <strong> {error.title} </strong>
      <br>
      {error.message}
    </div>
  {/each}
</div>

