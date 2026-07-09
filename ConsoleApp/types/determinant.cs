using System;
using Challenge.DataContracts;

namespace ConsoleApp.types;

public static class Determinant
{
    public static string Solve(TaskResponse taskResponse)
    {
        long[,] matrix = ParseMatrix(taskResponse.Question);
        long answer = FindDeterminant(matrix);

        return answer.ToString();
    }

    private static long[,] ParseMatrix(string question)
    {
        string[] rows = question.Split(@"\\");

        int size = rows.Length;
        long[,] matrix = new long[size, size];

        for (int i = 0; i < size; i++)
        {
            string[] elements = rows[i].Split('&');

            for (int j = 0; j < size; j++)
            {
                matrix[i, j] = long.Parse(elements[j].Trim());
            }
        }

        return matrix;
    }

    private static long FindDeterminant(long[,] matrix)
    {
        int n = matrix.GetLength(0);

        if (n == 1)
        {
            return matrix[0, 0];
        }

        if (n == 2)
        {
            return matrix[0, 0] * matrix[1, 1] - matrix[0, 1] * matrix[1, 0];
        }

        long determinant = 0;

        for (int p = 0; p < n; p++)
        {
            long[,] subMatrix = new long[n - 1, n - 1];

            for (int i = 1; i < n; i++)
            {
                int subColIndex = 0;

                for (int j = 0; j < n; j++)
                {
                    if (j == p)
                    {
                        continue;
                    }

                    subMatrix[i - 1, subColIndex] = matrix[i, j];
                    subColIndex++;
                }
            }

            if (p % 2 == 0)
            {
                determinant += matrix[0, p] * FindDeterminant(subMatrix);
            }
            else
            {
                determinant -= matrix[0, p] * FindDeterminant(subMatrix);
            }
        }

        return determinant;
    }
}