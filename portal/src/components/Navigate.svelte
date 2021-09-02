<script>
  export let href;
  export let replace = false;
  const {url} = window.router;

  $: if (href) {
    if (replace) {
      history.replaceState({}, '', url(href));
    } else {
      history.pushState({}, '', url(href));
    }
  }
</script>
