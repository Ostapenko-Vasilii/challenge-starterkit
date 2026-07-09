using Challenge;
using Challenge.DataContracts;
using ConsoleApp;
using System;
using System.Collections.Generic;
using System.IO;
using TaskStatus = Challenge.DataContracts.TaskStatus;



string secretFilePath = "TeamSecret.txt";
string teamSecret = "";

if (File.Exists(secretFilePath))
{
    teamSecret = File.ReadAllText(secretFilePath).Trim();
}

if (string.IsNullOrEmpty(teamSecret))
{
    if (!File.Exists(secretFilePath))
    {
        Console.WriteLine($"Файл '{secretFilePath}' не найден.");
    }
    else
    {
        Console.WriteLine($"Файл '{secretFilePath}' обнаружен, но пуст.");
    }

    Console.Write("Введи секретный ключ твоей команды: ");
    teamSecret = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(teamSecret))
    {
        Console.WriteLine($"Ключ команды не может быть пустым. Попробуй еще разз, либо впиши его сам в файл {secretFilePath}");
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



Console.WriteLine("Ожидание...");
var challengeClient = new ChallengeClient(teamSecret);

const string challengeId = "git-course"; // Вставь айди челенджа сюда
var challenge = await challengeClient.GetChallengeAsync(challengeId);

var utcNow = DateTime.UtcNow;
Round currentRound = null;
foreach (var round in challenge.Rounds)
{
    if (round.StartTimestamp < utcNow && utcNow < round.EndTimestamp)
        currentRound = round;
}

if (currentRound == null)
{
    Console.WriteLine("Раунд еще не начался");
    Console.ReadLine();
    return;
}

Console.WriteLine("Доступные типы задач в текущем раунде:");
foreach (var task in currentRound.TaskTypes)
{
    Console.WriteLine($"--- {task.Id}\n");
}

Console.Write("Введи тип задачи либо нажми Enter для дефолтного типа : ");
string taskType = Console.ReadLine()?.Trim();

var defaultTask = "starter"; // Можно написать сюда дефолт тип задачи что бы не вписывать каждый раз
if (string.IsNullOrEmpty(taskType))
{
    Console.WriteLine($"Тип не указан. По умолчанию: {defaultTask}");
}

bool Run = true;
while (Run)
{
    Console.Write("Введи количество задач: ");
    int taskAmount;
    while (!int.TryParse(Console.ReadLine(), out taskAmount) || taskAmount <= 0)
    {
        Console.Write("Некорректный ввод. Пожалуйста, введи положительное целое число: ");
    }

    Console.WriteLine($"Нажми y, чтобы получить (или дозапросить) {taskAmount} задач типа {taskType} в раунде {currentRound.Id}");
    Console.WriteLine($"Или Enter, чтобы перейти к следующему действию");
    var input = Console.ReadLine();

    if (input.Equals("y", StringComparison.OrdinalIgnoreCase))
    {
        // Выбор режима перед началом решения
        Console.WriteLine("\nВыбери режим решения задач:");
        Console.WriteLine("6 - Автоматический (отправлять ответы сразу)");
        Console.WriteLine("7 - Ручной (проверять ответы перед отправкой)");
        Console.Write("Твой выбор (6 или 7): ");
        bool isManualMode = Console.ReadLine()?.Trim() == "7";

        Console.WriteLine("\nПроверка существующих нерешенных заданий...");

        List<TaskResponse> pendingTasks = new List<TaskResponse>();

        try
        {
            pendingTasks = await challengeClient.GetTasksAsync(currentRound.Id, taskType, TaskStatus.Pending, 0, taskAmount);
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
                    await challengeClient.AskNewTaskAsync(currentRound.Id, taskType);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Не удалось запросить новую задачу {i + 1}: {ex.Message}");
                }
            }

            try
            {
                pendingTasks = await challengeClient.GetTasksAsync(currentRound.Id, taskType, TaskStatus.Pending, 0, taskAmount);
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

            Console.WriteLine($"Начало решения {tasksToSolve.Count} заданий\n");

            var hint = tasksToSolve[0].UserHint ?? "Формулировка отсутствует";
            Console.WriteLine($"  Формулировка: {hint}");

            for (int i = 0; i < tasksToSolve.Count; i++)
            {
                var task = tasksToSolve[i];
                Console.WriteLine($"\n  Задание {i + 1}, Вопрос: {task.Question}");

                var ans = Solver.Solve(task);

                if (isManualMode)
                {
                    Console.WriteLine($"  Ответ бота: {ans}");
                    Console.Write("  Отправить этот ответ? (y/ n - ввести свой): ");
                    var confirm = Console.ReadLine()?.Trim();

                    if (!confirm.Equals("y", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.Write("\nВведи свой вариант ответа: ");
                        ans = Console.ReadLine()?.Trim();
                    }
                }
                Console.WriteLine("Ожидание отправки решения...");

                var updTask = await challengeClient.CheckTaskAnswerAsync(task.Id, ans);
                Console.WriteLine($"  Статус ответа: {updTask.Status}");
            }
        }
        else
        {
            Console.WriteLine("Нет задач для решения или не удалось их запросить.");
        }
    }

    Console.WriteLine();
    Console.WriteLine("------------------------------------------------");
    Console.Write("Хочешь запросить еще задач этого типа? (y/n): ");
    var exitChoice = Console.ReadLine()?.Trim();

    if (!exitChoice.Equals("y", StringComparison.OrdinalIgnoreCase))
    {
        Run = false;
    }
}

Console.WriteLine();
Console.WriteLine("Программа завершена. Нажми ВВОД для закрытия окна...");
Console.ReadLine();
