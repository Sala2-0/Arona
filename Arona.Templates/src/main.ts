import { createApp } from 'vue';
import App from './App.vue';
import { router } from './router';

declare global {
    interface Window {
        __APP_DATA__?: any;
        __VUE_READY__?: boolean;
    }
}

const app = createApp(App);
app.use(router);
app.mount('#app');

router.isReady().then(() => {
    window.__VUE_READY__ = true;
});