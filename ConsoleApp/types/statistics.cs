using System;
using System.Collections.Generic;
using System.Linq;
using Challenge.DataContracts;

namespace ConsoleApp.types;

public static class Statistics
{
    public static string Solve(TaskResponse taskResponse)
    {
        var parts = taskResponse.Question.Split('|', 2);

        var function = parts[0].Trim().ToLowerInvariant();
        var numbers = new List<long>();

        if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
        {
            numbers = parts[1]
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(long.Parse)
                .ToList();
        }

        return function switch
        {
            "min" => numbers.Count == 0 ? "" : numbers.Min().ToString(),
            "max" => numbers.Count == 0 ? "" : numbers.Max().ToString(),
            "sum" => numbers.Count == 0 ? "" : numbers.Sum().ToString(),
            _ => throw new NotSupportedException()
        };
    }
}