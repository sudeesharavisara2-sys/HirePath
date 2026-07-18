const { chromium } = require("playwright");

async function main() {
  const baseUrl = process.env.BASE_URL || "http://127.0.0.1:5056";
  const browser = await chromium.launch();
  const page = await browser.newPage();

  const failed = [];
  const errors = [];
  page.on("requestfailed", (r) =>
    failed.push({ url: r.url(), errorText: r.failure() && r.failure().errorText })
  );
  page.on("console", (m) => {
    if (m.type() === "error" || m.type() === "warning") {
      errors.push({ type: m.type(), text: m.text() });
    }
  });

  const hrSelector = 'section[aria-label="HR Dashboard"] tbody tr';

  await page.goto(`${baseUrl}/`, { waitUntil: "networkidle" });
  const topRowsBefore = await page.$$eval(hrSelector, (rows) => rows.length).catch(() => 0);

  await page.goto(`${baseUrl}/Resume`, { waitUntil: "domcontentloaded" });
  await page.fill(
    "#resumeText",
    [
      "John Doe",
      "Software Engineer",
      "Summary: .NET developer",
      "Skills: C#, ASP.NET Core, Entity Framework, SQL Server",
      "Experience: 3 years",
      "Education: B.Tech",
      "Projects: Web API",
      "Certifications: Azure",
    ].join("\n")
  );
  await page.click("#analyzeBtn", { noWaitAfter: true });
  const overlayShown = await page
    .waitForFunction(() => {
      const el = document.getElementById("analysisOverlay");
      if (!el) return false;
      return getComputedStyle(el).display !== "none" && getComputedStyle(el).visibility !== "hidden";
    }, { timeout: 1500 })
    .then(() => true)
    .catch(() => false);

  const workflowActive = await page
    .waitForFunction(() => {
      const first = document.querySelector('[data-wf="resume_upload"]');
      return !!first && (first.getAttribute("data-state") === "active" || first.getAttribute("data-state") === "done");
    }, { timeout: 2000 })
    .then(() => true)
    .catch(() => false);

  await page.waitForLoadState("networkidle");

  const hasCharts = await page.evaluate(
    () => !!document.querySelector("#dimensionRadar") && !!document.querySelector("#dimensionBars")
  );
  const gaugeVals = await page.evaluate(() => ({
    ats: document.getElementById("atsGaugeNum")?.textContent || null,
    conf: document.getElementById("confidenceGaugeNum")?.textContent || null,
  }));

  await page.goto(`${baseUrl}/`, { waitUntil: "networkidle" });
  const topRowsAfter = await page.$$eval(hrSelector, (rows) => rows.length).catch(() => 0);

  console.log(
    JSON.stringify(
      { baseUrl, topRowsBefore, topRowsAfter, overlayShown, workflowActive, hasCharts, gaugeVals, failed, errors },
      null,
      2
    )
  );
  await browser.close();
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
