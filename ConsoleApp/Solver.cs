using Challenge.DataContracts;
using System;
using System.Linq;
using ConsoleApp.types;

namespace ConsoleApp;

public class Solver
{
    public static string Solve(TaskResponse taskResponse)
    {
        
        switch (taskResponse.TypeId)
        {
            case "math": return MathSolve.Solve(taskResponse);
            case "determinant": return Determinant.Solve(taskResponse);
            case "polynomial-root": return PolynomialRoot.Solve(taskResponse);
            case "cypher": return Cypher.Solve(taskResponse);
            case "steganography": return Steganography.Solve(taskResponse);
            case "shape": return Shape.Solve(taskResponse);
            case "statistics": return Statistics.Solve(taskResponse);
        }
        throw  new NotImplementedException();
    }
  
}
