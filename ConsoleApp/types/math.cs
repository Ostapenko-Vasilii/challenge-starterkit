using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Challenge.DataContracts;

namespace ConsoleApp.types;

public static class MathSolve
{
    public static string Solve(TaskResponse taskResponse)
{
    string task = taskResponse.Question.Replace(" ", "");
    
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
}