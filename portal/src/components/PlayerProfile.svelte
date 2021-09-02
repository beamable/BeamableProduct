<script>
    import PlayerData from '../services/players';
    import StatCollection from '../services/players';
    import FeatherIcon from './FeatherIcon';
    import WarningPopup from './WarningPopup';
    import AsyncInput from './AsyncInput';
    import ComponentButtons from './ComponentButtons';

    import { getServices } from '../services';

    const services = getServices()
    const { players } = services;
    const { realm } = services.realms;


    export let player;
    export let stats;

    let playerLocale = '?';
    let playerPlatform = '?';
    let playerInstallDate = '?';
    let gamerTag = 0;

    let isNetworking;


    $: hasStats = stats && stats.length;
    $: playerLocale = hasStats ? stats.find(s => s.name === 'locale').value : '?';
    $: playerPlatform = hasStats ? stats.find(s => s.name === 'THORIUM_GAME_PLATFORM').value : '?';
    $: playerInstallDate = hasStats ? stats.find(s => s.name === 'INSTALL_DATE').value : '?';

    realm.subscribe(function (realm) {
        if (realm) {
            gamerTag = player.gamerTagForRealm();
        }
    });


    function findThirdParty(thirdPartyName){
        if (!thirdPartyName) return false;
        return player.thirdParties.find(thirdParty => thirdParty.name === thirdPartyName);
    }

    async function deleteThirdParty(thirdParty){
        player = await players.removeThirdParty(player, thirdParty);
    }

    async function handleForgetUser(){
        const erasedPlayer = await players.forgetUser(player);
        player = erasedPlayer;
        player.email = player.email || '';
    }

    function isEmailValid(email) {
        const isValid = email.indexOf('@') > 2 && email.indexOf('.') > 3;
        if (!isValid) {
            return 'Email is invalid';
        }
    }

    async function handleEmailWrite(next, old){
        await players.updateEmail(player, next);
        return next;
    }

</script>

<style lang="scss">
    .player-profile {
        position: relative;
        padding: 14px 0px;
    }
    .player-profile .field {
        display: flex;
        flex-direction: row;
    }
    .player-profile .field label {
        margin-right: 12px;
        padding-top: 7px;
        min-width: 100px;
        max-width: 100px;
    }
    .player-profile .field .control-with-buttons {
        flex-grow: 1;
        display:flex;

    }
    .player-profile .field .control-with-buttons form,
    .player-profile .field .control-with-buttons .control {
        flex-grow: 1;
    }

    .third-parties {
        display: flex;
        flex-direction: row-reverse;
        flex-grow: 1;
    }

    .third-party-logo {
        position: absolute;
        top: -25%;
        bottom: 25%;
        right: 0;
        left: 0;
        cursor: pointer;
    }

    .third-party-logo.active {
      opacity: .9;
    }

    .third-party-logo.inactive {
      opacity: .3;
    }

    .third-party-logo.active:hover {
        opacity: 1;
    }
    .third-party-logo.inactive:hover {
        opacity: .4;
    }

    .third-party-button {
        padding: 0px;
        margin: 0px;
        background: none;
        border: none;
        position: relative;
        width: 64px;
        height: 64px;
        outline: none;
    }

    .third-party-logo.facebook{
        background: url(assets/Icons/facebook_thirdparty.svg) 0 0/100% 100%;
    }

    .third-party-logo.apple{
        background: url(assets/Icons/apple_thirdparty.svg) 0 0/100% 100%;
    }

    .buttons {
        width: 92px;
    }

    .divider {
        display: inline;
        position: absolute;
        top: 16px;
        bottom: 16px;
        border-right: solid 1px gray;
        left: calc(66% + 2px);
    }

</style>


<div class="columns player-profile">

    <ComponentButtons>
        <p class="control">
            <WarningPopup
                header="Forget User [GDPR]"
                message="Removing all Personal Identifying Information cannot be undone. This operation should only be completed to comply with GDPR."
                headerClass="light-header"
                left={25}
                top={-85}
                onConfirmFunction={() => handleForgetUser()}>
                <span slot="trigger" let:toggle>
                    <button class="button trigger-button" on:click|preventDefault|stopPropagation={toggle}>
                        <span class="icon is-small">
                            <FeatherIcon icon="archive"/>
                        </span>
                        <span>
                            Forget User
                        </span>
                    </button>

                </span>
                <span slot="primary-button">
                    Forget
                </span>
            </WarningPopup>
        </p>
    </ComponentButtons>


    <div class="column">
        <div class="field">
            <label class="label">Email</label>

            <AsyncInput
                value={player.email}
                inputType="email"
                placeholder="No Email Provided"
                onWrite={handleEmailWrite}
                onValidate={isEmailValid}
                editable={player.email !== undefined && player.email.length > 0}
                floatError={true}
                buttonTopPadding={4}
            />
        </div>
        <div class="field">
            <label class="label">DBID</label>
            <div class="control-with-buttons">
                <div class="control dbid-control">
                    <input class="input is-static" type="text" placeholder="dbid" readonly bind:value={gamerTag}>

                </div>
                <div class="buttons">
                    <!-- only exists to provide consisten spacing with email input. -->
                </div>
            </div>
        </div>

        <div class="field">
            <label class="label"> Associations</label>
            <div class="third-parties" style="padding-top: 5px; margin-right: 92px; height: 50px;">

                {#if player.thirdParties.length === 0}
                    <div>
                        No third party associations
                    </div>
                {:else}
                    {#each player.thirdParties as thirdParty}

                        <!-- 1202055381693493 -->
                        <WarningPopup
                            header="remove {thirdParty.name}"
                            message="Removing a third party association cannot be undone."
                            onConfirmFunction={() => deleteThirdParty(thirdParty)}>
                            <span slot="trigger" let:toggle>
                                <button class="third-party-button no-select" on:click|preventDefault|stopPropagation={toggle}>
                                    <div class="third-party-logo {thirdParty.name} active"></div>
                                </button>
                            </span>
                        </WarningPopup>

                    {/each}
                {/if}

            </div>
        </div>


    </div>

    <div class="divider">
    </div>

    <div class="column is-one-third">
        <div class="field">
            <label class="label">Install Date</label>
            <div class="control">
                <input class="input is-static" type="text" placeholder="?" readonly bind:value={playerInstallDate}>
            </div>
        </div>

        <div class="field">
            <label class="label">Location</label>
            <div class="control">
                <input class="input is-static" type="text" placeholder="?" readonly bind:value={playerLocale}>
            </div>
        </div>

        <div class="field">
            <label class="label">Platform</label>
            <div class="control">
                <input class="input is-static" type="text" placeholder="?" readonly bind:value={playerPlatform}>
            </div>
        </div>

    </div>
</div>
