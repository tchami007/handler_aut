using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TestAuto
{
    /// <summary>
    /// Define una colección de tests que se ejecutan secuencialmente para evitar conflictos de base de datos.
    /// Los tests de esta colección no se ejecutan en paralelo entre sí.
    /// </summary>
    [CollectionDefinition("DatabaseTests")]
    public class DatabaseTestCollection : ICollectionFixture<WebApplicationFactory<Handler.Program>>
    {
        // Esta clase no tiene código, solo sirve como punto de definición para la colección
    }
}