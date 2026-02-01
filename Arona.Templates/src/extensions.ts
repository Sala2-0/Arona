import starVictory from "./assets/star_victory.png";
import starDefeat from "./assets/star_defeat.png";
import starPending from "./assets/star_pending.png";

export function getStar(result: "victory" | "defeat" | "draw"): string {
    switch (result) {
        case "victory":
            return starVictory;
        case "defeat":
            return starDefeat;
        case "draw":
            return starDefeat;
        default:
            return starPending;
    }
}