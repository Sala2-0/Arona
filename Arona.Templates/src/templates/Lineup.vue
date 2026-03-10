<script setup lang="ts">
import {nextTick, onMounted} from "vue"
import {getClanColor, getOutcomeColor, getStar, stringify} from "../extensions.ts"

interface Player {
  Name: string
  Survived: boolean
  Ship: {
    Name: string
    Level: string
  }
}

interface Team {
  Tag: string
  Name: string
  TeamNumber: "Alpha" | "Bravo"
  League: number
  Division: number
  DivisionRating: number
  RatingDelta: number
  Stage: {
    Type: "promotion" | "demotion";
    Progress: ("victory" | "defeat" | "draw")[];
  } | null
  Players: Player[]
}

interface BattleInfo {
  IsVictory: boolean
  Ally: Team
  Opponent: Team
}

const exampleData: BattleInfo = {
  IsVictory: true,
  Ally: {
    Tag: "SEIA",
    Name: "Tepartiet",
    TeamNumber: "Alpha",
    League: 0,
    Division: 1,
    DivisionRating: 999,
    RatingDelta: 99,
    Stage: null,
    Players: [
      {
        Name: "Yurizono_Seia",
        Survived: true,
        Ship: {
          Name: "Ragnar",
          Level: "X"
        }
      },
      {
        Name: "Yurizono_Seia",
        Survived: true,
        Ship: {
          Name: "Ragnar",
          Level: "X"
        }
      },
      {
        Name: "Yurizono_Seia",
        Survived: true,
        Ship: {
          Name: "Ragnar",
          Level: "X"
        }
      },
      {
        Name: "Yurizono_Seia",
        Survived: true,
        Ship: {
          Name: "Ragnar",
          Level: "X"
        }
      },
      {
        Name: "Yurizono_Seia",
        Survived: true,
        Ship: {
          Name: "Ragnar",
          Level: "X"
        }
      },
      {
        Name: "Yurizono_Seia",
        Survived: true,
        Ship: {
          Name: "Ragnar",
          Level: "X"
        }
      },
      {
        Name: "Yurizono_Seia",
        Survived: true,
        Ship: {
          Name: "Ragnar",
          Level: "X"
        }
      }
    ]
  },
  Opponent: {
    Tag: "MIKA",
    Name: "Tepartiets kuppförsök",
    TeamNumber: "Alpha",
    League: 1,
    Division: 3,
    DivisionRating: 10,
    RatingDelta: -19,
    Stage: null,
    Players: [
      {
        Name: "Misono Mika",
        Survived: true,
        Ship: {
          Name: "Nakhimov",
          Level: "X"
        }
      },
      {
        Name: "Misono Mika",
        Survived: true,
        Ship: {
          Name: "Nakhimov",
          Level: "X"
        }
      },
      {
        Name: "Misono Mika",
        Survived: true,
        Ship: {
          Name: "Nakhimov",
          Level: "X"
        }
      },
      {
        Name: "Misono Mika",
        Survived: true,
        Ship: {
          Name: "Nakhimov",
          Level: "X"
        }
      },
      {
        Name: "Misono Mika",
        Survived: true,
        Ship: {
          Name: "Nakhimov",
          Level: "X"
        }
      },
      {
        Name: "Misono Mika",
        Survived: true,
        Ship: {
          Name: "Nakhimov",
          Level: "X"
        }
      },
      {
        Name: "Misono Mika",
        Survived: true,
        Ship: {
          Name: "Nakhimov",
          Level: "X"
        }
      },
    ]
  }
};

const data: BattleInfo = import.meta.env.DEV
    ? exampleData
    : window.__APP_DATA__

onMounted(async () => {
  await nextTick();
  window.__VUE_READY__ = true;
});
</script>

<template>
  <div id="root">
    <div class="lineup">
      <div class="clan-info">
        <p class="clan-name">{{ `[${data.Ally.Tag}] ${data.Ally.TeamNumber}` }}</p>

        <div class="circle" :style="{ color: getClanColor(data.Ally.League) }">
          <div class="line" v-for="_ in data.Ally.Division"></div>
        </div>

        <div class="rating-info">
          <div v-if="data.Ally.Stage !== null" class="progress">
            <img class="star" v-for="i in 5" :src="getStar(data.Ally.Stage.Progress[i - 1]! as 'victory')" alt="">
            <div :class="['arrow', data.Ally.Stage.Type === 'promotion' ? 'promotion' : 'demotion']"></div>
          </div>

          <p v-if="data.Ally.Stage === null">{{ data.Ally.DivisionRating }}</p>
          <p v-if="data.Ally.Stage === null" :style="{ 'color': getOutcomeColor(data.IsVictory) }">{{ stringify(data.Ally.RatingDelta) }}</p>
        </div>
      </div>
      
      <div class="players">
        <div v-for="player in data.Ally.Players" class="player">
          <p class="player-name">{{ player.Name }}</p>
          <div class="ship-info">
            <p>{{ player.Ship.Level }}</p>
            <p>{{ player.Ship.Name }}</p>
          </div>
        </div>
      </div>
    </div>

    <div class="lineup" style="align-items: flex-end">
      <div class="clan-info" style="flex-direction: row-reverse; text-align: right">
        <p class="clan-name">{{ `${data.Opponent.TeamNumber} [${data.Opponent.Tag}]` }}</p>

        <div class="circle" :style="{ color: getClanColor(data.Opponent.League) }">
          <div class="line" v-for="_ in data.Opponent.Division"></div>
        </div>

        <div class="rating-info" style="flex-direction: row-reverse; text-align: right">
          <div v-if="data.Opponent.Stage !== null" class="progress">
            <img class="star" v-for="i in 5" :src="getStar(data.Opponent.Stage.Progress[i - 1]! as 'victory')" alt="">
            <div :class="['arrow', data.Opponent.Stage.Type === 'promotion' ? 'promotion' : 'demotion']"></div>
          </div>

          <p v-if="data.Opponent.Stage === null">{{ data.Opponent.DivisionRating }}</p>
          <p v-if="data.Opponent.Stage === null" :style="{ 'color': getOutcomeColor(!data.IsVictory) }">{{ stringify(data.Opponent.RatingDelta) }}</p>
        </div>
      </div>

      <div class="players">
        <div v-for="player in data.Opponent.Players" class="player" style="flex-direction: row-reverse;">
          <p class="player-name" style="text-align: right">{{ player.Name }}</p>
          <div class="ship-info" style="flex-direction: row-reverse; text-align: right">
            <p>{{ player.Ship.Level }}</p>
            <p>{{ player.Ship.Name }}</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
p {
  margin: 0;
}

#root {
  font-family: "Inter", serif;
  color: white;

  width: 1800px; height: 700px;
  margin: 0;
  padding: 30px;
  box-sizing: border-box;

  background-color: #323232;

  display: flex;
  flex-direction: row;
  align-items: center;

  position: relative;
}

.lineup {
  width: 50%;

  .clan-info {
    display: flex;
    justify-content: space-around;
    align-items: center;
    
    margin-bottom: 70px;
    
    width: 100%;

    p {
      font-size: 35px;
      font-weight: bold;
      
      width: 200px;
    }

    .clan-name {
      width: 300px;
    }
    
    .rating-info {
      width: 500px;
      
      display: flex;
      justify-content: space-around;
    }
  }
  
  .player {
    display: flex;
    flex-direction: row;
    align-items: center;
    gap: 100px;
    
    margin: 25px 0 25px 0;
    
    p {
      font-size: 30px;
      font-weight: bold;
    }
    
    .ship-info {
      display: flex;
      flex-direction: row;
      gap: 20px;
    }
    
    .player-name {
      width: 500px;
    }
  }
}

.circle {
  width: 50px;
  height: 50px;

  border: solid 8px;
  border-radius: 100%;

  display: flex;
  align-items: center;
  justify-content: center;
  gap: 3px;
}

.line {
  height: 2.5em;
  width: 7px;

  background-color: #ffff;

  border-radius: 2px;
}

.star {
  width: 70px;
}

.progress {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 20px;

  font-size: 50px;
  font-weight: bold;

  position: relative;
  
  scale: 0.8;
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
</style>