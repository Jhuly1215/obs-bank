# 📡 Guía de Observabilidad - Bank Transactions API

> Guía completa para entender, operar y extender el stack de observabilidad del microservicio Bank Transactions API. Pensada para que cualquier persona pueda empezar desde cero.

---

## Tabla de Contenidos

1. [Arquitectura General](#1-arquitectura-general)
2. [Estructura de Archivos](#2-estructura-de-archivos)
3. [Servicios Docker](#3-servicios-docker)
4. [Endpoints de la API](#4-endpoints-de-la-api)
5. [Alertas en Grafana](#5-alertas-en-grafana)
6. [Alert-Bridge (Simulador WhatsApp)](#6-alert-bridge-simulador-whatsapp)
7. [Jaeger (Trazas)](#7-jaeger-trazas)
8. [Prometheus (Métricas)](#8-prometheus-métricas)
9. [Queries Prometheus — Referencia Rápida](#9-queries-prometheus--referencia-rápida)
10. [Queries Prometheus — Catálogo Completo](#10-queries-prometheus--catálogo-completo)
11. [Comandos Útiles](#11-comandos-útiles)

---

## 1. Arquitectura General

```
┌─────────────────────────────────────────────────────────────────┐
│                        Docker Compose                           │
│                                                                 │
│  ┌──────────────┐    OTLP/gRPC     ┌──────────────────┐        │
│  │  Bank API    │──────────────────>│  OTel Collector  │        │
│  │  (.NET 8)    │   :4317           │  :4317 / :8889   │        │
│  │  :5000       │                   └────────┬─────────┘        │
│  └──────────────┘                     ┌──────┼──────┐           │
│                                       │      │      │           │
│                                       ▼      ▼      ▼           │
│                               ┌───────┐ ┌───┐ ┌──────────┐     │
│                               │Prometh│ │Lok│ │  Jaeger  │     │
│                               │  eus  │ │ i │ │  :16686  │     │
│                               │ :9090 │ │:31│ └──────────┘     │
│                               └───┬───┘ │00 │                   │
│                                   │     └─┬─┘                   │
│                                   ▼       ▼                     │
│                              ┌──────────────┐                   │
│                              │   Grafana    │                   │
│                              │   :3000      │                   │
│                              └──────┬───────┘                   │
│                                     │ webhook                   │
│                                     ▼                           │
│                              ┌──────────────┐                   │
│                              │ Alert Bridge │                   │
│                              │   :3001      │                   │
│                              │ (WhatsApp)   │                   │
│                              └──────────────┘                   │
└─────────────────────────────────────────────────────────────────┘
```

**Flujo de datos:**
1. La **Bank API** envía métricas, logs y trazas al **OTel Collector** por gRPC (:4317)
2. El **OTel Collector** distribuye a: **Prometheus** (métricas), **Loki** (logs), **Jaeger** (trazas)
3. **Grafana** consulta a los tres para visualizar dashboards
4. Cuando una regla de alerta se activa, Grafana envía un webhook al **Alert Bridge**
5. El **Alert Bridge** guarda las alertas en archivos (simulando notificación WhatsApp)

---

## 2. Estructura de Archivos

```
obs-bank/
├── docker-compose.yml                    # 🐳 Orquestación de todos los servicios
├── docs/
│   └── observability-guide.md            # 📖 Esta guía
│
├── observability/                        # ⚙️ Configuración del stack
│   ├── otel-collector-config.yml         # Receptor OTLP → exporta a Prometheus/Loki/Jaeger
│   ├── prometheus.yml                    # Scrape config para Prometheus
│   ├── loki-config.yml                   # Config de Loki (almacén de logs)
│   ├── promtail-config.yml              # Config de Promtail (recolector de logs Docker)
│   │
│   └── grafana/provisioning/            # 📊 Auto-configuración de Grafana
│       ├── datasources/
│       │   └── datasources.yml           # Data sources: Prometheus, Loki, Jaeger
│       ├── dashboards/
│       │   ├── dashboards.yml            # Provider de dashboards
│       │   └── bank-api-dashboard.json   # Dashboard principal de observabilidad
│       └── alerting/                     # 🔔 ALERTAS (lo nuevo)
│           ├── contactpoints.yml         # Punto de contacto → webhook al alert-bridge
│           ├── policies.yml              # Política de notificación (ruta las alertas)
│           └── rules.yml                 # 3 reglas de alerta (errores, latencia, caída)
│
├── services/
│   ├── Bank.Transactions.Api/            # 🏦 Microservicio .NET
│   │   └── Bank.Transactions.Api/
│   │       ├── Program.cs                # Config OpenTelemetry + endpoints
│   │       ├── Dockerfile                # Imagen Docker de la API
│   │       └── Controllers/
│   │           ├── AutorizacionClienteController.cs  # SIBIL/BDN/solicitudes
│   │           ├── ClienteController.cs              # PCCU/PEP
│   │           ├── ConyugeController.cs              # Cónyuge (con errores simulados)
│   │           ├── CuentasController.cs              # Cuentas + TrafficSimulator
│   │           └── WeatherForecastController.cs      # Ejemplo default (ignorar)
│   │
│   └── alert-bridge/                     # 🌉 Puente de alertas (simulador WhatsApp)
│       ├── index.js                      # API Express (webhook + almacenamiento)
│       ├── package.json                  # Dependencias (express)
│       └── Dockerfile                    # Imagen Docker del bridge
│
└── functions/                            # ☁️ Firebase Cloud Functions (opcional, no desplegado)
    └── index.js
```

### Archivos clave de alerting

| Archivo | Ruta | Qué configura |
|---|---|---|
| **Contact Point** | `observability/grafana/provisioning/alerting/contactpoints.yml` | Define el webhook que Grafana llama cuando hay alerta: `http://alert-bridge:3001/webhook` |
| **Notification Policy** | `observability/grafana/provisioning/alerting/policies.yml` | Ruta TODAS las alertas al contact point "Alert Bridge". Configura tiempos de espera y repetición |
| **Alert Rules** | `observability/grafana/provisioning/alerting/rules.yml` | 3 reglas: tasa errores 5xx >3%, latencia P95 >500ms, API sin tráfico |

> **Nota**: Estos archivos se cargan automáticamente al iniciar Grafana gracias al volumen montado en `docker-compose.yml`.

---

## 3. Servicios Docker

Iniciar todo:
```bash
docker compose up -d
```

| Servicio | URL Local | Para qué |
|---|---|---|
| **Bank API** | http://localhost:5000 | Tu API .NET (Swagger en `/swagger`) |
| **Grafana** | http://localhost:3000 | Dashboards, alertas (admin/admin) |
| **Prometheus** | http://localhost:9090 | Consultar métricas directamente |
| **Jaeger** | http://localhost:16686 | Explorar trazas distribuidas |
| **Loki** | http://localhost:3100 | API de logs (se consulta desde Grafana) |
| **Alert Bridge** | http://localhost:3001 | Ver alertas guardadas |
| **OTel Collector** | :4317 (gRPC) / :8889 (métricas) | Recibe telemetría de la API (no tiene UI) |

---

## 4. Endpoints de la API

### Autorización de Clientes
| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/autorizacioncliente/jefes-autorizador` | Lista jefes autorizadores |
| GET | `/api/autorizacioncliente/busqueda-celular/{clienteId}` | Busca celular de cliente |
| GET | `/api/autorizacioncliente/busqueda-solicitud/{clienteId}` | Solicitudes pendientes |
| POST | `/api/autorizacioncliente/enviar-solicitud` | Envía solicitud + correo |
| POST | `/api/autorizacioncliente/autorizar-solicitud` | Autoriza con verificación SIBIL/BDN |
| GET | `/api/autorizacioncliente/lista-solicitudes/{codigoUsuario}` | Lista solicitudes del usuario |

### Cliente (PCCU / PEP)
| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/cliente/consulta-pccu/{clienteId}` | Verificación PCCU persona natural |
| GET | `/api/cliente/alerta-pep/{clienteId}` | Consulta alerta PEP (SIBIL/BDN) |
| POST | `/api/cliente/verifica-pep-pccu` | Verificación PEP mediante PCCU |
| GET | `/api/cliente/parametros` | Parámetros del sistema |

### Cónyuge
| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/conyuge/{clienteId}` | Obtiene datos del cónyuge (**5% errores simulados**) |
| PUT | `/api/conyuge/{clienteId}` | Modifica datos del cónyuge |

### Cuentas y Transacciones
| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/cuentas/apertura` | Apertura de cuenta (verifica PCCU+PEP) |
| POST | `/api/cuentas/deposito` | Depósito (alerta UIF si >50,000 BOB) |
| GET | `/api/cuentas/saldo/{nroCuenta}` | Consulta de saldo |
| POST | `/api/transactions/transfer` | Transferencia bancaria (**15% errores simulados**) |

### Simulador de Tráfico
| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/trafficsimulator/start?durationSeconds=60&requestsPerSecond=3` | Inicia tráfico automático |
| POST | `/api/trafficsimulator/stop` | Detiene la simulación |
| GET | `/api/trafficsimulator/status` | Estado de la simulación |

---

## 5. Alertas en Grafana

### ¿Cómo pensar al crear una alerta?

Hacete 3 preguntas:

> **1. ¿Qué puede fallar?** → Ej: "El endpoint de cónyuge lanza excepciones"
>
> **2. ¿Cómo lo mido?** → Ej: "Contando requests con status 5xx"
>
> **3. ¿Cuál es el umbral?** → Ej: "Si más del 3% son errores → alerta critical"

**Regla de oro**: Solo creá alertas si alguien va a **hacer algo** al recibirla. Si no requiere acción, mejor dejala como panel en el dashboard.

### Mapa de alertas por endpoint

| Endpoint | ¿Qué puede fallar? | Métrica a vigilar | Umbral sugerido |
|---|---|---|---|
| `/api/autorizacioncliente/autorizar-solicitud` | Verificación SIBIL/BDN timeout → respuesta lenta | **Latencia P95** | > 500ms |
| `/api/cliente/consulta-pccu` | Servicio PCCU externo caído → error 500 | **Tasa de errores 5xx** | > 5% |
| `/api/cliente/alerta-pep` | PEP positivo detectado (alerta de negocio) | **Logs con "ALERTA PEP DETECTADA"** | Cualquier ocurrencia |
| `/api/conyuge/{id}` | `ArgumentOutOfRangeException` (~5%) | **Tasa errores 5xx en esa ruta** | > 3% |
| `/api/cuentas/deposito` | Depósito > 50,000 BOB (reporte UIF) | **Logs con "reporte UIF"** | Cualquier ocurrencia |
| `/api/cuentas/apertura` | Timeout core bancario → 503 | **Errores 503** | > 1 en 5 min |
| **Todos los endpoints** | API completamente caída | **Requests/segundo = 0** | 0 req en 3 min |

### Reglas ya configuradas (automáticas)

Están en `observability/grafana/provisioning/alerting/rules.yml`:

| Regla | Umbral | Severidad | Se activa cuando... |
|---|---|---|---|
| **Alta tasa de errores 5xx** | > 3% de requests | `critical` | La proporción de errores 500 supera el 3% |
| **Latencia alta (P95 > 500ms)** | P95 > 500ms | `warning` | El 5% más lento de los requests tarda más de 500ms |
| **API sin tráfico** | < 0.001 req/s en 5min | `critical` | La API dejó de recibir peticiones (posible caída) |

### ¿Dónde ver las alertas en Grafana?

| Vista | URL |
|---|---|
| Reglas de alerta | http://localhost:3000/alerting/list |
| Contact points | http://localhost:3000/alerting/notifications |
| Historial de alertas | http://localhost:3000/alerting/history |

---

## 6. Alert-Bridge (Simulador WhatsApp)

### ¿Cómo funciona?

```
Grafana detecta umbral → POST /webhook → Alert Bridge → guarda en alerts.json + log diario
```

- **`alerts.json`** = "Bandeja de entrada" de WhatsApp (todas las alertas acumuladas)
- **`alerts-YYYY-MM-DD.log`** = Historial diario (como un chat de mensajes del día)
- **`GET /alerts`** = Como abrir la app WhatsApp y ver los mensajes recibidos

Para producción real con WhatsApp, solo hay que agregar una llamada a la API de Twilio WhatsApp dentro del `POST /webhook` en `services/alert-bridge/index.js`.

### Endpoints del Alert Bridge

| Método | URL | Función |
|---|---|---|
| GET | http://localhost:3001/alerts | **Ver todas las alertas guardadas** (la "bandeja de WhatsApp") |
| GET | http://localhost:3001/health | Health check del servicio |
| DELETE | http://localhost:3001/alerts | **Borrar todas las alertas** (vaciar la bandeja) |
| POST | http://localhost:3001/webhook | Recibir alerta (lo llama Grafana automáticamente) |

### ¿Cómo borrar las alertas de la bandeja?

**Opción 1 — Desde el navegador o Postman:**
```
DELETE http://localhost:3001/alerts
```

**Opción 2 — Desde PowerShell:**
```powershell
Invoke-RestMethod -Method DELETE -Uri http://localhost:3001/alerts
```

**Opción 3 — Desde terminal con curl:**
```bash
curl -X DELETE http://localhost:3001/alerts
```

Respuesta esperada:
```json
{ "status": "ok", "message": "Alertas limpiadas" }
```

> **Nota**: Las reglas de Grafana se evalúan cada minuto. Si la condición sigue cumpliéndose (ej: aún hay errores 5xx), Grafana enviará nuevas alertas después de limpiar. Esto es comportamiento normal.

---

## 7. Jaeger (Trazas)

**URL**: http://localhost:16686

### ¿Qué servicio elegir?

| Servicio | ¿Qué es? | ¿Cuándo usarlo? |
|---|---|---|
| **`bank-transactions-api`** ✅ | **Tu aplicación .NET**. Trazas de cada request HTTP | **Siempre usar este** |
| `jaeger-all-in-one` ❌ | Trazas internas de Jaeger sobre sí mismo | Nunca (ignorar) |

### ¿Qué ver en cada traza?

Una traza típica se ve así:

```
Trace: POST /api/autorizacioncliente/autorizar-solicitud  (350ms)
├── 🟦 HTTP POST handler                    [0ms ─────────── 350ms]
│   ├── 🟩 Verificación SIBIL               [10ms ── 130ms]
│   ├── 🟩 Consulta BDN                     [135ms ── 215ms]
│   └── 🟩 Envío correo confirmación        [220ms ── 270ms]
```

| Elemento | Qué significa | Qué buscar |
|---|---|---|
| **Span principal** (barra superior) | El request HTTP completo | Duración total y HTTP status code |
| **Spans hijos** (barras debajo) | Operaciones internas (llamadas externas, DB) | Cuál operación tarda más → cuello de botella |
| **Barra roja** | Error en ese span | Click para ver el stacktrace del error |
| **Tags** | Metadatos del span | `http.status_code`, `http.method`, `http.url` |
| **Duration** | Tiempo de cada operación | Comparar duraciones para identificar cuellos de botella |
| **Gaps** (espacios vacíos) | Tiempo sin actividad entre spans | Podría indicar espera por I/O o locks |

### Filtros útiles en Jaeger

| Filtro | Valor de ejemplo | Para qué |
|---|---|---|
| **Service** | `bank-transactions-api` | Siempre seleccionar tu API |
| **Operation** | `HTTP POST` ó ruta específica | Filtrar por tipo de operación |
| **Tags** | `http.status_code=500` | Solo ver requests que fallaron |
| **Min Duration** | `500ms` | Solo ver requests lentos |
| **Max Duration** | `50ms` | Solo ver requests rápidos (verificar normalidad) |
| **Lookback** | `Last Hour` / `Last 2 Hours` | Rango de tiempo a buscar |
| **Limit Results** | `20` | Cantidad de trazas a mostrar |

### Casos de uso típicos

| Quiero... | Filtros a usar |
|---|---|
| Ver por qué un request falló | Tags: `http.status_code=500`, buscar spans rojos |
| Encontrar los requests más lentos | Min Duration: `500ms`, ordenar por duración |
| Ver el flujo de una autorización | Operation: `HTTP POST`, buscar ruta `/autorizar-solicitud` |
| Comparar latencia entre endpoints | No filtrar Operation, ordenar por duración |

---

## 8. Prometheus (Métricas)

**URL**: http://localhost:9090

Prometheus almacena métricas numéricas en series de tiempo. Lo usás desde Grafana o directamente en la UI de Prometheus.

### Métricas disponibles de tu API

| Categoría | Métricas |
|---|---|
| **HTTP Server** | `http_server_request_duration_seconds_*`, `http_server_active_requests` |
| **HTTP Client** | `http_client_request_duration_seconds_*`, `http_client_open_connections` |
| **Runtime .NET** | `dotnet_process_memory_working_set_bytes`, `dotnet_process_cpu_time_seconds_total` |
| **GC .NET** | `dotnet_gc_collections_total`, `dotnet_gc_heap_allocated_bytes_total`, `dotnet_gc_last_collection_heap_size_bytes` |
| **Thread Pool** | `dotnet_thread_pool_thread_count_total`, `dotnet_thread_pool_queue_length_total` |
| **Excepciones** | `dotnet_exceptions_total` |

### Labels disponibles (para filtrar)

Las métricas HTTP tienen estos labels que podés usar para filtrar:

| Label | Valores posibles | Ejemplo de uso |
|---|---|---|
| `http_request_method` | GET, POST, PUT, DELETE | Filtrar por tipo de operación |
| `http_route` | `/api/conyuge/{clienteId}`, `/api/cuentas/apertura`, etc. | Filtrar por endpoint |
| `http_response_status_code` | 200, 500, 502, 503 | Filtrar errores vs. éxitos |

---

## 9. Queries Prometheus — Referencia Rápida

**Los queries más importantes para empezar:**

| Objetivo | Query | Tipo de gráfica |
|---|---|---|
| ¿Cuánto tráfico tiene mi API? | `sum(rate(http_server_request_duration_seconds_count[2m]))` | Time series (línea) |
| ¿Cuál endpoint recibe más tráfico? | `sum by (http_route) (rate(http_server_request_duration_seconds_count[2m]))` | Time series (múltiples líneas) |
| ¿Cuánto tarda en responder? (P95) | `histogram_quantile(0.95, sum(rate(http_server_request_duration_seconds_bucket[2m])) by (le))` | Time series |
| ¿Cuál endpoint es más lento? | `histogram_quantile(0.95, sum(rate(http_server_request_duration_seconds_bucket[2m])) by (le, http_route))` | Time series |
| ¿Mi API está fallando? (% errores) | `sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[2m])) / sum(rate(http_server_request_duration_seconds_count[2m]))` | Gauge / Stat |
| ¿Cuál endpoint falla más? | `sum by (http_route) (rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[2m]))` | Bar chart |
| ¿Cuánta memoria usa? | `dotnet_process_memory_working_set_bytes / 1024 / 1024` | Time series (MB) |
| ¿El GC está presionado? | `rate(dotnet_gc_collections_total[2m])` | Time series |
| ¿Hay excepciones lanzándose? | `rate(dotnet_exceptions_total[2m])` | Time series |

---

## 10. Queries Prometheus — Catálogo Completo

### 📊 A. Tráfico HTTP

**A1. Requests por segundo (desglosado)**
```promql
rate(http_server_request_duration_seconds_count[2m])
```
- **Gráfica**: Una línea por cada combinación método+ruta+status
- **Tabla**: Cada fila = combinación única (ej: `GET /api/conyuge/{clienteId} 200 → 0.45`)

**A2. Requests totales por segundo**
```promql
sum(rate(http_server_request_duration_seconds_count[2m]))
```
- **Gráfica**: Una sola línea de tráfico total
- **Tabla**: Un solo valor numérico (ej: `2.5` = 2.5 req/seg)

**A3. Requests por ruta (ranking)**
```promql
sum by (http_route) (rate(http_server_request_duration_seconds_count[2m]))
```
- **Gráfica**: Múltiples líneas, una por endpoint
- **Tabla**: Ranking de endpoints por volumen de tráfico

**A4. Requests por método HTTP**
```promql
sum by (http_request_method) (rate(http_server_request_duration_seconds_count[2m]))
```
- **Gráfica**: Líneas separadas para GET, POST, PUT, DELETE

**A5. Requests por código de estado**
```promql
sum by (http_response_status_code) (rate(http_server_request_duration_seconds_count[2m]))
```
- **Gráfica**: Líneas por código (200, 500, 502, 503)
- **Ideal para**: Detectar picos de errores visualmente

**A6. Requests activos ahora mismo**
```promql
http_server_active_requests
```
- **Gráfica**: Requests en proceso simultáneamente (0-5 en simulación, 50-100 en producción)

---

### 🐌 B. Latencia

**B1. Latencia promedio global**
```promql
rate(http_server_request_duration_seconds_sum[2m]) / rate(http_server_request_duration_seconds_count[2m])
```
- **Valor esperado**: 0.050 - 0.300 seg (50ms - 300ms)

**B2. Latencia promedio por ruta**
```promql
sum by (http_route) (rate(http_server_request_duration_seconds_sum[2m])) / sum by (http_route) (rate(http_server_request_duration_seconds_count[2m]))
```
- **Gráfica**: Una línea por endpoint → los más lentos arriba
- **Esperable**: `/autorizar-solicitud` más lenta que `/parametros`

**B3. Latencia P50 (mediana)**
```promql
histogram_quantile(0.50, sum(rate(http_server_request_duration_seconds_bucket[2m])) by (le))
```
- **Significado**: El 50% de los requests terminan antes de este tiempo
- **Valor esperado**: ~100ms

**B4. Latencia P95**
```promql
histogram_quantile(0.95, sum(rate(http_server_request_duration_seconds_bucket[2m])) by (le))
```
- **Significado**: El 95% de los requests terminan antes de este tiempo
- **Valor esperado**: ~300-500ms
- **La mejor métrica para alertar sobre rendimiento**

**B5. Latencia P99**
```promql
histogram_quantile(0.99, sum(rate(http_server_request_duration_seconds_bucket[2m])) by (le))
```
- **Significado**: Los peores 1% de requests
- **Valor esperado**: ~500ms-1s

**B6. Latencia P95 por ruta**
```promql
histogram_quantile(0.95, sum(rate(http_server_request_duration_seconds_bucket[2m])) by (le, http_route))
```
- **Gráfica**: Una línea P95 por cada endpoint → identifica los más lentos

---

### 🔴 C. Errores

**C1. Tasa de errores 5xx (porcentaje)**
```promql
sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[2m])) / sum(rate(http_server_request_duration_seconds_count[2m]))
```
- **Gráfica**: Porcentaje (0 a 1). Valor `0.05` = 5% de errores
- **Alerta sugerida**: si > 0.03 (3%)

**C2. Errores por ruta**
```promql
sum by (http_route) (rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[2m]))
```
- **Tabla**: `/api/conyuge/{clienteId}` tendrá más errores (~5%)

**C3. Errores por código específico**
```promql
sum by (http_response_status_code) (rate(http_server_request_duration_seconds_count{http_response_status_code=~"[45].."}[2m]))
```
- **Tabla**: Separado por 400, 404, 500, 502, 503

**C4. Solo errores 500**
```promql
sum(rate(http_server_request_duration_seconds_count{http_response_status_code="500"}[2m]))
```

**C5. Solo errores de servicios externos (502/503)**
```promql
sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"50[23]"}[2m]))
```
- **Si sube**: Problema en core bancario / BDN / SIBIL

---

### 🖥️ D. Runtime .NET

**D1. Memoria del proceso (MB)**
```promql
dotnet_process_memory_working_set_bytes / 1024 / 1024
```
- **Valor esperado**: 50-200 MB

**D2. Uso de CPU**
```promql
rate(dotnet_process_cpu_time_seconds_total[2m])
```
- **Valor**: `0.5` = 50% de un core. **Alerta si** > 0.8

**D3. Colecciones del Garbage Collector**
```promql
rate(dotnet_gc_collections_total[2m])
```
- **Gráfica**: Líneas por generación (gen0, gen1, gen2)
- **gen0**: Frecuente (normal) | **gen2**: Raro (si sube = memory pressure)

**D4. Tamaño del heap (MB)**
```promql
dotnet_gc_last_collection_heap_size_bytes / 1024 / 1024
```
- **Gráfica**: Sube y baja con cada GC collection

**D5. Bytes asignados por segundo (MB/s)**
```promql
rate(dotnet_gc_heap_allocated_bytes_total[2m]) / 1024 / 1024
```
- **Si sube constantemente**: Posible memory leak

**D6. Excepciones por segundo**
```promql
rate(dotnet_exceptions_total[2m])
```
- **Incluye**: El `ArgumentOutOfRangeException` del cónyuge
- **Alerta sugerida**: si > 1 exc/seg

**D7. Threads activos**
```promql
dotnet_thread_pool_thread_count_total
```
- **Si crece sin parar**: thread starvation

**D8. Contención de locks por segundo**
```promql
rate(dotnet_monitor_lock_contentions_total[2m])
```
- **Si sube**: Problemas de concurrencia

---

### 📡 E. HTTP Client (requests salientes)

**E1. Requests salientes por segundo**
```promql
rate(http_client_request_duration_seconds_count[2m])
```
- **Mide**: Las llamadas que TrafficSimulator hace internamente

**E2. Conexiones HTTP abiertas**
```promql
http_client_open_connections
```
- **Si crece indefinidamente**: connection leak

---

## 11. Comandos Útiles

### Docker
```bash
# Iniciar todo
docker compose up -d

# Ver estado
docker compose ps

# Reconstruir la API después de cambios
docker compose build bank-api && docker compose up -d bank-api

# Ver logs en tiempo real
docker compose logs -f bank-api
docker compose logs -f alert-bridge
docker compose logs -f grafana

# Reiniciar solo Grafana (para recargar config de alerting)
docker compose restart grafana
```

### Simulador de Tráfico
```bash
# Iniciar simulación (60s, 3 req/seg)
POST http://localhost:5000/api/trafficsimulator/start?durationSeconds=60&requestsPerSecond=3

# Detener
POST http://localhost:5000/api/trafficsimulator/stop

# Ver estado
GET http://localhost:5000/api/trafficsimulator/status
```

### Alert Bridge
```bash
# Ver alertas guardadas
GET http://localhost:3001/alerts

# Borrar todas las alertas
DELETE http://localhost:3001/alerts

# Health check
GET http://localhost:3001/health
```
