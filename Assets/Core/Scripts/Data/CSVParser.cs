using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Core.Scripts.Data
{
    public class CSVParser
    {
        private static readonly Regex CsvRegex = new Regex(@"
        (?:^|,)
        (?:
            ""(?<Text>(?:[^""]|"""")*)""
            |
            (?<Text>[^,]*) 
        )
        (?=,|$)",
            RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace
        );

        public static string[] Parse(string line)
        {
            var fields = new List<string>();
            foreach (Match m in CsvRegex.Matches(line))
            {
                string value = m.Groups["Text"].Value;
                value = value.Replace("\"\"", "\"");
                fields.Add(value);
            }
            return fields.ToArray();
        }
    }
}