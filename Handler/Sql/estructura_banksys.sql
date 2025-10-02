-- Tabla de cuentas
CREATE TABLE Cuentas (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NumeroCuenta NUMERIC(17,0) NOT NULL UNIQUE,
    Saldo DECIMAL(18,2) NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX UX_Cuentas_Numero ON Cuentas (Numero);

-- Tabla de movimientos
CREATE TABLE Movimientos (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NumeroCuenta NUMERIC(17,0) NOT NULL,
    Importe DECIMAL(18,2) NOT NULL,
    FechaMovimiento DATETIME NOT NULL,
    NumeroComprobante VARCHAR(50) NOT NULL,
    CodigoContrasiento CHAR(1) NULL,
    FechaReal DATETIME NOT NULL DEFAULT GETDATE(),
    FecEnlace DATETIME NULL,
    FunCod INT NOT NULL -- -1 para débito, +1 para crédito
);

-- Índice adicional para búsquedas frecuentes
CREATE INDEX IX_Movimientos_BusquedaOriginal
ON Movimientos (NumeroCuenta, Importe, FechaMovimiento, NumeroComprobante, CodigoContrasiento);

-- Store Procedure para consultar el saldo de una cuenta
CREATE PROCEDURE sp_ConsultarSaldoCuenta
    @NumeroCuenta NUMERIC(17,0)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @SaldoActual DECIMAL(18,2);
    SELECT @SaldoActual = Saldo FROM Cuentas WHERE NumeroCuenta = @NumeroCuenta;
    IF @SaldoActual IS NULL
    BEGIN
        RAISERROR('50002: La cuenta no existe.', 16, 1);
        RETURN;
    END
    SELECT @NumeroCuenta AS NumeroCuenta, @SaldoActual AS Saldo;
END

-- Store Procedure para débito y contrasiento (con transacción)
CREATE PROCEDURE sp_DebitarCuenta
    @NumeroCuenta NUMERIC(17,0),
    @Importe DECIMAL(18,2),
    @FechaMovimiento DATETIME,
    @NumeroComprobante VARCHAR(50),
    @Contrasiento CHAR(1) = NULL -- 'C' para contrasiento, NULL para débito normal
AS

BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @SaldoFinal DECIMAL(18,2);

        IF @Contrasiento IS NULL
        BEGIN
            DECLARE @SaldoActual DECIMAL(18,2);
            DECLARE @Comision DECIMAL(18,2);
            DECLARE @IVA DECIMAL(18,2);
            DECLARE @FechaRealMov DATETIME;
			
			-- Calculo de Comision e IVA
            SET @Comision = ROUND(@Importe * 0.08, 2);
            SET @IVA = ROUND(@Comision * 0.21, 2);

			-- Recuperacion de saldo
            SELECT @SaldoActual = Saldo FROM Cuentas WITH (ROWLOCK) WHERE NumeroCuenta = @NumeroCuenta;
            IF @SaldoActual IS NULL
            BEGIN
                RAISERROR('50002: La cuenta no existe.', 16, 1);
                ROLLBACK TRANSACTION;
                RETURN;
            END
            IF @SaldoActual < (@Importe + @Comision + @IVA)
            BEGIN
                RAISERROR('50001: Saldo insuficiente para debitar la cuenta (incluyendo comisión e IVA).', 16, 1);
                ROLLBACK TRANSACTION;
                RETURN;
            END
            -- Débito normal (importe + comisión + IVA)
            UPDATE Cuentas WITH (ROWLOCK)
            SET Saldo = Saldo - (@Importe + @Comision + @IVA)
            WHERE NumeroCuenta = @NumeroCuenta;

            SET @FechaRealMov = GETDATE();
            -- Movimiento principal
            INSERT INTO Movimientos (NumeroCuenta, Importe, FechaMovimiento, NumeroComprobante, CodigoContrasiento, FechaReal, FecEnlace, FunCod)
            VALUES (@NumeroCuenta, @Importe, @FechaMovimiento, @NumeroComprobante, NULL, @FechaRealMov, NULL, -1);
            -- Movimiento comisión
            INSERT INTO Movimientos (NumeroCuenta, Importe, FechaMovimiento, NumeroComprobante, CodigoContrasiento, FechaReal, FecEnlace, FunCod)
            VALUES (@NumeroCuenta, @Comision, @FechaMovimiento, @NumeroComprobante, NULL, @FechaRealMov, @FechaRealMov, -1);
            -- Movimiento IVA
            INSERT INTO Movimientos (NumeroCuenta, Importe, FechaMovimiento, NumeroComprobante, CodigoContrasiento, FechaReal, FecEnlace, FunCod)
            VALUES (@NumeroCuenta, @IVA, @FechaMovimiento, @NumeroComprobante, NULL, @FechaRealMov, @FechaRealMov, -1);
        END
        ELSE IF @Contrasiento = 'C'
        BEGIN
            DECLARE @IdMovimiento INT;
            DECLARE @FechaRealOriginal DATETIME;
            -- Buscar movimiento original
            SELECT @IdMovimiento = Id, @FechaRealOriginal = FechaReal
            FROM Movimientos
            WHERE NumeroCuenta = @NumeroCuenta
              AND Importe = @Importe
              AND FechaMovimiento = @FechaMovimiento
              AND NumeroComprobante = @NumeroComprobante
              AND CodigoContrasiento IS NULL;

            IF @IdMovimiento IS NULL
            BEGIN
                RAISERROR('50003: No existe movimiento original para contrasiento.', 16, 1);
                ROLLBACK TRANSACTION;
                RETURN;
            END
            -- Revertir el débito original
            UPDATE Cuentas WITH (ROWLOCK)
            SET Saldo = Saldo + @Importe
            WHERE NumeroCuenta = @NumeroCuenta;

            INSERT INTO Movimientos (NumeroCuenta, Importe, FechaMovimiento, NumeroComprobante, CodigoContrasiento, FechaReal, FecEnlace, FunCod)
            VALUES (@NumeroCuenta, @Importe, GETDATE(), @NumeroComprobante, 'C', GETDATE(), NULL, -1);

            -- Actualizar movimiento original
            UPDATE Movimientos
            SET CodigoContrasiento = 'M'
            WHERE Id = @IdMovimiento;

            -- Procesar enlazados (comisión e IVA)
            DECLARE cur_enlazados CURSOR FOR
                SELECT Id, Importe, CodigoContrasiento, FunCod
                FROM Movimientos
                WHERE NumeroCuenta = @NumeroCuenta
                  AND NumeroComprobante = @NumeroComprobante
                  AND FecEnlace = @FechaRealOriginal
                  AND CodigoContrasiento <> 'M';

            DECLARE @IdEnlazado INT, @ImporteEnlazado DECIMAL(18,2), @CodEnlazado CHAR(1), @FunCodEnlazado INT;

            OPEN cur_enlazados;
            FETCH NEXT FROM cur_enlazados INTO @IdEnlazado, @ImporteEnlazado, @CodEnlazado, @FunCodEnlazado;
            WHILE @@FETCH_STATUS = 0
            BEGIN
                -- Revertir el saldo de la comisión/IVA
                SELECT @FunCodEnlazado = @FunCodEnlazado * (-1)

               UPDATE Cuentas WITH (ROWLOCK)
                SET Saldo = Saldo + @ImporteEnlazado * @FunCodEnlazado
                WHERE NumeroCuenta = @NumeroCuenta;

                -- Insertar movimiento de contrasiento para el enlazado
                INSERT INTO Movimientos (NumeroCuenta, Importe, FechaMovimiento, NumeroComprobante, CodigoContrasiento, FechaReal, FecEnlace, FunCod)
                VALUES (@NumeroCuenta, @ImporteEnlazado, GETDATE(), @NumeroComprobante, 'C', GETDATE(), NULL, @FunCodEnlazado);

                -- Marcar el movimiento enlazado como contrasentado
                UPDATE Movimientos
                SET CodigoContrasiento = 'M'
                WHERE Id = @IdEnlazado;

                FETCH NEXT FROM cur_enlazados INTO @IdEnlazado, @ImporteEnlazado, @CodEnlazado, @FunCodEnlazado;
            END
            CLOSE cur_enlazados;
            DEALLOCATE cur_enlazados;
        END

        -- Obtener saldo final
        SELECT @SaldoFinal = Saldo FROM Cuentas WITH (ROWLOCK) WHERE NumeroCuenta = @NumeroCuenta;

        COMMIT TRANSACTION;
        SELECT @NumeroCuenta AS NumeroCuenta, @SaldoFinal AS SaldoFinal;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END

-- Store Procedure para crédito y contrasiento (con transacción)
CREATE PROCEDURE sp_CreditarCuenta
    @NumeroCuenta NUMERIC(17,0),
    @Importe DECIMAL(18,2),
    @FechaMovimiento DATETIME,
    @NumeroComprobante VARCHAR(50),
    @Contrasiento CHAR(1) = NULL -- 'C' para contrasiento, NULL para crédito normal
AS

BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @SaldoFinal DECIMAL(18,2);

        IF @Contrasiento IS NULL
        BEGIN
            DECLARE @Comision DECIMAL(18,2);
            DECLARE @IVA DECIMAL(18,2);
            DECLARE @FechaRealMov DATETIME;

			-- Calculo de comision e IVA
            SET @Comision = ROUND(@Importe * 0.08, 2);
            SET @IVA = ROUND(@Comision * 0.21, 2);

            -- Crédito normal (importe - comisión - IVA)
            UPDATE Cuentas WITH (ROWLOCK)
            SET Saldo = Saldo + @Importe - @Comision - @IVA
            WHERE NumeroCuenta = @NumeroCuenta;

            SET @FechaRealMov = GETDATE();
            -- Movimiento principal
            INSERT INTO Movimientos (NumeroCuenta, Importe, FechaMovimiento, NumeroComprobante, CodigoContrasiento, FechaReal, FecEnlace, FunCod)
            VALUES (@NumeroCuenta, @Importe, @FechaMovimiento, @NumeroComprobante, NULL, @FechaRealMov, NULL, 1);
            -- Movimiento comisión
            INSERT INTO Movimientos (NumeroCuenta, Importe, FechaMovimiento, NumeroComprobante, CodigoContrasiento, FechaReal, FecEnlace, FunCod)
            VALUES (@NumeroCuenta, @Comision, @FechaMovimiento, @NumeroComprobante, NULL, @FechaRealMov, @FechaRealMov, -1);
            -- Movimiento IVA
            INSERT INTO Movimientos (NumeroCuenta, Importe, FechaMovimiento, NumeroComprobante, CodigoContrasiento, FechaReal, FecEnlace, FunCod)
            VALUES (@NumeroCuenta, @IVA, @FechaMovimiento, @NumeroComprobante, NULL, @FechaRealMov, @FechaRealMov, -1);
        END
        ELSE IF @Contrasiento = 'C'
        BEGIN
            DECLARE @IdMovimiento INT;
            DECLARE @FechaRealOriginal DATETIME;
            -- Buscar movimiento original
            SELECT @IdMovimiento = Id, @FechaRealOriginal = FechaReal
            FROM Movimientos
            WHERE NumeroCuenta = @NumeroCuenta
              AND Importe = @Importe
              AND FechaMovimiento = @FechaMovimiento
              AND NumeroComprobante = @NumeroComprobante
              AND CodigoContrasiento IS NULL;

            IF @IdMovimiento IS NULL
            BEGIN
                RAISERROR('50003: No existe movimiento original para contrasiento.', 16, 1);
                ROLLBACK TRANSACTION;
                RETURN;
            END
            -- Revertir el crédito original
            UPDATE Cuentas WITH (ROWLOCK)
            SET Saldo = Saldo - @Importe
            WHERE NumeroCuenta = @NumeroCuenta;

            INSERT INTO Movimientos (NumeroCuenta, Importe, FechaMovimiento, NumeroComprobante, CodigoContrasiento, FechaReal, FecEnlace, FunCod)
            VALUES (@NumeroCuenta, @Importe, GETDATE(), @NumeroComprobante, 'C', GETDATE(), NULL, 1);

            -- Actualizar movimiento original
            UPDATE Movimientos
            SET CodigoContrasiento = 'M'
            WHERE Id = @IdMovimiento;

            -- Procesar enlazados (comisión e IVA)
            DECLARE cur_enlazados CURSOR FOR
                SELECT Id, Importe, CodigoContrasiento, FunCod
                FROM Movimientos
                WHERE NumeroCuenta = @NumeroCuenta
                  AND NumeroComprobante = @NumeroComprobante
                  AND FecEnlace = @FechaRealOriginal
                  AND CodigoContrasiento <> 'M';

            DECLARE @IdEnlazado INT, @ImporteEnlazado DECIMAL(18,2), @CodEnlazado CHAR(1), @FunCodEnlazado INT;
            OPEN cur_enlazados;
            FETCH NEXT FROM cur_enlazados INTO @IdEnlazado, @ImporteEnlazado, @CodEnlazado, @FunCodEnlazado;
            WHILE @@FETCH_STATUS = 0
            BEGIN

                SELECT @FunCodEnlazado = @FunCodEnlazado * (-1)

                -- Revertir el saldo de la comisión/IVA
                UPDATE Cuentas WITH (ROWLOCK)
                SET Saldo = Saldo - @ImporteEnlazado * @FunCodEnlazado
                WHERE NumeroCuenta = @NumeroCuenta;

                -- Insertar movimiento de contrasiento para el enlazado
                INSERT INTO Movimientos (NumeroCuenta, Importe, FechaMovimiento, NumeroComprobante, CodigoContrasiento, FechaReal, FecEnlace, FunCod)
                VALUES (@NumeroCuenta, @ImporteEnlazado, GETDATE(), @NumeroComprobante, 'C', GETDATE(), NULL, @FunCodEnlazado);

                -- Marcar el movimiento enlazado como contrasentado
                UPDATE Movimientos
                SET CodigoContrasiento = 'M'
                WHERE Id = @IdEnlazado;

                FETCH NEXT FROM cur_enlazados INTO @IdEnlazado, @ImporteEnlazado, @CodEnlazado, @FunCodEnlazado;
            END
            CLOSE cur_enlazados;
            DEALLOCATE cur_enlazados;
        END

        -- Obtener saldo final
        SELECT @SaldoFinal = Saldo FROM Cuentas WITH (ROWLOCK) WHERE NumeroCuenta = @NumeroCuenta;

        COMMIT TRANSACTION;
        SELECT @NumeroCuenta AS NumeroCuenta, @SaldoFinal AS SaldoFinal;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END