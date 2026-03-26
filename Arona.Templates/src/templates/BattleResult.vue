<script setup lang="ts">
import {getClanColor, getOutcomeColor, getStar, stringify} from "../extensions.ts"
import {nextTick, onMounted} from "vue"

interface Stage {
  Type: "promotion" | "demotion"
  Progress: ("victory" | "defeat" | "draw")[]
}

interface BattleResult {
  ClanTag: string
  ClanName: string
  IsVictory: boolean
  PointsDelta: number
  League: number
  Division: number
  DivisionRating: number
  Stage: Stage | null,
  IsLineupDataAvailable: boolean,
}

const exampleData: BattleResult = {
  ClanTag: "SEIA",
  ClanName: "Tepartiet",
  IsVictory: false,
  PointsDelta: -30,
  League: 0,
  Division: 1,
  DivisionRating: 144,
  Stage: {
    Type: "promotion",
    Progress: ["victory"]
  },
  IsLineupDataAvailable: false,
}

const data: BattleResult = import.meta.env.DEV
    ? exampleData
    : window.__APP_DATA__

onMounted(async () => {
  await nextTick();
  window.__VUE_READY__ = true;
});
</script>

<template>
  <div id="root">
    <h1 id="clan_name">{{ `[${data.ClanTag}] ${data.ClanName}` }}</h1>

    <p id="outcome" :style="{ 'color': getOutcomeColor(data.IsVictory) }">{{ data.IsVictory ? "Victory" : "Defeat" }}</p>

    <div class="circle" :style="{ color: getClanColor(data.League) }">
      <div class="line" v-for="_ in data.Division"></div>
    </div>

    <div v-if="data.Stage !== null" class="progress" style="margin-top: 50px">
      <img class="star" v-for="i in 5" :src="getStar(data.Stage.Progress[i - 1]! as 'victory')" alt="">
      <div :class="['arrow', data.Stage.Type === 'promotion' ? 'promotion' : 'demotion']"></div>
    </div>
    <p v-else id="points" style="margin-top: 50px">{{ data.DivisionRating }}</p>
    <p v-if="!data.Stage" id="points_delta" :style="{ 'color': getOutcomeColor(data.IsVictory) }">{{ stringify(data.PointsDelta) }}</p>

    <p v-if="data.IsLineupDataAvailable" class="lineup_data_info">Lineup data available</p>
    <p v-else class="lineup_data_info">Lineup data unavailable, use '/clan_monitor set_cookie' to view lineup data</p>
  </div>
</template>

<style scoped>
p {
  margin: 0;
}

#root {
  font-family: "Inter", serif;
  color: white;

  width: 1000px; height: 1100px;
  margin: 0;
  padding: 30px;
  box-sizing: border-box;

  background-color: #323232;

  display: flex;
  flex-direction: column;
  align-items: center;

  position: relative;
}

#clan_name {
  font-size: 40px;
  font-weight: bold;

  margin-top: 20px;
}

#outcome, #points {
  font-size: 85px;
  font-weight: 600;

  margin-bottom: 80px;
}

.circle {
  width: 150px;
  height: 150px;

  border: solid 20px;
  border-radius: 100%;

  display: flex;
  align-items: center;
  justify-content: center;
  gap: 12px;
}

.line {
  height: 6.5em;
  width: 20px;

  background-color: #ffff;

  border-radius: 2px;
}

#points_delta {
  font-size: 70px;
  font-weight: 600;
}

.star {
  width: 70px;
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
  width: 0;
  height: 0;

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

.lineup_data_info {
  font-size: 25px;
  font-weight: bold;

  position: absolute;
  bottom: 50px;
}
</style>