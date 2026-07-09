using System;
using System.Collections.Generic;
using System.Globalization;
using Challenge.DataContracts;

namespace ConsoleApp.types;

public static class PolynomialRoot
{
    private const double Eps = 0.000001;

    public static string Solve(TaskResponse taskResponse)
    {
        double[] coefficients = ParsePolynomial(taskResponse.Question);

        double? root = FindRoot(coefficients);

        if (root == null)
        {
            return "no roots";
        }

        return FormatAnswer(root.Value);
    }

    private static double[] ParsePolynomial(string question)
    {
        Dictionary<int, double> coefficients = new Dictionary<int, double>();

        string[] parts = question.Split('+');

        foreach (string rawPart in parts)
        {
            string part = rawPart.Trim();

            if (string.IsNullOrEmpty(part))
            {
                continue;
            }

            int power;
            double coefficient;

            if (part.Contains("x"))
            {
                int xIndex = part.IndexOf("*x", StringComparison.Ordinal);
                string coefficientText = part.Substring(0, xIndex).Trim();

                coefficient = ParseNumber(coefficientText);

                if (part.Contains("^"))
                {
                    int powerIndex = part.IndexOf("^", StringComparison.Ordinal);
                    string powerText = part.Substring(powerIndex + 1).Trim();

                    power = int.Parse(powerText);
                }
                else
                {
                    power = 1;
                }
            }
            else
            {
                coefficient = ParseNumber(part);
                power = 0;
            }

            if (!coefficients.ContainsKey(power))
            {
                coefficients[power] = 0;
            }

            coefficients[power] += coefficient;
        }

        int maxPower = 0;

        foreach (int power in coefficients.Keys)
        {
            if (power > maxPower)
            {
                maxPower = power;
            }
        }

        double[] result = new double[maxPower + 1];

        foreach (var pair in coefficients)
        {
            result[pair.Key] = pair.Value;
        }

        return result;
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

    private static double? FindRoot(double[] coefficients)
    {
        int degree = GetDegree(coefficients);

        if (degree == -1)
        {
            return 0;
        }

        if (degree == 0)
        {
            return null;
        }

        if (degree == 1)
        {
            return -coefficients[0] / coefficients[1];
        }

        double bound = GetBound(coefficients);

        double left = -bound;
        double leftValue = Calculate(coefficients, left);

        if (Math.Abs(leftValue) <= 0.001)
        {
            return left;
        }

        int steps = 50000;

        for (int i = 1; i <= steps; i++)
        {
            double right = -bound + 2 * bound * i / steps;
            double rightValue = Calculate(coefficients, right);

            if (Math.Abs(rightValue) <= 0.001)
            {
                return right;
            }

            if (leftValue * rightValue < 0)
            {
                return Bisection(coefficients, left, right);
            }

            left = right;
            leftValue = rightValue;
        }

        return TryNewton(coefficients, bound);
    }

    private static int GetDegree(double[] coefficients)
    {
        for (int i = coefficients.Length - 1; i >= 0; i--)
        {
            if (Math.Abs(coefficients[i]) > Eps)
            {
                return i;
            }
        }

        return -1;
    }

    private static double GetBound(double[] coefficients)
    {
        int degree = GetDegree(coefficients);

        if (degree <= 0)
        {
            return 10;
        }

        double leading = Math.Abs(coefficients[degree]);
        double max = 0;

        for (int i = 0; i < degree; i++)
        {
            double current = Math.Abs(coefficients[i] / leading);

            if (current > max)
            {
                max = current;
            }
        }

        double bound = 1 + max;

        if (bound < 10)
        {
            bound = 10;
        }

        return bound;
    }

    private static double Bisection(double[] coefficients, double left, double right)
    {
        double leftValue = Calculate(coefficients, left);

        for (int i = 0; i < 100; i++)
        {
            double middle = (left + right) / 2;
            double middleValue = Calculate(coefficients, middle);

            if (Math.Abs(middleValue) <= 0.001)
            {
                return middle;
            }

            if (leftValue * middleValue <= 0)
            {
                right = middle;
            }
            else
            {
                left = middle;
                leftValue = middleValue;
            }
        }

        return (left + right) / 2;
    }

    private static double? TryNewton(double[] coefficients, double bound)
    {
        double[] derivative = GetDerivative(coefficients);

        for (int startIndex = 0; startIndex <= 1000; startIndex++)
        {
            double x = -bound + 2 * bound * startIndex / 1000;

            for (int iteration = 0; iteration < 50; iteration++)
            {
                double value = Calculate(coefficients, x);

                if (Math.Abs(value) <= 0.001)
                {
                    return x;
                }

                double derivativeValue = Calculate(derivative, x);

                if (Math.Abs(derivativeValue) < Eps)
                {
                    break;
                }

                x -= value / derivativeValue;

                if (double.IsNaN(x) || double.IsInfinity(x))
                {
                    break;
                }

                if (x < -bound * 2 || x > bound * 2)
                {
                    break;
                }
            }
        }

        return null;
    }

    private static double[] GetDerivative(double[] coefficients)
    {
        if (coefficients.Length == 1)
        {
            return new double[] { 0 };
        }

        double[] derivative = new double[coefficients.Length - 1];

        for (int i = 1; i < coefficients.Length; i++)
        {
            derivative[i - 1] = coefficients[i] * i;
        }

        return derivative;
    }

    private static double Calculate(double[] coefficients, double x)
    {
        double result = 0;

        for (int i = coefficients.Length - 1; i >= 0; i--)
        {
            result = result * x + coefficients[i];
        }

        return result;
    }

    private static string FormatAnswer(double number)
    {
        if (Math.Abs(number) < Eps)
        {
            number = 0;
        }

        return number.ToString("0.######", CultureInfo.InvariantCulture);
    }
}