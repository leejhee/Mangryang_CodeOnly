using Core.Scripts.Data;
using Core.Scripts.Foundation.Define;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Tests
{
    [Category("StaticData")]
    public class StaticDataConsistencyTest
    {
        private const string DataGroupName = "DataTable";
        private const string AddressPrefix = "CSV/MEMCSV/";

        private string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private string XlsxRoot => Path.Combine(ProjectRoot, "DataGenerator", "XLSXS");
        private string CsvRoot => Path.Combine(
            Application.dataPath,
            "GamePlay", "Common", "CommonResources", "CSV", "MEMCSV");
        private string DataSourceRoot => Path.Combine(Application.dataPath, "Core", "Scripts", "Data");

        [Test]
        public void DataTableArtifacts_AreAligned()
        {
            string[] runtimeTables = GetRuntimeTableNames();
            string[] xlsxTables = GetFileNames(XlsxRoot, "*.xlsx");
            string[] csvTables = GetFileNames(CsvRoot, "*.csv");
            string[] sourceTables = GetFileNames(DataSourceRoot, "*Data.cs")
                .Where(name => name != nameof(SheetData))
                .ToArray();
            string[] addressableTables = GetDataTableEntries()
                .Select(entry => entry.address.Substring(AddressPrefix.Length))
                .OrderBy(name => name)
                .ToArray();

            Assert.That(xlsxTables, Is.EquivalentTo(runtimeTables), "XLSX 원본과 런타임 SheetData 타입이 다릅니다.");
            Assert.That(csvTables, Is.EquivalentTo(runtimeTables), "CSV 생성물과 런타임 SheetData 타입이 다릅니다.");
            Assert.That(sourceTables, Is.EquivalentTo(runtimeTables), "SheetData C# 소스가 누락되었습니다.");
            Assert.That(addressableTables, Is.EquivalentTo(runtimeTables), "Addressables DataTable 항목이 다릅니다.");
        }

        [Test, Timeout(15000)]
        public void AllCsvTables_HaveMatchingSchemaAndParseEveryRow()
        {
            foreach (Type tableType in GetRuntimeTableTypes())
            {
                string csvPath = Path.Combine(CsvRoot, tableType.Name + ".csv");
                string csv = File.ReadAllText(csvPath).TrimStart('\uFEFF');
                string[] lines = csv.Split('\n');

                Assert.That(lines.Length, Is.GreaterThanOrEqualTo(4), $"{tableType.Name}: 헤더 또는 데이터 행이 없습니다.");
                AssertSchemaMatches(tableType, lines);

                string[] dataLines = lines
                    .Skip(3)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToArray();
                long[] ids = dataLines
                    .Select(line => long.Parse(CSVParser.Parse(line.Trim())[0]))
                    .ToArray();

                Assert.That(ids, Has.All.GreaterThan(0), $"{tableType.Name}: ID는 0보다 커야 합니다.");
                Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Length), $"{tableType.Name}: 중복 ID가 있습니다.");

                var parser = (SheetData)Activator.CreateInstance(tableType);
                Dictionary<long, SheetData> parsed = parser.ParseAsync(csv, CancellationToken.None)
                    .GetAwaiter().GetResult();

                Assert.That(parsed, Is.Not.Null.And.Not.Empty, $"{tableType.Name}: 파싱 결과가 비었습니다.");
                Assert.That(parsed.Count, Is.EqualTo(dataLines.Length), $"{tableType.Name}: 일부 행이 파싱되지 않았습니다.");
            }
        }

        [Test]
        public void AddressableEntries_PointToExpectedCsvAssets()
        {
            foreach (AddressableAssetEntry entry in GetDataTableEntries())
            {
                string tableName = entry.address.Substring(AddressPrefix.Length);
                string expectedPath = $"Assets/GamePlay/Common/CommonResources/CSV/MEMCSV/{tableName}.csv";

                Assert.That(entry.AssetPath.Replace('\\', '/'), Is.EqualTo(expectedPath),
                    $"{entry.address}: Addressable 항목이 잘못된 CSV를 가리킵니다.");
            }
        }

        private static Type[] GetRuntimeTableTypes()
        {
            return typeof(SheetData).Assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(SheetData)))
                .OrderBy(type => type.Name)
                .ToArray();
        }

        private static string[] GetRuntimeTableNames() =>
            GetRuntimeTableTypes().Select(type => type.Name).ToArray();

        private static string[] GetFileNames(string root, string pattern)
        {
            return Directory.GetFiles(root, pattern)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !name.StartsWith("~$", StringComparison.Ordinal))
                .OrderBy(name => name)
                .ToArray();
        }

        private static AddressableAssetEntry[] GetDataTableEntries()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            Assert.That(settings, Is.Not.Null, "AddressableAssetSettings를 찾을 수 없습니다.");

            AddressableAssetGroup group = settings.FindGroup(DataGroupName);
            Assert.That(group, Is.Not.Null, $"Addressables 그룹 '{DataGroupName}'을 찾을 수 없습니다.");

            AddressableAssetEntry[] entries = group.entries
                .Where(entry => entry.address.StartsWith(AddressPrefix, StringComparison.Ordinal))
                .OrderBy(entry => entry.address)
                .ToArray();
            Assert.That(entries.Select(entry => entry.address).Distinct().Count(), Is.EqualTo(entries.Length),
                "중복 Addressable 주소가 있습니다.");
            return entries;
        }

        private static void AssertSchemaMatches(Type tableType, string[] lines)
        {
            string[] fieldNames = CSVParser.Parse(lines[1].Trim());
            string[] typeNames = CSVParser.Parse(lines[2].Trim());
            FieldInfo[] fields = tableType.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            var fieldsByName = fields.ToDictionary(field => field.Name);

            int columnCount = Math.Min(fieldNames.Length, typeNames.Length);
            for (int i = 0; i < columnCount; i++)
            {
                string fieldName = fieldNames[i];
                string typeName = typeNames[i];
                if (string.IsNullOrWhiteSpace(fieldName) || string.IsNullOrWhiteSpace(typeName) ||
                    fieldName.StartsWith("#", StringComparison.Ordinal) ||
                    typeName.StartsWith("#", StringComparison.Ordinal))
                    continue;

                Assert.That(fieldsByName.TryGetValue(fieldName, out FieldInfo field), Is.True,
                    $"{tableType.Name}: CSV 필드 '{fieldName}'에 대응하는 C# 필드가 없습니다.");
                Assert.That(field.FieldType, Is.EqualTo(ResolveFieldType(typeName)),
                    $"{tableType.Name}.{fieldName}: CSV 타입 '{typeName}'과 C# 타입이 다릅니다.");
            }
        }

        private static Type ResolveFieldType(string typeName)
        {
            if (typeName.EndsWith("[]", StringComparison.Ordinal))
            {
                Type elementType = ResolveFieldType(typeName.Substring(0, typeName.Length - 2));
                return typeof(List<>).MakeGenericType(elementType);
            }

            switch (typeName.ToLowerInvariant())
            {
                case "bool": return typeof(bool);
                case "short": return typeof(short);
                case "ushort": return typeof(ushort);
                case "int": return typeof(int);
                case "uint": return typeof(uint);
                case "long": return typeof(long);
                case "ulong": return typeof(ulong);
                case "float": return typeof(float);
                case "double": return typeof(double);
                case "string": return typeof(string);
            }

            Type enumType = typeof(SystemEnum).GetNestedType(typeName, BindingFlags.Public);
            Assert.That(enumType, Is.Not.Null, $"SystemEnum.{typeName}을 찾을 수 없습니다.");
            Assert.That(enumType.IsEnum, Is.True, $"SystemEnum.{typeName}은 enum 타입이 아닙니다.");
            return enumType;
        }
    }
}
