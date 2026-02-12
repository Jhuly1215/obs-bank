using Microsoft.AspNetCore.Mvc;

namespace Bank.Transactions.Api.Controllers
{
    /// <summary>
    /// Simula operaciones de autorización de clientes - basado en logs reales de ServiceRestOperacion
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AutorizacionClienteController : ControllerBase
    {
        private readonly ILogger<AutorizacionClienteController> _logger;
        private static readonly Random _rng = new();

        // Datos simulados de funcionarios del banco
        private static readonly string[][] Funcionarios = new[]
        {
            new[] { "505684", "EVER DAVID", "QUISPE", "SILISQUE", "OFICIAL DE NEGOCIOS", "erquispe@bancoecofuturo.com.bo", "TARIJA", "Bermejo (Rural)" },
            new[] { "505709", "ELIAS", "ALARCON", "GUTIERREZ", "OFICIAL DE NEGOCIOS SENIOR", "ealarcon@bancoecofuturo.com.bo", "EL ALTO", "Patacamaya (Rural)" },
            new[] { "173519", "NINFA ETELVINA", "MARTINEZ", "PORTAL", "OFICIAL DE NEGOCIOS", "nmartinez@bancoecofuturo.com.bo", "TARIJA", "Mercado Campesino" },
            new[] { "963136", "DINO HORACIO", "SULLCANI", "MURGUIA", "OFICIAL DE NEGOCIOS", "dsullcani@bancoecofuturo.com.bo", "TARIJA", "Yacuiba (Rural)" },
            new[] { "551483", "GLORIA", "ESCALANTE", "TAPIA", "OFICIAL DE PLATAFORMA", "gescalante@bancoecofuturo.com.bo", "TARIJA", "Central Tarija" },
            new[] { "389841", "FRANCIA VERONICA", "VILLAZANTE", "CONDORI", "ENCARGADO DE PLATAFORMA", "fvillazante@bancoecofuturo.com.bo", "EL ALTO", "Escoma (Rural)" },
            new[] { "806231", "NORAH", "CHOQUE", "APAZA", "OFICIAL DE NEGOCIOS", "nchoque@bancoecofuturo.com.bo", "EL ALTO", "Ciudad Satélite" },
            new[] { "716168", "MONICA", "VILLCA", "TOLA", "OFICIAL DE NEGOCIOS SENIOR", "movillca@bancoecofuturo.com.bo", "EL ALTO", "Ceja" },
        };

        private static readonly string[][] Clientes = new[]
        {
            new[] { "435989", "5801092", "ARENAS", "MARTINEZ", "MARIO", "" },
            new[] { "1130834", "9069403", "AJACOPA", "ALVARADO", "GUADALUPE", "" },
            new[] { "425287", "10641104", "OSORIO", "QUISPE", "RAYMUNDA", "CELIA" },
            new[] { "389697", "7210000", "ARMATA", "RUIZ", "NOEMI", "" },
            new[] { "28270", "3986390", "MAMANI", "MARINO", "DEMETRIA", "VICTORIA" },
            new[] { "51368", "6889839", "MAMANI", "YUJRA", "RODRIGO", "" },
            new[] { "93170", "1770126", "TEJERINA", "SALAZAR", "GENARO", "" },
            new[] { "76349", "6781135", "MOLLERICONA", "ARISMENDI", "MARIA", "ISABEL" },
            new[] { "149258", "10300153", "CASTRO", "MESSA", "JUSTINA", "" },
            new[] { "491173", "8627592", "LENIZ", "HUANCA", "RODRIGO", "RENAN" },
        };

        public AutorizacionClienteController(ILogger<AutorizacionClienteController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Lista jefes autorizadores disponibles
        /// </summary>
        [HttpGet("jefes-autorizador")]
        public IActionResult JefesAutorizador()
        {
            _logger.LogInformation("-> Ingreso a el Metodo JefesAutorizador...");
            Thread.Sleep(_rng.Next(30, 120));

            _logger.LogInformation("-> Jefes listados correctamente");
            _logger.LogInformation("-> Autorizadores listados correctamente");

            var funcionario = Funcionarios[_rng.Next(Funcionarios.Length)];
            return Ok(new
            {
                jefes = new[] {
                    new { codigoAgenda = funcionario[0], nombres = funcionario[1], apPaterno = funcionario[2], cargo = funcionario[4], sucursal = funcionario[6] }
                },
                total = _rng.Next(3, 8)
            });
        }

        /// <summary>
        /// Busca el celular de un cliente por su ID
        /// </summary>
        [HttpGet("busqueda-celular/{clienteId}")]
        public IActionResult BusquedaCelular(int clienteId)
        {
            _logger.LogInformation("-> Ingreso a el Metodo BusquedaCelular, cliente_id:  {ClienteId}", clienteId);
            Thread.Sleep(_rng.Next(50, 200));

            return Ok(new { clienteId, celular = $"7{_rng.Next(1000000, 9999999)}", encontrado = true });
        }

        /// <summary>
        /// Busca solicitudes pendientes de autorización de cambio de campos para un cliente
        /// </summary>
        [HttpGet("busqueda-solicitud/{clienteId}")]
        public IActionResult BusquedaSolicitudCliente(int clienteId)
        {
            _logger.LogInformation("-> Ingreso a el Metodo BusquedaSolicitudCliente...");
            Thread.Sleep(_rng.Next(30, 100));

            // 70% no tiene solicitudes (como en los logs reales)
            if (_rng.NextDouble() > 0.3)
            {
                _logger.LogWarning("-> No se tiene solicitudes de autorización de cambio de campos para este cliente");
                return Ok(new { clienteId, tieneSolicitudes = false, solicitudes = Array.Empty<object>() });
            }

            _logger.LogInformation("-> Solicitud y campos listada correctamente");
            return Ok(new
            {
                clienteId,
                tieneSolicitudes = true,
                solicitudes = new[]
                {
                    new { idSolicitud = _rng.Next(490000, 500000), estado = "PENDIENTE", campo = "DIRECCION" }
                }
            });
        }

        /// <summary>
        /// Envía una solicitud de autorización y notifica por correo
        /// </summary>
        [HttpPost("enviar-solicitud")]
        public IActionResult EnviarSolicitud([FromBody] SolicitudRequest request)
        {
            _logger.LogInformation("-> Ingreso a el Metodo enviarSolicitud...");
            Thread.Sleep(_rng.Next(100, 300));

            var idSolicitud = _rng.Next(498000, 500000);
            var funcionario = Funcionarios[_rng.Next(Funcionarios.Length)];
            var funcionario2 = Funcionarios[_rng.Next(Funcionarios.Length)];

            _logger.LogInformation("-> Proceso de envio de correo luego de guardar la solicitud de cambio");
            _logger.LogInformation("-> correo destinatario: {Correo}", funcionario[5]);
            _logger.LogInformation("-> correo destinatario: {Correo}", funcionario2[5]);
            Thread.Sleep(_rng.Next(50, 150));

            _logger.LogInformation("-> respuesta del envio de correo: ");
            _logger.LogInformation("-> Solicitud enviada correctamente, id_solicitud generado: {IdSolicitud}", idSolicitud);

            return Ok(new { idSolicitud, estado = "ENVIADA", correos = new[] { funcionario[5], funcionario2[5] } });
        }

        /// <summary>
        /// Autoriza una solicitud pendiente, verificando SIBIL y BDN
        /// </summary>
        [HttpPost("autorizar-solicitud")]
        public IActionResult AutorizarSolicitud([FromBody] AutorizarRequest request)
        {
            _logger.LogInformation("-> metodo AutorizarSolicitud: json enviado: {@request} {Request}", request);
            _logger.LogInformation("-> Previo a la autorizacion se realiza la verificacion SIBIL y BDN...");

            var cliente = Clientes[_rng.Next(Clientes.Length)];
            _logger.LogInformation(
                "-> Se inicia la verificacion SIBIL y BDN en el Autorizador con el metodo verificaSIBILBDN codigo_funcionario: {CodigoFuncionario} cliente_id: {ClienteId}",
                request.CodigoFuncionario, request.ClienteId);

            Thread.Sleep(_rng.Next(200, 500));

            // Simula envío a BDN
            _logger.LogInformation("-> Ingreso a el Metodo EnvioParamtrosBDN...");
            _logger.LogInformation(
                "->  EnvioParamtrosBDN envio a servicio http://192.168.200.167:89/api/v1/procesoBDN datos de envio : {Datos}",
                $"{{\"txtApEsposo\":\"\",\"txtMaterno\":\"{cliente[3]}\",\"ID_USUARIO\":\"CCO\",\"txtDocId\":\"{cliente[1]}\",\"TxtNombres1\":\"{cliente[4]}\",\"txtPaterno\":\"{cliente[2]}\",\"cliente_id\":{cliente[0]}}}");

            Thread.Sleep(_rng.Next(100, 400));

            // Simula respuesta BDN (a veces con error como en los logs reales)
            _logger.LogInformation("-> EnvioParamtrosBDN respuesta....  {{\"Message\":\"Ocurrio un error al consultar BDN: N: \"}}");
            _logger.LogInformation("-> Respuesta verificacion SIBIL y PEP: No existe coincidencias, puede autorizar , codigo de respuesta: 0");

            _logger.LogInformation("-> Se inicia la modificacion de datos por medio del autorizador");
            Thread.Sleep(_rng.Next(50, 200));
            _logger.LogInformation("-> La modificacion se realizo correctamente, se continua con el envio a correo de confirmacion");

            return Ok(new
            {
                clienteId = request.ClienteId,
                autorizado = true,
                verificacionSibil = 0,
                verificacionBdn = 0,
                mensaje = "Solicitud autorizada correctamente"
            });
        }

        /// <summary>
        /// Lista las solicitudes de autorización por usuario
        /// </summary>
        [HttpGet("lista-solicitudes/{codigoUsuario}")]
        public IActionResult ListaSolicitudesAutorizacion(int codigoUsuario)
        {
            _logger.LogInformation("-> metodo ListaSolicitudesAutorizacion: json enviado: {@usuario} {Usuario}", codigoUsuario);
            Thread.Sleep(_rng.Next(50, 200));

            return Ok(new
            {
                usuario = codigoUsuario,
                solicitudes = Enumerable.Range(1, _rng.Next(0, 5)).Select(_ => new
                {
                    idSolicitud = _rng.Next(490000, 500000),
                    clienteId = _rng.Next(10000, 1200000),
                    estado = "PENDIENTE",
                    fechaSolicitud = DateTime.Now.AddDays(-_rng.Next(0, 7)).ToString("yyyy-MM-dd HH:mm:ss")
                })
            });
        }
    }

    public class SolicitudRequest
    {
        public int ClienteId { get; set; }
        public int CodigoFuncionario { get; set; }
        public string Campo { get; set; } = "";
        public string ValorAnterior { get; set; } = "";
        public string ValorNuevo { get; set; } = "";
    }

    public class AutorizarRequest
    {
        public int ClienteId { get; set; }
        public int CodigoFuncionario { get; set; }
        public int IdSolicitud { get; set; }
    }
}
