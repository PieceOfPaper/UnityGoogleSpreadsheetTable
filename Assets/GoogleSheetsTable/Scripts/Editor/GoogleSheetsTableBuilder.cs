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

        public const string GENERATE_CODE_PATH = "Assets/GoogleSheetsTable/Scripts/Generated";
        public const string GENERATE_CODE_TEMP_PATH = "Temp/GoogleSheetsTable/Scripts/Generated";

        
        private bool m_IsEnableGoogleSheetAPI;

        private GoogleSheetsSetting m_Setting;
        private bool m_IsSettingModified;

        private List<GoogleSheetsSetting.Table> m_RequestGenerateTableList = new List<GoogleSheetsSetting.Table>();
        private List<GoogleSheetsSetting.Table> m_GeneratedTableList = new List<GoogleSheetsSetting.Table>();

        private string m_GenerateCodePath;
        private string m_GenerateCodeTempPath;

        public Vector2 m_TablesScroll;

        private void OnEnable()
        {
            m_GenerateCodePath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), GENERATE_CODE_PATH);
            m_GenerateCodeTempPath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), GENERATE_CODE_TEMP_PATH);
        }

        private void OnGUI()
        {
            if (m_Setting == null)
            {
                m_Setting = AssetDatabase.LoadAssetAtPath<GoogleSheetsSetting>(GoogleSheetsSetting.PATH);
                if (m_Setting == null)
                {
                    GoogleSheetsSetting.CreateSetting();
                    m_Setting = AssetDatabase.LoadAssetAtPath<GoogleSheetsSetting>(GoogleSheetsSetting.PATH);
                }
            }

            m_IsEnableGoogleSheetAPI = GoogleSheetsAPI.Instance.IsCertificating == false && GoogleSheetsAPI.Instance.IsCertificated == true;


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


        private void GenerateTable(GoogleSheetsSetting.Table table)
        {
            m_RequestGenerateTableList.Clear();
            m_GeneratedTableList.Clear();

            var tempStructPath = System.IO.Path.Combine(m_GenerateCodeTempPath, "Struct");
            var tempTableManagerPath = System.IO.Path.Combine(m_GenerateCodeTempPath, "TableManager");
            System.IO.Directory.CreateDirectory(tempStructPath);
            System.IO.Directory.CreateDirectory(tempTableManagerPath);
            var tempStructDirectoryInfo = new System.IO.DirectoryInfo(tempStructPath);
            var structFileInfos = tempStructDirectoryInfo.GetFiles();
            foreach (var fileInfo in structFileInfos)
                System.IO.File.Delete(fileInfo.FullName);
            var tempTableManagerDirectoryInfo = new System.IO.DirectoryInfo(tempTableManagerPath);
            var tableManagerFileInfos = tempTableManagerDirectoryInfo.GetFiles();
            foreach (var fileInfo in tableManagerFileInfos)
                System.IO.File.Delete(fileInfo.FullName);
            
            m_RequestGenerateTableList.Add(table);
            _GenerateTable(table);
        }
        
        private void GenerateTables(IEnumerable<GoogleSheetsSetting.Table> tables)
        {
            m_RequestGenerateTableList.Clear();
            m_GeneratedTableList.Clear();

            var tempStructPath = System.IO.Path.Combine(m_GenerateCodeTempPath, "Struct");
            var tempTableManagerPath = System.IO.Path.Combine(m_GenerateCodeTempPath, "TableManager");
            System.IO.Directory.CreateDirectory(tempStructPath);
            System.IO.Directory.CreateDirectory(tempTableManagerPath);
            var tempStructDirectoryInfo = new System.IO.DirectoryInfo(tempStructPath);
            var structFileInfos = tempStructDirectoryInfo.GetFiles();
            foreach (var fileInfo in structFileInfos)
                System.IO.File.Delete(fileInfo.FullName);
            var tempTableManagerDirectoryInfo = new System.IO.DirectoryInfo(tempTableManagerPath);
            var tableManagerFileInfos = tempTableManagerDirectoryInfo.GetFiles();
            foreach (var fileInfo in tableManagerFileInfos)
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
            GoogleSheetsAPI.Instance.RequestTable(table.spreadsheetId, $"{table.sheetName}!{table.dataRange}", values =>
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
                        case "boolean":
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

                var generateStruntPath = System.IO.Path.Combine(m_GenerateCodeTempPath, $"Struct/{table.sheetName}.cs");
                System.IO.File.WriteAllText(generateStruntPath, strBuilder.ToString());


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
                
                for (int rowIdx = 2; rowIdx < values.Count; rowIdx ++)
                {
                    var row = values[rowIdx];
                    for (int colIdx = 0; colIdx < row.Count; colIdx ++)
                    {
                        var value = row[colIdx];
                    }
                }

                lock (m_GeneratedTableList)
                {
                    m_GeneratedTableList.Add(table);
                }
            });
        }
        
    }
}