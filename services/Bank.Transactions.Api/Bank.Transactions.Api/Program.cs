using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Instrumentation.Runtime;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ====== Config de OTel ======
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://otel-collector:4317";
var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "bank-transactions-api";
var serviceVersion = builder.Configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0";

var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: serviceName, serviceVersion: serviceVersion);

// ====== LOGS -> OTLP ======
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddOpenTelemetry(o =>
{
    o.SetResourceBuilder(resourceBuilder);
    o.AddOtlpExporter(otlp =>
    {
        otlp.Endpoint = new Uri(otlpEndpoint);
    });

    o.IncludeScopes = true;
    o.IncludeFormattedMessage = true;
});

// ====== TRACES + METRICS -> OTLP ======
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName, serviceVersion))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(opt =>
            {
              
            })
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri(otlpEndpoint);
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri(otlpEndpoint);
            });
    });


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Endpoint demo de “transferencia”
app.MapPost("/api/transactions/transfer", (ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("Transfer");

    // transactionId: id de negocio (se usa en logs y traces, NO en métricas como label)
    var transactionId = Guid.NewGuid().ToString("N");

    // Logs estructurados (sin PII)
    logger.LogInformation("Transfer started transaction_id={transaction_id} channel={channel} outcome={outcome}",
        transactionId, "api", "pending");

    // Simula procesamiento
    Thread.Sleep(Random.Shared.Next(50, 250));

    var success = Random.Shared.NextDouble() > 0.15;
    if (!success)
    {
        logger.LogError("Transfer failed transaction_id={transaction_id} error_code={error_code} outcome={outcome}",
            transactionId, "INTERBANK_TIMEOUT", "failed");

        return Results.Problem("Transfer failed", statusCode: 502);
    }

    logger.LogInformation("Transfer completed transaction_id={transaction_id} outcome={outcome}",
        transactionId, "success");

    return Results.Ok(new { transactionId, outcome = "success" });
});

app.MapControllers();
app.Run();
