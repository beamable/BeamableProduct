<script>
	import { Beam } from 'beamable-sdk';

	let loading = false, error = null;
	let playerId = "None"

	async function init(){
		loading = true; error = null;

		const beam = await Beam.init({
			cid: '33995890352142336',
			pid: 'DE_33995890356336641',
			environment: 'dev',
		});
		playerId = beam.player.id;
		loading = false;
	}

</script>

<h1>Your player Id: {playerId}</h1>

<button onclick={init} disabled={loading}>
  {'Init Beamable'}
</button>