# Plan de Proyecto – Handler de Autorización de Débitos

Este plan de desarrollo está basado en el diseño técnico del handler y worker, estructurado en etapas con tareas, estimaciones de tiempo y comprobaciones manuales al finalizar cada etapa.

---

## Etapa 1: Configuración de Proyecto y Estructura Base  
**Duración estimada:** 2 días

- Crear solución y proyectos base (.NET Core 8 Web API para Handler y Worker).
- Configurar carpetas: Controller, Service, Infrastructure, Model, Shared.
- Configurar integración con SQL Server y RabbitMQ.
- Definir modelos de datos principales.
- Implementar autenticación JWT básica.
- **Comprobación manual:** Ejecutar scripts de test para verificar la estructura, conexión a base de datos y colas.

---

## Etapa 2: Implementación de APIs Principales del Handler  
**Duración estimada:** 4 días

- Implementar endpoints: Health, ActivarHandler, InactivarHandler, ConfigurarColas, Estadística, SaldoCuentaByCuenta, SaldoCuentaAll.
- Implementar lógica de registro y consulta de saldos.
- Configurar persistencia de solicitudes y estados.
- **Comprobación manual:** Disparar scripts de test sobre los endpoints para validar respuestas y persistencia.

---

## Etapa 3: Implementación de Procesamiento de Solicitudes y Colas  
**Duración estimada:** 5 días

- Implementar lógica de encolado por cuenta y control de concurrencia.
- Desarrollar worker para procesamiento asíncrono y comunicación con Core.
- Implementar timeout y fallback (autorización provisional).
- Integrar RabbitMQ para eventos y recuperación ante fallos.
- **Comprobación manual:** Ejecutar pruebas de concurrencia y recuperación, validando el procesamiento ordenado y la persistencia.

---

## Etapa 4: Integración y Procesos de Negocio  
**Duración estimada:** 3 días

- Integrar ADO.net para disparo de procesos en base Bansys.
- Implementar lógica de reversión/regularización ante rechazo del Core.
- Mejorar trazabilidad y auditoría de operaciones.
- **Comprobación manual:** Ejecutar scripts de test para validar integración con sistemas externos y trazabilidad.

---

## Etapa 5: Seguridad, Escalabilidad y Optimización  
**Duración estimada:** 2 días

- Revisar y reforzar autenticación JWT.
- Optimizar escalabilidad horizontal de workers.
- Validar tolerancia a fallos y recuperación automática.
- Documentar endpoints y procesos internos.
- **Comprobación manual:** Disparar scripts de test de seguridad, escalabilidad y recuperación.

---

## Etapa Final: Pruebas Integrales y Validación  
**Duración estimada:** 2 días

- Ejecutar pruebas integrales de todos los flujos.
- Validar funcionamiento con scripts de test automatizados y manuales.
- Documentar resultados y checklist de validación.
- Ajustar según hallazgos.

---

**Total estimado:** 18 días

---

Cada etapa incluye una tarea final de comprobación manual, disparando scripts de test que aseguren el funcionamiento antes de avanzar a la siguiente fase.
