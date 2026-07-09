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

        return "no roots";
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
}