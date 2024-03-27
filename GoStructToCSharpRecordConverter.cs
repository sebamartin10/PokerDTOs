using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

class GoStructToCSharpRecordConverter
{
    public static void ConvertGoStructsToCSharpRecords(string filePath)
    {
        string fileContent = File.ReadAllText(filePath);
        string outputFileName = Path.GetFileNameWithoutExtension(filePath) + ".cs";
        using (StreamWriter sw = new StreamWriter(outputFileName))
        {
            var structMatches = Regex.Matches(fileContent, @"type\s+(\w+)\s+struct\s+{([^}]+)}", RegexOptions.Singleline);
            sw.WriteLine($"using Newtonsoft.Json;\nusing System.Collections.Generic;\nusing System;\n");
            sw.WriteLine($"namespace PokerDTOs\n{{");
            foreach (Match match in structMatches)
            {
                string structName = match.Groups[1].Value.Trim();
                string fields = match.Groups[2].Value.Trim();
                var fieldMatches = Regex.Matches(fields, @"((\w+)\s+(\[\])?(\w+)\s*\`json:\""([^\""]+)\"".*?\`)", RegexOptions.Singleline);
                var fieldMatches2 = Regex.Matches(fields, @"(\w+)\s+(\[\])?(\w+)(\s*\`json:\""([^\""]+)\""[^`]*\`)?", RegexOptions.Singleline);
                List<string> csharpProperties = new List<string>();

                foreach (Match fieldMatch in fieldMatches2)
                {
                    string brackets = fieldMatch.Groups[2].Value.Trim();
                    string goType = fieldMatch.Groups[3].Value.Trim();
                    string jsonTagName = fieldMatch.Groups[5].Success ? fieldMatch.Groups[5].Value : string.Empty;
                    if (jsonTagName != string.Empty)
                    {
                        csharpProperties.Add($"\n\t\t[JsonProperty(\"{jsonTagName}\")]");
                    }
                    string fieldType = ConvertGoTypeToCSharp($"{fieldMatch.Groups[2].Value.Trim()}{fieldMatch.Groups[3].Value.Trim()}");
                    string fieldName = fieldMatch.Groups[1].Value.Trim();
                    csharpProperties.Add($"\t{fieldType} {fieldName}");
                }

                string recordTemplate = $"\tpublic record {structName}\n\t{{\n    {string.Join(";\n    ", csharpProperties)};\n}};\n";
                sw.WriteLine(recordTemplate);
            }
            sw.WriteLine($"}}");
        }
        Console.WriteLine($"Conversion complete. Output file: {outputFileName}");
    }

    private static string ConvertGoTypeToCSharp(string goType)
    {
        var typeMapping = new Dictionary<string, string>
        {
            { "int", "int" },
            { "int8", "sbyte" },
            { "int32", "int" },
            { "int64", "long" },
            { "string", "string" },
            { "bool", "bool" },
            { "Card", "Card" },
            { "uuid", "Guid" }
        };

        if (goType.StartsWith("[]"))
        {
            // Handle slices to List<T>
            string innerType = ConvertGoTypeToCSharp(goType.Substring(2));
            return $"List<{innerType}>";
        }
        else if (typeMapping.ContainsKey(goType))
        {
            return typeMapping[goType];
        }
        // Add other conversions as necessary

        return goType; // Fallback if no mapping found
    }
}

class Program
{
    static void Main(string[] args)
    {
        // if (args.Length != 1)
        // {
        //     Console.WriteLine("Usage: GoStructToCSharpRecordConverter <path-to-go-file>");
        //     return;
        // }

        GoStructToCSharpRecordConverter.ConvertGoStructsToCSharpRecords("..\\..\\..\\..\\models.go");
    }
}
