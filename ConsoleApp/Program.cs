using Challenge;
using Challenge.DataContracts;
using ConsoleApp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TaskStatus = Challenge.DataContracts.TaskStatus;

const string challengeId = "git-course";
const string secretFilePath = "TeamSecret.txt";
const int serverRetryDelayMs = 1500;
const int pendingRefreshInterval = 5;

var teamSecret = "";

if (File.Exists(secretFilePath))
{
    teamSecret = File.ReadAllText(secretFilePath).Trim();
}

if (string.IsNullOrEmpty(teamSecret))
{
    Console.WriteLine(!File.Exists(secretFilePath)
        ? $"Файл '{secretFilePath}' не найден."
        : $"Файл '{secretFilePath}' обнаружен, но пуст.");

    Console.Write("Введи секретный ключ твоей команды: ");
    teamSecret = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(teamSecret))
    {
        Console.WriteLine(
            $"Ключ команды не может быть пустым. Попробуй еще разз, либо впиши его сам в файл {secretFilePath}");
        Console.ReadLine();
        return;
    }

    try
    {
        File.WriteAllText(secretFilePath, teamSecret);
        Console.WriteLine($"Ключ успешно сохранен в файл '{secretFilePath}'");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Не удалось сохранить ключ в файл ({ex.Message}).");
        Console.WriteLine("Но сессия продолжит работу с введенным ключом.");
    }
}

var challengeClient = new ChallengeClient(teamSecret);

ChallengeResponse challenge;

try
{
    challenge = await WithServerRetryAsync(
        () => challengeClient.GetChallengeAsync(challengeId),
        "получение информации о челлендже",
        serverRetryDelayMs
    );
}
catch
{
    Console.WriteLine("Не удалось получить информацию о челлендже после повторной попытки.");
    Console.ReadLine();
    return;
}

var utcNow = DateTime.UtcNow;
var currentRound =
    challenge.Rounds.FirstOrDefault(round => round.StartTimestamp < utcNow && utcNow < round.EndTimestamp);

if (currentRound == null)
{
    Console.WriteLine("Раунд еще не начался или уже закончился.");
    Console.ReadLine();
    return;
}

var availableTaskTypes = currentRound.TaskTypes
    .Select(task => task.Id)
    .Where(id => !string.IsNullOrWhiteSpace(id))
    .ToList();

if (availableTaskTypes.Count == 0)
{
    Console.WriteLine("В текущем раунде нет доступных типов задач.");
    Console.ReadLine();
    return;
}

Console.WriteLine();
Console.WriteLine("Доступные типы задач в текущем раунде:");

foreach (var taskTypeId in availableTaskTypes)
    Console.WriteLine($"--- {taskTypeId}");

Console.WriteLine();
Console.WriteLine("Будут использоваться pending-задачи всех типов, а недостающие задачи API выберет случайно.");
Console.WriteLine($"Проверка pending-задач будет выполняться раз в {pendingRefreshInterval} задач.");

var random = new Random();
var run = true;

while (run)
{
    Console.WriteLine();
    Console.Write("Введи количество задач: ");

    int taskAmount;

    while (!int.TryParse(Console.ReadLine(), out taskAmount) || taskAmount <= 0)
    {
        Console.Write("Некорректный ввод. Введи положительное целое число: ");
    }

    Console.WriteLine();

    Console.WriteLine(
        $"Нажми y, чтобы получить или дозапросить {taskAmount} задач случайных типов в раунде {currentRound.Id}");

    Console.WriteLine("Или Enter, чтобы перейти к следующему действию");

    var input = Console.ReadLine()?.Trim();

    if (string.Equals(input, "y", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine();
        Console.WriteLine("Выбери режим решения задач:");
        Console.WriteLine("6 - Автоматический, отправлять ответы сразу");
        Console.WriteLine("7 - Ручной, проверять ответы перед отправкой");
        Console.Write("Твой выбор (6 или 7): ");

        var isManualMode = Console.ReadLine()?.Trim() == "7";

        await ProcessTasksPipelineAsync(
            challengeClient,
            currentRound.Id,
            availableTaskTypes,
            taskAmount,
            isManualMode,
            random,
            serverRetryDelayMs,
            pendingRefreshInterval
        );
    }

    Console.WriteLine();
    Console.WriteLine("------------------------------------------------");

    Console.Write("Хочешь запросить еще задач случайных типов? (y/n): ");

    var exitChoice = Console.ReadLine()?.Trim();
    if (!string.Equals(exitChoice, "y", StringComparison.OrdinalIgnoreCase))
        run = false;
}

Console.WriteLine();
Console.WriteLine("Программа завершена. Нажми ВВОД для закрытия окна...");
Console.ReadLine();

return;


static async Task ProcessTasksPipelineAsync(
    ChallengeClient challengeClient,
    string roundId,
    List<string> availableTaskTypes,
    int requiredAmount,
    bool isManualMode,
    Random random,
    int retryDelayMs,
    int pendingRefreshInterval)
{
    Console.WriteLine();

    var alreadyHandledTaskIds = new HashSet<string>();
    var queuedPendingTaskIds = new HashSet<string>();
    var pendingQueue = new Queue<TaskResponse>();

    for (var i = 0; i < requiredAmount; i++)
    {
        var currentTaskNumber = i + 1;

        Console.WriteLine();
        Console.WriteLine($"========== Задача {currentTaskNumber}/{requiredAmount} ==========");

        if (i % pendingRefreshInterval == 0)
        {
            Console.WriteLine("Проверка pending-задач всех типов...");

            var freshPendingTasks = await GetPendingRandomTasksAsync(
                challengeClient,
                roundId,
                availableTaskTypes,
                alreadyHandledTaskIds,
                random,
                retryDelayMs,
                pendingRefreshInterval
            );

            foreach (var pendingTask in freshPendingTasks)
            {
                if (alreadyHandledTaskIds.Contains(pendingTask.Id))
                    continue;

                if (!queuedPendingTaskIds.Add(pendingTask.Id))
                    continue;

                pendingQueue.Enqueue(pendingTask);
            }

            Console.WriteLine($"В очереди pending-задач: {pendingQueue.Count}");
        }

        var task = TryDequeuePendingTask(
            pendingQueue,
            queuedPendingTaskIds,
            alreadyHandledTaskIds
        );

        if (task == null)
        {
            Console.WriteLine("Подходящих pending-задач в очереди нет. Запрашиваем одну новую случайную задачу...");

            task = await TryAskNewRandomTaskAsync(
                challengeClient,
                roundId,
                retryDelayMs
            );
        }

        if (task == null)
        {
            Console.WriteLine($"Не удалось получить задачу {currentTaskNumber}/{requiredAmount}. Переходим дальше.");
            continue;
        }

        alreadyHandledTaskIds.Add(task.Id);

        await SolveAndSubmitTaskAsync(
            challengeClient,
            task,
            currentTaskNumber,
            requiredAmount,
            isManualMode,
            retryDelayMs
        );
    }
}


static async Task<List<TaskResponse>> GetPendingRandomTasksAsync(
    ChallengeClient challengeClient,
    string roundId,
    List<string> availableTaskTypes,
    HashSet<string> alreadyHandledTaskIds,
    Random random,
    int retryDelayMs,
    int countPerType)
{
    var result = new List<TaskResponse>();

    var shuffledTaskTypes = availableTaskTypes
        .OrderBy(_ => random.Next())
        .ToList();

    foreach (var taskType in shuffledTaskTypes)
    {
        try
        {
            var tasks = await WithServerRetryAsync(
                () => challengeClient.GetTasksAsync(
                    roundId,
                    taskType,
                    TaskStatus.Pending,
                    0,
                    countPerType
                ),
                $"получение pending-задач типа '{taskType}'",
                retryDelayMs
            );

            var newTasks = tasks
                .Where(task => !alreadyHandledTaskIds.Contains(task.Id))
                .ToList();

            Console.WriteLine($"Тип '{taskType}': найдено pending-задач: {newTasks.Count}");

            result.AddRange(newTasks);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Не удалось получить pending-задачи типа '{taskType}' после retry: {ex.Message}");
        }
    }

    return result
        .GroupBy(task => task.Id)
        .Select(group => group.First())
        .OrderBy(_ => random.Next())
        .ToList();
}


static TaskResponse TryDequeuePendingTask(
    Queue<TaskResponse> pendingQueue,
    HashSet<string> queuedPendingTaskIds,
    HashSet<string> alreadyHandledTaskIds)
{
    while (pendingQueue.Count > 0)
    {
        var task = pendingQueue.Dequeue();
        queuedPendingTaskIds.Remove(task.Id);

        if (alreadyHandledTaskIds.Contains(task.Id))
            continue;

        Console.WriteLine($"Взята pending-задача из очереди: {task.Id}");
        return task;
    }

    return null;
}


static async Task<TaskResponse> TryAskNewRandomTaskAsync(
    ChallengeClient challengeClient,
    string roundId,
    int retryDelayMs)
{
    try
    {
        var task = await WithServerRetryAsync(
            () => challengeClient.AskNewTaskAsync(roundId),
            "запрос новой случайной задачи",
            retryDelayMs
        );

        Console.WriteLine($"Получена новая случайная задача: {task.Id}");
        return task;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Не удалось запросить новую случайную задачу после retry: {ex.Message}");
        return null;
    }
}


static async Task SolveAndSubmitTaskAsync(
    ChallengeClient challengeClient,
    TaskResponse task,
    int currentTaskNumber,
    int totalTaskAmount,
    bool isManualMode,
    int retryDelayMs)
{
    Console.WriteLine();
    Console.WriteLine($"Решение задачи {currentTaskNumber}/{totalTaskAmount}");
    Console.WriteLine($"Id: {task.Id}");
    Console.WriteLine($"Формулировка: {task.UserHint ?? "Формулировка отсутствует"}");
    Console.WriteLine($"Вопрос: {task.Question}");

    string answer;

    try
    {
        answer = Solver.Solve(task);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при решении задачи: {ex.Message}");
        return;
    }

    if (isManualMode)
    {
        Console.WriteLine($"Ответ бота: {answer}");
        Console.Write("Отправить этот ответ? y - отправить, n - ввести свой: ");

        var confirm = Console.ReadLine()?.Trim();

        if (!string.Equals(confirm, "y", StringComparison.OrdinalIgnoreCase))
        {
            Console.Write("Введи свой вариант ответа: ");
            answer = Console.ReadLine()?.Trim();
        }
    }

    if (string.IsNullOrWhiteSpace(answer))
    {
        Console.WriteLine("Ответ пустой. Задача пропущена.");
        return;
    }

    Console.WriteLine("Ожидание отправки решения...");

    try
    {
        var updatedTask = await WithServerRetryAsync(
            () => challengeClient.CheckTaskAnswerAsync(task.Id, answer),
            $"отправка ответа на задачу {task.Id}",
            retryDelayMs
        );

        Console.WriteLine($"Статус ответа: {updatedTask.Status}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при отправке ответа после retry: {ex.Message}");
    }
}


static async Task<T> WithServerRetryAsync<T>(
    Func<Task<T>> action,
    string operationName,
    int retryDelayMs)
{
    try
    {
        return await action();
    }
    catch (Exception firstException)
    {
        Console.WriteLine();
        Console.WriteLine($"Ошибка сервера или API при операции: {operationName}");
        Console.WriteLine($"Причина: {firstException.Message}");
        Console.WriteLine($"Повторная попытка через {retryDelayMs} мс...");

        await Task.Delay(retryDelayMs);

        try
        {
            return await action();
        }
        catch (Exception secondException)
        {
            Console.WriteLine($"Повторная попытка не удалась при операции: {operationName}");
            Console.WriteLine($"Причина: {secondException.Message}");

            throw;
        }
    }
}