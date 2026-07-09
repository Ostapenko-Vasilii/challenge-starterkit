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

const bool useRandomTaskTypes = true;
const string selectedTaskType = null;

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
            useRandomTaskTypes,
            selectedTaskType,
            availableTaskTypes,
            taskAmount,
            isManualMode,
            random,
            serverRetryDelayMs
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
    bool useRandomTaskTypes,
    string selectedTaskType,
    List<string> availableTaskTypes,
    int requiredAmount,
    bool isManualMode,
    Random random,
    int retryDelayMs)
{
    Console.WriteLine();

    var alreadyHandledTaskIds = new HashSet<string>();

    for (var i = 0; i < requiredAmount; i++)
    {
        var currentTaskNumber = i + 1;

        Console.WriteLine();
        Console.WriteLine($"========== Задача {currentTaskNumber}/{requiredAmount} ==========");

        TaskResponse task;

        if (useRandomTaskTypes)
        {
            task = await TryGetPendingRandomTaskAsync(
                challengeClient,
                roundId,
                availableTaskTypes,
                alreadyHandledTaskIds,
                random,
                retryDelayMs
            );
        }
        else
        {
            task = await TryGetPendingTaskByTypeAsync(
                challengeClient,
                roundId,
                selectedTaskType,
                alreadyHandledTaskIds,
                retryDelayMs
            );
        }

        if (task == null)
        {
            Console.WriteLine(useRandomTaskTypes
                ? "Подходящих pending-задач не найдено. Запрашиваем одну новую случайную задачу..."
                : $"Подходящих pending-задач типа '{selectedTaskType}' не найдено. Запрашиваем одну новую задачу...");

            task = await TryAskNewTaskAsync(
                challengeClient,
                roundId,
                useRandomTaskTypes ? null : selectedTaskType,
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


static async Task<TaskResponse> TryGetPendingRandomTaskAsync(
    ChallengeClient challengeClient,
    string roundId,
    List<string> availableTaskTypes,
    HashSet<string> alreadyHandledTaskIds,
    Random random,
    int retryDelayMs)
{
    var shuffledTaskTypes = availableTaskTypes
        .OrderBy(_ => random.Next())
        .ToList();

    foreach (var taskType in shuffledTaskTypes)
    {
        var task = await TryGetPendingTaskByTypeAsync(
            challengeClient,
            roundId,
            taskType,
            alreadyHandledTaskIds,
            retryDelayMs
        );
        if (task == null) continue;

        Console.WriteLine($"Взята pending-задача случайного режима из типа '{taskType}': {task.Id}");
        return task;
    }

    return null;
}


static async Task<TaskResponse> TryGetPendingTaskByTypeAsync(
    ChallengeClient challengeClient,
    string roundId,
    string taskType,
    HashSet<string> alreadyHandledTaskIds,
    int retryDelayMs)
{
    const int maxPendingLookupOffset = 20;

    for (var offset = 0; offset < maxPendingLookupOffset; offset++)
    {
        List<TaskResponse> tasks;

        try
        {
            tasks = await WithServerRetryAsync(
                () => challengeClient.GetTasksAsync(
                    roundId,
                    taskType,
                    TaskStatus.Pending,
                    offset,
                    1
                ),
                $"получение одной pending-задачи типа '{taskType}', offset={offset}",
                retryDelayMs
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Не удалось получить pending-задачу типа '{taskType}' после retry: {ex.Message}");
            return null;
        }

        if (tasks.Count == 0)
            return null;

        var task = tasks[0];

        if (alreadyHandledTaskIds.Contains(task.Id))
            continue;

        Console.WriteLine($"Найдена pending-задача типа '{taskType}': {task.Id}");
        return task;
    }

    return null;
}


static async Task<TaskResponse> TryAskNewTaskAsync(
    ChallengeClient challengeClient,
    string roundId,
    string taskType,
    int retryDelayMs)
{
    try
    {
        if (string.IsNullOrWhiteSpace(taskType))
        {
            var task = await WithServerRetryAsync(
                () => challengeClient.AskNewTaskAsync(roundId),
                "запрос новой случайной задачи",
                retryDelayMs
            );

            Console.WriteLine($"Получена новая случайная задача: {task.Id}");
            return task;
        }

        var typedTask = await WithServerRetryAsync(
            () => challengeClient.AskNewTaskAsync(roundId, taskType),
            $"запрос новой задачи типа '{taskType}'",
            retryDelayMs
        );

        Console.WriteLine($"Получена новая задача типа '{taskType}': {typedTask.Id}");
        return typedTask;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Не удалось запросить новую задачу после retry: {ex.Message}");
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