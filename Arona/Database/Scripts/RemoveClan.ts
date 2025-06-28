import mongoose from "mongoose";
import { database_url } from "../../Config/config.json";

// <interpreter> AddClan.ts <clanId> <guildId>
const args = process.argv.slice(2);
const clanId = args[0];
const guildId = args[1];

const Data = mongoose.model("servers", new mongoose.Schema({ _id: String, clans: Object }), "servers");

mongoose.connect(database_url)
    .then(async () => {
        // Felhantering från överordnade koder ska göra detta helt säkert
        const guildDatabase: any = await Data.findOne({ _id: guildId });

        const clanTag = guildDatabase.clans[clanId].clan_tag;
        const clanName = guildDatabase.clans[clanId].clan_name;

        // Om nyliga raderad klan var det sista, sätt till null. Förhindrar att hela objektet tas bort.
        if (Object.keys(guildDatabase.clans).length === 1)
            guildDatabase.clans = null;

        else delete guildDatabase.clans[clanId];

        await guildDatabase.markModified("clans");
        const status = await guildDatabase.save();

        if (!status)
            return console.log(`❌ Failed to remove clan: \`[${clanTag}] ${clanName}\``);

        console.log(`✅ Removed clan: \`[${clanTag}] ${clanName}\``);
    })
    .catch(err => {
        console.log("Error: failed to connect to database", err);
        process.exit(1);
    })
    .finally(() => mongoose.disconnect());