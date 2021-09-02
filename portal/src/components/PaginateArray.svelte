<script>
    import Paginate from './Paginate';

    export let elements=[];
    export let pagedElements=[];
    export let paginationKey;
    export let position="top";

    let forcePageRefresh;

    $: if (elements && forcePageRefresh){
        forcePageRefresh();
    }

    function fetchPage(pageNumber, pageSize){
        const start = Math.max(0, pageNumber * pageSize);
        const end = Math.min(elements.length , start + pageSize );
        return elements.slice(start, end);
    }
</script>

<Paginate 
    paginationKey={paginationKey}
    position={position}
    totalElements={elements.length}
    bind:pagedElements={pagedElements}
    bind:forceRefresh={forcePageRefresh}
    fetchPage={fetchPage}/>