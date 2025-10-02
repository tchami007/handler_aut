namespace Handler.Services
{
    /// <summary>
    /// Clase utilitaria para la generación de datos de cuentas.
    /// </summary>
    public static class CuentaFactory
    {
        /// <summary>
        /// Genera el número y saldo de una cuenta a partir de un índice.
        /// </summary>
        /// <param name="indice">Índice de la cuenta</param>
        /// <returns>Tuple con número de cuenta y saldo</returns>
        public static (long numero, decimal saldo) GenerarCuenta(int indice)
        {
            long numero = 1000000000L + indice;
            decimal saldo = indice * 150.50m + 10000;
            return (numero, saldo);
        }
    }
}
