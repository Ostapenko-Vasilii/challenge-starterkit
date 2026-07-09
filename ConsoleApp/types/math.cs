using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Challenge.DataContracts;

namespace ConsoleApp.types;

public static class MathSolve
{
    public static string Solve(TaskResponse taskResponse)
    {
        string task = taskResponse.Question.Replace(" ", "");

        task = Regex.Replace(task, @"[IVXLCDM]+", match => DecodeRoman(match.Value).ToString());
        task = EvaluateFunctions(task);
        
        string[] tokens = Regex.Split(task, @"([+\-*/%()])");
        
        var operatorsStack = new Stack<string>();
        var operands = new Stack<int>();
        bool isUnaryCanBeNext = true; 

        foreach (var token in tokens)
        {
            if (string.IsNullOrEmpty(token)) continue;

            if (int.TryParse(token, out int number))
            {
                operands.Push(number);
                isUnaryCanBeNext = false;
            }
            else if (token == "(")
            {
                operatorsStack.Push(token);
                isUnaryCanBeNext = true;
            }
            else if (token == ")")
            {
                while (operatorsStack.Count > 0 && operatorsStack.Peek() != "(")
                {
                    ExecuteOperator(operatorsStack, operands);
                }
                if (operatorsStack.Count > 0) operatorsStack.Pop();
                isUnaryCanBeNext = false; 
            }
            else 
            {
                if (token == "-" && isUnaryCanBeNext)
                {
                    operands.Push(0); 
                }

                while (operatorsStack.Count > 0 && GetPriority(operatorsStack.Peek()) >= GetPriority(token))
                {
                    ExecuteOperator(operatorsStack, operands);
                }
                
                operatorsStack.Push(token);
                isUnaryCanBeNext = true;
            }
        }

        while (operatorsStack.Count > 0)
        {
            ExecuteOperator(operatorsStack, operands);
        }

        return operands.Pop().ToString();
    }

    private static string EvaluateFunctions(string expression)
    {
        string pattern = @"(min|max|left|right|sum|mul)\((-?\d+)\)\((-?\d+)\)";
        
        while (Regex.IsMatch(expression, pattern))
        {
            expression = Regex.Replace(expression, pattern, match =>
            {
                string func = match.Groups[1].Value;
                int a = int.Parse(match.Groups[2].Value);
                int b = int.Parse(match.Groups[3].Value);

                int result = func switch
                {
                    "min"   => Math.Min(a, b),
                    "max"   => Math.Max(a, b),
                    "left"  => a,
                    "right" => b,
                    "sum"   => a + b,
                    "mul"   => a * b,
                    _ => throw new NotSupportedException($"Функция {func} не поддерживается")
                };
                
                return result.ToString();
            });
        }
        
        return expression;
    }

    private static int GetPriority(string op)
    {
        return op switch
        {
            "+" or "-" => 1,
            "*" or "/" or "%" => 2,
            _ => 0
        };
    }

    private static void ExecuteOperator(Stack<string> operators, Stack<int> operands)
    {
        string op = operators.Pop();
        
        int right = operands.Pop();
        int left = operands.Pop();

        int result = op switch
        {
            "+" => left + right,
            "-" => left - right,
            "*" => left * right,
            "/" => left / right, 
            "%" => left % right,
            _ => throw new InvalidOperationException(op)
        };

        operands.Push(result);
    }
    
    private static int DecodeRoman(string roman)
    {
        var romanMap = new Dictionary<char, int> {
            {'I', 1}, {'V', 5}, {'X', 10}, {'L', 50}, {'C', 100}, {'D', 500}, {'M', 1000}
        };
    
        int total = 0;
        int prevValue = 0;
    
        foreach (var c in roman.Reverse())
        {
            if (!romanMap.TryGetValue(c, out int currentValue)) continue;
        
            if (currentValue < prevValue)
            {
                total -= currentValue;
            }
            else
            {
                total += currentValue;
            }
            prevValue = currentValue;
        }
    
        return total;
    }
}