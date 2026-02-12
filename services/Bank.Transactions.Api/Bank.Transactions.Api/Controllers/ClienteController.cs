using Microsoft.AspNetCore.Mvc;

namespace Bank.Transactions.Api.Controllers
{
    /// <summary>
    /// Simula consultas PCCU y alertas PEP - basado en logs reales de ServiceRestOperacion
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ClienteController : ControllerBase
    {
        private readonly ILogger<ClienteController> _logger;
        private static readonly Random _rng = new();

        private static readonly string[][] Clientes = new[]
        {
            new[] { "435989", "5801092", "ARENAS", "MARTINEZ", "MARIO", "", "NATURAL" },
            new[] { "1130834", "9069403", "AJACOPA", "ALVARADO", "GUADALUPE", "", "NATURAL" },
            new[] { "425287", "10641104", "OSORIO", "QUISPE", "RAYMUNDA", "CELIA", "NATURAL" },
            new[] { "389697", "7210000", "ARMATA", "RUIZ", "NOEMI", "", "NATURAL" },
            new[] { "28270", "3986390", "MAMANI", "MARINO", "DEMETRIA", "VICTORIA", "NATURAL" },
            new[] { "51368", "6889839", "MAMANI", "YUJRA", "RODRIGO", "", "NATURAL" },
            new[] { "93170", "1770126", "TEJERINA", "SALAZAR", "GENARO", "", "NATURAL" },
            new[] { "1130831", "4455044", "VEIZAGA", "SALCEDO", "ALVINO", "", "NATURAL" },
            new[] { "1130840", "7243943", "RODRIGUEZ", "CARVAJAL", "VIZNEY", "VICENTE", "NATURAL" },
            new[] { "1130841", "6676662", "CHOQUE", "", "JUANA", "", "NATURAL" },
        };

        private static readonly string[] TiposTransaccion = new[]
        {
            "ACTUALIZACIÓN CLIENTE", "ALTA CLIENTE", "APERTURA DE CUENTA", "ACTUALIZACIÓN CLIENTE"
        };

        public ClienteController(ILogger<ClienteController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Consulta PCCU para persona natural (verificación de identidad)
        /// </summary>
        [HttpPost("consulta-pccu/{clienteId}")]
        public IActionResult ConsultaPCCU(int clienteId)
        {
            var intento = 1;
            _logger.LogInformation("-> **** INTENTO = {Intento}****", intento);

            var cliente = Clientes[_rng.Next(Clientes.Length)];
            var tipoTransaccion = TiposTransaccion[_rng.Next(TiposTransaccion.Length)];
            var codigoAgenda = _rng.Next(100000, 1100000).ToString();

            _logger.LogInformation("-> DENTRO EL METODO: consultaPCCUPersonaNatural");
            _logger.LogInformation("-> cageUsuarioConsulta: {CodigoAgenda}", codigoAgenda);
            _logger.LogInformation("-> idCliente: {ClienteId}", clienteId);
            _logger.LogInformation("-> tipoTransaccion: {TipoTransaccion}", tipoTransaccion);

            // Simula el JSON de envío como en los logs reales
            var json = $"{{\"TipoPersona\":\"{cliente[6]}\",\"UsuarioSolicitante\":{{\"CodigoAgenda\":\"{codigoAgenda}\"}},\"Natural\":{{\"NumDocumento\":\"{cliente[1]}\",\"PrimerApellido\":\"{cliente[2]}\",\"SegundoApellido\":\"{cliente[3]}\",\"PrimerNombre\":\"{cliente[4]}\",\"SegundoNombre\":\"{cliente[5]}\"}},\"Sistema\":\"ANA\",\"TipoTransaccion\":\"{tipoTransaccion}\"}}";
            _logger.LogInformation("-> JSON:{Json}", json);

            Thread.Sleep(_rng.Next(200, 800));

            // 90% exitoso, 10% falla
            var success = _rng.NextDouble() > 0.1;
            _logger.LogInformation("-> response.IsSuccessStatusCode:{IsSuccess}", success);
            _logger.LogInformation("-> respuestaConsulta:{Respuesta}", success);

            if (success)
            {
                _logger.LogInformation("-> respuestaPCCU:{Respuesta}", true);
                _logger.LogInformation("-> ***********************\tREALIZÓ CORRECTAMENTE LA CONSULTA PCCU!!!");

                return Ok(new
                {
                    clienteId,
                    exito = true,
                    tipoTransaccion,
                    documento = cliente[1],
                    nombre = $"{cliente[4]} {cliente[2]} {cliente[3]}",
                    mensaje = "Consulta PCCU realizada correctamente"
                });
            }

            _logger.LogWarning("-> respuestaPCCU:{Respuesta} - Error en la consulta", false);
            return StatusCode(500, new { clienteId, exito = false, error = "Error en consulta PCCU" });
        }

        /// <summary>
        /// Consulta de alerta PEP (Persona Expuesta Políticamente) para un cliente
        /// </summary>
        [HttpGet("alerta-pep/{clienteId}")]
        public IActionResult AlertaPep(int clienteId)
        {
            _logger.LogInformation("-> Ingreso a el Metodo alertaPep...");
            Thread.Sleep(_rng.Next(50, 200));

            _logger.LogInformation("-> Ingreso a el Metodo alertaPep...");
            _logger.LogInformation("-> AdminNegocios..clientes_alerta_pep_mostrar");
            _logger.LogInformation("-> @cliente_id:{ClienteId}", clienteId);

            // Simula distintos códigos de respuesta
            var codigo = _rng.NextDouble() > 0.85 ? 1 : 0;
            var mensaje = codigo == 0 ? "alerta obtenida correctamente" : "No se obtuvo ningún resultado";
            var codAgenda = codigo == 0 ? _rng.Next(100000, 1100000) : 0;

            // Simula alertas SIBIL y BDN (raramente positivas, como en los logs reales)
            var alertaSibil = _rng.NextDouble() > 0.95 ? 1 : 0;
            var alertaBdn = _rng.NextDouble() > 0.95 ? 1 : 0;

            _logger.LogInformation("-> alert.codigo:{Codigo}", codigo);
            _logger.LogInformation("-> alert.mensaje:{Mensaje}", mensaje);
            _logger.LogInformation("-> alert.cliente_id:{ClienteId}", clienteId);
            _logger.LogInformation("-> alert.cod_agenda:{CodAgenda}", codAgenda);
            _logger.LogInformation("-> alert.alerta_sibil:{AlertaSibil}", alertaSibil);
            _logger.LogInformation("-> alert.alerta_bdn:{AlertaBdn}", alertaBdn);

            // Si hay alerta SIBIL o BDN, logear como advertencia
            if (alertaSibil == 1 || alertaBdn == 1)
            {
                _logger.LogWarning("⚠️ ALERTA PEP DETECTADA para cliente {ClienteId} - SIBIL:{AlertaSibil} BDN:{AlertaBdn}",
                    clienteId, alertaSibil, alertaBdn);
            }

            return Ok(new
            {
                clienteId,
                codigo,
                mensaje,
                codAgenda,
                alertaSibil,
                alertaBdn,
                esPep = alertaSibil == 1 || alertaBdn == 1
            });
        }

        /// <summary>
        /// Verificación PEP mediante PCCU
        /// </summary>
        [HttpPost("verifica-pep-pccu")]
        public IActionResult VerificaPepPCCU([FromBody] VerificaPepRequest request)
        {
            _logger.LogInformation("-> Ingreso a el Metodo VerificaPepPCCU...");
            _logger.LogInformation("-> cod_sfi_usuario:{CodSfiUsuario}", request.CodSfiUsuario);
            _logger.LogInformation("-> CodigoSfi:{CodigoSfi}", request.CodigoSfi);
            _logger.LogInformation("-> ClienteId:{ClienteId}", request.ClienteId);

            Thread.Sleep(_rng.Next(200, 600));

            var cliente = Clientes[_rng.Next(Clientes.Length)];

            _logger.LogInformation("-> DENTRO EL METODO: consultaPCCUPersonaNatural");
            _logger.LogInformation("-> cageUsuarioConsulta: {CodSfiUsuario}", request.CodSfiUsuario);
            _logger.LogInformation("-> idCliente: {ClienteId}", request.ClienteId);
            _logger.LogInformation("-> tipoTransaccion: ACTUALIZACIÓN CLIENTE");

            Thread.Sleep(_rng.Next(200, 500));

            _logger.LogInformation("-> response.IsSuccessStatusCode: {IsSuccess}", true);
            _logger.LogInformation("-> respuestaConsulta:{Respuesta}", true);
            _logger.LogInformation("-> CodigoAgendaUsuario:{CodSfiUsuario}", request.CodSfiUsuario);
            _logger.LogInformation("-> NroDocumentoCliente:{NroDoc}", cliente[1]);

            var coincidencia = _rng.NextDouble() > 0.9;

            return Ok(new
            {
                clienteId = request.ClienteId,
                coincidencia,
                esBDN = 0,
                esSibil = 0,
                mensaje = coincidencia ? "Se encontraron coincidencias" : "Se comprobo correctamente",
                estado = true
            });
        }

        /// <summary>
        /// Obtiene parámetros del sistema
        /// </summary>
        [HttpGet("parametros")]
        public IActionResult ObtieneParametros()
        {
            _logger.LogInformation("-> Ingreso a el Metodo ObtieneParametros...");
            Thread.Sleep(_rng.Next(20, 80));
            _logger.LogInformation("-> El proceso continua...");

            return Ok(new { parametros = new { monedaBase = "BOB", limiteTransaccion = 50000, timeoutSegundos = 30 } });
        }
    }

    public class VerificaPepRequest
    {
        public int CodSfiUsuario { get; set; }
        public int CodigoSfi { get; set; }
        public int ClienteId { get; set; }
    }
}
