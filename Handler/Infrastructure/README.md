# Infrastructure

Contiene la configuración de acceso a datos, contexto de Entity Framework y repositorios para el manejo de datos.

## 📊 **Componentes**

### **DbContext**
- `HandlerDbContext.cs` - Contexto principal de Entity Framework

### **Repositories - Interfaces**
- `ICuentaRepository.cs` - Operaciones de datos para entidades Cuenta
- `ISolicitudRepository.cs` - Operaciones de datos para entidades SolicitudDebito  
- `ILogOperacionRepository.cs` - Operaciones de datos para entidades LogOperacion

### **Repositories - Implementaciones**
- `CuentaRepository.cs` - Implementación completa para manejo de cuentas
- `SolicitudRepository.cs` - Implementación completa para manejo de solicitudes
- `LogOperacionRepository.cs` - Implementación completa para manejo de logs

## 🎯 **Responsabilidades**

Esta capa es responsable de:

- **Acceso a Datos**: Manejo directo de Entity Framework y base de datos
- **Consultas Específicas**: Queries optimizadas por dominio de negocio
- **Operaciones CRUD**: Create, Read, Update, Delete por entidad
- **Validaciones de Persistencia**: Verificaciones a nivel de datos
- **Operaciones en Lote**: Manejo eficiente de múltiples registros

## 🔄 **Patrón Repository**

Cada entidad tiene su propio repositorio que:

1. **Encapsula** el acceso a DbContext
2. **Abstrae** las consultas específicas del dominio  
3. **Facilita** el testing mediante interfaces
4. **Centraliza** la lógica de acceso a datos
5. **Permite** optimizaciones específicas por entidad

## 📝 **Uso desde Services**

```csharp
// ✅ Correcto: Service usando Repository
public class SaldoService : ISaldoService
{
    private readonly ICuentaRepository _cuentaRepository;
    
    public SaldoService(ICuentaRepository cuentaRepository)
    {
        _cuentaRepository = cuentaRepository;
    }
    
    public async Task<SaldoCuentaDto?> GetSaldoByCuentaAsync(long numero)
    {
        var cuenta = await _cuentaRepository.GetByNumeroAsync(numero);
        return cuenta == null ? null : new SaldoCuentaDto 
        { 
            NumeroCuenta = cuenta.Numero, 
            Saldo = cuenta.Saldo 
        };
    }
}
```