<script>
    export let logRow=undefined;
    export let onSelected = () => {}

    let message = '';
    let payload = {};

    $: payload = logRow == undefined ? {} : parsePayload(logRow)
    $: message = payload.message;
    $: timestamp = payload.timestamp;
    $: level = payload.level == undefined ? '' : payload.level;
    $: dateText = (new Date(timestamp).toUTCString())
    $: levelClass = getLevelClass(level)

    function getLevelClass(level){
        if (['Fatal', 'Error'].indexOf(level) > -1){
            return 'danger'
        }
        if (level == 'Warning'){
            return 'warn';
        }
        if (level == 'Info' || level == undefined){
            return 'normal'
        }
        return 'faded'
    }

    function parsePayload(data){
        try {
            const raw = JSON.parse(data.message);
            return {
                message: raw['__m'],
                timestamp: data.timestamp,
                level: raw['__l'] || 'Warn',
                raw,
            }
        } catch {
            return {
                message: data.message,
                timestamp: data.timestamp,
                level: 'Warn'
            }
        }
    }

    function handleClick(){
        let data = {
            payload,
            message,
            level,
            timestamp,
            dateText,
            levelClass,
            raw: logRow
        }
        if (onSelected){
            onSelected(data);
        }
    }

</script>

<style lang="scss">
    .log-row {
        max-height: 48px;
        height: 48px;
        cursor: pointer;

        &:hover {
            background-color: rgba(25, 70, 220, .4);
        }

        .time {
            font-size: 12px;
            opacity: .9;
        }
        .line {
            flex-direction: row;
            display: flex;
            font-size: 18px;
            
            .level {
                min-width: 100px;
                opacity: .8;
                text-transform: uppercase;
                &.faded {
                    color: lightgreen;
                }
                &.danger {
                    color: red;
                    font-weight: bold;
                }
                &.warn {
                    color: orange;
                }
                &.normal {
                    color: cornflowerblue;
                }
            }

            .log-message {
                white-space: nowrap;
                overflow: hidden;
                text-overflow: ellipsis;
                flex-grow: 1;
            }
        }

    }
</style>

<div class="log-row" on:click={evt => handleClick()}>
    <div class="time">
        {dateText}
    </div>
    <div class="line ">
        <div class="level {levelClass}">
            {level}
        </div>
        <pre class="log-message">
            {message}
        </pre>
    </div>

</div>