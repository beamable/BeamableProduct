<script>

    import { writable, derived, get } from 'svelte/store';
    import FeatherIcon from '../../../../../../../../../components/FeatherIcon';
    import { getServices } from '../../../../../../../../../services';
    import Chart from 'chart.js';
    import flatpickr from "flatpickr";

    const { microservices } = getServices();

    export let route //current route
    let serviceName;
    // let periodStore = writable(30);
    // let startTimeStore = writable(Math.floor(new Date().getTime()/1000)-(60 * 60));
    // let endTimeStore = writable(Math.floor(new Date().getTime()/1000));

    $:serviceName = route.params.serviceName

    let cpuDataStore;
    let memoryDataStore;
    let loadingFlags = {

    }
    let isLoading = true;
    $:isLoading = Object.keys(loadingFlags).findIndex(k => loadingFlags[k]) > -1 // TODO: Replace with .any() if it exists

    let startDateStore = writable(new Date(new Date() - (1000*60*60)))
    let endDateStore = writable(new Date())

    let startTimeStore = derived([startDateStore, endDateStore], (args) => Math.min(new Date(args[0]).getTime(), new Date(args[1]).getTime()) / 1000)
    let endTimeStore = derived([startDateStore, endDateStore], (args) => Math.max(new Date(args[0]).getTime(), new Date(args[1]).getTime()) / 1000)
    let periodStore = derived([startTimeStore, endTimeStore], (args) => {
        var start = args[0];
        var end = args[1];
        var secondsInRange = Math.abs(start - end);
        var maxSampleCount = 400;
        var period = secondsInRange / maxSampleCount; 
        var roundUpTo60 = Math.ceil(period / 60) * 60;
        return roundUpTo60;
    })


    // let startDate;
    // let endDate;
    // ELEMENTS TO BIND TO
    let cpuChartCanvas;
    let memoryChartCanvas;
    let startDateElement;
    let endDateElement;

    let timeSeriesOptions = {
        legend: {
            labels: {
                fontColor: "white",
                fontSize: 14
            }
        },
        scales: {
            yAxes: [{
                ticks: {
                    fontColor: "white",
                },
                gridLines: {
                    color: "white",
                    borderDash: [2, 5],
                },
                scaleLabel: {
                    display: true,
                    labelString: "% Capacity",
                    fontColor: "white"
                }
            }],
            xAxes: [{
                type: 'time',
                color: 'white',
                time: {
                    unit: 'minute'
                },
                ticks: {
                    fontColor: "white",
                }
            }]
        }
    }

    let labelToDataTable = {
        'average': 'average',
        'min': 'min',
        'max': 'max'
    }

    let cpuData = []

    let cpuLine = createDataLine('Cpu');
    let memoryLine = createDataLine('Memory');

    let cpuChart;
    let memoryChart;


    function hookupStreamToChart(metric, chart){
        var store = microservices.createMetricStream(serviceName, metric, periodStore, startTimeStore, endTimeStore)
        store.subscribe(data => {
            if (data == undefined) return;

            loadingFlags[metric] = data.loading;

            chart.data.datasets.forEach(function(dataset) {
                var dataField = labelToDataTable[dataset.label]
            
                dataset.data = data.data.map(d => ({
                    x: new Date(d.timestamp * 1000),
                    y: d[dataField]
                })).sort( (a, b) => a.x > b.x ? -1 : 1);
            });
            chart.update();
        })
        return store;
    }

    function initializeElements(){
        var cpuCtx = document.getElementById("cpu-chart").getContext("2d"); // TODO: replace with bound variables
        var memoryCtx = document.getElementById("memory-chart").getContext("2d");
        cpuChart = new Chart(cpuCtx, {
            type: "line",
            data: cpuLine,
            options: timeSeriesOptions
        });
        memoryChart = new Chart(memoryCtx, {
            type: "line",
            data: memoryLine,
            options: timeSeriesOptions
        });

        
        flatpickr(startDateElement, {
            enableTime: true,
            maxDate: new Date(),
            dateFormat: 'Z',
            altInput: true,
            altFormat: 'Y-m-d h:i K'
        });

        flatpickr(endDateElement, {
            enableTime: true,
            maxDate: new Date(),
            dateFormat: 'Z',
            altInput: true,
            altFormat: 'Y-m-d h:i K'
        });

    }
    $: if(cpuChartCanvas && !cpuChart){ // TODO: Check for afterRender()
        initializeElements()
        if (serviceName && !cpuDataStore && cpuChart){
            cpuDataStore = hookupStreamToChart('cpu', cpuChart)
        }
        if (serviceName && !memoryDataStore && memoryChart){
            memoryDataStore = hookupStreamToChart('memory', memoryChart)
        }
    }

    function createDataLine(metric){
        function createDataset(label, thickness, color){
            return {
                label: label,
                fill: false,
                lineTension: 0.01,
                borderWidth: thickness,
                borderColor: color,
                borderCapStyle: "butt",
                borderDash: [],
                borderDashOffset: 0.0,
                borderJoinStyle: "miter",
                pointBorderColor: color,
                pointBackgroundColor: color,
                pointBorderWidth: 1,
                pointHoverRadius: 5,
                pointHoverBackgroundColor: "rgb(0, 0, 0)",
                pointHoverBorderColor: color,
                pointHoverBorderWidth: 2,
                pointRadius: 1,
                pointHitRadius: 10,
                data: []
            }
        }
        return {
            datasets: [
                createDataset('average', 1.5, 'rgb(40, 130, 245)'),
                createDataset('min', 1, 'rgb(100, 240, 110)'),
                createDataset('max', 1, 'rgb(240, 130, 50)'),
            ]
        }
    }

</script>

<div class="charts">
    <div class="chart-header">
        <h2>Metrics</h2>
        <h1>{serviceName}</h1>

        <div class="filter">
            <div class="field">
                <label class="label">Start Date</label>
                <div class="control">
                    <input class="input is-info" type="text" placerholder="Pick a start date" bind:this={startDateElement} bind:value={$startDateStore}/>
                </div>
            </div>
            <div class="field">
                <label class="label">End Date</label>
                <div class="control">
                    <input class="input is-info" type="text" placerholder="Pick an end date" bind:this={endDateElement} bind:value={$endDateStore}/>
                </div>
            </div>
        </div>

    </div>

    {#if isLoading }
        <div class="main-loading">
            Fetching Metrics...
            <progress class="progress is-large is-info" max="100">60%</progress>
        </div>
    {/if}
    <div class="chart-wrapper" class:loading={isLoading}>
        <label>Cpu Utilization</label>
        <canvas bind:this={cpuChartCanvas} id="cpu-chart"></canvas>
    </div>
    <div class="chart-wrapper" class:loading={isLoading}>
        <label>Memory Utilization</label>
        <canvas bind:this={memoryChartCanvas} id="memory-chart"></canvas>
    </div>
</div>


<style lang="scss">
.charts{
    $left: 150px;
    $top: 140px;

    .chart-header {
        position: fixed;
        top: 70px;
        left: $left;
        right: 12px;
        h1 {
            font-size: 22px;
            font-weight: bold;
            margin-bottom: 12px;
        }
        .filter {
            display: flex;
            .field {
                flex-grow: 1;
                margin-right: 12px;
            }
        }
                   
    }

    position: absolute;
    left: 24px;
    top: calc(#{$top} + 120px);
    right: 24px;

    display: flex;
    flex-direction: row;
}
.chart-wrapper {
    &.loading {
        opacity: .3;
    }
    text-align: center;
    width: 50vw;
}
.main-loading {
    position: absolute;
    text-align: center;
    left: calc(50%-100px);
    top: 50%;
    progress {
        width: 200px;
        height: 8px;
        &.progress.is-info:indeterminate{
            background-color: #655f5f;
            background-image: linear-gradient(90deg,#3698d3b5 30%,#655f5f 0);
        }
    }
}
</style>