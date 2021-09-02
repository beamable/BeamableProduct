<script>
  import feather from 'feather-icons';
  $: ({ icon, ...attrs } = $$props);
	$: icon = feather.icons[icon];
</script>

{#if icon}
	{@html icon.toSvg(attrs)}
{/if}
