import mongoose from "mongoose";
import { database_url } from "../../Config/config.json";

// <interpreter> AddClan.ts <guildId> <guildName>
const args = process.argv.slice(2);
const guildId = args[0];
const guildName = args[1];

const Data = mongoose.model("servers", new mongoose.Schema({ _id: String, clans: Object }), "servers");

mongoose.connect(database_url)
    .then(async () => {
        const guildDatabase: any = await Data.findOne({ _id: guildId });

        if (!guildDatabase)
            return console.log("C#: No database");

        if (guildDatabase.clans === null)
            return console.log(`C#: No clans`);

        console.log(JSON.stringify(guildDatabase.clans, null, 2));
    })
    .catch(err => {
        console.log("Error: failed to connect to database", err);
        process.exit(1);
    })
    .finally(() => mongoose.disconnect());