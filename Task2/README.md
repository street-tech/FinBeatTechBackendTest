# Задание 2: SQL Функция

Этот каталог содержит решение для Задания 2.

## GetDailyClientPayments.sql

SQL скрипт для создания табличной функции `dbo.GetDailyClientPayments` в MS SQL Server.

**Назначение функции:**

Функция принимает на вход ID клиента (`@ClientId`), начальную дату (`@Sd`) и конечную дату (`@Ed`) интервала.
Она возвращает таблицу с двумя колонками:
*   `Dt` (DATE): Каждая дата в указанном интервале.
*   `TotalAmount` (MONEY): Суммарная сумма платежей клиента за эту дату. Если платежей не было, возвращается 0.

**Примечание:**

Для оптимальной производительности на больших объемах данных таблица client.ClientPayments должна иметь индекс по полям ClientId и Dt. Например: CREATE INDEX IX_ClientPayments_ClientId_Dt ON client.ClientPayments (ClientId, Dt);

**Пример использования:**

```sql
SELECT Dt, TotalAmount
FROM dbo.GetDailyClientPayments(@ClientId_value, @StartDate_value, @EndDate_value);