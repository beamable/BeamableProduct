<script>
    import FeatherIcon from './FeatherIcon';
    import ModalCard from './ModalCard';
    export let dataObject={};
    export let downloadName='data';
    export let title='Raw Data';
    export let buttonName='View';
    export let iconName="database";
    export let resolveFunction = resolveData;

    let preElement;
    let isLoading = false;
    let json;

    async function init(){
        isLoading = true;
        try {
            const dataObject = await resolveFunction();
            if (typeof dataObject === 'object'){
                json = JSON.stringify(dataObject, null, 2);
            } else {
                json = dataObject;
            }
        } finally {
            isLoading = false;
        }

    }

    async function open(toggle){
        await init();
        toggle();
    }

    function resolveData(){
        return dataObject;
    }

    function copyFile(toggle){
        const textArea = document.createElement('textarea');
        textArea.textContent = preElement.textContent;
        document.body.append(textArea);
        textArea.select();
        document.execCommand("copy");
        textArea.remove();
    }

    function downloadFile(toggle) {
        const type = "application/json";
        const data = json;
        const fileName = `${downloadName}.json`;
        // Create an invisible A element
        const a = document.createElement("a");
        a.style.display = "none";
        document.body.appendChild(a);

        // Set the HREF to a Blob representation of the data to be downloaded
        a.href = window.URL.createObjectURL(
            new Blob([data], { type })
        );

        // Use download attribute to set set desired file name
        a.setAttribute("download", fileName);

        // Trigger the download by simulating click
        a.click();

        // Cleanup
        window.URL.revokeObjectURL(a.href);
        document.body.removeChild(a);
    }

</script>

<p class="modal-wrap">
<ModalCard class="is-xsmall has-light-bg add-currency">
    <div slot="trigger-evt" let:toggle let:active>
        <button class="button trigger-button" class:is-loading={isLoading} on:click|preventDefault|stopPropagation={evt => open(toggle)}>
            <span class="icon is-small">
                <FeatherIcon icon={iconName}/>
            </span>
            {#if buttonName && buttonName.length > 0}
                <span>
                    {buttonName}
                </span>
            {/if}
        </button>
    </div>
    <h3 slot="title">
        {title}
    </h3>
    <span slot="body" class="json-wrapper" class:is-loading={isLoading}>
        <pre bind:this={preElement}>
            {json}
        </pre>
    </span>

    <span slot="buttons" let:toggle>
        <button class="button" on:click|preventDefault|stopPropagation={toggle}>
            <span>Close</span>
        </button>
        <button class="button" on:click|preventDefault|stopPropagation={copyFile}>
            <span>Copy</span>
        </button>
        <button class="button is-info" on:click|preventDefault|stopPropagation={evt => downloadFile(toggle)}>
            <span>Download</span>
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

    .json-wrapper {
        background: #333336;
    }


    .trigger-button {
        padding: 6px 12px;
    }
    p.modal-wrap {
        padding-top: 5px;
    }
</style>