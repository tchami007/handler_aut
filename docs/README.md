# Documentación - Handler de Autorización

Esta carpeta contiene toda la documentación técnica del proyecto Handler de Autorización.

## 📁 Estructura de Documentación

### 🏗️ **diseno_solicitudes/** - Arquitectura de Solicitudes
**Documentación completa del diseño de métodos de procesamiento de solicitudes**

- **`diseno_metodos_solicitud.md`** - Arquitectura y decisiones de diseño
- **`matriz_comparacion_servicios.md`** - Comparación técnica entre servicios  
- **`quick_reference_solicitudes.md`** - Referencia rápida y troubleshooting
- **`README.md`** - Guía de uso de la documentación

**📊 Servicios Cubiertos:**
- ✅ SolicitudService (Tradicional)
- ✅ SolicitudCommandQueueBackgroundService (Diferido)
- ✅ SolicitudCommandQueueInmediateService (Híbrido)

**🔧 Aspectos Técnicos:**
- ✅ Transacciones serializables
- ✅ Sistema de reintentos unificado
- ✅ Códigos de error estandarizados
- ✅ Control de concurrencia optimista

---

### 📋 Documentos Generales del Proyecto

#### Análisis y Arquitectura
- `ANALISIS_ARQUITECTURA_3_CAPAS.md` - Análisis de arquitectura en 3 capas
- `ANALISIS_ARQUITECTURA_CLASICA_3_CAPAS.md` - Arquitectura clásica
- `diseno_tecnico.md` - Diseño técnico general
- `estado_proyecto.md` - Estado actual del proyecto

#### Desarrollo y Configuración
- `api_registro_solicitud.md` - API de registro de solicitudes
- `configuracion_handler.md` - Configuración del handler
- `control_concurrencia.md` - Control de concurrencia
- `servicio_estadistica.md` - Servicio de estadísticas

#### Infraestructura y Repositorios
- `INFRASTRUCTURE_REPOSITORIES_COMPLETADO.md` - Repositorios completados
- `CORRECCION_INFRASTRUCTURE_REPOSITORIES.md` - Correcciones en repositorios
- `correccion_transacciones_serializables.md` - Correcciones de transacciones

#### Análisis de Errores y Optimizaciones
- `analisis_errores_concurrencia.md` - Análisis de errores de concurrencia
- `optimizacion_logging_performance.md` - Optimización de logging
- `refactorizacion_completada_reporte.md` - Reporte de refactorización
- `refactorizacion_logging.md` - Refactorización de logging
- `refactorizacion_solicitud_command_services.md` - Refactorización de servicios

#### Operaciones y Mantenimiento
- `limite_configuracion_colas.md` - Configuración de límites de colas
- `limpieza_colas_rabbitmq.md` - Limpieza de colas RabbitMQ
- `instrucciones_git.md` - Instrucciones de Git
- `flujo_ramas.md` - Flujo de ramas

#### Resúmenes de Cambios
- `RESUMEN_CAMBIOS_ENERO_2025.md` - Resumen de cambios de enero 2025

#### Documentos de Referencia
- `idea.md` - Ideas y conceptos
- `Handler de autorización (documento uso interno) - Documentos de Google.pdf` - Documento interno

#### Planes y Roadmap
- `plan/` - Carpeta con planes de desarrollo

---

## 🎯 Guía de Navegación

### 👨‍💻 Para Desarrolladores Nuevos
1. **Inicio:** Leer `estado_proyecto.md` para contexto general
2. **Arquitectura:** Revisar `diseno_tecnico.md` y análisis de arquitectura
3. **Solicitudes:** Explorar `diseno_solicitudes/` para lógica de negocio
4. **Configuración:** Consultar `configuracion_handler.md`

### 🔧 Para Troubleshooting
1. **Solicitudes:** Usar `diseno_solicitudes/quick_reference_solicitudes.md`
2. **Concurrencia:** Revisar `analisis_errores_concurrencia.md`
3. **Performance:** Consultar optimizaciones de logging
4. **RabbitMQ:** Ver `limpieza_colas_rabbitmq.md`

### 🏗️ Para Arquitectura y Diseño
1. **Análisis:** Documentos de análisis de arquitectura
2. **Diseño:** `diseno_tecnico.md` y `diseno_solicitudes/`
3. **Infraestructura:** Documentos de repositories y correcciones
4. **Refactorización:** Reportes de refactorización completados

### 📊 Para Análisis y Métricas
1. **Servicios:** `diseno_solicitudes/matriz_comparacion_servicios.md`
2. **Performance:** Documentos de optimización
3. **Estadísticas:** `servicio_estadistica.md`
4. **Monitoreo:** Documentos de logging y performance

---

## 📅 Última Actualización

**Fecha:** 9 de octubre de 2025  
**Cambio Principal:** Creación de carpeta `diseno_solicitudes/` con documentación unificada de métodos de solicitud

**Estado de Documentación:** ✅ Actualizada y organizada

---

## 📝 Convenciones de Documentación

### 📁 Organización
- **Carpetas temáticas** para conjuntos relacionados de documentación
- **README.md** en cada carpeta para guía de navegación
- **Nombres descriptivos** y consistentes

### 📖 Formato
- **Markdown** para todos los documentos
- **Emojis** para mejor legibilidad
- **Tablas** para comparaciones técnicas
- **Código** con syntax highlighting

### 🔄 Mantenimiento
- **Fechas de actualización** en documentos principales
- **Versionado** para cambios importantes
- **Referencias cruzadas** entre documentos relacionados

---

**👥 Mantenido por:** Equipo Handler de Autorización  
**📂 Ubicación:** `/docs/`