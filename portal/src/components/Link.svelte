<script>
  const {url} = window.router;

  export let href;
</script>

<slot href={url(href)} />
