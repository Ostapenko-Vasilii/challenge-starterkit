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
            if (type.StartsWith("prime multiplicator"))
            {
                string[] parts = type.Split('=');
                if (parts.Length > 1)
                {
                    string multStr = parts[1].Split(' ')[0].Trim();
                    if (int.TryParse(multStr, out int mult))
                    {
                        return DecryptPrimeMultiplicator(enqrCode, mult);
                    }
                }
            }
            if (type.StartsWith("Vigenere's code"))
            {
                string[] parts = type.Split('=');
                if (parts.Length > 1)
                {
                    string key = parts[1];
                    return DecryptVigenere(enqrCode, key);
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

        private static string DecryptPrimeMultiplicator(string enqr, int mult)
        {
            var allText = "abcdefghijklmnopqrstuvwxyz0123456789' ";
            int abcLen = allText.Length;
            int mod = abcLen + 1;
            int inv = -1;
            for (int i = 1; i < mod; i++)
            {
                if ((mult * i) % mod == 1)
                {
                    inv = i;
                    break;
                }
            }

            if (inv == -1)
                throw new Exception("на всякий");

            var deq = new List<char>();

            foreach (var l in enqr)
            {
                int newIndex = allText.IndexOf(l);
                if (newIndex == -1) continue; 
                int charIndex = ((newIndex + 1) * inv) % mod - 1;

                if (charIndex < 0)
                {
                    charIndex += mod;
                }

                deq.Add(allText[charIndex]);
            }

            return new string(deq.ToArray());
        }

        private static string DecryptVigenere(string enqr, string key)
        {
            var allText = "abcdefghijklmnopqrstuvwxyz0123456789' ";
            var len = allText.Length;
            var deq = new List<char>();

            for (int i = 0; i < enqr.Length; i++)
            {
                int textIdx = allText.IndexOf(enqr[i]);
                int keyIdx = allText.IndexOf(key[i % key.Length]);

                if (textIdx == -1 || keyIdx == -1) continue;

                int deqInd = (textIdx - keyIdx) % len;
                if (deqInd < 0)
                {
                    deqInd += len;
                }
                deq.Add(allText[deqInd]);
            }

            return new string(deq.ToArray());
        }
    }
}
