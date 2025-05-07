markdown
# Задание 1: Task Management Service

Этот проект реализует простой сервис управления задачами пользователя (Task Management Service) в рамках тестового задания FinBeat Tech.

Сервис включает:
*   REST API для CRUD операций над задачами (`TaskManager.Api`).
*   Асинхронное взаимодействие через RabbitMQ для уведомления об изменениях задач.
*   Сервис-слушатель (`TaskEventListener`) для логирования событий из RabbitMQ.
*   Логирование с использованием Serilog.
*   Трассировку с использованием OpenTelemetry.
*   Юнит-тесты для сервисного слоя.
*   Поддержку запуска через Docker Compose.

## Структура Проекта

*   `TaskManager.Api`: ASP.NET Core Web API.
*   `TaskManager.Application`: Сервисы, DTO, интерфейсы репозиториев и брокера.
*   `TaskManager.Domain`: Доменные сущности и перечисления.
*   `TaskManager.Infrastructure`: Реализация доступа к данным (EF Core, MS SQL), взаимодействие с RabbitMQ.
*   `TaskEventListener`: Worker Service для прослушивания очереди RabbitMQ.
*   `TaskManager.Tests`: Юнит-тесты.
*   `Dockerfile.api`, `Dockerfile.listener`: Файлы для сборки Docker-образов.
*   `docker-compose.yml`: Файл для оркестрации контейнеров.

## Технологический Стек

*   .NET 8 / ASP.NET Core
*   Entity Framework Core 8
*   MS SQL Server
*   RabbitMQ
*   Serilog (Логирование)
*   OpenTelemetry (Трассировка)
*   NUnit, NSubstitute (Тестирование)
*   Docker / Docker Compose

## Требования к окружению

*   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
*   [Docker](https://www.docker.com/products/docker-desktop/) (для запуска через Docker Compose)
*   SQL Server (локальный или в Docker)
*   RabbitMQ (локальный или в Docker)
*   Опционально: IDE (Visual Studio, VS Code, Rider)

## Запуск Проекта

### 1. Локальный запуск (через IDE или CLI)

*   **Настройка Базы Данных:**
    *   Убедитесь, что у вас запущен экземпляр MS SQL Server.
    *   Обновите строку подключения `ConnectionStrings:DefaultConnection` в файле `Task1/TaskManager.Api/appsettings.Development.json` для соответствия вашему SQL Server.
*   **Настройка RabbitMQ:**
    *   Убедитесь, что у вас запущен экземпляр RabbitMQ.
    *   Обновите настройки `RabbitMq` в файлах `Task1/TaskManager.Api/appsettings.Development.json` и `Task1/TaskEventListener/appsettings.Development.json`, если ваш RabbitMQ запущен не на `localhost:5672` или требует аутентификации.
*   **Применение миграций БД:**
    *   Откройте терминал в папке `Task1`.
    *   Выполните команду: `dotnet ef database update --startup-project ./TaskManager.Api/TaskManager.Api.csproj`
*   **Запуск сервисов:**
    *   Запустите проект `TaskManager.Api` (API).
    *   Запустите проект `TaskEventListener` (Listener).
    *   API будет доступен по адресу (по умолчанию): `https://localhost:7XXX` или `http://localhost:5XXX` (см. `launchSettings.json`). Swagger UI доступен по корневому адресу.
    *   Логи обоих сервисов будут выводиться в консоль и в файлы в папках `Task1/TaskManager.Api/Logs` и `Task1/TaskEventListener/Logs`.

### 2. Запуск через Docker Compose

*   **Предварительные требования:** Docker Desktop должен быть запущен.
*   **Пароль SQL Server:** **ВАЖНО!** Откройте файл `Task1/docker-compose.yml` и замените `yourStrong(!)Password` на ваш собственный надежный пароль для пользователя `sa` SQL Server. Этот же пароль используется в строке подключения `ConnectionStrings__DefaultConnection` для сервиса `task-api`.
*   **Сборка и запуск:**
    *   Откройте терминал в папке `Task1`.
    *   Выполните команду: `docker-compose up --build`
    *   Эта команда соберет образы для API и Listener, запустит контейнеры SQL Server, RabbitMQ, API и Listener.
    *   API будет доступен по адресу: `http://localhost:8080` (Swagger UI по этому же адресу).
    *   RabbitMQ Management UI: `http://localhost:15672` (логин/пароль: guest/guest).
    *   Логи контейнеров можно просмотреть с помощью `docker-compose logs -f` или через Docker Desktop.
*   **Остановка:**
    *   Нажмите `Ctrl+C` в терминале, где запущен `docker-compose up`.
    *   Для удаления контейнеров и сети выполните: `docker-compose down`
    *   Для удаления volumes (данных БД и RabbitMQ): `docker-compose down -v`

## Запуск Тестов

*   Откройте терминал в папке `Task1`.
*   Выполните команду: `dotnet test Task1.sln`

## Примеры Запросов к API (cURL)

*Предполагается, что API запущен локально на порту 5000 (HTTP) или через Docker на 8080.*

*   **Создать Задачу:**
    ```bash
    curl -X POST "http://localhost:8080/api/Tasks" -H "Content-Type: application/json" -d '{"title": "Новая задача из cURL", "description": "Описание задачи", "status": 1}'
    ```
*   **Получить все задачи:**
    ```bash
    curl -X GET "http://localhost:8080/api/Tasks"
    ```
*   **Получить задачу по ID (замените {id} на реальный ID):**
    ```bash
    curl -X GET "http://localhost:8080/api/Tasks/{id}"
    ```
*   **Обновить задачу (замените {id} на реальный ID):**
    ```bash
    curl -X PUT "http://localhost:8080/api/Tasks/{id}" -H "Content-Type: application/json" -d '{"title": "Обновленная задача", "description": "Новое описание", "status": 2}'
    ```
*   **Удалить задачу (замените {id} на реальный ID):**
    ```bash
    curl -X DELETE "http://localhost:8080/api/Tasks/{id}"
    ```

## Observability

*   **Логи:** Собираются с помощью Serilog и выводятся в консоль и файлы (`Logs/api-log-*.txt`, `Logs/listener-log-*.txt`).
*   **Трассировка:** Собираются с помощью OpenTelemetry и выводятся в консоль. Включают информацию о запросах ASP.NET Core, вызовах EF Core и операциях RabbitMQ. Для просмотра в UI (Jaeger, Tempo) нужно настроить соответствующий OTLP Exporter.