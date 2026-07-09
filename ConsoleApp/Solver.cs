using Challenge.DataContracts;
using System;
using ConsoleApp.types;

namespace ConsoleApp;

public class Solver
{
    public static string Solve(TaskResponse taskResponse) => taskResponse.TypeId switch
    {
        "math" => MathSolve.Solve(taskResponse),
        "determinant" => Determinant.Solve(taskResponse),
        "polynomial-root" => PolynomialRoot.Solve(taskResponse),
        "cypher" => Cypher.Solve(taskResponse),
        "steganography" => Steganography.Solve(taskResponse),
        "shape" => Shape.Solve(taskResponse),
        "statistics" => Statistics.Solve(taskResponse),
        _ => throw new NotImplementedException()
    };
}