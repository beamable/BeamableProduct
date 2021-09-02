<script>
    import dagre from 'dagre';
    import graphlib from 'graphlib';
    import joint from "jointjs";
    import { onMount, onDestroy, afterUpdate } from 'svelte';
    import { getServices } from '../../../../services';
    import svgPanZoom from 'svg-pan-zoom/src/svg-pan-zoom.js';
    import Card from '../../../../components/Card';
    import RealmProperties from '../../../../components/RealmProperties';
    const { realms } = getServices()
    export let route //current route

    const { changeRoute } = window.router;

    let orgId;
    let gameId;

    realms.cid.subscribe(value => orgId = value);
    $:gameId = route.params.gameId;

    let treeContainer;
    let graphLayout;
    let graph;
    let paper;
    let boxIds = [];
    let boxLookup = {};
    let panZoom;
    let panInterval;

    let projects = undefined;
    let projectMap = {};
    let selectedProject;

    let realmSubscription = realms.gameRealms.subscribe(data => {
        if (!data){
            projects = undefined;
            return;
        }
        projects = data.map(upgradeProject)
        generateGraph(projects);
    });

    onDestroy( () => {
        projects = [];
        projectMap = {};
        selectedProject = undefined;
        realmSubscription();
    });


    onMount(async () => {
        if (projects){
            generateGraph(projects);

        }
    });

    $: if (projects && projects.length ) {
        projectMap = projects.reduce( (agg, curr) => ({
            ...agg, 
            [curr.pid]: curr
        }), {});

        selectedProject = selectedProject || projects[projects.findIndex(p => !p.archived)];
       
        selectProject(boxLookup[selectedProject.pid])
    }

    function upgradeProject(project){
        // opportunity to add view level data to network model.
        return {
            ...project,
        }
    }

    afterUpdate( () => {
        if (paper){
            paper.dumpViews();
        }
        
        if (selectedProject){
        
            selectProject(boxLookup[selectedProject.pid])
        }
    })

    function getPanPosition(model){
        if (!model) return {x: 0, y: 0}

        var realZoom= panZoom.getSizes().realZoom;
        var bb = treeContainer.getBoundingClientRect();

        var x = -model.attributes.position.x*realZoom + (bb.width * .5) - (model.attributes.size.width * .5 * realZoom) ;
        var y = -model.attributes.position.y*realZoom + (bb.height * .5) - (model.attributes.size.height * .5 * realZoom);

        return {
            x,
            y
        }
    }

    function selectProject(model){
        if (!panZoom) return;

        var currentPan = panZoom.getPan();
        var targetPan = getPanPosition(model);
        var panAmount = {
            x: targetPan.x - currentPan.x,
            y: targetPan.y - currentPan.y
        }
        // TODO: On first load, don't animate (somehow)
        animatePan(panAmount);

        model.attr({
            body: {
                fill: '#0095f1'
            }
        });
    }

    function unselectProject(model){
        model.attr({
            body: {
                fill: 'grey'
            }
        });
    }


    function animatePan(amount){ 
        clearInterval(panInterval)

        var animationTime = 300 // ms
        , animationStepTime = 15 // one frame per 30 ms
        , animationSteps = animationTime / animationStepTime
        , animationStep = 0
        , stepX = amount.x / animationSteps
        , stepY = amount.y / animationSteps

        panInterval = setInterval(function(){
        if (animationStep++ < animationSteps) {
            panZoom.panBy({x: stepX, y: stepY})
        } else {
            // Cancel interval
            clearInterval(panInterval)
        }
        }, animationStepTime)
    }

    function generateGraph(projects) {
        if (!treeContainer) {
            return; // cannot update graph without parent element set.
        }

        graph = new joint.dia.Graph;
        var gridSize = 1;
        var currentScale = 1;
        boxIds = [];
        paper = new joint.dia.Paper({
            el: treeContainer,
            model: graph,
            width: '100%',
            height: '100%',
            gridSize: gridSize,
            snapLinks: true,
            linkPinning: false,
            embeddingMode: true,
            interactive: () => false
        });

        paper.on('cell:pointerdblclick', function(cellView){
            var pid = cellView.model.attributes.attrs.id;
            if (pid){
                changeRoute(`/:orgId/games/:gameId/realms/${pid}/`);
            }
        });

        paper.on('cell:mouseover', function(cellView){
            var pid = cellView.model.attributes.attrs.id;
            var project = projectMap[pid];
            if (project == selectedProject){
                cellView.model.attr({
                    body: {
                        fill: '#0095f1'
                    }
                });
            } else {
                cellView.model.attr({
                    body: {
                        fill: '#6490aa'
                    }
                });          
            }
        });
        paper.on('cell:mouseout', function(cellView){
            var pid = cellView.model.attributes.attrs.id;
            var project = projectMap[pid];
            if (project == selectedProject){
                selectProject(cellView.model);
            } else {
                unselectProject(cellView.model);
            }
        });

        paper.on('cell:pointerdown', function(cellView, evt, x, y){
            var pid = cellView.model.attributes.attrs.id;
            if (!pid) return;
            
            boxIds.forEach(id => {
                unselectProject(graph.getCell(id));
            })

            selectProject(cellView.model)

            var project = projectMap[pid];
            selectedProject = project;
        });

        var svgNode = treeContainer.childNodes.item(2);

        panZoom = svgPanZoom(svgNode, 
        {
            center: true,
            zoomEnabled: true,
            panEnabled: true,
            dblClickZoomEnabled: false,
            controlIconsEnabled: false,
            fit: false,
            minZoom: 0.5,
            maxZoom:2,
            zoomScaleSensitivity: 0.5
        });

        var nodeLookup = {};
       

        projects.forEach(project => {
            if (project.archived){
                return;
            }

            let box = createProjectBox(project);
            nodeLookup[project.pid] = box.id;
            boxIds.push(box.id)
            graph.addCell(box);
        })

        projects.forEach(project => {
            if (project.archived){
                return;
            }
            let links = createProjectLinks(project, nodeLookup);
            if (links){
                links.forEach(link => {
                    link.addTo(graph);
                })
            }
        })

        graphLayout = joint.layout.DirectedGraph.layout(graph, {
            dagre: dagre,
            graphlib: graphlib,
            nodeSep: 50,
            edgeSep: 50,
            rankDir: "TB",
            clusterPadding: {
                top: 10,
                left: 10,
                right: 10,
                bottom: 10
            }
        });
       
       
       setTimeout(() => {
            boxIds.forEach(id => {
                selectProject(graph.getCell(id))
                unselectProject(graph.getCell(id));
            })

            if (selectedProject){
                panZoom.pan(getPanPosition(boxLookup[selectedProject.pid]));
                selectProject(boxLookup[selectedProject.pid])
            }
            paper.dumpViews();
       },1)


    }

    function createProjectBox(project) {
        let box = new joint.shapes.standard.Rectangle();
        box.resize(150, 50);
        box.attr({
            id: project.pid,
            link: {
                xlinkShow: 'new',
                cursor: 'pointer'
            },
            body: {
                rx: '15'
            },
            label: {
                text: project.projectName,
                fill: 'white'
            }
        });
        unselectProject(box);
        boxLookup[project.pid] = box;
        return box;
    }

    function createProjectLinks(project, nodeLookup) {
        var children;

        if (project.children !== undefined) {
            children = project.children.filter(c => nodeLookup[c] && !nodeLookup[c].archived);
        } else {
            children = [];
        }

        var links = children.map(childPid => {
            var link = new joint.shapes.standard.Link();
            link.target({id: nodeLookup[project.pid]});
            link.source({id: nodeLookup[childPid]});
            return link;
        }).filter(l => l);

        return links;
    }

</script>

<style>
    .panel {
        background-color: #454545;
    }
    .realm-graph-wrapper {
    }

    .realm-graph-wrapper :global(.panel.has-background) {
        padding: 1px;
    }
    .realm-graph-wrapper :global(.panel > div) {
        height: calc(100vh - 600px);
        overflow: hidden;

    }
    :global(svg g[data-type="standard.Rectangle"]) {
        cursor: pointer;
    }
</style>



<RealmProperties realm={selectedProject} realmMap={projectMap} orgId={orgId} gameId={gameId}/>
<div class="realm-graph-wrapper">
    <Card title="Game Tree" data={projects} loadingHeight={373}>
        <div id="game-tree-container" bind:this={treeContainer} ></div>
    </Card>
</div>
