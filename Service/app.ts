import express from 'express';
import { chromium } from 'playwright';

const app = express();
app.use(express.json());

const browser = await chromium.launch();

app.post("/verify", async (req, res) => {
    const buildLink: string = req.body.link;

    if (!buildLink.includes("share.wowssb.com"))
        return res.status(400).send("Invalid build link format");

    const page = await browser.newPage();

    try {
        await page.goto(buildLink, { timeout: 10000 });

        if (await page.title() !== "WoWs ShipBuilder")
            return res.status(400).send("Invalid build link");

        return res.status(200);
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

        await page.getByRole('button', { name: 'Share Build Image' }).click();

        const element = await page.$("#image");

        const screenshotBuffer = await element?.screenshot({ type: "png" });

        if (!screenshotBuffer) {
            await browser.close();
            return res.type("text/plain").send(null);
        }

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

app.listen(3000);