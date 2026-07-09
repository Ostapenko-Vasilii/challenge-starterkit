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

var challenge = await challengeClient.GetChallengeAsync(challengeId);

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
Console.WriteLine("Выбери режим запроса задач:");
Console.WriteLine("1 - Запрашивать задачи определенного типа");
Console.WriteLine("2 - Запрашивать задачи случайных типов");
Console.Write("Твой выбор (1 или 2): ");

var typeModeInput = Console.ReadLine()?.Trim();
var useRandomTaskTypes = typeModeInput == "2";

string selectedTaskType = null;
const string defaultTaskType = "starter";

if (!useRandomTaskTypes)
{
    Console.Write("Введи тип задачи либо нажми Enter для дефолтного типа: ");
    selectedTaskType = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(selectedTaskType))
    {
        selectedTaskType = defaultTaskType;
        Console.WriteLine($"Тип не указан. По умолчанию: {selectedTaskType}");
    }

    if (!availableTaskTypes.Contains(selectedTaskType))
    {
        Console.WriteLine($"Тип задачи '{selectedTaskType}' недоступен в текущем раунде.");
        Console.WriteLine("Доступные типы:");

        foreach (var taskTypeId in availableTaskTypes)
        {
            Console.WriteLine($"--- {taskTypeId}");
        }

        Console.ReadLine();
        return;
    }
}
else
{
    Console.WriteLine("Будут использоваться pending-задачи всех типов, а недостающие задачи API выберет случайно.");
}

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

    if (useRandomTaskTypes)
    {
        Console.WriteLine(
            $"Нажми y, чтобы получить или дозапросить {taskAmount} задач случайных типов в раунде {currentRound.Id}");
    }
    else
    {
        Console.WriteLine(
            $"Нажми y, чтобы получить или дозапросить {taskAmount} задач типа {selectedTaskType} в раунде {currentRound.Id}");
    }

    Console.WriteLine("Или Enter, чтобы перейти к следующему действию");

    var input = Console.ReadLine()?.Trim();

    if (string.Equals(input, "y", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine();
        Console.WriteLine("Выбери режим решения задач:");
        Console.WriteLine("6 - Автоматический, отправлять ответы сразу");
        Console.WriteLine("7 - Ручной, проверять ответы перед отправкой");
        Console.Write("Твой выбор (6 или 7): ");

        bool isManualMode = Console.ReadLine()?.Trim() == "7";

        List<TaskResponse> tasksToSolve;

        if (useRandomTaskTypes)
        {
            tasksToSolve = await GetOrRequestRandomTasksAsync(
                challengeClient,
                currentRound.Id,
                availableTaskTypes,
                taskAmount,
                random
            );
        }
        else
        {
            tasksToSolve = await GetOrRequestTasksByTypeAsync(
                challengeClient,
                currentRound.Id,
                selectedTaskType,
                taskAmount
            );
        }

        tasksToSolve = tasksToSolve.Take(taskAmount).ToList();

        if (tasksToSolve.Count > 0)
        {
            await SolveTasksAsync(challengeClient, tasksToSolve, isManualMode);
        }
        else
        {
            Console.WriteLine("Нет задач для решения или не удалось их запросить.");
        }
    }

    Console.WriteLine();
    Console.WriteLine("------------------------------------------------");

    Console.Write(useRandomTaskTypes
        ? "Хочешь запросить еще задач случайных типов? (y/n): "
        : "Хочешь запросить еще задач этого типа? (y/n): ");

    var exitChoice = Console.ReadLine()?.Trim();

    if (!string.Equals(exitChoice, "y", StringComparison.OrdinalIgnoreCase))
        run = false;
}

Console.WriteLine();
Console.WriteLine("Программа завершена. Нажми ВВОД для закрытия окна...");
Console.ReadLine();

return;

static async Task<List<TaskResponse>> GetOrRequestRandomTasksAsync(
    ChallengeClient challengeClient,
    string roundId,
    List<string> availableTaskTypes,
    int requiredAmount,
    Random random)
{
    Console.WriteLine();
    Console.WriteLine("Проверка существующих нерешенных заданий всех типов...");

    var pendingTasks = new List<TaskResponse>();

    foreach (var taskType in availableTaskTypes)
    {
        try
        {
            var tasksOfType = await challengeClient.GetTasksAsync(
                roundId,
                taskType,
                TaskStatus.Pending,
                0,
                requiredAmount
            );

            Console.WriteLine($"Тип '{taskType}': найдено pending-задач: {tasksOfType.Count}");

            pendingTasks.AddRange(tasksOfType);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении pending-задач типа '{taskType}': {ex.Message}");
        }
    }

    pendingTasks = pendingTasks
        .GroupBy(task => task.Id)
        .Select(group => group.First())
        .OrderBy(_ => random.Next())
        .ToList();

    Console.WriteLine($"Всего найдено pending-задач всех типов: {pendingTasks.Count}");

    var tasksToRequest = requiredAmount - pendingTasks.Count;
    if (tasksToRequest <= 0) return pendingTasks;

    Console.WriteLine($"Pending-задач не хватает. Запрашиваем еще {tasksToRequest} новых случайных задач...");

    for (var i = 0; i < tasksToRequest; i++)
    {
        try
        {
            var task = await challengeClient.AskNewTaskAsync(roundId);
            pendingTasks.Add(task);

            Console.WriteLine($"Получена новая случайная задача {i + 1}/{tasksToRequest}: {task.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Не удалось запросить случайную задачу {i + 1}/{tasksToRequest}: {ex.Message}");
        }
    }

    pendingTasks = pendingTasks
        .GroupBy(task => task.Id)
        .Select(group => group.First())
        .OrderBy(_ => random.Next())
        .ToList();

    return pendingTasks;
}

static async Task<List<TaskResponse>> GetOrRequestTasksByTypeAsync(
    ChallengeClient challengeClient,
    string roundId,
    string taskType,
    int requiredAmount)
{
    Console.WriteLine();
    Console.WriteLine($"Проверка существующих нерешенных заданий типа '{taskType}'...");

    var pendingTasks = new List<TaskResponse>();

    try
    {
        pendingTasks = await challengeClient.GetTasksAsync(
            roundId,
            taskType,
            TaskStatus.Pending,
            0,
            requiredAmount
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при получении pending-задач типа '{taskType}': {ex.Message}");
    }

    Console.WriteLine($"Найдено pending-задач типа '{taskType}': {pendingTasks.Count}");

    var tasksToRequest = requiredAmount - pendingTasks.Count;
    if (tasksToRequest <= 0) return pendingTasks;

    Console.WriteLine(
        $"Pending-задач не хватает. Запрашиваем еще {tasksToRequest} новых задач типа '{taskType}'...");

    for (var i = 0; i < tasksToRequest; i++)
    {
        try
        {
            var task = await challengeClient.AskNewTaskAsync(roundId, taskType);

            pendingTasks.Add(task);

            Console.WriteLine($"Получена новая задача типа '{taskType}' {i + 1}/{tasksToRequest}: {task.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Не удалось запросить новую задачу типа '{taskType}' {i + 1}/{tasksToRequest}: {ex.Message}");
        }
    }

    return pendingTasks;
}


static async Task SolveTasksAsync(
    ChallengeClient challengeClient,
    List<TaskResponse> tasksToSolve,
    bool isManualMode)
{
    Console.WriteLine();
    Console.WriteLine($"Начало решения {tasksToSolve.Count} заданий");

    for (var i = 0; i < tasksToSolve.Count; i++)
    {
        var task = tasksToSolve[i];

        Console.WriteLine();
        Console.WriteLine($"Задание {i + 1}/{tasksToSolve.Count}");
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
            continue;
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
            continue;
        }

        Console.WriteLine("Ожидание отправки решения...");

        try
        {
            var updatedTask = await challengeClient.CheckTaskAnswerAsync(task.Id, answer);
            Console.WriteLine($"Статус ответа: {updatedTask.Status}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при отправке ответа: {ex.Message}");
        }
    }
}