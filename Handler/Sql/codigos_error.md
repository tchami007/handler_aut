# Códigos de Error – Handler SQL

Este documento detalla los códigos de error utilizados en los procedimientos almacenados del sistema Banksys.

| Código  | Mensaje                                        | Descripción                                                        |
|---------|------------------------------------------------|--------------------------------------------------------------------|
| 50001   | Saldo insuficiente para debitar la cuenta.      | El débito no se realiza porque la cuenta no tiene saldo suficiente. |
| 50002   | La cuenta no existe.                           | El número de cuenta no se encuentra en la base de datos.            |
| 50003   | No existe movimiento original para contrasiento.| No se encuentra el movimiento original para revertir (contrasiento).|

**Uso:**
- Los códigos se devuelven en el mensaje de error SQL (RAISERROR) para facilitar la integración y el manejo de errores en la aplicación.
- Se recomienda capturar y mostrar el mensaje y el código en la capa de negocio y en los logs.

---

Actualiza este documento si se agregan nuevos códigos o procedimientos.
