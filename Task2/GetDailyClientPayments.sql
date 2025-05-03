-- Создание схемы (если необходимо)
-- IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'client')
-- BEGIN
--     EXEC('CREATE SCHEMA client');
-- END
-- GO

-- Создание таблицы для примера (если ее нет)
-- IF OBJECT_ID('client.ClientPayments', 'U') IS NULL
-- BEGIN
--     CREATE TABLE client.ClientPayments (
--         Id BIGINT IDENTITY(1,1) PRIMARY KEY,
--         ClientId BIGINT NOT NULL,
--         Dt DATETIME2(0) NOT NULL,
--         Amount MONEY NOT NULL
--     );
--
--     -- Пример данных
--     INSERT INTO client.ClientPayments (ClientId, Dt, Amount) VALUES
--     (1, '2022-01-03 17:24:00', 100),
--     (1, '2022-01-05 17:24:14', 200),
--     (1, '2022-01-05 18:23:34', 250),
--     (1, '2022-01-07 10:12:38', 50),
--     (2, '2022-01-05 17:24:14', 278),
--     (2, '2022-01-10 12:39:29', 300);
-- END
-- GO

CREATE FUNCTION dbo.GetDailyClientPayments (
    @ClientId BIGINT,
    @Sd DATE,
    @Ed DATE
)
RETURNS @ResultTable TABLE (
    Dt DATE,
    TotalAmount MONEY
)
AS
BEGIN
    -- Рекурсивный CTE для генерации всех дат в диапазоне
    ;WITH DateSeries AS (
        SELECT @Sd AS Dt
        UNION ALL
        SELECT DATEADD(day, 1, Dt)
        FROM DateSeries
        WHERE Dt < @Ed
    ),
    -- CTE для агрегации сумм платежей по дням для нужного клиента
    DailyPayments AS (
        SELECT
            CAST(Dt AS DATE) AS PaymentDate,
            SUM(Amount) AS DailyAmount
        FROM client.ClientPayments
        WHERE ClientId = @ClientId
          AND CAST(Dt AS DATE) BETWEEN @Sd AND @Ed
        GROUP BY CAST(Dt AS DATE)
    )
    -- Вставляем результат в возвращаемую таблицу
    INSERT INTO @ResultTable (Dt, TotalAmount)
    SELECT
        ds.Dt,
        ISNULL(dp.DailyAmount, 0) AS TotalAmount
    FROM DateSeries ds
    LEFT JOIN DailyPayments dp ON ds.Dt = dp.PaymentDate
    OPTION (MAXRECURSION 0); -- Снимаем ограничение на глубину рекурсии (важно для больших диапазонов дат)

    RETURN;
END;
GO


-- Тест 1
-- SELECT Dt, TotalAmount AS Сумма
-- FROM dbo.GetDailyClientPayments(1, '2022-01-02', '2022-01-07');

-- Ожидаемый результат 1:
-- Dt         Сумма
-- 2022-01-02 0.00
-- 2022-01-03 100.00
-- 2022-01-04 0.00
-- 2022-01-05 450.00
-- 2022-01-06 0.00
-- 2022-01-07 50.00

-- Тест 2
-- SELECT Dt, TotalAmount AS Сумма
-- FROM dbo.GetDailyClientPayments(2, '2022-01-04', '2022-01-11');

-- Ожидаемый результат 2:
-- Dt         Сумма
-- 2022-01-04 0.00
-- 2022-01-05 278.00
-- 2022-01-06 0.00
-- 2022-01-07 0.00
-- 2022-01-08 0.00
-- 2022-01-09 0.00
-- 2022-01-10 300.00
-- 2022-01-11 0.00