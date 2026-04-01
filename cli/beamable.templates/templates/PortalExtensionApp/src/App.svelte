<script lang="ts">

    import {Beam, type ExtensionContext} from '@beamable/portal-toolkit'

    export let context: ExtensionContext

    let beam: Beam

    context.beam.then(b => beam = b);

</script>

<beam-card style="margin-bottom: 20px">
    <beam-card-title>
        PortalExtensionApp
    </beam-card-title>
    <beam-card-text>
        <div  style="padding: 18px">
            <div>
                Player ID {beam?.player.id}
            </div>
            <beam-btn color="primary">
                Click
            </beam-btn>
        </div>
    </beam-card-text>
</beam-card>