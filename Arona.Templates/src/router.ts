import { createRouter, createWebHashHistory } from "vue-router";
import Ratings from "./templates/Ratings.vue";
import BattleResult from "./templates/BattleResult.vue"
import Lineup from "./templates/Lineup.vue"

export const router = createRouter({
    history: createWebHashHistory(),
    routes: [
        { path: "/ratings", name: "Ratings", component: Ratings },
        { path: "/battle-result", name: "Battle Result", component: BattleResult },
        { path: "/lineup", name: "Lineup", component: Lineup },
    ],
});