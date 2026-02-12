using Microsoft.AspNetCore.Mvc;

namespace Bank.Transactions.Api.Controllers
{
    /// <summary>
    /// Simula operaciones de cuentas y transacciones bancarias
    /// Incluye apertura de cuentas DPF, depósitos y consultas de saldos
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CuentasController : ControllerBase
    {
        private readonly ILogger<CuentasController> _logger;
        private static readonly Random _rng = new();

        private static readonly string[] Monedas = { "BOB", "USD" };
        private static readonly string[] TiposCuenta = { "CAJA DE AHORRO", "CUENTA CORRIENTE", "DPF", "AHORRO FUTURO" };
        private static readonly string[] Sucursales = {
            "Bermejo", "Patacamaya", "Yacuiba", "El Alto", "La Paz Centro",
            "Santa Cruz", "Cochabamba", "Oruro", "Tarija", "Sucre"
        };

        public CuentasController(ILogger<CuentasController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Apertura de cuenta simulada
        /// </summary>
        [HttpPost("apertura")]
        public IActionResult AperturaCuenta([FromBody] AperturaCuentaRequest request)
        {
            var nroCuenta = $"{_rng.Next(100, 999)}-{_rng.Next(100000, 999999)}-{_rng.Next(1000, 9999)}";
            var sucursal = Sucursales[_rng.Next(Sucursales.Length)];
            var tipoCuenta = TiposCuenta[_rng.Next(TiposCuenta.Length)];
            var moneda = Monedas[_rng.Next(Monedas.Length)];

            _logger.LogInformation("-> Ingreso a el Metodo AperturaCuenta...");
            _logger.LogInformation("-> cliente_id:{ClienteId}, tipo_cuenta:{TipoCuenta}, moneda:{Moneda}, sucursal:{Sucursal}",
                request.ClienteId, tipoCuenta, moneda, sucursal);

            // Verificación PCCU 
            _logger.LogInformation("-> Se realiza verificación PCCU previo a apertura...");
            Thread.Sleep(_rng.Next(200, 500));
            _logger.LogInformation("-> Verificación PCCU completada exitosamente");

            // Verificación PEP
            _logger.LogInformation("-> Consulta alerta PEP previo a apertura...");
            Thread.Sleep(_rng.Next(100, 300));
            _logger.LogInformation("-> Resultado alerta PEP: Sin coincidencias");

            // Proceso de apertura
            _logger.LogInformation("-> Proceso de apertura de cuenta iniciado...");
            Thread.Sleep(_rng.Next(200, 600));

            // 5% falla
            if (_rng.NextDouble() < 0.05)
            {
                _logger.LogError("-> Error en apertura de cuenta para cliente_id:{ClienteId} - Timeout en core bancario",
                    request.ClienteId);
                return StatusCode(503, new { error = "Timeout en core bancario", clienteId = request.ClienteId });
            }

            _logger.LogInformation("-> Cuenta aperturada correctamente nro_cuenta:{NroCuenta} tipo:{TipoCuenta} moneda:{Moneda}",
                nroCuenta, tipoCuenta, moneda);

            return Ok(new
            {
                clienteId = request.ClienteId,
                nroCuenta,
                tipoCuenta,
                moneda,
                sucursal,
                estado = "ACTIVA",
                fechaApertura = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        /// <summary>
        /// Depósito simulado
        /// </summary>
        [HttpPost("deposito")]
        public IActionResult Deposito([FromBody] DepositoRequest request)
        {
            var transaccionId = $"TXN-{DateTime.Now:yyyyMMdd}-{_rng.Next(100000, 999999)}";

            _logger.LogInformation("-> Ingreso a el Metodo RegistraDeposito...");
            _logger.LogInformation("-> transaccion_id:{TxnId} cuenta:{Cuenta} monto:{Monto} moneda:{Moneda}",
                transaccionId, request.NroCuenta, request.Monto, request.Moneda);

            Thread.Sleep(_rng.Next(100, 400));

            // Validación de monto para operaciones sospechosas (>50,000 BOB)
            if (request.Monto > 50000)
            {
                _logger.LogWarning("⚠️ Operación sujeta a reporte UIF - Monto:{Monto} {Moneda} supera umbral de 50,000 BOB - cuenta:{Cuenta}",
                    request.Monto, request.Moneda, request.NroCuenta);
            }

            _logger.LogInformation("-> Depósito registrado exitosamente txn_id:{TxnId}", transaccionId);

            return Ok(new
            {
                transaccionId,
                cuenta = request.NroCuenta,
                monto = request.Monto,
                moneda = request.Moneda,
                estado = "PROCESADO",
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        /// <summary>
        /// Consulta de saldo
        /// </summary>
        [HttpGet("saldo/{nroCuenta}")]
        public IActionResult ConsultaSaldo(string nroCuenta)
        {
            _logger.LogInformation("-> Ingreso a el Metodo ConsultaSaldo cuenta:{NroCuenta}", nroCuenta);
            Thread.Sleep(_rng.Next(30, 150));

            var moneda = Monedas[_rng.Next(Monedas.Length)];
            var saldo = Math.Round(_rng.NextDouble() * 100000, 2);

            _logger.LogInformation("-> Saldo consultado correctamente cuenta:{NroCuenta} saldo:{Saldo} {Moneda}",
                nroCuenta, saldo, moneda);

            return Ok(new
            {
                nroCuenta,
                saldo,
                moneda,
                disponible = Math.Round(saldo * 0.95, 2),
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }
    }

    /// <summary>
    /// Genera tráfico automático simulado sobre todos los endpoints bancarios
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TrafficSimulatorController : ControllerBase
    {
        private readonly ILogger<TrafficSimulatorController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly Random _rng = new();
        private static CancellationTokenSource? _runningSimulation;

        public TrafficSimulatorController(
            ILogger<TrafficSimulatorController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Inicia una simulación de tráfico durante N segundos
        /// </summary>
        [HttpPost("start")]
        public IActionResult Start([FromQuery] int durationSeconds = 60, [FromQuery] int requestsPerSecond = 2)
        {
            if (_runningSimulation != null && !_runningSimulation.IsCancellationRequested)
            {
                return Conflict(new { error = "Ya hay una simulación en curso. Use /stop primero." });
            }

            _runningSimulation = new CancellationTokenSource();
            var token = _runningSimulation.Token;
            var baseUrl = $"http://localhost:{Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") ?? "8080"}";

            _logger.LogInformation("🚀 Iniciando simulación de tráfico: {Duration}s, {Rps} req/s", durationSeconds, requestsPerSecond);

            _ = Task.Run(async () =>
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(baseUrl);
                var delay = 1000 / Math.Max(1, requestsPerSecond);
                var endTime = DateTime.Now.AddSeconds(durationSeconds);
                var count = 0;

                var endpoints = new (string Method, string Path, Func<HttpContent?> Body)[]
                {
                    ("GET", "/api/autorizacioncliente/jefes-autorizador", () => null),
                    ("GET", $"/api/autorizacioncliente/busqueda-celular/{_rng.Next(10000, 1200000)}", () => null),
                    ("GET", $"/api/autorizacioncliente/busqueda-solicitud/{_rng.Next(10000, 1200000)}", () => null),
                    ("POST", "/api/autorizacioncliente/enviar-solicitud",
                        () => JsonContent.Create(new { ClienteId = _rng.Next(10000, 1200000), CodigoFuncionario = _rng.Next(100000, 999999), Campo = "DIRECCION", ValorAnterior = "Calle 1", ValorNuevo = "Avenida 2" })),
                    ("POST", "/api/autorizacioncliente/autorizar-solicitud",
                        () => JsonContent.Create(new { ClienteId = _rng.Next(10000, 1200000), CodigoFuncionario = _rng.Next(100000, 999999), IdSolicitud = _rng.Next(490000, 500000) })),
                    ("POST", $"/api/cliente/consulta-pccu/{_rng.Next(10000, 1200000)}", () => null),
                    ("GET", $"/api/cliente/alerta-pep/{_rng.Next(10000, 1200000)}", () => null),
                    ("POST", "/api/cliente/verifica-pep-pccu",
                        () => JsonContent.Create(new { CodSfiUsuario = _rng.Next(100000, 999999), CodigoSfi = _rng.Next(100000, 999999), ClienteId = _rng.Next(10000, 1200000) })),
                    ("GET", "/api/cliente/parametros", () => null),
                    ("GET", $"/api/conyuge/{_rng.Next(10000, 1200000)}", () => null),
                    ("POST", "/api/cuentas/apertura",
                        () => JsonContent.Create(new { ClienteId = _rng.Next(10000, 1200000) })),
                    ("POST", "/api/cuentas/deposito",
                        () => JsonContent.Create(new { NroCuenta = $"{_rng.Next(100,999)}-{_rng.Next(100000,999999)}-{_rng.Next(1000,9999)}", Monto = _rng.Next(100, 80000), Moneda = "BOB" })),
                    ("GET", $"/api/cuentas/saldo/{_rng.Next(100, 999)}-{_rng.Next(100000, 999999)}-{_rng.Next(1000, 9999)}", () => null),
                    ("POST", "/api/transactions/transfer", () => null),
                };

                while (DateTime.Now < endTime && !token.IsCancellationRequested)
                {
                    try
                    {
                        var ep = endpoints[_rng.Next(endpoints.Length)];
                        HttpResponseMessage response;

                        if (ep.Method == "POST" || ep.Method == "PUT")
                        {
                            response = await client.PostAsync(ep.Path, ep.Body());
                        }
                        else
                        {
                            response = await client.GetAsync(ep.Path);
                        }

                        count++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("Simulación - error en request: {Error}", ex.Message);
                    }

                    await Task.Delay(delay, token);
                }

                _logger.LogInformation("✅ Simulación finalizada. Total requests: {Count}", count);
            }, token);

            return Ok(new
            {
                estado = "iniciada",
                duracion = $"{durationSeconds}s",
                requestsPorSegundo = requestsPerSecond,
                mensaje = "La simulación se ejecuta en segundo plano. Use GET /api/trafficsimulator/status para verificar."
            });
        }

        /// <summary>
        /// Detiene la simulación en curso
        /// </summary>
        [HttpPost("stop")]
        public IActionResult Stop()
        {
            if (_runningSimulation == null || _runningSimulation.IsCancellationRequested)
            {
                return Ok(new { mensaje = "No hay simulación en curso" });
            }

            _runningSimulation.Cancel();
            _logger.LogInformation("⏹️ Simulación detenida manualmente");
            return Ok(new { estado = "detenida" });
        }

        /// <summary>
        /// Estado de la simulación
        /// </summary>
        [HttpGet("status")]
        public IActionResult Status()
        {
            var running = _runningSimulation != null && !_runningSimulation.IsCancellationRequested;
            return Ok(new { simulacionActiva = running });
        }
    }

    public class AperturaCuentaRequest
    {
        public int ClienteId { get; set; }
    }

    public class DepositoRequest
    {
        public string NroCuenta { get; set; } = "";
        public decimal Monto { get; set; }
        public string Moneda { get; set; } = "BOB";
    }
}
