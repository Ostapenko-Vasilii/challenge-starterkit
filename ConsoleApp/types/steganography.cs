using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Challenge.DataContracts;

namespace ConsoleApp.types;

public static class Steganography
{
    public static string Solve(TaskResponse taskResponse)
    {
        var text = taskResponse.Question;
        var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
    
        if (lines.Length == 0) return string.Empty;

        if (lines.First().Split(' ').Length > 1)
        {
            var indexes = lines.First().Split(' ').Select(x => int.Parse(x) - 1).ToArray();
            var result = new StringBuilder();
        
            var targetLine = lines[1]; 
            foreach (var i in indexes)
            {
                if (i >= 0 && i < targetLine.Length)
                    result.Append(targetLine[i]);
            }
            return result.ToString();
        }
        else
        {
            var roman = lines.First().Trim();
            int index = DecodeRoman(roman) - 1;

            var result = new StringBuilder();
            foreach (var line in lines.Skip(1))
            {
                if (index >= 0 && index < line.Length)
                {
                    result.Append(line[index]);
                }
            }
            return result.ToString();
        }
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