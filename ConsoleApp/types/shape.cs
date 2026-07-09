using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Challenge.DataContracts;

namespace ConsoleApp.types;

public static partial class Shape
{
    public static string Solve(TaskResponse taskResponse)
    {
        var matches = PointRegex().Matches(taskResponse.Question);

        var points = new List<Point>();

        foreach (Match match in matches)
        {
            var x = long.Parse(match.Groups[1].Value);
            var y = long.Parse(match.Groups[2].Value);

            points.Add(new Point(x, y));
        }

        if (points.Count == 0)
            return "circle";

        var uniquePoints = points
            .GroupBy(point => $"{point.X},{point.Y}")
            .Select(group => group.First())
            .ToList();

        var minX = uniquePoints.Min(point => point.X);
        var maxX = uniquePoints.Max(point => point.X);
        var minY = uniquePoints.Min(point => point.Y);
        var maxY = uniquePoints.Max(point => point.Y);

        var width = maxX - minX + 1;
        var height = maxY - minY + 1;

        var boundingBoxArea = (double)width * height;
        var density = uniquePoints.Count / boundingBoxArea;

        var possibleAnswers = GetPossibleAnswers(taskResponse.Question);

        var scores = new Dictionary<string, double>
        {
            ["square"] = Math.Abs(density - 1.0),
            ["circle"] = Math.Abs(density - Math.PI / 4.0),
            ["equilateraltriangle"] = Math.Abs(density - 0.5)
        };

        return scores
            .Where(pair => possibleAnswers.Count == 0 || possibleAnswers.Contains(pair.Key))
            .OrderBy(pair => pair.Value)
            .First()
            .Key;
    }

    private readonly struct Point
    {
        public Point(long x, long y)
        {
            X = x;
            Y = y;
        }

        public long X { get; }
        public long Y { get; }
    }

    private static HashSet<string> GetPossibleAnswers(string question)
    {
        var match = PossibleAnswersRegex().Match(question);

        if (!match.Success)
            return new HashSet<string>();

        return match.Groups[1].Value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet();
    }

    [GeneratedRegex(@"\((-?\d+)\s*,\s*(-?\d+)\)")]
    private static partial Regex PointRegex();

    [GeneratedRegex(@"Possible answers=([^|]+)\|")]
    private static partial Regex PossibleAnswersRegex();
}
