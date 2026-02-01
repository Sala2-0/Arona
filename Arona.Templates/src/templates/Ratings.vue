<script setup lang="ts">
import { onMounted, nextTick } from 'vue';
import { getStar } from '../extensions';

interface ClanRating {
    Name: string;
    Color: string;
    League: number;
    Division: number;
    DivisionRating: number;
    GlobalRank: number;
    RegionRank: number;
    Stage: Stage | null;
    Teams: TeamRating[];
}

interface TeamRating {
    TeamNumber: number;
    TeamName: string;
    Color: string;
    Battles: number;
    WinRate: number;
    SuccessFactor: number;
    League: number;
    Division: number;
    DivisionRating: number;
    Stage: Stage | null;
}

interface Stage {
    Type: "promotion" | "demotion";
    Progress: ("victory" | "defeat" | "draw")[];
}

const exampleData: ClanRating = {
    Name: '[NTT] Entity',
    Color: '#bee7bd',
    League: 1,
    Division: 1,
    DivisionRating: 64,
    GlobalRank: 41,
    RegionRank: 14,
    Teams: [
        {
            TeamNumber: 1,
            TeamName: 'Alpha',
            Color: '#cc9966',
            Battles: 2,
            WinRate: 100,
            SuccessFactor: 3.12,
            League: 4,
            Division: 1,
            DivisionRating: 99,
            Stage: {
                Type: "promotion",
                Progress: ["defeat"]
            }
        },
        {
            TeamNumber: 2,
            TeamName: 'Bravo',
            Color: '#bee7bd',
            Battles: 185,
            WinRate: 55.14,
            SuccessFactor: 11.9,
            League: 1,
            Division: 1,
            DivisionRating: 64,
            Stage: null
        }
    ],
    Stage: null
};

const data: ClanRating = import.meta.env.DEV
    ? exampleData
    : window.__APP_DATA__;

onMounted(async () => {
    await nextTick();
    window.__VUE_READY__ = true;
});
</script>

<template>
    <div id="clan_root">
        <div id="top">
            <div class="circle" :style="{ color: data.Color }">
                <div class="line" v-for="_ in data.Division"></div>
            </div>

            <div class="progress"  v-if="data.Stage === null">
                <p>{{ data.DivisionRating }}</p>
            </div>

            <div class="progress" v-else>
                <img class="star" v-for="i in 5" :src="getStar(data.Stage.Progress[i - 1]!)" alt="">
                <div :class="['arrow', data.Stage.Type === 'promotion' ? 'promotion' : 'demotion']"></div>
            </div>

            <p id="clan_name">{{ data.Name }}</p>
        </div>

        <div id="ratings">
            <div class="rating" v-for="team in data.Teams">
                <p class="rating-name">{{ team.TeamNumber }}</p>

                <div class="circle" :style="{ color: team.Color }">
                    <div class="line" v-for="_ in team.Division"></div>
                </div>

                <div class="progress"  v-if="team.Stage === null">
                    <p>{{ team.DivisionRating }}</p>
                </div>

                <div class="progress" v-else>
                <img class="star" v-for="k in 5" :src="getStar(team.Stage.Progress[k - 1]!)" alt="">
                <div :class="['arrow', team.Stage.Type === 'promotion' ? 'promotion' : 'demotion']"></div>
                </div>

                <div class="stats">
                    <div class="stats-parameter">
                        <p>Battles:</p>
                        <p>{{ team.Battles }}</p>
                    </div>

                    <div class="stats-parameter">
                        <p>Win rate:</p>
                        <p>{{ team.WinRate }}%</p>
                    </div>

                    <div class="stats-parameter">
                        <p>S/F:</p>
                        <p>{{ team.SuccessFactor }}</p>
                    </div>
                </div>
            </div>
        </div>

        <div class="rankings">
            <p>Global ranking: #{{ data.GlobalRank }}</p>
            <p>Region ranking: #{{ data.RegionRank }}</p>
        </div>
    </div>
</template>

<style scoped>
p {
    margin: 0;
}

#clan_root {
    font-family: "Inter";
    color: white;

    width: 1000px; height: 1100px;
    margin: 0;

    background-color: #323232;

    display: flex;
    flex-direction: column;
    align-items: center;
}

.circle {
    width: 100px;
    height: 100px;

    border: solid 15px;
    border-radius: 100%;

    display: flex;
    align-items: center;
    justify-content: center;
    gap: 7px;
}

.line {
    height: 4.3em;
    width: 13px;

    background-color: #ffff;

    border-radius: 2px;
}

.star {
    width: 70px;
}

#top {
    margin-top: 50px;

    display: flex;
    align-items: center;
    flex-direction: column;
}

.progress {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 20px;

    margin-top: 10px;

    font-size: 50px;
    font-weight: bold;

    position: relative;
}

.arrow {
    width: 0px;
    height: 0px;

    border-style: solid;
    border-width: 0 20px 45px 20px;

    position: absolute;
    right: -1.2em
}

.promotion {
    border-color: transparent transparent #2ab45f transparent;
}

.demotion {
    transform: rotate(180deg);
    border-color: transparent transparent #FF4532 transparent;
}

#clan_name {
    font-size: 40px;
    font-weight: bold;

    margin-top: 20px;
}

#ratings {
    display: flex;
    flex-direction: row;
    justify-content: center;
    gap: 70px;

    margin-top: 60px;
}

.rating {
    width: 400px;
    height: 600px;

    background-color: #3D3D3D;

    display: flex;
    flex-direction: column;
    align-items: center;

    .circle {
        width: 60px;
        height: 60px;

        border-width: 10px;

        gap: 5px;
    }

    .line {
        height: 2.5em;
        width: 8px;

        border-radius: 0;
    }

    .rating-name {
        font-size: 40px;
        font-weight: bold;
        text-align: center;

        margin-top: 10px;
        margin-bottom: 15px;
    }

    .progress {
        transform: scale(0.6);
    }
}

.stats {
    display: flex;
    align-items: center;
    flex-direction: column;
    gap: 70px;

    width: 100%;

    margin-top: 40px;
}

.stats-parameter {
    width: 90%;

    display: flex;
    justify-content: space-between;

    font-size: 30px;
    font-weight: bold;
}

.rankings {
    display: flex;
    justify-content: space-between;
    align-items: center;
    
    width: 75%;
    height: 100%;

    font-size: 30px;
    font-weight: bold;
    text-align: center;
    
    p {
        width: 300px;
    }
}
</style>