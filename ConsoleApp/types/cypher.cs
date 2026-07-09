using Challenge.DataContracts;
using System;
using System.Collections.Generic;


namespace ConsoleApp
{
    public class Cypher
    {
        public static string Solve(TaskResponse taskResponse)
        {
            var question = taskResponse.Question.Trim('#');

            string[] fullCode = question.Split('#');

            var type = fullCode[0];
            var enqrCode = fullCode[1];

            if (type.Equals("reversed"))
            {
                char[] charArray = enqrCode.ToCharArray();
                Array.Reverse(charArray);
                return new string(charArray);
            }
            if (type.StartsWith("Caesar's code"))
            {
                string[] eqParts = type.Split('=');
                if (eqParts.Length == 2 && int.TryParse(eqParts[1].Trim(), out int shift))
                {
                    return DecryptCeasar(enqrCode, shift);
                }
            }

            throw new Exception();
        }

        private static string DecryptCeasar(string enqr, int shift)
        {
            var allText = "abcdefghijklmnopqrstuvwxyz0123456789' ";
            var deq = new List <char>();
            var Len = allText.Length;
            
            foreach (var l in enqr)
            {
                var ind = allText.IndexOf(l);
                var deqInd = (ind - shift) % Len;
                if (deqInd < 0)
                {
                    deqInd += Len;
                }
                deq.Add(allText[deqInd]);
            }

            return new string(deq.ToArray());
        }
    }
}
