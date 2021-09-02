import 'dom4';
import App from './App.svelte';

import config, {Config} from './config';
declare global {
  interface Window { config: Config; }
}
window.config = config;

export const app = new App({
  target: document.body
});

export default app;
