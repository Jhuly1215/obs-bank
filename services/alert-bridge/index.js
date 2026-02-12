const express = require("express");
const fs = require("fs");
const path = require("path");

const app = express();
const PORT = 3001;

// Directorio donde se guardan los logs de alertas
const ALERTS_DIR = "/data/alerts";

// Asegurar que el directorio existe
if (!fs.existsSync(ALERTS_DIR)) {
    fs.mkdirSync(ALERTS_DIR, { recursive: true });
}

app.use(express.json());

// ==========================================
// GET /health - Verificar que está vivo
// ==========================================
app.get("/health", (req, res) => {
    res.json({ status: "ok", service: "alert-bridge", timestamp: new Date().toISOString() });
});

// ==========================================
// GET /alerts - Ver todas las alertas guardadas
// ==========================================
app.get("/alerts", (req, res) => {
    try {
        const alertFile = path.join(ALERTS_DIR, "alerts.json");
        if (!fs.existsSync(alertFile)) {
            return res.json({ alerts: [], total: 0 });
        }
        const content = fs.readFileSync(alertFile, "utf-8");
        const alerts = JSON.parse(content);
        res.json({ alerts, total: alerts.length });
    } catch (err) {
        res.status(500).json({ error: "Error leyendo alertas", details: err.message });
    }
});

// ==========================================
// POST /webhook - Recibir alertas de Grafana
// ==========================================
app.post("/webhook", (req, res) => {
    const alertData = req.body;
    const timestamp = new Date().toISOString();

    console.log(`\n${"=".repeat(60)}`);
    console.log(`🚨 ALERTA RECIBIDA - ${timestamp}`);
    console.log(`${"=".repeat(60)}`);

    // Procesar cada alerta individual
    const processedAlerts = [];

    if (alertData.alerts && Array.isArray(alertData.alerts)) {
        alertData.alerts.forEach((alert, i) => {
            const alertEntry = {
                id: `${Date.now()}-${i}`,
                receivedAt: timestamp,
                status: alert.status || "unknown",
                labels: alert.labels || {},
                annotations: alert.annotations || {},
                startsAt: alert.startsAt || null,
                endsAt: alert.endsAt || null,
                fingerprint: alert.fingerprint || null,
                grafanaUrl: alert.generatorURL || null,
            };

            console.log(`  [${i + 1}] Status: ${alertEntry.status}`);
            console.log(`      Labels: ${JSON.stringify(alertEntry.labels)}`);
            if (alertEntry.annotations.summary) {
                console.log(`      Summary: ${alertEntry.annotations.summary}`);
            }

            processedAlerts.push(alertEntry);
        });
    } else {
        // Formato simple (no array)
        const alertEntry = {
            id: `${Date.now()}-0`,
            receivedAt: timestamp,
            status: alertData.state || alertData.status || "unknown",
            title: alertData.title || "Sin titulo",
            message: alertData.message || JSON.stringify(alertData),
            raw: alertData,
        };
        console.log(`  Status: ${alertEntry.status}`);
        console.log(`  Title: ${alertEntry.title}`);
        processedAlerts.push(alertEntry);
    }

    // ==========================================
    // Guardar en archivo JSON persistente
    // ==========================================
    try {
        const alertFile = path.join(ALERTS_DIR, "alerts.json");
        let existingAlerts = [];

        if (fs.existsSync(alertFile)) {
            const content = fs.readFileSync(alertFile, "utf-8");
            existingAlerts = JSON.parse(content);
        }

        existingAlerts.push(...processedAlerts);

        // Mantener solo las ultimas 1000 alertas
        if (existingAlerts.length > 1000) {
            existingAlerts = existingAlerts.slice(-1000);
        }

        fs.writeFileSync(alertFile, JSON.stringify(existingAlerts, null, 2));
        console.log(`  ✅ ${processedAlerts.length} alerta(s) guardada(s) en archivo`);
        console.log(`  📁 Total en archivo: ${existingAlerts.length}`);
    } catch (err) {
        console.error(`  ❌ Error guardando alerta: ${err.message}`);
    }

    // ==========================================
    // Guardar log legible por fecha
    // ==========================================
    try {
        const date = new Date().toISOString().split("T")[0]; // 2026-02-12
        const logFile = path.join(ALERTS_DIR, `alerts-${date}.log`);
        const logLine = processedAlerts.map((a) => {
            return `[${a.receivedAt}] [${a.status}] ${JSON.stringify(a.labels || a.title)} ${a.annotations?.summary || a.message || ""}`;
        }).join("\n");

        fs.appendFileSync(logFile, logLine + "\n");
    } catch (err) {
        console.error(`  ❌ Error escribiendo log: ${err.message}`);
    }

    console.log(`${"=".repeat(60)}\n`);

    res.status(200).json({
        status: "ok",
        message: "Alerta(s) recibida(s) y guardada(s)",
        count: processedAlerts.length,
        timestamp,
    });
});

// ==========================================
// DELETE /alerts - Limpiar todas las alertas
// ==========================================
app.delete("/alerts", (req, res) => {
    try {
        const alertFile = path.join(ALERTS_DIR, "alerts.json");
        if (fs.existsSync(alertFile)) {
            fs.writeFileSync(alertFile, "[]");
        }
        res.json({ status: "ok", message: "Alertas limpiadas" });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// ==========================================
// Iniciar servidor
// ==========================================
app.listen(PORT, "0.0.0.0", () => {
    console.log(`\n🌉 Alert Bridge corriendo en http://0.0.0.0:${PORT}`);
    console.log(`   POST /webhook   → Recibir alertas de Grafana`);
    console.log(`   GET  /alerts    → Ver alertas guardadas`);
    console.log(`   GET  /health    → Health check`);
    console.log(`   DELETE /alerts  → Limpiar alertas\n`);
});
