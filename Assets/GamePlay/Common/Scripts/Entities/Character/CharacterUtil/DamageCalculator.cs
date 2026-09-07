using AngelBeat;
using Core.Scripts.Foundation.Define;
using GamePlay.Features.Battle.Scripts.Unit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

public static class DamageCalculator
{
    public static float Evaluate(
        string formula, 
        List<SystemEnum.eStats> inputStats,
        List<SystemEnum.eKeyword> inputKeywords, 
        CharBase client)
    {
        List<long> statInputs = inputStats.Select(stat => client.RuntimeStat.GetStat(stat)).ToList();
        List<float> keywordInputs = inputKeywords.Select(kw => (float)client.KeywordInfo.GetKeywordCount(kw)).ToList();
        return Evaluate(formula, statInputs, keywordInputs);
    }

    public static float Evaluate(string formula, List<long> inputStats, List<float> inputKeywords)
    {
        if(string.IsNullOrWhiteSpace(formula)) return 0f;
        
        formula = Regex.Replace(formula, @"\{(\d+)\}", 
            m => inputStats[int.Parse(m.Groups[1].Value)].ToString(CultureInfo.InvariantCulture));
        formula = Regex.Replace(formula, @"\[(\d+)\]", 
            m => inputKeywords[int.Parse(m.Groups[1].Value)].ToString(CultureInfo.InvariantCulture));
        
        List<string> expr = ExpressionConvert(formula);
        return EvaluateFinal(expr);
    }

    private static List<string> ExpressionConvert(string expr)
    {
        Stack<string> opStack = new();
        List<string> output = new();
        List<string> tokens = Tokenize(expr);

        Dictionary<string, int> precedence = new() { ["+"] = 1, ["-"] = 1, ["*"] = 2, ["/"] = 2 };
        foreach (var token in tokens)
        {
            if(float.TryParse(token, out _))
                output.Add(token);
            else if (token == "(")
                opStack.Push(token);
            else if (token == ")")
            {
                while (opStack.Peek() != "(")
                {
                    output.Add(opStack.Pop());
                }
                opStack.Pop();
            }
            else if (precedence.ContainsKey(token))
            {
                while(opStack.Count > 0 && precedence.TryGetValue(opStack.Peek(), out int p) && p >= precedence[token])
                    output.Add(opStack.Pop());
                opStack.Push(token);
            }
        }
        while(opStack.Count > 0)
            output.Add(opStack.Pop());
        return output;
    }

    private static List<string> Tokenize(string expr)
    {
        List<string> tokens = new();
        Regex regex = new(@"(\d+\.\d+|\d+|[()+\-*/])");
        foreach(Match m in regex.Matches(expr.Replace(" ", "")))
            tokens.Add(m.Value);
        return tokens;
    }

    private static float EvaluateFinal(List<string> expr)
    {
        Stack<float> stack = new();
        foreach (var token in expr)
        {
            if (float.TryParse(token, out float num))
            {
                stack.Push(num);
            }
            else
            {
                float second = stack.Pop();
                float first = stack.Pop();
                stack.Push(token switch
                {
                    "+" => first + second,
                    "-" => first - second,
                    "*" => first * second,
                    "/" => first / second,
                    _ => throw new ArgumentException($"Invalid operator: {token}")
                });
            }
        }
        return stack.Pop();
    }
}