import axios from 'axios';
import mongoose from "mongoose";
import { database_url } from "../../Config/config.json";

const Data = mongoose.model("servers", new mongoose.Schema({ _id: String, channel_id: String, clans: Object }), "servers");

const returnMessages: {
    guild_id: string,
    channel_id: string,
    type: string,
    message: string | {
        title: string,
        time: number,
        team: string,
        game_result: string,
        color: string,
        points: string,
        result: string,
    }
}[] = [];

mongoose.connect(database_url)
    .then(async () => {
        const guilds = await Data.find({});

        for (const guild of guilds) {
            if (guild.clans === null) continue;

            const clanIds = Object.keys(guild.clans);
            const fetchPromises = clanIds.map(async clanId => {
                const region = guild.clans[clanId]["region"];
                try {
                    const res = await axios.get(`https://clans.worldofwarships.${region}/api/clanbase/${clanId}/claninfo/`);
                    return ({
                        clan_id: clanId,
                        data: res.data,
                    });
                } catch (err) {
                    return ({
                        error: "API error, ignoring clan"
                    });
                }
            });

            for (const clan of await Promise.all(fetchPromises) as { clan_id: string, data?: any, error?: string }[]) {
                // Ignorara klanen och fortsätt om fel uppstår för klanen
                if (clan.error) continue;

                const id = clan.clan_id;

                const SEASON_NUMBER = clan.data["clanview"]["wows_ladder"]["season_number"];

                const tag = clan.data["clanview"]["clan"]["tag"];
                const name = clan.data["clanview"]["clan"]["name"];
                const primeTime: number | null = clan.data["clanview"]["wows_ladder"]["prime_time"];

                if (guild.clans[id]["prime_time"]["active"] === null && primeTime !== null) {
                    returnMessages.push({
                        guild_id: guild["_id"]!,
                        channel_id: guild["channel_id"]!,
                        type: "Started playing",
                        message: `\`[${tag}] ${name}\` has started playing.`
                    });
                }
                guild.clans[id]["prime_time"]["planned"] = clan.data["clanview"]["wows_ladder"]["planned_prime_time"];
                guild.clans[id]["prime_time"]["active"] = primeTime;

                // Sista slagtig
                const lastBattleUnix = Math.floor(new Date(clan.data["clanview"]["wows_ladder"]["last_battle_at"]).getTime() / 1000);

                // Slag resultat
                for (const dbRating of guild.clans[id].ratings) {
                    const apiRating = clan.data["clanview"]["wows_ladder"]["ratings"].find((r: any) => r.team_number === dbRating.team_number
                        && r.season_number === SEASON_NUMBER);

                    let pushMessage = false;

                    const message = {
                        guild_id: guild["_id"]!,
                        channel_id: guild["channel_id"]!,
                        type: "Finished battle",
                        message: {
                            title: `\`[${tag}] ${name}\` finished a battle`,
                            time: lastBattleUnix,
                            team: apiRating.team_number === 1 ? "Alpha" : "Bravo",
                            game_result: "",
                            color: "",
                            points: ``,
                            result: ""
                        }
                    }

                    if (apiRating.stage !== null) {
                        if (apiRating["stage"]["progress"].length === 0 && dbRating.qualification === null) {
                            message.message.game_result = apiRating.stage.type === "promotion" ? "Victory" : "Defeat";
                            pushMessage = true;
                        }

                        else if (apiRating["stage"]["progress"].length > dbRating.qualification?.progress.length) {
                            message.message.game_result = apiRating.stage.progress[apiRating.stage.progress.length - 1] === "victory" ? "Victory" : "Defeat";
                            pushMessage = true;
                        }

                        if (pushMessage) {
                            const type = apiRating.stage.type === "promotion"
                                ? "Qualification for"
                                : "Qualification to stay in";
                            const league = getLeague(apiRating.stage.target_league - (apiRating.stage.type === "demotion" ? 1 : 0));
                            const progress = stageFormat(apiRating.stage.progress);

                            message.message.result = `${type} ${league} league`;
                            message.message.color = message.message.game_result === "Victory" ? "00FF00" : "FF0000";
                            message.message.points = `[${progress}]`;
                        }
                    }

                    else if (dbRating.qualification !== null) {
                        if (apiRating.league < dbRating.league) {
                            message.message.game_result = "Victory";
                            message.message.result = `Promoted to ${getLeague(apiRating.league)} ${getDivision(apiRating.division)} (${apiRating.division_rating})`;
                            message.message.color = "00FF00";
                            message.message.points = "Qualified";
                        }

                        else if (apiRating.league > dbRating.league) {
                            message.message.game_result = "Defeat";
                            message.message.result = `Demoted to ${getLeague(apiRating.league)} ${getDivision(apiRating.division)} (${apiRating.division_rating})`;
                            message.message.color = "FF0000";
                            message.message.points = "Failed to qualify";
                        }

                        else if (apiRating.league === dbRating.league && dbRating.qualification.type == "demotion") {
                            message.message.game_result = "Victory";
                            message.message.result = `Staying in ${getLeague(apiRating.league)} ${getDivision(apiRating.division)} (${apiRating.division_rating})`;
                            message.message.color = "00FF00";
                            message.message.points = "Qualified";
                        }

                        else if (apiRating.league === dbRating.league && dbRating.qualification.type == "promotion") {
                            message.message.game_result = "Defeat";
                            message.message.result = `Demoted to ${getLeague(apiRating.league)} ${getDivision(apiRating.division)} (${apiRating.division_rating})`;
                            message.message.color = "FF0000";
                            message.message.points = "Failed to qualify";
                        }

                        pushMessage = true;
                    }

                    else if (apiRating.division !== dbRating.division) {
                        const promoted = apiRating.division < dbRating.division;

                        message.message.game_result = promoted
                            ? "Victory"
                            : "Defeat";
                        message.message.color = promoted
                            ? "00FF00" // Grön
                            : "FF0000"; // Röd
                        message.message.result = promoted
                            ? `Promoted to ${getLeague(apiRating.league)} ${getDivision(apiRating.division)} (${apiRating.division_rating})`
                            : `Demoted to ${getLeague(apiRating.league)} ${getDivision(apiRating.division)} (${apiRating.division_rating})`;
                        message.message.points = promoted
                            ? `+${(apiRating.division_rating + 100) - dbRating.rating}`
                            : `-${(dbRating.rating + 100) - apiRating.division_rating}`;

                        pushMessage = true;
                    }

                    else if (apiRating.division_rating !== dbRating.rating) {
                        const victory = apiRating.division_rating > dbRating.rating;

                        message.message.game_result = victory
                            ? "Victory"
                            : "Defeat";
                        message.message.color = victory
                            ? "00FF00"
                            : "FF0000";
                        message.message.points = victory
                            ? `+${apiRating.division_rating - dbRating.rating}`
                            : `-${dbRating.rating - apiRating.division_rating}`;
                        message.message.result = `${getLeague(apiRating.league)} ${getDivision(apiRating.division)} (${apiRating.division_rating})`;

                        pushMessage = true;
                    }

                    if (pushMessage) returnMessages.push(message);

                    guild.clans[id].ratings.map((r: any) => {
                        if (r.team_number === apiRating.team_number) {
                            r.rating = apiRating.division_rating;
                            r.league = apiRating.league;
                            r.division = apiRating.division;
                            r.qualification = apiRating.stage === null ? null : {
                                type: apiRating.stage.type,
                                target_league: apiRating.stage.target_league,
                                target_division: apiRating.stage.target_division,
                                progress: apiRating.stage.progress,
                                battles: apiRating.stage.battles,
                                victories_required: apiRating.stage.victories_required,
                            };
                        }
                    });
                }
            }

            guild.markModified("clans");
            await guild.save();
        }

        console.log(JSON.stringify(returnMessages, null, 2));
    })
    .catch(err => {
        console.log("Error: failed to connect to database", err);
        process.exit(1);
    })
    .finally(() => mongoose.disconnect());

function getLeague(league: number): string {
    switch (league) {
        case 0: return "Hurricane";
        case 1: return "Typhoon";
        case 2: return "Storm";
        case 3: return "Gale";
        case 4: return "Squall";
        default: return "undefined";
    }
}

function getDivision(division: number): string {
    switch (division) {
        case 1: return "I";
        case 2: return "II";
        case 3: return "III";
        default: return "undefined";
    }
}

function stageFormat(progress: string[]): string {
    const formatted: string[] = [" ⬛ ", " ⬛ ", " ⬛ ", " ⬛ ", " ⬛ "];
    for (let p = 0; p < progress.length; p++)
        formatted[p] = progress[p] === "victory" ? " 🟩 " : " 🟥 ";

    return formatted.join("");
}