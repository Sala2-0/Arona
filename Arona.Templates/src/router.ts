import { createRouter, createWebHashHistory } from "vue-router";
import Ratings from "./templates/Ratings.vue";

export const router = createRouter({
    history: createWebHashHistory(),
    routes: [
        { path: "/ratings", name: "Ratings", component: Ratings },
    ],
});