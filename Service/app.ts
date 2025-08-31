import express from 'express';
import { chromium } from 'playwright';
import fs from 'fs';

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

app.post("/ratings", async (req, res) => {
    const data = req.body.data;

    const page = await browser.newPage();

    try {
        let html = fs.readFileSync("templates/ratings.html", "utf-8");
        html = html.replace(
            "</head>",
            `<script>window.__APP_DATA__ = ${JSON.stringify(data)};</script></head>`
        );

        await page.setContent(html, { waitUntil: "domcontentloaded" });

        const element = await page.waitForSelector("#main", { timeout: 5000 });

        const buffer = await element?.screenshot({ type: "png" });

        res.type("png").send(buffer);
    } catch (error) {
        console.error("Error processing image:", error);
        res.status(500).send("Internal Server Error");
    }
    finally {
        await page.close();
    }
});

app.use("/assets", express.static("templates/assets"));

app.listen(3000, () => console.log("Server started, port 3000"));