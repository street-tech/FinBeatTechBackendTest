-- �������� ����� (���� ����������)
-- IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'client')
-- BEGIN
--     EXEC('CREATE SCHEMA client');
-- END
-- GO

-- �������� ������� ��� ������� (���� �� ���)
-- IF OBJECT_ID('client.ClientPayments', 'U') IS NULL
-- BEGIN
--     CREATE TABLE client.ClientPayments (
--         Id BIGINT IDENTITY(1,1) PRIMARY KEY,
--         ClientId BIGINT NOT NULL,
--         Dt DATETIME2(0) NOT NULL,
--         Amount MONEY NOT NULL
--     );
--
--     -- ������ ������
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
    -- ����������� CTE ��� ��������� ���� ��� � ���������
    ;WITH DateSeries AS (
        SELECT @Sd AS Dt
        UNION ALL
        SELECT DATEADD(day, 1, Dt)
        FROM DateSeries
        WHERE Dt < @Ed
    ),
    -- CTE ��� ��������� ���� �������� �� ���� ��� ������� �������
    DailyPayments AS (
        SELECT
            CAST(Dt AS DATE) AS PaymentDate,
            SUM(Amount) AS DailyAmount
        FROM client.ClientPayments
        WHERE ClientId = @ClientId
          AND CAST(Dt AS DATE) BETWEEN @Sd AND @Ed
        GROUP BY CAST(Dt AS DATE)
    )
    -- ��������� ��������� � ������������ �������
    INSERT INTO @ResultTable (Dt, TotalAmount)
    SELECT
        ds.Dt,
        ISNULL(dp.DailyAmount, 0) AS TotalAmount
    FROM DateSeries ds
    LEFT JOIN DailyPayments dp ON ds.Dt = dp.PaymentDate
    OPTION (MAXRECURSION 0); -- ������� ����������� �� ������� �������� (����� ��� ������� ���������� ���)

    RETURN;
END;
GO


-- ���� 1
-- SELECT Dt, TotalAmount AS �����
-- FROM dbo.GetDailyClientPayments(1, '2022-01-02', '2022-01-07');

-- ��������� ��������� 1:
-- Dt         �����
-- 2022-01-02 0.00
-- 2022-01-03 100.00
-- 2022-01-04 0.00
-- 2022-01-05 450.00
-- 2022-01-06 0.00
-- 2022-01-07 50.00

-- ���� 2
-- SELECT Dt, TotalAmount AS �����
-- FROM dbo.GetDailyClientPayments(2, '2022-01-04', '2022-01-11');

-- ��������� ��������� 2:
-- Dt         �����
-- 2022-01-04 0.00
-- 2022-01-05 278.00
-- 2022-01-06 0.00
-- 2022-01-07 0.00
-- 2022-01-08 0.00
-- 2022-01-09 0.00
-- 2022-01-10 300.00
-- 2022-01-11 0.00