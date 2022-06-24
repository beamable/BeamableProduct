import widget from '../src/Widget.svelte';
export default function(mountSite){
    new widget({target: mountSite, props: {}});
}