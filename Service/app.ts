import express from 'express';
import { chromium } from 'playwright';

const app = express();
app.use(express.json());

const browser = await chromium.launch();

app.post("/verify", async (req, res) => {
    const buildLink: string = req.body.link;

    if (!buildLink.includes("share.wowssb.com"))
        return res.status(400).send("Invalid build link");

    const page = await browser.newPage();

    try {
        await page.goto(buildLink, { timeout: 10000 });

        const title = await page.title();

        if (title !== "WoWs ShipBuilder")
            return res.status(400).send("Invalid build link");

        return res.status(200).send();
    }
    catch (error) {
        console.error("Error processing build link:", error);
        res.status(500).send("Internal Server Error");
    }
    finally {
        await page.close();
    }
});

app.post("/build", async (req, res) => {
    const buildLink: string = req.body.link;

    const page = await browser.newPage();

    try {
        await page.goto(buildLink, { timeout: 10000 });

        if (await page.title() !== "WoWs ShipBuilder")
            return res.status(400).send("Invalid build link");

        const shareLinkButton = await page.waitForSelector("button:has-text('Share Build Image')", { timeout: 5000 });
        await shareLinkButton?.click();

        const element = await page.waitForSelector("#image", { timeout: 5000 });

        const screenshotBuffer = await element?.screenshot({ type: "png" });

        res.type("png").send(screenshotBuffer);
    }
    catch (error) {
        console.error("Error processing build link:", error);
        res.status(500).send("Internal Server Error");
    }
    finally {
        await page.close();
    }
});

app.listen(3000, () => console.log("Server started, port 3000"));