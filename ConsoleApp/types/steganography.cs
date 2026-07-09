using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Challenge.DataContracts;

namespace ConsoleApp.types;

public static class Steganography
{
    private static readonly Dictionary<string, int> ChemicalElements = new(StringComparer.OrdinalIgnoreCase)
    {
        {"H", 1}, {"He", 2}, {"Li", 3}, {"Be", 4}, {"B", 5}, {"C", 6}, {"N", 7}, {"O", 8}, {"F", 9}, {"Ne", 10},
        {"Na", 11}, {"Mg", 12}, {"Al", 13}, {"Si", 14}, {"P", 15}, {"S", 16}, {"Cl", 17}, {"Ar", 18}, {"K", 19}, {"Ca", 20},
        {"Sc", 21}, {"Ti", 22}, {"V", 23}, {"Cr", 24}, {"Mn", 25}, {"Fe", 26}, {"Co", 27}, {"Ni", 28}, {"Cu", 29}, {"Zn", 30},
        {"Ga", 31}, {"Ge", 32}, {"As", 33}, {"Se", 34}, {"Br", 35}, {"Kr", 36}, {"Rb", 37}, {"Sr", 38}, {"Y", 39}, {"Zr", 40},
        {"Nb", 41}, {"Mo", 42}, {"Tc", 43}, {"Ru", 44}, {"Rh", 45}, {"Pd", 46}, {"Ag", 47}, {"Cd", 48}, {"In", 49}, {"Sn", 50},
        {"Sb", 51}, {"Te", 52}, {"I", 53}, {"Xe", 54}, {"Cs", 55}, {"Ba", 56}, {"La", 57}, {"Ce", 58}, {"Pr", 59}, {"Nd", 60},
        {"Pm", 61}, {"Sm", 62}, {"Eu", 63}, {"Gd", 64}, {"Tb", 65}, {"Dy", 66}, {"Ho", 67}, {"Er", 68}, {"Tm", 69}, {"Yb", 70},
        {"Lu", 71}, {"Hf", 72}, {"Ta", 73}, {"W", 74}, {"Re", 75}, {"Os", 76}, {"Ir", 77}, {"Pt", 78}, {"Au", 79}, {"Hg", 80},
        {"Tl", 81}, {"Pb", 82}, {"Bi", 83}, {"Po", 84}, {"At", 85}, {"Rn", 86}, {"Fr", 87}, {"Ra", 88}, {"Ac", 89}, {"Th", 90},
        {"Pa", 91}, {"U", 92}, {"Np", 93}, {"Pu", 94}, {"Am", 95}, {"Cm", 96}, {"Bk", 97}, {"Cf", 98}, {"Es", 99}, {"Fm", 100},
        {"Md", 101}, {"No", 102}, {"Lr", 103}, {"Rf", 104}, {"Db", 105}, {"Sg", 106}, {"Bh", 107}, {"Hs", 108}, {"Mt", 109}, {"Ds", 110},
        {"Rg", 111}, {"Cn", 112}, {"Nh", 113}, {"Fl", 114}, {"Mc", 115}, {"Lv", 116}, {"Ts", 117}, {"Og", 118}
    };

    public static string Solve(TaskResponse taskResponse)
    {
        var text = taskResponse.Question;
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrEmpty(l))
                        .ToArray();

        if (lines.Length < 2) return string.Empty;

        var firstLine = lines[0];
        var targetLine = lines[1];

        var elementMatches = Regex.Matches(firstLine, @"\[([A-Za-z]{1,2})\]");

        if (elementMatches.Count > 0)
        {
            var result = new StringBuilder();
            foreach (Match match in elementMatches)
            {
                var symbol = match.Groups[1].Value;
                if (ChemicalElements.TryGetValue(symbol, out int atomNumber))
                {
                    int index = atomNumber - 1; 
                    if (index >= 0 && index < targetLine.Length)
                    {
                        result.Append(targetLine[index]);
                    }
                }
            }
            return result.ToString();
        }

        firstLine = Regex.Replace(firstLine, @"[IVXLCDM]+", match => DecodeRoman(match.Value).ToString());

        if (firstLine.Split(' ').Length > 1)
        {
            var indexes = firstLine.Split(' ').Select(x => int.Parse(x) - 1).ToArray();
            var result = new StringBuilder();
        
            foreach (var i in indexes)
            {
                if (i >= 0 && i < targetLine.Length)
                    result.Append(targetLine[i]);
            }
            return result.ToString();
        }
        else
        {
            var roman = firstLine.Trim();
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