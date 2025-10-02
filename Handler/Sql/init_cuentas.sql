-- Elimina todos los registros de las tablas Cuentas y SolicitudesDebito
DELETE FROM SolicitudesDebito;
DELETE FROM Cuentas;

-- Inserta 1000 cuentas con saldos positivos diferentes
DECLARE @i INT = 1;
WHILE @i <= 1000
BEGIN
    INSERT INTO Cuentas (Numero, Saldo)
    VALUES (1000000000 + @i, @i * 15000.50);
    SET @i = @i + 1;
END;