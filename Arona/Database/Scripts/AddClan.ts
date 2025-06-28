import mongoose from "mongoose";
import axios from "axios";
import pLimit from "p-limit";
import { database_url, wg_api } from "../../Config/config.json";

// <interpreter> AddClan.ts <clanId> <region> <guildId>
const args = process.argv.slice(2);
const clanId = args[0];
const region = args[1];
const guildId = args[2];

const GuildFormat = new mongoose.Schema({
    _id: String,
    clans: Object
});

type ClanFormat = {
    clan_id: number,
    region: string,
    clan_tag: string,
    clan_name: string,
    recent_battles: { time: number, points: number }[],
    prime_time: {
        planned: number | null,
        active: number | null
    },
    ratings: {
        team_number: number, // Alpha = 1, Bravo = 2
        league: number,
        division: number,
        rating: number,
        qualification: {
            type: string,
            target_league: number,
            target_division: number,
            progress: string[],
            battles: number,
            victories_required: number,
        } | null
    }[],
    members: {
        player_id: number,
        battles: number,
    }[]
};

const Data = mongoose.model("servers", GuildFormat, "servers");

mongoose.connect(database_url)
    .then(async () => {
        let guildDatabase: any = await Data.findOne({ _id: guildId });

        if (!guildDatabase)
            guildDatabase = new Data({ _id: guildId, clans: {} });

        if (guildDatabase.clans === null)
            guildDatabase.clans = {};

        if (Object.keys(guildDatabase.clans).length > 5)
            return console.log("📦❌ Maximum database limit reached, **remove a clan to add another**");

        if (Object.keys(guildDatabase.clans).includes(clanId))
            return console.log("❌ clan already exists in database.");
        
        let clan_stats, clan_members;
        try {
            const [stats, members] = await Promise.all([
                axios.get(`https://clans.worldofwarships.${region}/api/clanbase/${clanId}/claninfo/`),
                axios.get(`https://api.worldofwarships.${region}/wows/clans/info/?application_id=${wg_api}&clan_id=${clanId}`)
            ]);
            clan_stats = stats.data;
            clan_members = members.data["data"][clanId]["members_ids"];
        } catch (err) {
            return console.log("❌ API Error.");
        }

        // const res: any = await axios.get(`https://clans.worldofwarships.${region}/api/clanbase/${clanId}/claninfo/`)
        //     .catch(err => {
        //         return console.log("Error: API error.");
        //     });
        const { clanview: { clan: { name, tag } } } = clan_stats;
        const { clanview: { wows_ladder: { planned_prime_time: planned, prime_time: active, ratings, last_battle_at: lbt, season_number } } } = clan_stats;

        const clanInstance: ClanFormat = {
            clan_id: parseInt(clanId),
            region: region,
            clan_tag: tag,
            clan_name: name,
            recent_battles: [],
            prime_time: {
                planned: planned,
                active: active
            },
            ratings: [],
            members: []
        };

        for (const r of ratings) {
            if (r.season_number !== season_number) continue;

            clanInstance.ratings.push({
                team_number: r.team_number,
                league: r.league,
                division: r.division,
                rating: r.division_rating,
                qualification: r.stage === null ? null : {
                    type: r.stage.type,
                    target_league: r.stage.target_league,
                    target_division: r.stage.target_division,
                    progress: r.stage.progress,
                    battles: r.stage.battles,
                    victories_required: r.stage.victories_required
                }
            });
        }

        const limit = pLimit(5);
        const memberDataPromise = clan_members.map(async (memberId: number) => {
            return await limit(async () => {
               try {
                   const [username, seasons] = await Promise.all([
                       axios.get(`https://api.worldofwarships.${region}/wows/clans/accountinfo/?application_id=${wg_api}&account_id=${memberId}`),
                       axios.get(`https://api.worldofwarships.${region}/wows/clans/seasonstats/?application_id=${wg_api}&account_id=${memberId}`)
                   ]);

                   const currentSeason = seasons.data["data"][memberId]["seasons"].find((s: any) => s.season_id === season_number);
                   
                   return ({
                       player_id: memberId,
                       username: username.data["data"][memberId]["account_name"],
                       battles: currentSeason ? currentSeason.battles : 0,
                   });
               } catch (err) {
                   return ({
                       error: "API error, ignoring member"
                   });
               }
           });
        });
        
        for (const member of await Promise.all(memberDataPromise) as { player_id: number, username: string, battles: number, error?: string }[]) {
            // Ignorera medlemmen och fortsätt om fel uppstår för medlemmen
            if (member.error) continue;
            
            clanInstance.members.push(member);
        }

        guildDatabase.clans[clanId] = clanInstance;
        guildDatabase.markModified("clans");
        guildDatabase.markModified("members");
        const status = await guildDatabase.save();

        if (!status)
            return console.log(`❌ Failed to add clan \`[${tag}] ${name}\``);

        console.log(`✅ Added clan: \`[${tag}] ${name}\``);
    })
    .catch(err => {
        console.log("Error: failed to connect to database", err);
        process.exit(1);
    })
    .finally(() => mongoose.disconnect());