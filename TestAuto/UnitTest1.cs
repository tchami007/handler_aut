using Xunit;

namespace TestAuto
{
    /// <summary>
    /// Test de ejemplo básico para validar la configuración y ejecución de pruebas automáticas en el proyecto.
    /// No corresponde a una validación funcional de la etapa 1, solo verifica el entorno de test.
    /// </summary>
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Assert.True(1 + 1 == 2);
        }
    }
}
