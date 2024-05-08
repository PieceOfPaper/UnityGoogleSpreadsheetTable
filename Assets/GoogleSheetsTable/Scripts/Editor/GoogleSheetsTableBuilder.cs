using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace GoogleSheetsTable
{
    public class GoogleSheetsTableBuilder : EditorWindow
    {
        [UnityEditor.MenuItem("Tools/Google Sheets Table/Open Builder")]
        private static void OpenWindow()
        {
            var window = EditorWindow.GetWindow<GoogleSheetsTableBuilder>();
        }

        private const string GENERATE_CODE_TEMP_PATH = "Temp/GoogleSheetsTable/Scripts";
        private const string GENERATE_BINARY_TEMP_PATH = "Temp/GoogleSheetsTable/Binary";

        public GoogleSheetsSetting m_Setting;
        public Vector2 m_TablesScroll;
        
        private bool m_IsEnableGoogleSheetAPI;
        private bool m_IsSettingModified;

        private string m_ExportCodePath;
        private string m_ExportCodeFullPath;
        private string m_ExportCodeTempPath;
        
        private string m_ExportBinaryPath;
        private string m_ExportBinaryFullPath;
        private string m_ExportBinaryTempPath;

        private List<GoogleSheetsSetting.Table> m_RequestGenerateTableList = new List<GoogleSheetsSetting.Table>();
        private List<GoogleSheetsSetting.Table> m_GeneratedTableList = new List<GoogleSheetsSetting.Table>();



        private void OnEnable()
        {
            m_ExportCodeTempPath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), GENERATE_CODE_TEMP_PATH);
            m_ExportBinaryTempPath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), GENERATE_BINARY_TEMP_PATH);

            var lastSettingGUID = EditorPrefs.GetString("GoogleSheetsTableBuilder_LastSettingGUID");
            var lastSettingPath = string.IsNullOrWhiteSpace(lastSettingGUID) ? string.Empty : AssetDatabase.GUIDToAssetPath(lastSettingGUID);
            m_Setting = string.IsNullOrWhiteSpace(lastSettingPath) ? null : AssetDatabase.LoadAssetAtPath<GoogleSheetsSetting>(lastSettingPath);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            var inputSetting = (GoogleSheetsSetting)EditorGUILayout.ObjectField("Setting", m_Setting, typeof(GoogleSheetsSetting), false);
            if (GUILayout.Button("Create", GUILayout.ExpandWidth(false)))
            {
                var path = EditorUtility.SaveFilePanel("Create Setting", Application.dataPath, "Setting", "asset");
                if (string.IsNullOrWhiteSpace(path) == false && path.StartsWith(Application.dataPath))
                {
                    var asset = ScriptableObject.CreateInstance<GoogleSheetsSetting>();
                    AssetDatabase.CreateAsset(asset, path.Replace(Application.dataPath, "Assets"));
                    inputSetting = asset;
                }
            }
            EditorGUILayout.EndHorizontal();
            if (m_Setting != inputSetting)
            {
                m_Setting = inputSetting;
                if (m_Setting != null)
                {
                    var settingPath = AssetDatabase.GetAssetPath(m_Setting);
                    var settingGUID = AssetDatabase.GUIDFromAssetPath(settingPath);
                    EditorPrefs.SetString("GoogleSheetsTableBuilder_LastSettingGUID", settingGUID.ToString());
                }
            }
            if (m_Setting == null)
            {
                EditorGUILayout.HelpBox("Need Setting", MessageType.Error);
                return;
            }
            
            m_IsEnableGoogleSheetAPI = GoogleSheetsAPI.Instance.IsCertificating == false && GoogleSheetsAPI.Instance.IsCertificated == true;

            m_ExportCodePath = EditorGUILayout.TextField("Export Code Path", m_Setting.exportCodePath);
            m_ExportBinaryPath = EditorGUILayout.TextField("Export Binary Path", m_Setting.exportBinaryPath);
            if (m_ExportCodePath != m_Setting.exportCodePath) m_IsSettingModified = true;
            if (m_ExportBinaryPath != m_Setting.exportBinaryPath) m_IsSettingModified = true;
            m_ExportCodePath = m_Setting.exportCodePath;
            m_ExportCodeFullPath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), m_ExportCodePath);
            m_ExportBinaryPath = m_Setting.exportBinaryPath;
            m_ExportBinaryFullPath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), m_ExportBinaryPath);

            EditorGUILayout.Space();
            OnGUI_Certificate();
            EditorGUILayout.Space();
            OnGUI_Tables();

            if (m_IsSettingModified == true)
            {
                m_IsSettingModified = false;
                EditorUtility.SetDirty(m_Setting);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if (m_RequestGenerateTableList.Count > 0)
            {
                if (m_RequestGenerateTableList.Count <= m_GeneratedTableList.Count)
                {
                    EditorUtility.ClearProgressBar();
                    m_RequestGenerateTableList.Clear();
                    m_GeneratedTableList.Clear();
                    
                    System.IO.Directory.CreateDirectory(m_ExportCodeFullPath);
                    System.IO.Directory.CreateDirectory(m_ExportBinaryFullPath);
                    var tempCodeDirectoryInfo = new System.IO.DirectoryInfo(m_ExportCodeTempPath);
                    var codeFileInfos = tempCodeDirectoryInfo.GetFiles();
                    foreach (var fileInfo in codeFileInfos)
                    {
                        System.IO.File.Copy(fileInfo.FullName, System.IO.Path.Combine(m_ExportCodeFullPath, fileInfo.Name), true);
                    }
                    var tempExportDirectoryInfo = new System.IO.DirectoryInfo(m_ExportBinaryTempPath);
                    var exportFileInfos = tempExportDirectoryInfo.GetFiles();
                    foreach (var fileInfo in exportFileInfos)
                    {
                        System.IO.File.Copy(fileInfo.FullName, System.IO.Path.Combine(m_ExportBinaryFullPath, fileInfo.Name), true);
                    }
                    AssetDatabase.Refresh();
                }
                else
                {
                    EditorUtility.DisplayProgressBar("Google Sheets Table Builder", $"Generating Table ...", (float)m_GeneratedTableList.Count / m_RequestGenerateTableList.Count);
                }
            }
            
            Repaint();
        }

        private void OnGUI_Certificate()
        {
            GUILayout.Label("Certificate", EditorStyles.boldLabel);
            
            var googleClientSecretsPath = EditorGUILayout.DelayedTextField("Client Secrets", m_Setting.googleClientSecretsPath);
            if (m_Setting.googleClientSecretsPath != googleClientSecretsPath)
            {
                m_Setting.googleClientSecretsPath = googleClientSecretsPath;
                m_IsSettingModified = true;
            }
            
            EditorGUILayout.BeginHorizontal();
            var stateMessage = "Need Certificated";
            var stateMessageType = MessageType.Error;
            if (GoogleSheetsAPI.Instance.IsCertificating)
            {
                stateMessage = "Certificating";
                stateMessageType = MessageType.Warning;
            }
            else if (GoogleSheetsAPI.Instance.IsCertificated)
            {
                stateMessage = "Certificated";
                stateMessageType = MessageType.Info;
            }
            EditorGUILayout.HelpBox(stateMessage, stateMessageType);
            using (new EditorGUI.DisabledScope(GoogleSheetsAPI.Instance.IsCertificating == true || GoogleSheetsAPI.Instance.IsCertificated == true))
            {
                if (GUILayout.Button("Certificate", GUILayout.ExpandWidth(false)))
                {
                    if (m_Setting.googleClientSecretsPath.StartsWith("Assets/"))
                    {
                        var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(m_Setting.googleClientSecretsPath);
                        GoogleSheetsAPI.Instance.Certificate(textAsset.text);   
                    }
                    else
                    {
                        var text = System.IO.File.ReadAllText(m_Setting.googleClientSecretsPath);
                        GoogleSheetsAPI.Instance.Certificate(text);   
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void OnGUI_Tables()
        {
            GUILayout.Label("Tables", EditorStyles.boldLabel);
            
            var tableList = new List<GoogleSheetsSetting.Table>();
            if (m_Setting.tableSettings != null) tableList.AddRange(m_Setting.tableSettings);

            int removeIndex = -1;
            m_TablesScroll = EditorGUILayout.BeginScrollView(m_TablesScroll);
            for (int i = 0; i < tableList.Count; i ++)
            {
                var data = tableList[i];
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical(GUILayout.Width(20f));
                using (new EditorGUI.DisabledScope(i == 0))
                {
                    if (GUILayout.Button("▲"))
                    {
                        GUI.FocusControl(null);
                        var temp = tableList[i - 1];
                        tableList[i - 1] = data;
                        tableList[i] = temp;
                        m_IsSettingModified = true;
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndHorizontal();
                        continue;
                    }
                }
                using (new EditorGUI.DisabledScope((i + 1) >= tableList.Count))
                {
                    if (GUILayout.Button("▼"))
                    {
                        GUI.FocusControl(null);
                        var temp = tableList[i + 1];
                        tableList[i + 1] = data;
                        tableList[i] = temp;
                        m_IsSettingModified = true;
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndHorizontal();
                        continue;
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical(GUILayout.Width(100f));
                GUILayout.Label("Table Name");
                GUILayout.Label("Spreadsheet ID");
                GUILayout.Label("Sheet Name");
                GUILayout.Label("Data Name");
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                data.tableName = EditorGUILayout.TextField(data.tableName, GUILayout.ExpandWidth(true));
                data.spreadsheetId = EditorGUILayout.TextField(data.spreadsheetId, GUILayout.ExpandWidth(true));
                data.sheetName = EditorGUILayout.TextField(data.sheetName, GUILayout.ExpandWidth(true));
                data.dataRange = EditorGUILayout.TextField(data.dataRange, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical(GUILayout.Width(100f));
                if (GUILayout.Button("Delete"))
                {
                    GUI.FocusControl(null);
                    removeIndex = i;
                }
                using (new EditorGUI.DisabledScope(m_IsEnableGoogleSheetAPI == false || m_RequestGenerateTableList.Count > 0))
                {
                    if (GUILayout.Button("Generate"))
                    {
                        GUI.FocusControl(null);
                        GenerateTable(data);
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                if ((i + 1) < tableList.Count)
                    EditorGUILayout.Space();

                if (tableList[i].Equals(data) == false)
                {
                    tableList[i] = data;
                    m_IsSettingModified = true;
                }
            }
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add", GUILayout.ExpandWidth(false)))
            {
                GUI.FocusControl(null);
                tableList.Add(new GoogleSheetsSetting.Table());
                m_IsSettingModified = true;
            }
            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(m_IsEnableGoogleSheetAPI == false || m_RequestGenerateTableList.Count > 0))
            {
                if (GUILayout.Button("Generate All"))
                {
                    GUI.FocusControl(null);
                    GenerateTables(tableList);
                }
            }

            EditorGUILayout.EndScrollView();

            if (removeIndex >= 0)
            {
                tableList.RemoveAt(removeIndex);
                m_IsSettingModified = true;
            }

            m_Setting.tableSettings = tableList.ToArray();
        }


        private void GenerateTable(GoogleSheetsSetting.Table table) => GenerateTables(new [] { table });
        
        private void GenerateTables(IEnumerable<GoogleSheetsSetting.Table> tables)
        {
            m_RequestGenerateTableList.Clear();
            m_GeneratedTableList.Clear();

            System.IO.Directory.CreateDirectory(m_ExportCodeTempPath);
            System.IO.Directory.CreateDirectory(m_ExportBinaryTempPath);
            var tempCodeDirectoryInfo = new System.IO.DirectoryInfo(m_ExportCodeTempPath);
            var codeFileInfos = tempCodeDirectoryInfo.GetFiles();
            foreach (var fileInfo in codeFileInfos)
                System.IO.File.Delete(fileInfo.FullName);
            var tempExportDirectoryInfo = new System.IO.DirectoryInfo(m_ExportBinaryTempPath);
            var exportFileInfos = tempExportDirectoryInfo.GetFiles();
            foreach (var fileInfo in exportFileInfos)
                System.IO.File.Delete(fileInfo.FullName);

            if (tables != null)
            {
                m_RequestGenerateTableList.AddRange(tables);
                foreach (var table in tables)
                    _GenerateTable(table);
            }
        }

        private void _GenerateTable(GoogleSheetsSetting.Table table)
        {
            GoogleSheetsAPI.Instance.RequestTable(table.spreadsheetId,
                $"{table.sheetName}!{table.dataRange}",
                values =>
                {
                    if (values == null || values.Count == 0)
                    {
                        lock (m_GeneratedTableList)
                        {
                            m_GeneratedTableList.Add(table);
                        }
                        Debug.LogError($"Table Generate Error - {table.tableName} 값이 없음.");
                        return;
                    }
                    
                    var colNames = new List<string>();
                    var colTypes = new List<string>();
                    for (var rowIdx = 0; rowIdx < values.Count; rowIdx ++)
                    {
                        if (rowIdx >= 2) break;
                        var row = values[rowIdx];
                        for (var colIdx = 0; colIdx < row.Count; colIdx ++)
                        {
                            var value = row[colIdx];
                            var valueStr = value == null ? string.Empty : value.ToString().Trim();
                            switch (rowIdx)
                            {
                                case 0:
                                    colNames.Add(valueStr);
                                    break;
                                case 1:
                                    switch (valueStr)
                                    {
                                        case "string32":
                                            colTypes.Add("FixedString32Bytes");
                                            break;
                                        case "string64":
                                            colTypes.Add("FixedString64Bytes");
                                            break;
                                        case "string128":
                                            colTypes.Add("FixedString128Bytes");
                                            break;
                                        case "string256":
                                            colTypes.Add("FixedString256Bytes");
                                            break;
                                        case "string512":
                                            colTypes.Add("FixedString512Bytes");
                                            break;
                                        case "string4096":
                                            colTypes.Add("FixedString4096Bytes");
                                            break;
                                        default:
                                            colTypes.Add(valueStr);
                                            break;
                                    }
                                    break;
                            }
                        }
                    }
                    
                    if (colNames.Count == 0 || colTypes.Count == 0)
                    {
                        lock (m_GeneratedTableList)
                        {
                            m_GeneratedTableList.Add(table);
                        }
                        Debug.LogError($"Table Generate Error - {table.tableName} 열 갯수 부족");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(colNames[0]))
                    {
                        lock (m_GeneratedTableList)
                        {
                            m_GeneratedTableList.Add(table);
                        }
                        Debug.LogError($"Table Generate Error - {table.tableName} 첫번째 열 이름이 없음");
                        return;
                    }
                    
                    if (string.IsNullOrWhiteSpace(colTypes[0]))
                    {
                        lock (m_GeneratedTableList)
                        {
                            m_GeneratedTableList.Add(table);
                        }
                        Debug.LogError($"Table Generate Error - {table.tableName} 첫번째 열 타입이 없음");
                        return;
                    }

                    var strBuilder = new System.Text.StringBuilder();
                    strBuilder.AppendLine("using System;");
                    strBuilder.AppendLine("using UnityEngine;");
                    strBuilder.AppendLine("using Unity.Mathematics;");
                    strBuilder.AppendLine("using Unity.Collections;");
                    strBuilder.AppendLine("namespace GoogleSheetsTable");
                    strBuilder.AppendLine("{");
                    strBuilder.AppendLineFormat("\tpublic partial struct {0}", table.tableName);
                    strBuilder.AppendLine("\t{");

                    var colCnt = System.Math.Min(colNames.Count, colTypes.Count);
                    for (var colIdx = 0; colIdx < colCnt; colIdx ++)
                    {
                        if (string.IsNullOrWhiteSpace(colNames[colIdx])) continue;
                        if (string.IsNullOrWhiteSpace(colTypes[colIdx])) continue;

                        var colName = colNames[colIdx];
                        var colType = colTypes[colIdx];

                        if (colTypes[colIdx].StartsWith("enum:"))
                        {
                            colType = colTypes[colIdx].Substring(5);
                        }
                        else
                        {
                            switch (colType)
                            {
                                case "ColorCode":
                                    colType = "Color";
                                    break;
                            }
                        }
                        
                        strBuilder.AppendLineFormat("\t\tpublic readonly {0} {1};", colType, colName);
                    }
                    strBuilder.AppendLineFormat("\t\tpublic {0}(System.IO.BinaryReader binaryReader)", table.tableName);
                    strBuilder.AppendLine("\t\t{");
                    for (var colIdx = 0; colIdx < colCnt; colIdx ++)
                    {
                        if (string.IsNullOrWhiteSpace(colNames[colIdx])) continue;
                        if (string.IsNullOrWhiteSpace(colTypes[colIdx])) continue;

                        if (colTypes[colIdx].StartsWith("enum:"))
                        {
                            var assembly = System.Reflection.Assembly.GetAssembly(typeof(GoogleSheetsAPI));
                            var enumName = colTypes[colIdx].Substring(5);
                            var enumType = assembly.GetType(enumName);
                            if (enumType != null)
                            {
                                var underlyingType = Enum.GetUnderlyingType(enumType);
                                if (underlyingType == typeof(byte))
                                    strBuilder.AppendLineFormat("\t\t\t{0} = ({1})binaryReader.ReadByte();", colNames[colIdx], enumName);
                                else if (underlyingType == typeof(short))
                                    strBuilder.AppendLineFormat("\t\t\t{0} = ({1})binaryReader.ReadInt16();", colNames[colIdx], enumName);
                                else if (underlyingType == typeof(int))
                                    strBuilder.AppendLineFormat("\t\t\t{0} = ({1})binaryReader.ReadInt32();", colNames[colIdx], enumName);
                                else if (underlyingType == typeof(long))
                                    strBuilder.AppendLineFormat("\t\t\t{0} = ({1})binaryReader.ReadInt64();", colNames[colIdx], enumName);
                            }
                        }
                        else
                        {
                            switch (colTypes[colIdx])
                            {
                                case "FixedString32Bytes":
                                case "FixedString64Bytes":
                                case "FixedString128Bytes":
                                case "FixedString256Bytes":
                                case "FixedString512Bytes":
                                case "FixedString4096Bytes":
                                    strBuilder.AppendLineFormat("\t\t\t{0} = new {1}(binaryReader.ReadString());", colNames[colIdx], colTypes[colIdx]);
                                    break;
                                case "string":
                                case "String":
                                    strBuilder.AppendLineFormat("\t\t\t{0} = binaryReader.ReadString();", colNames[colIdx]);
                                    break;
                                case "byte":
                                case "Byte":
                                    strBuilder.AppendLineFormat("\t\t\t{0} = binaryReader.ReadByte();", colNames[colIdx]);
                                    break;
                                case "short":
                                case "Int16":
                                    strBuilder.AppendLineFormat("\t\t\t{0} = binaryReader.ReadInt16();", colNames[colIdx]);
                                    break;
                                case "int":
                                case "Int32":
                                    strBuilder.AppendLineFormat("\t\t\t{0} = binaryReader.ReadInt32();", colNames[colIdx]);
                                    break;
                                case "long":
                                case "Int64":
                                    strBuilder.AppendLineFormat("\t\t\t{0} = binaryReader.ReadInt64();", colNames[colIdx]);
                                    break;
                                case "decimal":
                                case "Decimal":
                                    strBuilder.AppendLineFormat("\t\t\t{0} = binaryReader.ReadDecimal();", colNames[colIdx]);
                                    break;
                                case "float":
                                case "Single":
                                    strBuilder.AppendLineFormat("\t\t\t{0} = binaryReader.ReadSingle();", colNames[colIdx]);
                                    break;
                                case "double":
                                case "Double":
                                    strBuilder.AppendLineFormat("\t\t\t{0} = binaryReader.ReadDouble();", colNames[colIdx]);
                                    break;
                                case "bool":
                                case "Boolean":
                                    strBuilder.AppendLineFormat("\t\t\t{0} = binaryReader.ReadBoolean();", colNames[colIdx]);
                                    break;
                                case "Vector2Int":
                                case "int2":
                                    strBuilder.AppendLineFormat("\t\t\t{0} = new {1}(binaryReader.ReadInt32(), binaryReader.ReadInt32());", colNames[colIdx], colTypes[colIdx]);
                                    break;
                                case "Vector3Int":
                                case "int3":
                                    strBuilder.AppendLineFormat("\t\t\t{0} = new {1}(binaryReader.ReadInt32(), binaryReader.ReadInt32(), binaryReader.ReadInt32());", colNames[colIdx], colTypes[colIdx]);
                                    break;
                                case "Vector4Int":
                                case "int4":
                                    strBuilder.AppendLineFormat("\t\t\t{0} = new {1}(binaryReader.ReadInt32(), binaryReader.ReadInt32(), binaryReader.ReadInt32(), binaryReader.ReadInt32());", colNames[colIdx], colTypes[colIdx]);
                                    break;
                                case "Vector2":
                                case "float2":
                                    strBuilder.AppendLineFormat("\t\t\t{0} = new {1}(binaryReader.ReadSingle(), binaryReader.ReadSingle());", colNames[colIdx], colTypes[colIdx]);
                                    break;
                                case "Vector3":
                                case "float3":
                                    strBuilder.AppendLineFormat("\t\t\t{0} = new {1}(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());", colNames[colIdx], colTypes[colIdx]);
                                    break;
                                case "Vector4":
                                case "float4":
                                    strBuilder.AppendLineFormat("\t\t\t{0} = new {1}(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());", colNames[colIdx], colTypes[colIdx]);
                                    break;
                                case "Color":
                                case "ColorCode":
                                    strBuilder.AppendLineFormat("\t\t\t{0} = new Color32(binaryReader.ReadByte(), binaryReader.ReadByte(), binaryReader.ReadByte(), binaryReader.ReadByte());", colNames[colIdx]);
                                    break;
                                default:
                                    strBuilder.AppendLineFormat("\t\t\t{0} = default;", colNames[colIdx]);
                                    break;
                            }
                        }
                    }
                    strBuilder.AppendLine("\t\t}");
                    strBuilder.AppendLine("\t}");
                    strBuilder.AppendLine("}");

                    try
                    {
                        System.IO.File.WriteAllText(System.IO.Path.Combine(m_ExportCodeTempPath, $"{table.tableName}.cs"), strBuilder.ToString());
                    }
                    catch (Exception e)
                    {
                        lock (m_GeneratedTableList)
                        {
                            m_GeneratedTableList.Add(table);
                        }
                        Debug.LogError($"Table Generate Error - {table.tableName} Struct 코드 저장 실패\n{e}");
                        return;
                    }

                    strBuilder.Clear();
                    strBuilder.AppendLine("using System.Collections;");
                    strBuilder.AppendLine("using System.Collections.Generic;");
                    strBuilder.AppendLine("using System.Linq;");
                    strBuilder.AppendLine("namespace GoogleSheetsTable");
                    strBuilder.AppendLine("{");
                    strBuilder.AppendLine("\tpublic partial class TableManager");
                    strBuilder.AppendLine("\t{");
                    strBuilder.AppendLineFormat("\t\tprivate readonly Dictionary<{1}, {0}> m_Dic{0} = new Dictionary<int, {0}>();", table.tableName, colTypes[0]);
                    strBuilder.AppendLineFormat("\t\tpublic void LoadTable_{0}(System.IO.BinaryReader binaryReader)", table.tableName);
                    strBuilder.AppendLine("\t\t{");
                    strBuilder.AppendLineFormat("\t\t\tm_Dic{0}.Clear();", table.tableName);
                    strBuilder.AppendLine("\t\t\tvar count = binaryReader.ReadInt32();");
                    strBuilder.AppendLine("\t\t\tfor (var i = 0; i < count; i ++)");
                    strBuilder.AppendLine("\t\t\t{");
                    strBuilder.AppendLineFormat("\t\t\t\tvar data = new {0}(binaryReader);", table.tableName);
                    strBuilder.AppendLineFormat("\t\t\t\tm_Dic{0}.Add(data.{1}, data);", table.tableName, colNames[0]);
                    strBuilder.AppendLine("\t\t\t}");
                    strBuilder.AppendLine("\t\t}");
                    strBuilder.AppendLineFormat("\t\tpublic {0} Get{0}By{1}({2} {3})", table.tableName, colNames[0], colTypes[0], colNames[0].ToLower());
                    strBuilder.AppendLine("\t\t{");
                    strBuilder.AppendLineFormat("\t\t\tif (m_Dic{0}.ContainsKey({1}) == false) return default;", table.tableName, colNames[0].ToLower());
                    strBuilder.AppendLineFormat("\t\t\treturn m_Dic{0}[{1}];", table.tableName, colNames[0].ToLower());
                    strBuilder.AppendLine("\t\t}");
                    strBuilder.AppendLineFormat("\t\tpublic int Get{0}DataCount()", table.tableName);
                    strBuilder.AppendLine("\t\t{");
                    strBuilder.AppendLineFormat("\t\t\treturn m_Dic{0}.Count;", table.tableName);
                    strBuilder.AppendLine("\t\t}");
                    strBuilder.AppendLineFormat("\t\tpublic IEnumerable<{0}> GetAll{0}Data()", table.tableName);
                    strBuilder.AppendLine("\t\t{");
                    strBuilder.AppendLineFormat("\t\t\treturn m_Dic{0}.Values.ToArray();", table.tableName);
                    strBuilder.AppendLine("\t\t}");
                    strBuilder.AppendLine("\t}");
                    strBuilder.AppendLine("}");

                    try
                    {
                        System.IO.File.WriteAllText(System.IO.Path.Combine(m_ExportCodeTempPath, $"TableManager_{table.tableName}.cs"), strBuilder.ToString());
                    }
                    catch (Exception e)
                    {
                        lock (m_GeneratedTableList)
                        {
                            m_GeneratedTableList.Add(table);
                        }
                        Debug.LogError($"Table Generate Error - {table.tableName} TableManager 코드 저장 실패\n{e}");
                        return;
                    }


                    try
                    {
                        var fileStream = new System.IO.FileStream(System.IO.Path.Combine(m_ExportBinaryTempPath, $"{table.tableName.ToLower()}.bytes"), System.IO.FileMode.Create);
                        var binaryWriter = new System.IO.BinaryWriter(fileStream);
                        binaryWriter.Write(values.Count - 2);
                        for (var rowIdx = 2; rowIdx < values.Count; rowIdx ++)
                        {
                            var row = values[rowIdx];
                            for (var colIdx = 0; colIdx < row.Count; colIdx ++)
                            {
                                if (string.IsNullOrWhiteSpace(colNames[colIdx])) continue;
                                if (string.IsNullOrWhiteSpace(colTypes[colIdx])) continue;

                                var value = row[colIdx];
                                var valueStr = value == null ? string.Empty : value.ToString();
                                if (colIdx == 0 && string.IsNullOrWhiteSpace(valueStr)) break;

                                if (colTypes[colIdx].StartsWith("enum:"))
                                {
                                    var assembly = System.Reflection.Assembly.GetAssembly(typeof(GoogleSheetsAPI));
                                    var enumName = colTypes[colIdx].Substring(5);
                                    var enumType = assembly.GetType(enumName);
                                    if (enumType != null)
                                    {
                                        var enumNames = Enum.GetNames(enumType);
                                        var enumValues = Enum.GetValues(enumType);
                                        var underlyingType = Enum.GetUnderlyingType(enumType);
                                        
                                        object enumValue = null;
                                        var splitValueStr = valueStr.Split(',');
                                        for (var strIdx = 0; strIdx < splitValueStr.Length; strIdx ++)
                                        {
                                            for (int enumIdx = 0; enumIdx < enumNames.Length; enumIdx ++)
                                            {
                                                if (splitValueStr[strIdx].Trim() == enumNames[enumIdx])
                                                {
                                                    if (enumValue == null)
                                                    {
                                                        enumValue = enumValues.GetValue(enumIdx);
                                                    }
                                                    else
                                                    {
                                                        if (underlyingType == typeof(sbyte))
                                                            enumValue = (sbyte)enumValue | (sbyte)enumValues.GetValue(enumIdx);
                                                        else if (underlyingType == typeof(short))
                                                            enumValue = (short)enumValue | (short)enumValues.GetValue(enumIdx);
                                                        else if (underlyingType == typeof(int))
                                                            enumValue = (int)enumValue | (int)enumValues.GetValue(enumIdx);
                                                        else if (underlyingType == typeof(long))
                                                            enumValue = (long)enumValue | (long)enumValues.GetValue(enumIdx);
                                                        else if (underlyingType == typeof(byte))
                                                            enumValue = (byte)enumValue | (byte)enumValues.GetValue(enumIdx);
                                                        else if (underlyingType == typeof(ushort))
                                                            enumValue = (ushort)enumValue | (ushort)enumValues.GetValue(enumIdx);
                                                        else if (underlyingType == typeof(uint))
                                                            enumValue = (uint)enumValue | (uint)enumValues.GetValue(enumIdx);
                                                        else if (underlyingType == typeof(ulong))
                                                            enumValue = (ulong)enumValue | (ulong)enumValues.GetValue(enumIdx);
                                                    }
                                                    break;
                                                }
                                            }
                                        }
                                        
                                        if (underlyingType == typeof(sbyte))
                                            binaryWriter.Write(enumValue == null ? default : (sbyte)enumValue);
                                        else if (underlyingType == typeof(short))
                                            binaryWriter.Write(enumValue == null ? default : (short)enumValue);
                                        else if (underlyingType == typeof(int))
                                            binaryWriter.Write(enumValue == null ? default : (int)enumValue);
                                        else if (underlyingType == typeof(long))
                                            binaryWriter.Write(enumValue == null ? default : (long)enumValue);
                                        else if (underlyingType == typeof(byte))
                                            binaryWriter.Write(enumValue == null ? default : (byte)enumValue);
                                        else if (underlyingType == typeof(ushort))
                                            binaryWriter.Write(enumValue == null ? default : (ushort)enumValue);
                                        else if (underlyingType == typeof(uint))
                                            binaryWriter.Write(enumValue == null ? default : (uint)enumValue);
                                        else if (underlyingType == typeof(ulong))
                                            binaryWriter.Write(enumValue == null ? default : (ulong)enumValue);
                                    }
                                }
                                else
                                {
                                    switch (colTypes[colIdx])
                                    {
                                        case "FixedString32Bytes":
                                        case "FixedString64Bytes":
                                        case "FixedString128Bytes":
                                        case "FixedString512Bytes":
                                        case "FixedString4096Bytes":
                                        case "string":
                                        case "String":
                                            binaryWriter.Write(string.IsNullOrWhiteSpace(valueStr) ? string.Empty : valueStr);
                                            break;
                                        case "byte":
                                        case "Byte":
                                            binaryWriter.Write(string.IsNullOrWhiteSpace(valueStr) ? default : byte.Parse(valueStr));
                                            break;
                                        case "short":
                                        case "Int16":
                                            binaryWriter.Write(string.IsNullOrWhiteSpace(valueStr) ? default : short.Parse(valueStr));
                                            break;
                                        case "int":
                                        case "Int32":
                                            binaryWriter.Write(string.IsNullOrWhiteSpace(valueStr) ? default : int.Parse(valueStr));
                                            break;
                                        case "long":
                                        case "Int64":
                                            binaryWriter.Write(string.IsNullOrWhiteSpace(valueStr) ? default : long.Parse(valueStr));
                                            break;
                                        case "decimal":
                                        case "Decimal":
                                            binaryWriter.Write(string.IsNullOrWhiteSpace(valueStr) ? default : decimal.Parse(valueStr));
                                            break;
                                        case "float":
                                        case "Single":
                                            binaryWriter.Write(string.IsNullOrWhiteSpace(valueStr) ? default : float.Parse(valueStr));
                                            break;
                                        case "double":
                                        case "Double":
                                            binaryWriter.Write(string.IsNullOrWhiteSpace(valueStr) ? default : double.Parse(valueStr));
                                            break;
                                        case "bool":
                                        case "Boolean":
                                            binaryWriter.Write(string.IsNullOrWhiteSpace(valueStr) ? default : bool.Parse(valueStr));
                                            break;
                                        case "Vector2Int":
                                        case "int2":
                                            {
                                                var splited = valueStr.Split(',');
                                                binaryWriter.Write(splited.Length <= 0 || string.IsNullOrWhiteSpace(splited[0]) ? default : int.Parse(splited[0].Trim()));
                                                binaryWriter.Write(splited.Length <= 1 && string.IsNullOrWhiteSpace(splited[1]) ? default : int.Parse(splited[1].Trim()));
                                            }
                                            break;
                                        case "Vector3Int":
                                        case "int3":
                                            {
                                                var splited = valueStr.Split(',');
                                                binaryWriter.Write(splited.Length <= 0 || string.IsNullOrWhiteSpace(splited[0]) ? default : int.Parse(splited[0].Trim()));
                                                binaryWriter.Write(splited.Length <= 1 || string.IsNullOrWhiteSpace(splited[1]) ? default : int.Parse(splited[1].Trim()));
                                                binaryWriter.Write(splited.Length <= 2 || string.IsNullOrWhiteSpace(splited[2]) ? default : int.Parse(splited[2].Trim()));
                                            }
                                            break;
                                        case "Vector4Int":
                                        case "int4":
                                            {
                                                var splited = valueStr.Split(',');
                                                binaryWriter.Write(splited.Length <= 0 || string.IsNullOrWhiteSpace(splited[0]) ? default : int.Parse(splited[0].Trim()));
                                                binaryWriter.Write(splited.Length <= 1 || string.IsNullOrWhiteSpace(splited[1]) ? default : int.Parse(splited[1].Trim()));
                                                binaryWriter.Write(splited.Length <= 2 || string.IsNullOrWhiteSpace(splited[2]) ? default : int.Parse(splited[2].Trim()));
                                                binaryWriter.Write(splited.Length <= 3 || string.IsNullOrWhiteSpace(splited[3]) ? default : int.Parse(splited[3].Trim()));
                                            }
                                            break;
                                        case "Vector2":
                                        case "float2":
                                            {
                                                var splited = valueStr.Split(',');
                                                binaryWriter.Write(splited.Length <= 0 || string.IsNullOrWhiteSpace(splited[0]) ? default : float.Parse(splited[0].Trim()));
                                                binaryWriter.Write(splited.Length <= 1 || string.IsNullOrWhiteSpace(splited[1]) ? default : float.Parse(splited[1].Trim()));
                                            }
                                            break;
                                        case "Vector3":
                                        case "float3":
                                            {
                                                var splited = valueStr.Split(',');
                                                binaryWriter.Write(splited.Length <= 0 || string.IsNullOrWhiteSpace(splited[0]) ? default : float.Parse(splited[0].Trim()));
                                                binaryWriter.Write(splited.Length <= 1 || string.IsNullOrWhiteSpace(splited[1]) ? default : float.Parse(splited[1].Trim()));
                                                binaryWriter.Write(splited.Length <= 2 || string.IsNullOrWhiteSpace(splited[2]) ? default : float.Parse(splited[2].Trim()));
                                            }
                                            break;
                                        case "Vector4":
                                        case "float4":
                                            {
                                                var splited = valueStr.Split(',');
                                                binaryWriter.Write(splited.Length <= 0 || string.IsNullOrWhiteSpace(splited[0]) ? default : float.Parse(splited[0].Trim()));
                                                binaryWriter.Write(splited.Length <= 1 || string.IsNullOrWhiteSpace(splited[1]) ? default : float.Parse(splited[1].Trim()));
                                                binaryWriter.Write(splited.Length <= 2 || string.IsNullOrWhiteSpace(splited[2]) ? default : float.Parse(splited[2].Trim()));
                                                binaryWriter.Write(splited.Length <= 3 || string.IsNullOrWhiteSpace(splited[3]) ? default : float.Parse(splited[3].Trim()));
                                            }
                                            break;
                                        case "Color":
                                            {
                                                var splited = valueStr.Split(',');
                                                binaryWriter.Write(splited.Length <= 0 || string.IsNullOrWhiteSpace(splited[0]) ? default : byte.Parse(splited[0].Trim()));
                                                binaryWriter.Write(splited.Length <= 1 || string.IsNullOrWhiteSpace(splited[1]) ? default : byte.Parse(splited[1].Trim()));
                                                binaryWriter.Write(splited.Length <= 2 || string.IsNullOrWhiteSpace(splited[2]) ? default : byte.Parse(splited[2].Trim()));
                                                if (splited.Length < 4)
                                                    binaryWriter.Write(byte.MaxValue);
                                                else
                                                    binaryWriter.Write(string.IsNullOrWhiteSpace(splited[3]) ? default : byte.Parse(splited[3].Trim()));
                                            }
                                            break;
                                        case "ColorCode":
                                            {
                                                var color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                                                UnityEngine.ColorUtility.TryParseHtmlString(valueStr, out color);
                                                binaryWriter.Write((byte)Mathf.RoundToInt(color.r * byte.MaxValue));
                                                binaryWriter.Write((byte)Mathf.RoundToInt(color.g * byte.MaxValue));
                                                binaryWriter.Write((byte)Mathf.RoundToInt(color.b * byte.MaxValue));
                                                binaryWriter.Write((byte)Mathf.RoundToInt(color.a * byte.MaxValue));
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                        binaryWriter.Close();
                        fileStream.Close();
                    }
                    catch (Exception e)
                    {
                        lock (m_GeneratedTableList)
                        {
                            m_GeneratedTableList.Add(table);
                        }
                        Debug.LogError($"Table Generate Error - {table.tableName} Binary 저장 실패\n{e}");
                        return;
                    }

                    lock (m_GeneratedTableList)
                    {
                        m_GeneratedTableList.Add(table);
                    }
                });
        }

    }
}