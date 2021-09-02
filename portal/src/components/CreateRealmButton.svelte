<script>
    import FeatherIcon from './FeatherIcon';
    import ModalCard from './ModalCard';
    import { get } from 'svelte/store';

    import { getServices } from '../services';

    const services = getServices();
    const { realms, router } = getServices();
    const { orgId } = services.router;

    export let title;
    export let buttonText='Create New Realm';

    let cid;
    let realmName;

    orgId.subscribe(value => {
        cid = value;
    });

    function open(toggle) {
        realmName = "";
        toggle();
    }

    async function createRealm(toggle) {
        await realms.createGame(realmName);
        toggle();
    }

</script>

<p class="modal-wrap">
<ModalCard class="is-xsmall has-light-bg add-currency">
    <div slot="trigger-evt" let:toggle let:active>
        <button class="button trigger-button" on:click|preventDefault|stopPropagation={evt => open(toggle)}>
            <span class="icon is-small">
                <FeatherIcon icon="plus"/>
            </span>
            <span>{buttonText}</span>
        </button>
    </div>
    <h3 slot="title">{title}</h3>

    <span slot="body">
        <span>
            Are you sure you want to create a new game?
        </span>
        <div class="field">
            <div class="control">
                <input class="input is-primary" type="string" placeholder="New game name" bind:value={realmName}>
            </div>
        </div>
    </span>

    <span slot="buttons" let:toggle>
        <button class="button" on:click|preventDefault={toggle}>
            <span>Close</span>
        </button>
        <button class="button is-info" on:click|preventDefault={evt => createRealm(toggle)}>
            <span>Create</span>
        </button>
    </span>
</ModalCard>
</p>

<style lang="scss">
    pre {
        color: white;
        background: black;
        text-align: left;
        user-select: text;
        cursor: text;
        max-height: 500px;
    }
    .trigger-button {
        padding: 6px 12px;
    }
</style>