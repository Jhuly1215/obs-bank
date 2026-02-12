using Microsoft.AspNetCore.Mvc;

namespace Bank.Transactions.Api.Controllers
{
    /// <summary>
    /// Simula operaciones de cónyuge - basado en logs reales de ServiceRestOperacion
    /// Incluye simulación de errores reales (ArgumentOutOfRangeException)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ConyugeController : ControllerBase
    {
        private readonly ILogger<ConyugeController> _logger;
        private static readonly Random _rng = new();

        public ConyugeController(ILogger<ConyugeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Obtiene datos del cónyuge de un cliente
        /// </summary>
        [HttpGet("{clienteId}")]
        public IActionResult ConyugeObtiene(int clienteId)
        {
            _logger.LogInformation("-> Ingreso a el Metodo ConyugeObtiene...");
            Thread.Sleep(_rng.Next(20, 80));

            _logger.LogInformation("-> verifica conyuge...");

            // Simula el error real que aparece en los logs de producción (~5% de las veces)
            if (_rng.NextDouble() < 0.05)
            {
                _logger.LogError(
                    "Ocurrio un error -> System.ArgumentOutOfRangeException: Index was out of range. Must be non-negative and less than the size of the collection.\n" +
                    "Parameter name: index\n" +
                    "   at System.Collections.Generic.List`1.get_Item(Int32 index)\n" +
                    "   at System.Linq.Enumerable.ElementAt[TSource](IEnumerable`1 source, Int32 index)\n" +
                    "   at ServiceRestOperacion.Service.ConyugeService.ConyugeObtiene(Int32 cliente_id) in D:\\ServiceRestOperacion\\Service\\ConyugeService.cs:line 283");

                return StatusCode(500, new
                {
                    clienteId,
                    error = "ArgumentOutOfRangeException",
                    mensaje = "Index was out of range. Must be non-negative and less than the size of the collection."
                });
            }

            _logger.LogInformation("-> El proceso continua...");
            Thread.Sleep(_rng.Next(20, 60));

            return Ok(new
            {
                clienteId,
                tieneConyuge = _rng.NextDouble() > 0.4,
                conyuge = new
                {
                    nombre = "MARIA",
                    apellidoPaterno = "QUISPE",
                    apellidoMaterno = "CONDORI",
                    documento = $"{_rng.Next(1000000, 13000000)}"
                }
            });
        }

        /// <summary>
        /// Modifica datos del cónyuge de un cliente
        /// </summary>
        [HttpPut("{clienteId}")]
        public IActionResult ConyugeModifica(int clienteId)
        {
            _logger.LogInformation("-> Ingreso a el Metodo ConyugeModifica...");
            Thread.Sleep(_rng.Next(50, 200));

            // 95% éxito
            if (_rng.NextDouble() > 0.05)
            {
                _logger.LogInformation("-> datos conyuge actualiza... actualizacion correcta");
                return Ok(new { clienteId, actualizado = true, mensaje = "datos conyuge actualiza... actualizacion correcta" });
            }

            _logger.LogError("-> Error al actualizar datos del cónyuge para cliente_id: {ClienteId}", clienteId);
            return StatusCode(500, new { clienteId, actualizado = false, error = "Error al actualizar datos" });
        }
    }
}
