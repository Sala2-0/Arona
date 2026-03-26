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

export function getClanColor(league: number) {
    switch (league) {
        case 0: return "#cda4ff"
        case 1: return "#bee7bd"
        case 2: return "#e3d6a0"
        case 3: return "#cce4e4"
        case 4: return "#cc9966"
        default: return "#ffffff"
    }
}

export function getOutcomeColor(outcome: boolean) {
    // #fc6501 - Defeat
    // #4CE8AA - Victory

    return outcome ? "#4CE8AA" : "#fc6501"
}

export function stringify(pointsDelta: number) {
    const sign = pointsDelta >= 0 ? "+" : "-";
    return `${sign}${Math.abs(pointsDelta)}`;
}