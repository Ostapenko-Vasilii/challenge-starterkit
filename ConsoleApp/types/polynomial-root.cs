using System;
using System.Globalization;
using Challenge.DataContracts;

namespace ConsoleApp.types;

public static class PolynomialRoot
{
    public static string Solve(TaskResponse taskResponse)
    {
        var coefficients = ParsePolynomial(taskResponse.Question);

        double a = coefficients.a;
        double b = coefficients.b;
        double c = coefficients.c;

        if (Math.Abs(a) < 0.0000001)
        {
            if (Math.Abs(b) < 0.0000001)
            {
                if (Math.Abs(c) < 0.0000001)
                {
                    return "0";
                }

                return "no roots";
            }

            double root = -c / b;
            return FormatAnswer(root);
        }

        double discriminant = b * b - 4 * a * c;

        if (discriminant < -0.0000001)
        {
            return "no roots";
        }

        if (Math.Abs(discriminant) < 0.0000001)
        {
            discriminant = 0;
        }

        double sqrtD = Math.Sqrt(discriminant);

        double root1 = (-b + sqrtD) / (2 * a);
        double root2 = (-b - sqrtD) / (2 * a);

        if (root1 >= 0)
        {
            return FormatAnswer(root1);
        }

        return FormatAnswer(root2);
    }

    private static (double a, double b, double c) ParsePolynomial(string question)
    {
        double a = 0;
        double b = 0;
        double c = 0;

        string[] parts = question.Split('+');

        foreach (string rawPart in parts)
        {
            string part = rawPart.Trim();

            if (part.Contains("x^2"))
            {
                a = GetCoefficient(part, "*x^2");
            }
            else if (part.Contains("x"))
            {
                b = GetCoefficient(part, "*x");
            }
            else
            {
                c = ParseNumber(part);
            }
        }

        return (a, b, c);
    }

    private static double GetCoefficient(string part, string variablePart)
    {
        string coefficient = part.Replace(variablePart, "").Trim();

        return ParseNumber(coefficient);
    }

    private static double ParseNumber(string value)
    {
        value = value.Trim();

        if (value.StartsWith("(") && value.EndsWith(")"))
        {
            value = value.Substring(1, value.Length - 2);
        }

        return double.Parse(value, CultureInfo.InvariantCulture);
    }

    private static string FormatAnswer(double number)
    {
        if (Math.Abs(number) < 0.0000001)
        {
            number = 0;
        }

        return number.ToString("G17", CultureInfo.InvariantCulture);
    }
}