using Challenge;
using Challenge.DataContracts;
using ConsoleApp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskStatus = Challenge.DataContracts.TaskStatus;


// Данное приложение можно запускать под Windows, Linux, Mac.
// Для запуска приложения необходимо скачать и установить .NET 8.
// Скачать можно тут: https://dotnet.microsoft.com/download/dotnet


const string teamSecret = ""; // Вставь сюда ключ команды
if (string.IsNullOrEmpty(teamSecret))
{
    Console.WriteLine("Задай секрет своей команды, чтобы можно было делать запросы от ее имени");
    Console.ReadLine();
    return;
}

var challengeClient = new ChallengeClient(teamSecret);

const string challengeId = "git-course"; // Вставь айди челенджа сюда
Console.WriteLine($"Нажми ВВОД, чтобы получить информацию о соревновании {challengeId}");
Console.ReadLine();
Console.WriteLine("Ожидание...");
var challenge = await challengeClient.GetChallengeAsync(challengeId);
Console.WriteLine(challenge.Description);
Console.WriteLine();
Console.WriteLine("----------------");
Console.WriteLine();

const string taskType = "cypher"; //  Вставь тип задачи

var utcNow = DateTime.UtcNow;
string currentRound = null;
foreach (var round in challenge.Rounds)
{
    if (round.StartTimestamp < utcNow && utcNow < round.EndTimestamp)
        currentRound = round.Id;
}

var taskAmount = 10;
Console.WriteLine($"Нажми y, чтобы получить (или дозапросить) {taskAmount} задач типа {taskType} в раунде {currentRound}");
Console.WriteLine($"Или Enter, чтобы перейти к следующему действию");
var input = Console.ReadLine();

if (input.Equals("y"))
{
    Console.WriteLine("Проверка существующих нерешенных заданий...");

    List<TaskResponse> pendingTasks = new List<TaskResponse>();

    try
    {
        pendingTasks = await challengeClient.GetTasksAsync(currentRound, taskType, TaskStatus.Pending, 0, taskAmount);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при получении существующих задач: {ex.Message}");
    }

    Console.WriteLine($"Найдено существующих нерешенных задач: {pendingTasks.Count}");

    int tasksToRequest = taskAmount - pendingTasks.Count;

    // Дозапрос
    if (tasksToRequest > 0)
    {
        Console.WriteLine($"Запрашиваем еще {tasksToRequest} новых заданий...");
        for (int i = 0; i < tasksToRequest; i++)
        {
            try
            {
                await challengeClient.AskNewTaskAsync(currentRound, taskType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось запросить новую задачу {i + 1}: {ex.Message}");
            }
        }

        try
        {
            pendingTasks = await challengeClient.GetTasksAsync(currentRound, taskType, TaskStatus.Pending, 0, taskAmount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обновлении списка задач: {ex.Message}");
        }
    }

    // Решение задач
    if (pendingTasks.Count > 0)
    {
        var tasksToSolve = pendingTasks.Count > taskAmount ? pendingTasks.GetRange(0, taskAmount) : pendingTasks;

        Console.WriteLine($"Начало решения {tasksToSolve.Count} заданий");

        // Чтоб не падало когда нету формулировки
        var hint = tasksToSolve[0].UserHint ?? "Формулировка отсутствует";
        Console.WriteLine($"  Формулировка: {hint}");

        for (int i = 0; i < tasksToSolve.Count; i++)
        {
            var task = tasksToSolve[i];
            Console.WriteLine($"  Задание {i + 1}, Вопрос: {task.Question}");
            Console.WriteLine("Ожидание решения...");

            var ans = Solver.Solve(task);
            var updTask = await challengeClient.CheckTaskAnswerAsync(task.Id, ans);

            Console.WriteLine($"Статус ответа: {updTask.Status}");
        }
    }
    else
    {
        Console.WriteLine("Нет задач для решения и не удалось их запросить.");
    }
}

//Console.WriteLine("----------------");
//Console.WriteLine();

//Console.WriteLine($"Нажми ВВОД, чтобы получить задачу типа {taskType} в раунде {currentRound}");
//Console.ReadLine();
//Console.WriteLine("Ожидание...");
//var newTask = await challengeClient.AskNewTaskAsync(currentRound, taskType);
//Console.WriteLine($"  Новое задание, статус {newTask.Status}");
//Console.WriteLine($"  Формулировка: {newTask.UserHint}");
//Console.WriteLine($"                {newTask.Question}");
//Console.WriteLine();
//Console.WriteLine("----------------");
//Console.WriteLine();

//var answer = Solver.Solve(newTask);

//Console.WriteLine($"Нажми ВВОД, чтобы ответить на полученную задачу самым правильным ответом: {answer}");
//Console.ReadLine();
//Console.WriteLine("Ожидание...");
//var updatedTask = await challengeClient.CheckTaskAnswerAsync(newTask.Id, answer);
//Console.WriteLine($"  Новое задание, статус {updatedTask.Status}");
//Console.WriteLine($"  Формулировка:  {updatedTask.UserHint}");
//Console.WriteLine($"                 {updatedTask.Question}");
//Console.WriteLine($"  Ответ команды: {updatedTask.TeamAnswer}");
//Console.WriteLine();
//if (updatedTask.Status == TaskStatus.Success)
//    Console.WriteLine($"Ура! Ответ угадан!");
//else if (updatedTask.Status == TaskStatus.Failed)
//    Console.WriteLine($"Похоже ответ не подошел и задачу больше сдать нельзя...");
//Console.WriteLine();
//Console.WriteLine("----------------");
//Console.WriteLine();

Console.WriteLine($"Нажми ВВОД, чтобы завершить работу программы");
Console.ReadLine();