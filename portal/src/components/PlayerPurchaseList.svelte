<script>
    import FilterSearch from './FilterSearch';
    import FeatherIcon from './FeatherIcon';
    import Paginate from './Paginate';
    import RawDataButton from './RawDataButton';
    import PageList from './PageList';
    import { getServices } from '../services';

    const { payments } = getServices();

    export let player; // :PlayerData
    export let pagedPayments = [];

    let totalEntries = 0;
    let pagedEntries = [];
    let hasAnyData = true;

    const stateMap = {
        'COMPLETED': 'good',
        'RESOLVED': 'good',
        'STARTED': 'pending',
        'VERIFIED': 'pending',
        'PROVISIONED': 'pending',
        'PENDING': 'pending',
        'FAILED': 'bad',
        'DISPUTED': 'bad',
        'CANCELLED': 'cancelled',
        'CLIENT_ISSUE': 'bad',
    }

    async function fetchPage(pageNumber, pageSize){
        const start = pageNumber * pageSize;
        const data = await payments.fetchPlayerPaymentPage(player, start, pageSize);
        pagedPayments = data;

        if (data === null) return [];
        
        if (pageNumber == 0 && data.length == 0){
            hasAnyData = false;
        }

        return data.map(transformPayment)
    }

    function transformPayment(payment){
        return {
            payment,
            store: payment.details.gameplace.substr('stores.'.length),
            quantity: payment.details.quantity,
            listing: payment.details.reference.substr('listings.'.length),
            sku: payment.details.sku.substr('skus.'.length),
            localPrice: payment.details.localPrice,
            localCurrency: payment.details.localCurrency,
            updated: toUTCString(payment.updated),
            state: payment.txstate
        }
    }

    function toUTCString(timeNumber){
        const d = new Date(timeNumber);
        return d.toLocaleString();
    }

</script>

<div class="payment-list">

    {#if hasAnyData}
        {#if pagedEntries.length > 0}
            <Paginate 
                paginationKey="payments"
                position="top"
                unbounded={true}
                bind:pagedElements={pagedEntries}
                fetchPage={fetchPage}/>

            <PageList elements={pagedEntries}
                headers={[
                    {name: 'updated', width: '23%'},
                    {name: 'qty', width: '7%'},
                    {name: 'listing', width: '22%'},
                    {name: 'provider', width: '22%'},
                    {name: 'value', width: '10%'},
                ]}
                leftFlagClass={e => stateMap[e.state]}>
            
                <div slot="element" let:element>

                    <div class="updated">
                        {element.updated}
                    </div>
                    <div>
                        <input type="text" class="input is-static narrow" readonly value={element.quantity}/>
                    </div>
                    <div>
                        <input type="text" class="input is-static" readonly value={element.listing}/>
                    </div>
                    <div>
                        <input type="text" class="input is-static" readonly value={element.payment.providername}/>
                    </div>
                    <div class="money">
                        {element.localPrice} {element.localCurrency}
                    </div>

                    <RawDataButton dataObject={element.payment} downloadName={'purchase_' + element.payment.txid}/>
                    
                </div>
            </PageList>
        {:else}
            No purchases on this page.
        {/if}

        <Paginate 
            paginationKey="payments"
            position="bottom"
            unbounded={true}
            bind:pagedElements={pagedEntries}
            fetchPage={fetchPage}/>
    {:else}
        There are no purchases for this player.
    {/if}

</div>

<style lang="scss">
    $red: #FF5c5c;
    $green: #77B769;
    $blue: #4497B7;
    $yellow: #EBC65F;
    .payment-list :global(.flags.good) {
        border-left-color: $green;
    }
    .payment-list :global(.flags.pending) {
        border-left-color: $yellow;
    }
    .payment-list :global(.flags.bad) {
        border-left-color: $red;
    }
    .payment-list :global(.flags.cancelled) {
        border-left-color: $blue;
    }

    .narrow {
        max-width: 40px;
        padding:12px;
        text-align: center;
    }
    .money {
        flex-grow: 1;
    }
    .updated {
        width: 180px;
    }

</style>