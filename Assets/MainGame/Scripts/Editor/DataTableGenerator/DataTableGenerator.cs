//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using System.IO.Compression;
using System.Xml.Linq;
using System.Linq;

namespace MainGame.Editor.DataTableTools
{
    public sealed class DataTableGenerator
    {
        private const string DataTablePath = "Assets/MainGame/DataTables/Text";
        private const string BytesDataPath = "Assets/MainGame/DataTables/Bytes";
        private const string CSharpCodePath = "Assets/MainGame/Scripts/DataTable";

        private const string CSharpCodeTemplateFileName = "Assets/MainGame/Configs/DataTableCodeTemplate.txt";
        private static readonly Regex EndWithNumberRegex = new Regex(@"\d+$");
        private static readonly Regex NameRegex = new Regex(@"^[A-Z][A-Za-z0-9_]*$");

        public static DataTableProcessor CreateDataTableProcessor(string dataTableName)
        {
            return new DataTableProcessor(Utility.Path.GetRegularPath(Path.Combine(DataTablePath, dataTableName + ".txt")), Encoding.UTF8, 1, 2, null, 3, 4, 1);
        }

        public static bool CheckRawData(DataTableProcessor dataTableProcessor, string dataTableName)
        {
            for (int i = 0; i < dataTableProcessor.RawColumnCount; i++)
            {
                string name = dataTableProcessor.GetName(i);
                if (string.IsNullOrEmpty(name) || name == "#")
                {
                    continue;
                }

                if (!NameRegex.IsMatch(name))
                {
                    Debug.LogWarning(Utility.Text.Format("Check raw data failure. DataTableName='{0}' Name='{1}'", dataTableName, name));
                    return false;
                }
            }

            return true;
        }
        /// <summary>
        /// 生成配置表二进制数据文件
        /// </summary>
        /// <param name="dataTableProcessor"></param>
        /// <param name="dataTableName"></param>
        public static void GenerateDataFile(DataTableProcessor dataTableProcessor, string dataTableName)
        {
            string binaryDataFileName = Utility.Path.GetRegularPath(Path.Combine(BytesDataPath, dataTableName + ".bytes"));
            if (!dataTableProcessor.GenerateDataFile(binaryDataFileName) && File.Exists(binaryDataFileName))
            {
                File.Delete(binaryDataFileName);
            }
        }
        /// <summary>
        /// 生成配置表代码文件
        /// </summary>
        /// <param name="dataTableProcessor"></param>
        /// <param name="dataTableName"></param>
        public static void GenerateCodeFile(DataTableProcessor dataTableProcessor, string dataTableName)
        {
            dataTableProcessor.SetCodeTemplate(CSharpCodeTemplateFileName, Encoding.UTF8);
            dataTableProcessor.SetCodeGenerator(DataTableCodeGenerator);

            string csharpCodeFileName = Utility.Path.GetRegularPath(Path.Combine(CSharpCodePath, "DR" + dataTableName + ".cs"));
            if (!dataTableProcessor.GenerateCodeFile(csharpCodeFileName, Encoding.UTF8, dataTableName) && File.Exists(csharpCodeFileName))
            {
                File.Delete(csharpCodeFileName);
            }
        }

        /// <summary>
        /// 生成配置表枚举文件
        /// </summary>
        /// <param name="dataTableProcessor"></param>
        /// <param name="dataTableName"></param>
        // public static void GenerateEnumFile(DataTableProcessor dataTableProcessor, string dataTableName)
        // {
        //     dataTableProcessor.SetCodeTemplate(CSharpCodeTemplateFileName, Encoding.UTF8);
        //     dataTableProcessor.SetCodeGenerator(DataTableCodeGenerator);
        //     string csharpCodeFileName = Utility.Path.GetRegularPath(Path.Combine(CSharpCodePath, "DR" + dataTableName + "Enum.cs"));
        //     if (!dataTableProcessor.GenerateCodeFile(csharpCodeFileName, Encoding.UTF8, dataTableName) && File.Exists(csharpCodeFileName))
        //     {
        //         File.Delete(csharpCodeFileName);
        //     }
        // }


        private static void DataTableCodeGenerator(DataTableProcessor dataTableProcessor, StringBuilder codeContent, object userData)
        {
            string dataTableName = (string)userData;

            codeContent.Replace("__DATA_TABLE_NAME_SPACE__", "MainGame");
            codeContent.Replace("__DATA_TABLE_CLASS_NAME__", "DR" + dataTableName);
            codeContent.Replace("__DATA_TABLE_COMMENT__", dataTableProcessor.GetValue(0, 1) + "。");
            codeContent.Replace("__DATA_TABLE_ID_COMMENT__", "获取" + dataTableProcessor.GetComment(dataTableProcessor.IdColumn) + "。");
            codeContent.Replace("__DATA_TABLE_PROPERTIES__", GenerateDataTableProperties(dataTableProcessor));
            codeContent.Replace("__DATA_TABLE_ENUM__", GenerateDataTableEnum(dataTableProcessor, dataTableName));
            codeContent.Replace("__DATA_TABLE_PARSER__", GenerateDataTableParser(dataTableProcessor));
            codeContent.Replace("__DATA_TABLE_PROPERTY_ARRAY__", GenerateDataTablePropertyArray(dataTableProcessor));
        }

        private static string GenerateDataTableProperties(DataTableProcessor dataTableProcessor)
        {
            StringBuilder stringBuilder = new StringBuilder();
            bool firstProperty = true;
            for (int i = 0; i < dataTableProcessor.RawColumnCount; i++)
            {
                if (dataTableProcessor.IsCommentColumn(i))
                {
                    // 注释列
                    continue;
                }

                if (dataTableProcessor.IsIdColumn(i))
                {
                    // 编号列
                    continue;
                }

                if (firstProperty)
                {
                    firstProperty = false;
                }
                else
                {
                    stringBuilder.AppendLine().AppendLine();
                }

                stringBuilder
                    .AppendLine("        /// <summary>")
                    .AppendFormat("        /// 获取{0}。", dataTableProcessor.GetComment(i)).AppendLine()
                    .AppendLine("        /// </summary>")
                    .AppendFormat("        public {0} {1}", dataTableProcessor.GetLanguageKeyword(i), dataTableProcessor.GetName(i)).AppendLine()
                    .AppendLine("        {")
                    .AppendLine("            get;")
                    .AppendLine("            private set;")
                    .Append("        }");
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 生成数据表参数枚举
        /// </summary>
        /// <param name="dataTableProcessor"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private static string GenerateDataTableEnum(DataTableProcessor dataTableProcessor, string tableName)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder
                .AppendLine("    /// <summary>")
                .AppendFormat("    /// {0}枚举。", tableName).AppendLine()
                .AppendLine("    /// </summary>")
                .AppendFormat("    public enum {0}Enum : int", tableName).AppendLine()
                .AppendLine("    {")
                .AppendLine("        /// <summary>")
                .AppendLine("        /// 未知。")
                .AppendLine("        /// <summary>")
                .AppendLine("        Unknown = 0,");

            for (int i = 4; i < dataTableProcessor.RawRowCount; i++)
            {
                stringBuilder
                .AppendLine("        /// <summary>")
                .AppendFormat("        /// {0}", dataTableProcessor.GetValue(i, 2)).AppendLine()
                .AppendLine("        /// </summary>")
                .AppendFormat("        {0} = {1},", dataTableProcessor.GetValue(i, 3), dataTableProcessor.GetValue(i, 1)).AppendLine();
            }

            stringBuilder.AppendLine("    }");

            return stringBuilder.ToString();
        }

        private static string GenerateDataTableParser(DataTableProcessor dataTableProcessor)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder
                .AppendLine("        public override bool ParseDataRow(string dataRowString, object userData)")
                .AppendLine("        {")
                .AppendLine("            string[] columnStrings = dataRowString.Split(DataTableExtension.DataSplitSeparators);")
                .AppendLine("            for (int i = 0; i < columnStrings.Length; i++)")
                .AppendLine("            {")
                .AppendLine("                columnStrings[i] = columnStrings[i].Trim(DataTableExtension.DataTrimSeparators);")
                .AppendLine("            }")
                .AppendLine()
                .AppendLine("            int index = 0;");

            for (int i = 0; i < dataTableProcessor.RawColumnCount; i++)
            {
                if (dataTableProcessor.IsCommentColumn(i))
                {
                    // 注释列
                    stringBuilder.AppendLine("            index++;");
                    continue;
                }

                if (dataTableProcessor.IsIdColumn(i))
                {
                    // 编号列
                    stringBuilder.AppendLine("            m_Id = int.Parse(columnStrings[index++]);");
                    continue;
                }

                if (dataTableProcessor.IsSystem(i))
                {
                    string languageKeyword = dataTableProcessor.GetLanguageKeyword(i);
                    if (languageKeyword == "string")
                    {
                        stringBuilder.AppendFormat("            {0} = columnStrings[index++];", dataTableProcessor.GetName(i)).AppendLine();
                    }
                    else
                    {
                        stringBuilder.AppendFormat("            {0} = {1}.Parse(columnStrings[index++]);", dataTableProcessor.GetName(i), languageKeyword).AppendLine();
                    }
                }
                else
                {
                    stringBuilder.AppendFormat("            {0} = DataTableExtension.Parse{1}(columnStrings[index++]);", dataTableProcessor.GetName(i), dataTableProcessor.GetType(i).Name).AppendLine();
                }
            }

            stringBuilder.AppendLine()
                .AppendLine("            GeneratePropertyArray();")
                .AppendLine("            return true;")
                .AppendLine("        }")
                .AppendLine()
                .AppendLine("        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)")
                .AppendLine("        {")
                .AppendLine("            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))")
                .AppendLine("            {")
                .AppendLine("                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))")
                .AppendLine("                {");

            for (int i = 0; i < dataTableProcessor.RawColumnCount; i++)
            {
                if (dataTableProcessor.IsCommentColumn(i))
                {
                    // 注释列
                    continue;
                }

                if (dataTableProcessor.IsIdColumn(i))
                {
                    // 编号列
                    stringBuilder.AppendLine("                    m_Id = binaryReader.Read7BitEncodedInt32();");
                    continue;
                }

                string languageKeyword = dataTableProcessor.GetLanguageKeyword(i);
                if (languageKeyword == "int" || languageKeyword == "uint" || languageKeyword == "long" || languageKeyword == "ulong")
                {
                    stringBuilder.AppendFormat("                    {0} = binaryReader.Read7BitEncoded{1}();", dataTableProcessor.GetName(i), dataTableProcessor.GetType(i).Name).AppendLine();
                }
                else
                {
                    stringBuilder.AppendFormat("                    {0} = binaryReader.Read{1}();", dataTableProcessor.GetName(i), dataTableProcessor.GetType(i).Name).AppendLine();
                }
            }

            stringBuilder
                .AppendLine("                }")
                .AppendLine("            }")
                .AppendLine()
                .AppendLine("            GeneratePropertyArray();")
                .AppendLine("            return true;")
                .Append("        }");

            return stringBuilder.ToString();
        }

        private static string GenerateDataTablePropertyArray(DataTableProcessor dataTableProcessor)
        {
            List<PropertyCollection> propertyCollections = new List<PropertyCollection>();
            for (int i = 0; i < dataTableProcessor.RawColumnCount; i++)
            {
                if (dataTableProcessor.IsCommentColumn(i))
                {
                    // 注释列
                    continue;
                }

                if (dataTableProcessor.IsIdColumn(i))
                {
                    // 编号列
                    continue;
                }

                string name = dataTableProcessor.GetName(i);
                if (!EndWithNumberRegex.IsMatch(name))
                {
                    continue;
                }

                string propertyCollectionName = EndWithNumberRegex.Replace(name, string.Empty);
                int id = int.Parse(EndWithNumberRegex.Match(name).Value);

                PropertyCollection propertyCollection = null;
                foreach (PropertyCollection pc in propertyCollections)
                {
                    if (pc.Name == propertyCollectionName)
                    {
                        propertyCollection = pc;
                        break;
                    }
                }

                if (propertyCollection == null)
                {
                    propertyCollection = new PropertyCollection(propertyCollectionName, dataTableProcessor.GetLanguageKeyword(i));
                    propertyCollections.Add(propertyCollection);
                }

                propertyCollection.AddItem(id, name);
            }

            StringBuilder stringBuilder = new StringBuilder();
            bool firstProperty = true;
            foreach (PropertyCollection propertyCollection in propertyCollections)
            {
                if (firstProperty)
                {
                    firstProperty = false;
                }
                else
                {
                    stringBuilder.AppendLine().AppendLine();
                }

                stringBuilder
                    .AppendFormat("        private KeyValuePair<int, {1}>[] m_{0} = null;", propertyCollection.Name, propertyCollection.LanguageKeyword).AppendLine()
                    .AppendLine()
                    .AppendFormat("        public int {0}Count", propertyCollection.Name).AppendLine()
                    .AppendLine("        {")
                    .AppendLine("            get")
                    .AppendLine("            {")
                    .AppendFormat("                return m_{0}.Length;", propertyCollection.Name).AppendLine()
                    .AppendLine("            }")
                    .AppendLine("        }")
                    .AppendLine()
                    .AppendFormat("        public {1} Get{0}(int id)", propertyCollection.Name, propertyCollection.LanguageKeyword).AppendLine()
                    .AppendLine("        {")
                    .AppendFormat("            foreach (KeyValuePair<int, {1}> i in m_{0})", propertyCollection.Name, propertyCollection.LanguageKeyword).AppendLine()
                    .AppendLine("            {")
                    .AppendLine("                if (i.Key == id)")
                    .AppendLine("                {")
                    .AppendLine("                    return i.Value;")
                    .AppendLine("                }")
                    .AppendLine("            }")
                    .AppendLine()
                    .AppendFormat("            throw new GameFrameworkException(Utility.Text.Format(\"Get{0} with invalid id '{{0}}'.\", id));", propertyCollection.Name).AppendLine()
                    .AppendLine("        }")
                    .AppendLine()
                    .AppendFormat("        public {1} Get{0}At(int index)", propertyCollection.Name, propertyCollection.LanguageKeyword).AppendLine()
                    .AppendLine("        {")
                    .AppendFormat("            if (index < 0 || index >= m_{0}.Length)", propertyCollection.Name).AppendLine()
                    .AppendLine("            {")
                    .AppendFormat("                throw new GameFrameworkException(Utility.Text.Format(\"Get{0}At with invalid index '{{0}}'.\", index));", propertyCollection.Name).AppendLine()
                    .AppendLine("            }")
                    .AppendLine()
                    .AppendFormat("            return m_{0}[index].Value;", propertyCollection.Name).AppendLine()
                    .Append("        }");
            }

            if (propertyCollections.Count > 0)
            {
                stringBuilder.AppendLine().AppendLine();
            }

            stringBuilder
                .AppendLine("        private void GeneratePropertyArray()")
                .AppendLine("        {");

            firstProperty = true;
            foreach (PropertyCollection propertyCollection in propertyCollections)
            {
                if (firstProperty)
                {
                    firstProperty = false;
                }
                else
                {
                    stringBuilder.AppendLine().AppendLine();
                }

                stringBuilder
                    .AppendFormat("            m_{0} = new KeyValuePair<int, {1}>[]", propertyCollection.Name, propertyCollection.LanguageKeyword).AppendLine()
                    .AppendLine("            {");

                int itemCount = propertyCollection.ItemCount;
                for (int i = 0; i < itemCount; i++)
                {
                    KeyValuePair<int, string> item = propertyCollection.GetItem(i);
                    stringBuilder.AppendFormat("                new KeyValuePair<int, {0}>({1}, {2}),", propertyCollection.LanguageKeyword, item.Key.ToString(), item.Value).AppendLine();
                }

                stringBuilder.Append("            };");
            }

            stringBuilder
                .AppendLine()
                .Append("        }");

            return stringBuilder.ToString();
        }

        private sealed class PropertyCollection
        {
            private readonly string m_Name;
            private readonly string m_LanguageKeyword;
            private readonly List<KeyValuePair<int, string>> m_Items;

            public PropertyCollection(string name, string languageKeyword)
            {
                m_Name = name;
                m_LanguageKeyword = languageKeyword;
                m_Items = new List<KeyValuePair<int, string>>();
            }

            public string Name
            {
                get
                {
                    return m_Name;
                }
            }

            public string LanguageKeyword
            {
                get
                {
                    return m_LanguageKeyword;
                }
            }

            public int ItemCount
            {
                get
                {
                    return m_Items.Count;
                }
            }

            public KeyValuePair<int, string> GetItem(int index)
            {
                if (index < 0 || index >= m_Items.Count)
                {
                    throw new GameFrameworkException(Utility.Text.Format("GetItem with invalid index '{0}'.", index));
                }

                return m_Items[index];
            }

            public void AddItem(int id, string propertyName)
            {
                m_Items.Add(new KeyValuePair<int, string>(id, propertyName));
            }
        }
    

        #region Excel Open XML 解析

        /// <summary>
        /// 从 Excel 文件中读取所有工作表名称
        /// </summary>
        public static List<string> GetSheetNamesFromExcel(string excelFilePath)
        {
            var sheetNames = new List<string>();

            using (var fileStream = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var zip = new ZipArchive(fileStream, ZipArchiveMode.Read))
            {
                var workbookEntry = zip.GetEntry("xl/workbook.xml");
                if (workbookEntry == null) return sheetNames;

                using (var stream = workbookEntry.Open())
                {
                    var doc = XDocument.Load(stream);
                    XNamespace ns = doc.Root.GetDefaultNamespace();

                    var sheets = doc.Root.Element(ns + "sheets")?.Elements(ns + "sheet");
                    if (sheets != null)
                    {
                        foreach (var sheet in sheets)
                        {
                            string name = sheet.Attribute("name")?.Value;
                            if (!string.IsNullOrEmpty(name))
                                sheetNames.Add(name);
                        }
                    }
                }
            }

            return sheetNames;
        }

        /// <summary>
        /// 从 Excel 文件导出所有工作表为制表符分隔的 txt 文件
        /// </summary>
        public static int ExportSheetsFromExcel(string excelFilePath, string textFolder, HashSet<string> duplicateSheetNames)
        {
            int count = 0;
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(excelFilePath);

            using (var fileStream = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var zip = new ZipArchive(fileStream, ZipArchiveMode.Read))
            {
                // 读取共享字符串表
                var sharedStrings = ReadSharedStrings(zip);

                // 读取工作簿获取工作表名称和 rId 映射
                var sheetInfos = ReadWorkbookSheets(zip);

                // 读取关系文件获取 rId 到实际文件路径的映射
                var relationships = ReadWorkbookRelationships(zip);

                foreach (var sheetInfo in sheetInfos)
                {
                    if (!relationships.TryGetValue(sheetInfo.RId, out string sheetRelPath))
                        continue;

                    // 关系中的路径是相对于 xl/ 目录的
                    string entryPath = "xl/" + sheetRelPath;
                    var sheetEntry = zip.GetEntry(entryPath);
                    if (sheetEntry == null)
                    {
                        sheetEntry = zip.GetEntry(sheetRelPath);
                        if (sheetEntry == null)
                        {
                            Debug.LogWarning($"找不到工作表文件: {entryPath}");
                            continue;
                        }
                    }

                    // 读取工作表内容并转换为制表符分隔格式
                    string tsvContent = ReadSheetAsTsv(sheetEntry, sharedStrings);

                    // 确定输出文件名：重名工作表添加 Excel 文件名后缀
                    string outputFileName;
                    if (duplicateSheetNames.Contains(sheetInfo.Name))
                    {
                        outputFileName = $"{sheetInfo.Name}_{fileNameWithoutExt}.txt";
                    }
                    else
                    {
                        outputFileName = $"{sheetInfo.Name}.txt";
                    }

                    string outputPath = Path.Combine(textFolder, outputFileName);
                    File.WriteAllText(outputPath, tsvContent, new UTF8Encoding(false));
                    Debug.Log($"已导出: {outputFileName}");
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 读取共享字符串表（xl/sharedStrings.xml）
        /// </summary>
        private static List<string> ReadSharedStrings(ZipArchive zip)
        {
            var strings = new List<string>();
            var entry = zip.GetEntry("xl/sharedStrings.xml");
            if (entry == null) return strings;

            using (var stream = entry.Open())
            {
                var doc = XDocument.Load(stream);
                XNamespace ns = doc.Root.GetDefaultNamespace();

                foreach (var si in doc.Root.Elements(ns + "si"))
                {
                    // 简单文本
                    var t = si.Element(ns + "t");
                    if (t != null)
                    {
                        strings.Add(t.Value);
                    }
                    else
                    {
                        // 富文本：拼接所有 <r> 中的 <t> 元素
                        var sb = new StringBuilder();
                        foreach (var r in si.Elements(ns + "r"))
                        {
                            var rt = r.Element(ns + "t");
                            if (rt != null) sb.Append(rt.Value);
                        }
                        strings.Add(sb.ToString());
                    }
                }
            }

            return strings;
        }

        private struct SheetInfo
        {
            public string Name;
            public string RId;
        }

        /// <summary>
        /// 读取工作簿中的工作表定义（xl/workbook.xml）
        /// </summary>
        private static List<SheetInfo> ReadWorkbookSheets(ZipArchive zip)
        {
            var sheets = new List<SheetInfo>();
            var entry = zip.GetEntry("xl/workbook.xml");
            if (entry == null) return sheets;

            using (var stream = entry.Open())
            {
                var doc = XDocument.Load(stream);
                XNamespace ns = doc.Root.GetDefaultNamespace();
                XNamespace rNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

                var sheetsElement = doc.Root.Element(ns + "sheets");
                if (sheetsElement != null)
                {
                    foreach (var sheet in sheetsElement.Elements(ns + "sheet"))
                    {
                        sheets.Add(new SheetInfo
                        {
                            Name = sheet.Attribute("name")?.Value ?? "",
                            RId = sheet.Attribute(rNs + "id")?.Value ?? ""
                        });
                    }
                }
            }

            return sheets;
        }

        /// <summary>
        /// 读取工作簿关系文件（xl/_rels/workbook.xml.rels）
        /// </summary>
        private static Dictionary<string, string> ReadWorkbookRelationships(ZipArchive zip)
        {
            var rels = new Dictionary<string, string>();
            var entry = zip.GetEntry("xl/_rels/workbook.xml.rels");
            if (entry == null) return rels;

            using (var stream = entry.Open())
            {
                var doc = XDocument.Load(stream);
                XNamespace ns = doc.Root.GetDefaultNamespace();

                foreach (var rel in doc.Root.Elements(ns + "Relationship"))
                {
                    string id = rel.Attribute("Id")?.Value;
                    string target = rel.Attribute("Target")?.Value;
                    if (id != null && target != null)
                    {
                        rels[id] = target;
                    }
                }
            }

            return rels;
        }

        /// <summary>
        /// 将工作表内容读取为制表符分隔的字符串
        /// </summary>
        private static string ReadSheetAsTsv(ZipArchiveEntry sheetEntry, List<string> sharedStrings)
        {
            var sb = new StringBuilder();

            using (var stream = sheetEntry.Open())
            {
                var doc = XDocument.Load(stream);
                XNamespace ns = doc.Root.GetDefaultNamespace();

                var sheetData = doc.Root.Element(ns + "sheetData");
                if (sheetData == null) return "";

                // 收集所有行，确定最大行号
                var rows = sheetData.Elements(ns + "row").ToList();
                if (rows.Count == 0) return "";

                foreach (var row in rows)
                {
                    var cells = row.Elements(ns + "c").ToList();
                    if (cells.Count == 0)
                    {
                        sb.AppendLine();
                        continue;
                    }

                    // 解析每个单元格的列索引和值
                    int maxCol = 0;
                    var cellValues = new Dictionary<int, string>();

                    foreach (var cell in cells)
                    {
                        string cellRef = cell.Attribute("r")?.Value;
                        if (string.IsNullOrEmpty(cellRef)) continue;

                        int colIndex = ParseColumnIndex(cellRef);
                        if (colIndex > maxCol) maxCol = colIndex;

                        string value = GetCellValue(cell, ns, sharedStrings);
                        cellValues[colIndex] = value;
                    }

                    // 构建制表符分隔的行
                    var rowValues = new string[maxCol + 1];
                    for (int i = 0; i <= maxCol; i++)
                    {
                        rowValues[i] = cellValues.ContainsKey(i) ? cellValues[i] : "";
                    }

                    sb.AppendLine(string.Join("\t", rowValues));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 获取单元格的值
        /// </summary>
        private static string GetCellValue(XElement cell, XNamespace ns, List<string> sharedStrings)
        {
            string cellType = cell.Attribute("t")?.Value;
            var valueElement = cell.Element(ns + "v");
            string rawValue = valueElement?.Value ?? "";

            switch (cellType)
            {
                case "s": // 共享字符串
                    if (int.TryParse(rawValue, out int ssIndex) && ssIndex < sharedStrings.Count)
                        return sharedStrings[ssIndex];
                    return "";

                case "inlineStr": // 内联字符串
                    var isElement = cell.Element(ns + "is");
                    var tElement = isElement?.Element(ns + "t");
                    if (tElement != null)
                        return tElement.Value;
                    // 富文本内联字符串
                    if (isElement != null)
                    {
                        var inlineSb = new StringBuilder();
                        foreach (var r in isElement.Elements(ns + "r"))
                        {
                            var rt = r.Element(ns + "t");
                            if (rt != null) inlineSb.Append(rt.Value);
                        }
                        return inlineSb.ToString();
                    }
                    return "";

                case "b": // 布尔值
                    return rawValue == "1" ? "TRUE" : "FALSE";

                case "e": // 错误值
                    return rawValue;

                default: // 数字或其他
                    return rawValue;
            }
        }

        /// <summary>
        /// 从单元格引用（如 "A1", "BC23"）解析出列索引（0-based）
        /// </summary>
        private static int ParseColumnIndex(string cellReference)
        {
            int col = 0;
            for (int i = 0; i < cellReference.Length; i++)
            {
                char c = cellReference[i];
                if (c >= 'A' && c <= 'Z')
                {
                    col = col * 26 + (c - 'A' + 1);
                }
                else
                {
                    break;
                }
            }
            return col - 1;
        }

        #endregion
    
    
    }
}
