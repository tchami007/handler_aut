# Diseño de Solicitudes - Documentación Técnica

Esta carpeta contiene la documentación completa sobre el diseño y arquitectura de los métodos de procesamiento de solicitudes en el Handler de Autorización.

## 📁 Contenido de la Carpeta

### 📖 Documentos Principales

#### 1. **`diseno_metodos_solicitud.md`** - Documento Arquitectural Principal
🎯 **Propósito:** Documentación completa de la arquitectura y decisiones de diseño

**Incluye:**
- ✅ Arquitectura de los 3 servicios de solicitud
- ✅ Configuración de transacciones serializables
- ✅ Sistema de reintentos unificado
- ✅ Códigos de estado completos (0, 1-5, 97-99)
- ✅ Manejo de excepciones detallado
- ✅ Control de concurrencia optimista
- ✅ Validaciones de negocio
- ✅ Distribución de colas RabbitMQ
- ✅ Métricas y monitoreo
- ✅ Configuración por ambiente

**Cuándo usar:** Para entender la arquitectura completa o implementar nuevos servicios.

---

#### 2. **`matriz_comparacion_servicios.md`** - Análisis Comparativo
🎯 **Propósito:** Comparación técnica detallada entre los servicios

**Incluye:**
- 📊 Tabla comparativa de características técnicas
- 🔄 Diagramas de flujo de procesamiento
- 📈 Métricas de performance estimadas
- 🎯 Casos de uso recomendados por servicio
- 🔧 Configuración específica por ambiente
- 🚨 Patrones de error característicos
- 🔄 Guías de migración entre servicios
- 🐛 Debugging específico por servicio

**Cuándo usar:** Para elegir el servicio adecuado según el caso de uso o para migrar entre servicios.

---

#### 3. **`quick_reference_solicitudes.md`** - Referencia Rápida
🎯 **Propósito:** Guía práctica para desarrollo y troubleshooting

**Incluye:**
- 🚨 Códigos de error con acciones sugeridas
- 🔄 Patrones de código copy & paste
- 🔧 Soluciones quick fix para problemas comunes
- 📋 Checklists de implementación
- 🐛 Guías de debugging paso a paso
- 📊 Queries SQL para diagnóstico

**Cuándo usar:** Para implementación rápida, debugging o resolución de problemas.

---

## 🎯 Servicios Documentados

### 1. **SolicitudService** (Tradicional)
- **Patrón:** Secuencial completo
- **Uso:** APIs que requieren ID inmediato
- **Performance:** Alta latencia, bajo throughput

### 2. **SolicitudCommandQueueBackgroundService** (Diferido)
- **Patrón:** Validación rápida + procesamiento background
- **Uso:** APIs de alta concurrencia
- **Performance:** Baja latencia, alto throughput

### 3. **SolicitudCommandQueueInmediateService** (Híbrido)
- **Patrón:** Saldo inmediato + registro diferido
- **Uso:** Balance entre performance y consistencia
- **Performance:** Latencia media, throughput alto

---

## 🔧 Características Técnicas Unificadas

### ✅ Transacciones
- **Nivel:** `IsolationLevel.Serializable`
- **Uso:** Operaciones críticas de saldo
- **Beneficio:** Máxima consistencia

### ✅ Reintentos
- **Cantidad:** 10 reintentos
- **Tiempo:** Aleatorio 50-100ms
- **Manejo:** DbUpdateConcurrencyException, deadlocks

### ✅ Códigos de Error
- **0:** Autorizada ✅
- **1-5:** Rechazos por validación ❌
- **97-99:** Errores técnicos 🚨

### ✅ Control de Concurrencia
- **Método:** Concurrencia optimista
- **Implementación:** RowVersion en tabla Cuentas
- **Detección:** Automática por Entity Framework

---

## 📋 Cómo Usar Esta Documentación

### 🆕 Para Nuevos Desarrolladores
1. Leer `diseno_metodos_solicitud.md` para entender la arquitectura
2. Revisar `matriz_comparacion_servicios.md` para casos de uso
3. Usar `quick_reference_solicitudes.md` para implementación

### 🔧 Para Debugging
1. Consultar códigos de error en `quick_reference_solicitudes.md`
2. Usar patrones de troubleshooting específicos
3. Aplicar quick fixes según el problema

### 🚀 Para Nuevas Implementaciones
1. Copiar patrones de `quick_reference_solicitudes.md`
2. Seguir checklist de implementación
3. Validar contra arquitectura en documento principal

### 📊 Para Análisis de Performance
1. Revisar métricas en `matriz_comparacion_servicios.md`
2. Aplicar configuración específica por ambiente
3. Usar queries de diagnóstico incluidas

---

## 🔄 Evolución de la Documentación

### Versión Actual: 3.0 (Unificada)
- ✅ Criterios unificados entre servicios
- ✅ Transacciones serializables consistentes
- ✅ Códigos de error estandarizados
- ✅ Reintentos con patrones aleatorios

### Historial de Cambios
- **v1.0:** Implementación básica sin transacciones
- **v2.0:** Transacciones + EnableRetryOnFailure (problemático)
- **v3.0:** Transacciones serializables + reintentos manuales (actual)

### Próximas Mejoras Planificadas
- [ ] Métricas automáticas con Prometheus
- [ ] Circuit breaker para cuentas problemáticas
- [ ] Cache de validaciones
- [ ] Batching de operaciones

---

## 📞 Soporte y Mantenimiento

### 🐛 Reportar Problemas
1. Consultar `quick_reference_solicitudes.md` para soluciones rápidas
2. Revisar logs con códigos de error específicos
3. Aplicar troubleshooting según el patrón de error

### 📝 Actualizar Documentación
1. Mantener sincronización con cambios de código
2. Actualizar versiones y fechas
3. Agregar nuevos patrones o casos de uso

### 🧪 Testing
1. Usar configuración de testing específica
2. Validar bajo carga según métricas documentadas
3. Verificar códigos de error en scenarios de fallo

---

**📅 Última Actualización:** 9 de octubre de 2025  
**👥 Mantenido por:** Equipo Handler de Autorización  
**📍 Ubicación:** `/docs/diseno_solicitudes/`