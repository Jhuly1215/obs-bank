const {onRequest} = require("firebase-functions/v2/https");
const logger = require("firebase-functions/logger");

/**
 * Cloud Function: Recibe webhooks de Grafana y reenvía alertas.
 * Por ahora, logea la alerta. Luego se conectará a WhatsApp/Twilio.
 */
exports.grafanaAlert = onRequest({maxInstances: 5}, (req, res) => {
  if (req.method !== "POST") {
    res.status(405).send("Method Not Allowed");
    return;
  }

  const alertData = req.body;

  logger.info("Alerta recibida de Grafana", {
    title: alertData.title || "Sin título",
    state: alertData.state || alertData.status || "unknown",
    message: alertData.message || JSON.stringify(alertData),
  });

  // Log de todas las alertas individuales
  if (alertData.alerts && Array.isArray(alertData.alerts)) {
    alertData.alerts.forEach((alert, i) => {
      logger.info(`Alerta ${i + 1}`, {
        status: alert.status,
        labels: alert.labels,
        annotations: alert.annotations,
        startsAt: alert.startsAt,
        endsAt: alert.endsAt,
      });
    });
  }

  // TODO: Aquí se conectará Twilio/WhatsApp Business API
  // const accountSid = "TU_ACCOUNT_SID";
  // const authToken = "TU_AUTH_TOKEN";
  // const client = require("twilio")(accountSid, authToken);
  // client.messages.create({
  //   from: "whatsapp:+14155238886",
  //   to: "whatsapp:+591XXXXXXXX",
  //   body: `🚨 ALERTA: ${alertData.title}\n${alertData.message}`,
  // });

  res.status(200).json({
    status: "ok",
    message: "Alerta recibida correctamente",
    alertCount: alertData.alerts ? alertData.alerts.length : 1,
  });
});
