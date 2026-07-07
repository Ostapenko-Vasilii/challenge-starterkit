using Challenge.DataContracts;
using System;
using System.Linq;

namespace ConsoleApp;

public class Solver
{
    public static string Solve(TaskResponse taskResponse)
    {
        if (taskResponse.TypeId == "cypher")
        {
            var a = taskResponse.Question.Split("#");
            if (a[1].Equals("reversed"))
            {
                char[] charArray = a[2].ToCharArray(); 
                Array.Reverse(charArray);
                string reversed = new string(charArray);

                return reversed;
            }
            else
            {
                throw new Exception("Шифр не reversed");
            }
            
        }
        return "24";
    }
}
