using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

        public const string GENERATE_CODE_PATH = "Assets/GoogleSheetsTable/Scripts/Generated";
        public const string GENERATE_CODE_TEMP_PATH = "Temp/GoogleSheetsTable/Scripts/Generated";
        public const string GENERATE_BINARY_TEMP_PATH = "Temp/GoogleSheetsTable/Binary";

        public GoogleSheetsSetting m_Setting;
        public Vector2 m_TablesScroll;
        
        private bool m_IsEnableGoogleSheetAPI;
        private bool m_IsSettingModified;

        private string m_ExportPath;
        private string m_ExportFullPath;
        private string m_ExportTempPath;

        private List<GoogleSheetsSetting.Table> m_RequestGenerateTableList = new List<GoogleSheetsSetting.Table>();
        private List<GoogleSheetsSetting.Table> m_GeneratedTableList = new List<GoogleSheetsSetting.Table>();

        private string m_GenerateCodePath;
        private string m_GenerateCodeTempPath;


        private void OnEnable()
        {
            m_GenerateCodePath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), GENERATE_CODE_PATH);
            m_GenerateCodeTempPath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), GENERATE_CODE_TEMP_PATH);
            m_ExportTempPath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), GENERATE_BINARY_TEMP_PATH);

            var lastSettingGUID = EditorPrefs.GetString("GoogleSheetsTableBuilder_LastSettingGUID");
            var lastSettingPath = string.IsNullOrWhiteSpace(lastSettingGUID) ? string.Empty : AssetDatabase.GUIDToAssetPath(lastSettingGUID);
            m_Setting = string.IsNullOrWhiteSpace(lastSettingPath) ? null : AssetDatabase.LoadAssetAtPath<GoogleSheetsSetting>(lastSettingPath);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            var inputSetting = (GoogleSheetsSetting)EditorGUILayout.ObjectField("Setting", m_Setting, typeof(GoogleSheetsSetting));
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
                    m_ExportPath = m_Setting.exportPath;
                    m_ExportFullPath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), m_ExportPath);

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

            m_ExportPath = EditorGUILayout.TextField("Export Path", m_Setting.exportPath);
            if (m_ExportPath != m_Setting.exportPath)
            {
                m_ExportPath = m_Setting.exportPath;
                m_ExportFullPath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), m_ExportPath);
                m_IsSettingModified = true;
            }

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
                    
                    System.IO.Directory.CreateDirectory(System.IO.Path.Combine(m_GenerateCodePath, "Struct"));
                    System.IO.Directory.CreateDirectory(System.IO.Path.Combine(m_GenerateCodePath, "TableManager"));
                    System.IO.Directory.CreateDirectory(m_ExportFullPath);
                    var tempStructDirectoryInfo = new System.IO.DirectoryInfo(System.IO.Path.Combine(m_GenerateCodeTempPath, "Struct"));
                    var structFileInfos = tempStructDirectoryInfo.GetFiles();
                    foreach (var fileInfo in structFileInfos)
                    {
                        System.IO.File.Copy(fileInfo.FullName, System.IO.Path.Combine(m_GenerateCodePath, $"Struct/{fileInfo.Name}"), true);
                    }
                    var tempTableManagerDirectoryInfo = new System.IO.DirectoryInfo(System.IO.Path.Combine(m_GenerateCodeTempPath, "TableManager"));
                    var tableManagerFileInfos = tempTableManagerDirectoryInfo.GetFiles();
                    foreach (var fileInfo in tableManagerFileInfos)
                    {
                        System.IO.File.Copy(fileInfo.FullName, System.IO.Path.Combine(m_GenerateCodePath, $"TableManager/{fileInfo.Name}"), true);
                    }
                    var tempExportDirectoryInfo = new System.IO.DirectoryInfo(m_ExportTempPath);
                    var exportFileInfos = tempExportDirectoryInfo.GetFiles();
                    foreach (var fileInfo in exportFileInfos)
                    {
                        System.IO.File.Copy(fileInfo.FullName, System.IO.Path.Combine(m_ExportFullPath, fileInfo.Name), true);
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
                    var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(m_Setting.googleClientSecretsPath);
                    GoogleSheetsAPI.Instance.Certificate(textAsset.text);
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
                EditorGUILayout.BeginVertical();
                data.spreadsheetId = EditorGUILayout.TextField("Spreadsheet id", data.spreadsheetId);
                data.sheetName = EditorGUILayout.TextField("Sheet Name", data.sheetName);
                data.dataRange = EditorGUILayout.TextField("Data Range", data.dataRange);
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();
                if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                {
                    removeIndex = i;
                }
                using (new EditorGUI.DisabledScope(m_IsEnableGoogleSheetAPI == false || m_RequestGenerateTableList.Count > 0))
                {
                    if (GUILayout.Button("Generate", GUILayout.ExpandWidth(false)))
                    {
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
            EditorGUILayout.Space();
            if (GUILayout.Button("+", GUILayout.ExpandWidth(true)))
            {
                tableList.Add(new GoogleSheetsSetting.Table());
                m_IsSettingModified = true;
            }
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(m_IsEnableGoogleSheetAPI == false || m_RequestGenerateTableList.Count > 0))
            {
                if (GUILayout.Button("Generate All"))
                {
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

            var tempStructPath = System.IO.Path.Combine(m_GenerateCodeTempPath, "Struct");
            var tempTableManagerPath = System.IO.Path.Combine(m_GenerateCodeTempPath, "TableManager");
            System.IO.Directory.CreateDirectory(tempStructPath);
            System.IO.Directory.CreateDirectory(tempTableManagerPath);
            System.IO.Directory.CreateDirectory(m_ExportTempPath);
            var tempStructDirectoryInfo = new System.IO.DirectoryInfo(tempStructPath);
            var structFileInfos = tempStructDirectoryInfo.GetFiles();
            foreach (var fileInfo in structFileInfos)
                System.IO.File.Delete(fileInfo.FullName);
            var tempTableManagerDirectoryInfo = new System.IO.DirectoryInfo(tempTableManagerPath);
            var tableManagerFileInfos = tempTableManagerDirectoryInfo.GetFiles();
            foreach (var fileInfo in tableManagerFileInfos)
                System.IO.File.Delete(fileInfo.FullName);
            var tempExportDirectoryInfo = new System.IO.DirectoryInfo(m_ExportTempPath);
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
                    var colNames = new List<string>();
                    var colTypes = new List<string>();
                    for (int rowIdx = 0; rowIdx < values.Count; rowIdx ++)
                    {
                        if (rowIdx >= 2) break;
                        var row = values[rowIdx];
                        for (int colIdx = 0; colIdx < row.Count; colIdx ++)
                        {
                            var value = row[colIdx];
                            switch (rowIdx)
                            {
                                case 0:
                                    colNames.Add(value.ToString());
                                    break;
                                case 1:
                                    colTypes.Add(value.ToString());
                                    break;
                            }
                        }
                    }

                    if (colNames.Count == 0 || colTypes.Count == 0)
                    {
                        throw new Exception("Column 부족");
                    }

                    var strBuilder = new System.Text.StringBuilder();
                    strBuilder.AppendLine("namespace GoogleSheetsTable");
                    strBuilder.AppendLine("{");
                    strBuilder.AppendLineFormat("\tpublic partial struct {0}", table.sheetName);
                    strBuilder.AppendLine("\t{");

                    var colCnt = System.Math.Min(colNames.Count, colTypes.Count);
                    for (int colIdx = 0; colIdx < colCnt; colIdx ++)
                    {
                        strBuilder.AppendLineFormat("\t\tpublic readonly {0} {1};", colTypes[colIdx], colNames[colIdx]);
                    }
                    strBuilder.AppendLineFormat("\t\tpublic {0}(System.IO.BinaryReader binaryReader)", table.sheetName);
                    strBuilder.AppendLine("\t\t{");
                    for (int colIdx = 0; colIdx < colCnt; colIdx ++)
                    {
                        strBuilder.AppendFormat("\t\t\t{0} = ", colNames[colIdx]);
                        switch (colTypes[colIdx])
                        {
                            case "string":
                                strBuilder.Append("binaryReader.ReadString();");
                                break;
                            case "short":
                                strBuilder.Append("binaryReader.ReadInt16();");
                                break;
                            case "int":
                                strBuilder.Append("binaryReader.ReadInt32();");
                                break;
                            case "long":
                                strBuilder.Append("binaryReader.ReadInt64();");
                                break;
                            case "float":
                                strBuilder.Append("binaryReader.ReadSingle();");
                                break;
                            case "bool":
                                strBuilder.Append("binaryReader.ReadBoolean();");
                                break;
                            default:
                                strBuilder.Append("default;");
                                break;
                        }
                        strBuilder.Append('\n');
                    }
                    strBuilder.AppendLine("\t\t}");
                    strBuilder.AppendLine("\t}");
                    strBuilder.AppendLine("}");

                    var generateStructPath = System.IO.Path.Combine(m_GenerateCodeTempPath, $"Struct/{table.sheetName}.cs");
                    System.IO.File.WriteAllText(generateStructPath, strBuilder.ToString());


                    strBuilder.Clear();
                    strBuilder.AppendLine("using System.Collections;");
                    strBuilder.AppendLine("using System.Collections.Generic;");
                    strBuilder.AppendLine("namespace GoogleSheetsTable");
                    strBuilder.AppendLine("{");
                    strBuilder.AppendLine("\tpublic partial class TableManager");
                    strBuilder.AppendLine("\t{");
                    strBuilder.AppendLineFormat("\t\tprivate Dictionary<{1}, {0}> m_Dic{0} = new Dictionary<int, {0}>();", table.sheetName, colTypes[0]);
                    strBuilder.AppendLineFormat("\t\tpublic void LoadTable_{0}(System.IO.BinaryReader binaryReader)", table.sheetName);
                    strBuilder.AppendLine("\t\t{");
                    strBuilder.AppendLineFormat("\t\t\tm_Dic{0}.Clear();", table.sheetName);
                    strBuilder.AppendLine("\t\t\tvar count = binaryReader.ReadInt32();");
                    strBuilder.AppendLine("\t\t\tfor (int i = 0; i < count; i ++)");
                    strBuilder.AppendLine("\t\t\t{");
                    strBuilder.AppendLineFormat("\t\t\t\tvar data = new {0}(binaryReader);", table.sheetName);
                    strBuilder.AppendLineFormat("\t\t\t\tm_Dic{0}.Add(data.{1}, data);", table.sheetName, colNames[0]);
                    strBuilder.AppendLine("\t\t\t}");
                    strBuilder.AppendLine("\t\t}");
                    strBuilder.AppendLine("\t}");
                    strBuilder.AppendLine("}");

                    var generateTableManagerPath = System.IO.Path.Combine(m_GenerateCodeTempPath, $"TableManager/TableManager_{table.sheetName}.cs");
                    System.IO.File.WriteAllText(generateTableManagerPath, strBuilder.ToString());


                    var fileStream = new System.IO.FileStream(System.IO.Path.Combine(m_ExportTempPath, $"{table.sheetName.ToLower()}.bytes"), FileMode.Create);
                    var binaryWriter = new System.IO.BinaryWriter(fileStream);
                    binaryWriter.Write(values.Count - 2);
                    for (int rowIdx = 2; rowIdx < values.Count; rowIdx ++)
                    {
                        var row = values[rowIdx];
                        for (int colIdx = 0; colIdx < row.Count; colIdx ++)
                        {
                            var value = row[colIdx];
                            var valueStr = value == null ? string.Empty : value.ToString();
                            switch (colTypes[colIdx])
                            {
                                case "string":
                                    binaryWriter.Write(valueStr);
                                    break;
                                case "short":
                                    binaryWriter.Write(string.IsNullOrWhiteSpace(valueStr) ? default : short.Parse(valueStr));
                                    break;
                                case "int":
                                    binaryWriter.Write(string.IsNullOrWhiteSpace(valueStr) ? default : int.Parse(valueStr));
                                    break;
                                case "long":
                                    binaryWriter.Write(string.IsNullOrWhiteSpace(valueStr) ? default : long.Parse(valueStr));
                                    break;
                                case "float":
                                    binaryWriter.Write(string.IsNullOrWhiteSpace(valueStr) ? default : float.Parse(valueStr));
                                    break;
                                case "bool":
                                    binaryWriter.Write(string.IsNullOrWhiteSpace(valueStr) ? default : bool.Parse(valueStr));
                                    break;
                            }
                        }
                    }
                    binaryWriter.Close();
                    fileStream.Close();

                    lock (m_GeneratedTableList)
                    {
                        m_GeneratedTableList.Add(table);
                    }
                });
        }

    }
}