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

        var outerContour = GetOuterContour(points);

        if (IsSquare(outerContour))
            return "square";

        if (IsTriangleWithEqualSides(outerContour))
            return "equilateraltriangle";

        return "circle";
    }

    private struct Point(long x, long y)
    {
        public readonly long X = x;
        public readonly long Y = y;
    }

    private static long GetTurnDirection(Point previous, Point current, Point next)
    {
        return (current.X - previous.X) * (next.Y - previous.Y)
             - (current.Y - previous.Y) * (next.X - previous.X);
    }

    private static long GetSquaredDistance(Point first, Point second)
    {
        var dx = first.X - second.X;
        var dy = first.Y - second.Y;

        return dx * dx + dy * dy;
    }

    private static List<Point> GetOuterContour(List<Point> points)
    {
        points = points
            .GroupBy(point => $"{point.X},{point.Y}")
            .Select(group => group.First())
            .OrderBy(point => point.X)
            .ThenBy(point => point.Y)
            .ToList();

        if (points.Count <= 1)
            return points;

        var lowerContour = new List<Point>();

        foreach (var point in points)
        {
            while (lowerContour.Count >= 2 &&
                   GetTurnDirection(lowerContour[^2], lowerContour[^1], point) <= 0)
            {
                lowerContour.RemoveAt(lowerContour.Count - 1);
            }

            lowerContour.Add(point);
        }

        var upperContour = new List<Point>();

        for (var i = points.Count - 1; i >= 0; i--)
        {
            var point = points[i];

            while (upperContour.Count >= 2 &&
                   GetTurnDirection(upperContour[^2], upperContour[^1], point) <= 0)
            {
                upperContour.RemoveAt(upperContour.Count - 1);
            }

            upperContour.Add(point);
        }

        lowerContour.RemoveAt(lowerContour.Count - 1);
        upperContour.RemoveAt(upperContour.Count - 1);

        lowerContour.AddRange(upperContour);

        return lowerContour;
    }

    private static bool IsSquare(List<Point> outerContour)
    {
        if (outerContour.Count != 4)
            return false;

        long[] sides =
        [
            GetSquaredDistance(outerContour[0], outerContour[1]),
            GetSquaredDistance(outerContour[1], outerContour[2]),
            GetSquaredDistance(outerContour[2], outerContour[3]),
            GetSquaredDistance(outerContour[3], outerContour[0])
        ];

        if (sides.Any(side => side == 0))
            return false;

        var allSidesAreEqual = sides.All(side => side == sides[0]);

        var firstDiagonal = GetSquaredDistance(outerContour[0], outerContour[2]);
        var secondDiagonal = GetSquaredDistance(outerContour[1], outerContour[3]);

        var diagonalsAreEqual = firstDiagonal == secondDiagonal;

        return allSidesAreEqual && diagonalsAreEqual;
    }

    private static bool IsTriangleWithEqualSides(List<Point> outerContour)
    {
        if (outerContour.Count != 3)
            return false;

        var firstSide = GetSquaredDistance(outerContour[0], outerContour[1]);
        var secondSide = GetSquaredDistance(outerContour[1], outerContour[2]);
        var thirdSide = GetSquaredDistance(outerContour[2], outerContour[0]);

        return firstSide > 0 &&
               firstSide == secondSide &&
               secondSide == thirdSide;
    }

    [GeneratedRegex(@"\((-?\d+)\s*,\s*(-?\d+)\)")]
    private static partial Regex PointRegex();
}