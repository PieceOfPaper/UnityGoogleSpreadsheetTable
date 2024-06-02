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
        [MenuItem("Tools/Google Sheets Table/Open Builder")]
        private static void OpenWindow()
        {
            var window = EditorWindow.GetWindow<GoogleSheetsTableBuilder>();
        }

        private const string GENERATE_CODE_TEMP_PATH = "Temp/GoogleSheetsTable/Scripts";
        private const string GENERATE_XML_TEMP_PATH = "Temp/GoogleSheetsTable/Xml";

        public GoogleSheetsSetting m_Setting;
        public Vector2 m_TablesScroll;
        
        private bool m_IsEnableGoogleSheetAPI;
        private int m_GoogleSheetAPIRetryCount;
        
        private bool m_IsSettingModified;

        private string m_ExportCodePath;
        private string m_ExportCodeFullPath;
        private string m_ExportCodeTempPath;
        
        private string m_ExportXmlPath;
        private string m_ExportXmlFullPath;
        private string m_ExportXmlTempPath;

        private List<GoogleSheetsSetting.Table> m_RequestGenerateTableCodeList = new List<GoogleSheetsSetting.Table>();
        private List<GoogleSheetsSetting.Table> m_GeneratedTableCodeList = new List<GoogleSheetsSetting.Table>();
        private List<GoogleSheetsSetting.Table> m_RequestGenerateTableXmlList = new List<GoogleSheetsSetting.Table>();
        private List<GoogleSheetsSetting.Table> m_GeneratedTableXmlList = new List<GoogleSheetsSetting.Table>();



        private void OnEnable()
        {
            m_ExportCodeTempPath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), GENERATE_CODE_TEMP_PATH);
            m_ExportXmlTempPath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), GENERATE_XML_TEMP_PATH);

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

            if (GoogleSheetsAPI.Instance.IsCertificating == false && GoogleSheetsAPI.Instance.IsCertificated == false &&
                string.IsNullOrWhiteSpace(m_Setting.googleClientSecretsPath) == false &&
                m_GoogleSheetAPIRetryCount < 3)
            {
                Certificate();
                m_GoogleSheetAPIRetryCount ++;
            }
            else if (GoogleSheetsAPI.Instance.IsCertificated == true)
            {
                m_GoogleSheetAPIRetryCount = 0;
            }

            m_IsEnableGoogleSheetAPI = GoogleSheetsAPI.Instance.IsCertificating == false && GoogleSheetsAPI.Instance.IsCertificated == true;

            m_ExportCodePath = EditorGUILayout.TextField("Export Code Path", m_Setting.exportCodePath);
            m_ExportXmlPath = EditorGUILayout.TextField("Export Xml Path", m_Setting.exportXmlPath);
            if (m_ExportCodePath != m_Setting.exportCodePath) m_IsSettingModified = true;
            if (m_ExportXmlPath != m_Setting.exportXmlPath) m_IsSettingModified = true;
            m_Setting.exportCodePath = m_ExportCodePath;
            m_ExportCodeFullPath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), m_ExportCodePath);
            m_Setting.exportXmlPath = m_ExportXmlPath;
            m_ExportXmlFullPath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), m_ExportXmlPath);

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

            if (m_RequestGenerateTableCodeList.Count > 0 || m_RequestGenerateTableXmlList.Count > 0)
            {
                if (m_RequestGenerateTableCodeList.Count <= m_GeneratedTableCodeList.Count && m_RequestGenerateTableXmlList.Count <= m_GeneratedTableXmlList.Count)
                {
                    if (m_RequestGenerateTableCodeList.Count > 0)
                    {
                        m_RequestGenerateTableCodeList.Clear();
                        m_GeneratedTableCodeList.Clear();
                        System.IO.Directory.CreateDirectory(m_ExportCodeFullPath);
                        var tempCodeDirectoryInfo = new System.IO.DirectoryInfo(m_ExportCodeTempPath);
                        var codeFileInfos = tempCodeDirectoryInfo.GetFiles();
                        foreach (var fileInfo in codeFileInfos)
                        {
                            System.IO.File.Copy(fileInfo.FullName, System.IO.Path.Combine(m_ExportCodeFullPath, fileInfo.Name), true);
                        }
                    }

                    if (m_RequestGenerateTableXmlList.Count > 0)
                    {
                        m_RequestGenerateTableXmlList.Clear();
                        m_GeneratedTableXmlList.Clear();
                        System.IO.Directory.CreateDirectory(m_ExportXmlFullPath);
                        var tempExportXmlDirectoryInfo = new System.IO.DirectoryInfo(m_ExportXmlTempPath);
                        var exportXmlFileInfos = tempExportXmlDirectoryInfo.GetFiles();
                        foreach (var fileInfo in exportXmlFileInfos)
                        {
                            System.IO.File.Copy(fileInfo.FullName, System.IO.Path.Combine(m_ExportXmlFullPath, fileInfo.Name), true);
                        }
                    }
                    
                    EditorUtility.ClearProgressBar();
                    AssetDatabase.Refresh();
                }
                else
                {
                    EditorUtility.DisplayProgressBar("Google Sheets Table Builder", $"Generating Table ...", (float)(m_GeneratedTableCodeList.Count + m_GeneratedTableXmlList.Count) / (m_RequestGenerateTableCodeList.Count + m_RequestGenerateTableXmlList.Count));
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
                    Certificate();
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
                if (GUILayout.Button("Open"))
                {
                    if (m_IsEnableGoogleSheetAPI == true)
                        GoogleSheetsAPI.Instance.OpenTable(data.spreadsheetId, data.sheetName);
                    else
                        Application.OpenURL($"https://docs.google.com/spreadsheets/d/{data.spreadsheetId}");
                }
                if (GUILayout.Button("Delete"))
                {
                    GUI.FocusControl(null);
                    var result = EditorUtility.DisplayDialog("Google Sheets Table Builer", $"Are you sure you want to delete this table?\n\nTable Name: {data.tableName}", "Delete", "Cancel");
                    if (result == true)
                    {
                        removeIndex = i;
                    }
                }
                using (new EditorGUI.DisabledScope(m_IsEnableGoogleSheetAPI == false || m_RequestGenerateTableCodeList.Count > 0))
                {
                    if (GUILayout.Button("Generate Code"))
                    {
                        GUI.FocusControl(null);
                        GenerateTable_Code(data);
                    }
                }
                using (new EditorGUI.DisabledScope(m_IsEnableGoogleSheetAPI == false || m_RequestGenerateTableXmlList.Count > 0))
                {
                    if (GUILayout.Button("Generate Xml"))
                    {
                        GUI.FocusControl(null);
                        GenerateTable_Xml(data);
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

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(m_IsEnableGoogleSheetAPI == false || m_RequestGenerateTableCodeList.Count > 0))
                {
                    if (GUILayout.Button("Generate All Code"))
                    {
                        GUI.FocusControl(null);
                        GenerateTables_Code(tableList);
                    }
                }
                using (new EditorGUI.DisabledScope(m_IsEnableGoogleSheetAPI == false || m_RequestGenerateTableXmlList.Count > 0))
                {
                    if (GUILayout.Button("Generate All Xml"))
                    {
                        GUI.FocusControl(null);
                        GenerateTables_Xml(tableList);
                    }
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


        private void Certificate()
        {
            if (m_Setting == null)
                return;
            
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

        private void GenerateTable_Code(GoogleSheetsSetting.Table table) => GenerateTables_Code(new [] { table });
        
        private void GenerateTables_Code(IEnumerable<GoogleSheetsSetting.Table> tables)
        {
            m_RequestGenerateTableCodeList.Clear();
            m_GeneratedTableCodeList.Clear();

            System.IO.Directory.CreateDirectory(m_ExportCodeTempPath);
            
            var tempCodeDirectoryInfo = new System.IO.DirectoryInfo(m_ExportCodeTempPath);
            var codeFileInfos = tempCodeDirectoryInfo.GetFiles();
            foreach (var fileInfo in codeFileInfos)
                System.IO.File.Delete(fileInfo.FullName);

            if (tables != null)
            {
                m_RequestGenerateTableCodeList.AddRange(tables);
                foreach (var table in tables)
                    _GenerateTable_Code(table);
            }
        }
        
        private void GenerateTable_Xml(GoogleSheetsSetting.Table table) => GenerateTables_Xml(new [] { table });
        
        private void GenerateTables_Xml(IEnumerable<GoogleSheetsSetting.Table> tables)
        {
            m_RequestGenerateTableXmlList.Clear();
            m_GeneratedTableXmlList.Clear();

            System.IO.Directory.CreateDirectory(m_ExportXmlTempPath);
            
            var tempExportXmlDirectoryInfo = new System.IO.DirectoryInfo(m_ExportXmlTempPath);
            var exportXmlFileInfos = tempExportXmlDirectoryInfo.GetFiles();
            foreach (var fileInfo in exportXmlFileInfos)
                System.IO.File.Delete(fileInfo.FullName);

            if (tables != null)
            {
                m_RequestGenerateTableXmlList.AddRange(tables);
                foreach (var table in tables)
                    _GenerateTable_Xml(table);
            }
        }

        
        private void _GenerateTable_Code(GoogleSheetsSetting.Table table)
        {
            GoogleSheetsAPI.Instance.RequestTable(table.spreadsheetId,
                $"{table.sheetName}!{table.dataRange}",
                values =>
                {
                    if (values == null || values.Count == 0)
                    {
                        lock (m_GeneratedTableCodeList)
                        {
                            if (m_GeneratedTableCodeList.Contains(table))
                                Debug.LogError("????");
                            else
                                m_GeneratedTableCodeList.Add(table);
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
                        lock (m_GeneratedTableCodeList)
                        {
                            if (m_GeneratedTableCodeList.Contains(table))
                                Debug.LogError("????");
                            else
                                m_GeneratedTableCodeList.Add(table);
                        }
                        Debug.LogError($"Table Generate Error - {table.tableName} 열 갯수 부족");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(colNames[0]))
                    {
                        lock (m_GeneratedTableCodeList)
                        {
                            if (m_GeneratedTableCodeList.Contains(table))
                                Debug.LogError("????");
                            else
                                m_GeneratedTableCodeList.Add(table);
                        }
                        Debug.LogError($"Table Generate Error - {table.tableName} 첫번째 열 이름이 없음");
                        return;
                    }
                    
                    if (string.IsNullOrWhiteSpace(colTypes[0]))
                    {
                        lock (m_GeneratedTableCodeList)
                        {
                            if (m_GeneratedTableCodeList.Contains(table))
                                Debug.LogError("????");
                            else
                                m_GeneratedTableCodeList.Add(table);
                        }
                        Debug.LogError($"Table Generate Error - {table.tableName} 첫번째 열 타입이 없음");
                        return;
                    }

                    var strBuilder = new System.Text.StringBuilder();

                    // Generate Table Struct Code
                    try
                    {
                        strBuilder.Clear();
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
                            else if (colTypes[colIdx].StartsWith("array:"))
                            {
                                colType = colTypes[colIdx].Substring(6) + "[]";
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
                        strBuilder.AppendLineFormat("\t\tpublic {0}(System.Xml.XmlReader xmlReader)", table.tableName);
                        strBuilder.AppendLine("\t\t{");
                        for (var colIdx = 0; colIdx < colCnt; colIdx ++)
                        {
                            if (string.IsNullOrWhiteSpace(colNames[colIdx])) continue;
                            if (string.IsNullOrWhiteSpace(colTypes[colIdx])) continue;

                            strBuilder.AppendLineFormat("\t\t\t{0} = default;", colNames[colIdx]);
                            if (colTypes[colIdx].StartsWith("enum:"))
                            {
                                strBuilder.AppendLineFormat("\t\t\tParseUtility.TryParseEnum(xmlReader.GetAttribute(\"{0}\"), out {0});", colNames[colIdx]);
                            }
                            else if (colTypes[colIdx].StartsWith("array:"))
                            {
                                var arrayTypeName = colTypes[colIdx].Substring(6);
                                switch (arrayTypeName)
                                {
                                    case "byte":
                                    case "Byte":
                                        strBuilder.AppendLineFormat("\t\t\tif (ParseUtility.TryParseArrayByte(xmlReader.GetAttribute(\"{0}\"), out {0}) == false) {0} = Array.Empty<{1}>();", colNames[colIdx], arrayTypeName);
                                        break;
                                    case "short":
                                    case "Int16":
                                        strBuilder.AppendLineFormat("\t\t\tif (ParseUtility.TryParseArrayByte(xmlReader.GetAttribute(\"{0}\"), out {0}) == false) {0} = Array.Empty<{1}>();", colNames[colIdx], arrayTypeName);
                                        break;
                                    case "int":
                                    case "Int32":
                                        strBuilder.AppendLineFormat("\t\t\tif (ParseUtility.TryParseArrayInt(xmlReader.GetAttribute(\"{0}\"), out {0}) == false) {0} = Array.Empty<{1}>();", colNames[colIdx], arrayTypeName);
                                        break;
                                    case "long":
                                    case "Int64":
                                        strBuilder.AppendLineFormat("\t\t\tif (ParseUtility.TryParseArrayLong(xmlReader.GetAttribute(\"{0}\"), out {0}) == false) {0} = Array.Empty<{1}>();", colNames[colIdx], arrayTypeName);
                                        break;
                                    case "decimal":
                                    case "Decimal":
                                        strBuilder.AppendLineFormat("\t\t\tif (ParseUtility.TryParseArrayDecimal(xmlReader.GetAttribute(\"{0}\"), out {0}) == false) {0} = Array.Empty<{1}>();", colNames[colIdx], arrayTypeName);
                                        break;
                                    case "float":
                                    case "Single":
                                        strBuilder.AppendLineFormat("\t\t\tif (ParseUtility.TryParseArrayFloat(xmlReader.GetAttribute(\"{0}\"), out {0}) == false) {0} = Array.Empty<{1}>();", colNames[colIdx], arrayTypeName);
                                        break;
                                    case "double":
                                    case "Double":
                                        strBuilder.AppendLineFormat("\t\t\tif (ParseUtility.TryParseArrayDouble(xmlReader.GetAttribute(\"{0}\"), out {0}) == false) {0} = Array.Empty<{1}>();", colNames[colIdx], arrayTypeName);
                                        break;
                                    case "bool":
                                    case "Boolean":
                                        strBuilder.AppendLineFormat("\t\t\tif (ParseUtility.TryParseArrayBool(xmlReader.GetAttribute(\"{0}\"), out {0}) == false) {0} = Array.Empty<{1}>();", colNames[colIdx], arrayTypeName);
                                        break;
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
                                        strBuilder.AppendLineFormat("\t\t\t{0} = new {1}(xmlReader.GetAttribute(\"{0}\") == null ? string.Empty : xmlReader.GetAttribute(\"{0}\"));",
                                            colNames[colIdx],
                                            colTypes[colIdx]);
                                        break;
                                    case "string":
                                    case "String":
                                        strBuilder.AppendLineFormat("\t\t\t{0} = xmlReader.GetAttribute(\"{0}\");", colNames[colIdx]);
                                        break;
                                    case "byte":
                                    case "Byte":
                                        strBuilder.AppendLineFormat("\t\t\tbyte.TryParse(xmlReader.GetAttribute(\"{0}\"), out {0});", colNames[colIdx]);
                                        break;
                                    case "short":
                                    case "Int16":
                                        strBuilder.AppendLineFormat("\t\t\tshort.TryParse(xmlReader.GetAttribute(\"{0}\"), out {0});", colNames[colIdx]);
                                        break;
                                    case "int":
                                    case "Int32":
                                        strBuilder.AppendLineFormat("\t\t\tint.TryParse(xmlReader.GetAttribute(\"{0}\"), out {0});", colNames[colIdx]);
                                        break;
                                    case "long":
                                    case "Int64":
                                        strBuilder.AppendLineFormat("\t\t\tlong.TryParse(xmlReader.GetAttribute(\"{0}\"), out {0});", colNames[colIdx]);
                                        break;
                                    case "decimal":
                                    case "Decimal":
                                        strBuilder.AppendLineFormat("\t\t\tdecimal.TryParse(xmlReader.GetAttribute(\"{0}\"), out {0});", colNames[colIdx]);
                                        break;
                                    case "float":
                                    case "Single":
                                        strBuilder.AppendLineFormat("\t\t\tfloat.TryParse(xmlReader.GetAttribute(\"{0}\"), out {0});", colNames[colIdx]);
                                        break;
                                    case "double":
                                    case "Double":
                                        strBuilder.AppendLineFormat("\t\t\tdouble.TryParse(xmlReader.GetAttribute(\"{0}\"), out {0});", colNames[colIdx]);
                                        break;
                                    case "bool":
                                    case "Boolean":
                                        strBuilder.AppendLineFormat("\t\t\tbool.TryParse(xmlReader.GetAttribute(\"{0}\"), out {0});", colNames[colIdx]);
                                        break;
                                    case "Vector2Int":
                                        strBuilder.AppendLineFormat("\t\t\tParseUtility.TryParseVector2Int(xmlReader.GetAttribute(\"{0}\"), out {0});", colNames[colIdx]);
                                        break;
                                    case "int2":
                                        strBuilder.AppendLineFormat("\t\t\tParseUtility.TryParseInt2(xmlReader.GetAttribute(\"{0}\"), out {0});", colNames[colIdx]);
                                        break;
                                    case "Vector3Int":
                                        strBuilder.AppendLineFormat("\t\t\tParseUtility.TryParseVector3Int(xmlReader.GetAttribute(\"{0}\"), out {0});", colNames[colIdx]);
                                        break;
                                    case "int3":
                                        strBuilder.AppendLineFormat("\t\t\tParseUtility.TryParseInt3(xmlReader.GetAttribute(\"{0}\"), out {0});", colNames[colIdx]);
                                        break;
                                    case "int4":
                                        strBuilder.AppendLineFormat("\t\t\tParseUtility.TryParseInt4(xmlReader.GetAttribute(\"{0}\"), out {0});", colNames[colIdx]);
                                        break;
                                    case "Vector2":
                                        strBuilder.AppendLineFormat("\t\t\tParseUtility.TryParseVector2(xmlReader.GetAttribute(\"{0}\"), out {0});", colNames[colIdx]);
                                        break;
                                    case "float2":
                                        strBuilder.AppendLineFormat("\t\t\tParseUtility.TryParseFloat2(xmlReader.GetAttribute(\"{0}\"), out {0});", colNames[colIdx]);
                                        break;
                                    case "Vector3":
                                        strBuilder.AppendLineFormat("\t\t\tParseUtility.TryParseVector3(xmlReader.GetAttribute(\"{0}\"), out {0});", colNames[colIdx]);
                                        break;
                                    case "float3":
                                        strBuilder.AppendLineFormat("\t\t\tParseUtility.TryParseFloat3(xmlReader.GetAttribute(\"{0}\"), out {0});", colNames[colIdx]);
                                        break;
                                    case "Vector4":
                                        strBuilder.AppendLineFormat("\t\t\tParseUtility.TryParseVector4(xmlReader.GetAttribute(\"{0}\"), out {0});", colNames[colIdx]);
                                        break;
                                    case "float4":
                                        strBuilder.AppendLineFormat("\t\t\tParseUtility.TryParseFloat4(xmlReader.GetAttribute(\"{0}\"), out {0});", colNames[colIdx]);
                                        break;
                                    case "Color":
                                        strBuilder.AppendLineFormat("\t\t\tParseUtility.TryParseColor(xmlReader.GetAttribute(\"{0}\"), out {0});", colNames[colIdx]);
                                        break;
                                    case "ColorCode":
                                        strBuilder.AppendLineFormat("\t\t\tColorUtility.TryParseHtmlString(xmlReader.GetAttribute(\"{0}\"), out {0});", colNames[colIdx]);
                                        break;
                                }
                            }
                        }
                        strBuilder.AppendLine("\t\t}");
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
                            else if (colTypes[colIdx].StartsWith("array:"))
                            {
                                var arrayTypeName = colTypes[colIdx].Substring(6);
                                strBuilder.AppendLineFormat("\t\t\t{0} = new {1}[binaryReader.ReadInt32()];", colNames[colIdx], arrayTypeName);
                                strBuilder.AppendLineFormat("\t\t\tfor (var i = 0; i < {0}.Length; i ++)", colNames[colIdx]);
                                switch (arrayTypeName)
                                {
                                    case "byte":
                                    case "Byte":
                                        strBuilder.AppendLineFormat("\t\t\t\t{0}[i] = binaryReader.ReadByte();", colNames[colIdx]);
                                        break;
                                    case "short":
                                    case "Int16":
                                        strBuilder.AppendLineFormat("\t\t\t\t{0}[i] = binaryReader.ReadInt16();", colNames[colIdx]);
                                        break;
                                    case "int":
                                    case "Int32":
                                        strBuilder.AppendLineFormat("\t\t\t\t{0}[i] = binaryReader.ReadInt32();", colNames[colIdx]);
                                        break;
                                    case "long":
                                    case "Int64":
                                        strBuilder.AppendLineFormat("\t\t\t\t{0}[i] = binaryReader.ReadInt64();", colNames[colIdx]);
                                        break;
                                    case "decimal":
                                    case "Decimal":
                                        strBuilder.AppendLineFormat("\t\t\t\t{0}[i] = binaryReader.ReadDecimal();", colNames[colIdx]);
                                        break;
                                    case "float":
                                    case "Single":
                                        strBuilder.AppendLineFormat("\t\t\t\t{0}[i] = binaryReader.ReadSingle();", colNames[colIdx]);
                                        break;
                                    case "double":
                                    case "Double":
                                        strBuilder.AppendLineFormat("\t\t\t\t{0}[i] = binaryReader.ReadDouble();", colNames[colIdx]);
                                        break;
                                    case "bool":
                                    case "Boolean":
                                        strBuilder.AppendLineFormat("\t\t\t\t{0}[i] = binaryReader.ReadBoolean();", colNames[colIdx]);
                                        break;
                                    default:
                                        strBuilder.AppendLineFormat("\t\t\t\t{0}[i] = default;", colNames[colIdx]);
                                        break;
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
                                    case "int4":
                                        strBuilder.AppendLineFormat("\t\t\t{0} = new {1}(binaryReader.ReadInt32(), binaryReader.ReadInt32(), binaryReader.ReadInt32(), binaryReader.ReadInt32());",
                                            colNames[colIdx],
                                            colTypes[colIdx]);
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
                                        strBuilder.AppendLineFormat("\t\t\t{0} = new {1}(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());",
                                            colNames[colIdx],
                                            colTypes[colIdx]);
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
                        strBuilder.AppendLineFormat("\t\tpublic void ExportBinary(System.IO.BinaryWriter binaryWriter)", table.tableName);
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
                                    strBuilder.AppendLineFormat("\t\t\tbinaryWriter.Write(({1}){0});", colNames[colIdx], underlyingType.Name);
                                }
                            }
                            else if (colTypes[colIdx].StartsWith("array:"))
                            {
                                strBuilder.AppendLineFormat("\t\t\tbinaryWriter.Write({0} == null ? 0 : {0}.Length);", colNames[colIdx]);
                                strBuilder.AppendLineFormat("\t\t\tif ({0} != null) for (var i = 0; i < {0}.Length; i ++) binaryWriter.Write({0}[i]);", colNames[colIdx]);
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
                                        strBuilder.AppendLineFormat("\t\t\tbinaryWriter.Write({0}.ToString());", colNames[colIdx]);
                                        break;
                                    case "string":
                                    case "String":
                                    case "byte":
                                    case "Byte":
                                    case "short":
                                    case "Int16":
                                    case "int":
                                    case "Int32":
                                    case "long":
                                    case "Int64":
                                    case "decimal":
                                    case "Decimal":
                                    case "float":
                                    case "Single":
                                    case "double":
                                    case "Double":
                                    case "bool":
                                    case "Boolean":
                                        strBuilder.AppendLineFormat("\t\t\tbinaryWriter.Write({0});", colNames[colIdx]);
                                        break;
                                    case "Vector2Int":
                                    case "int2":
                                    case "Vector2":
                                    case "float2":
                                        strBuilder.AppendLineFormat("\t\t\tbinaryWriter.Write({0}.x);", colNames[colIdx]);
                                        strBuilder.AppendLineFormat("\t\t\tbinaryWriter.Write({0}.y);", colNames[colIdx]);
                                        break;
                                    case "Vector3Int":
                                    case "int3":
                                    case "Vector3":
                                    case "float3":
                                        strBuilder.AppendLineFormat("\t\t\tbinaryWriter.Write({0}.x);", colNames[colIdx]);
                                        strBuilder.AppendLineFormat("\t\t\tbinaryWriter.Write({0}.y);", colNames[colIdx]);
                                        strBuilder.AppendLineFormat("\t\t\tbinaryWriter.Write({0}.z);", colNames[colIdx]);
                                        break;
                                    case "int4":
                                    case "Vector4":
                                    case "float4":
                                        strBuilder.AppendLineFormat("\t\t\tbinaryWriter.Write({0}.x);", colNames[colIdx]);
                                        strBuilder.AppendLineFormat("\t\t\tbinaryWriter.Write({0}.y);", colNames[colIdx]);
                                        strBuilder.AppendLineFormat("\t\t\tbinaryWriter.Write({0}.z);", colNames[colIdx]);
                                        strBuilder.AppendLineFormat("\t\t\tbinaryWriter.Write({0}.w);", colNames[colIdx]);
                                        break;
                                    case "Color":
                                    case "ColorCode":
                                        strBuilder.AppendLineFormat("\t\t\tbinaryWriter.Write((byte)({0}.r * byte.MaxValue));", colNames[colIdx]);
                                        strBuilder.AppendLineFormat("\t\t\tbinaryWriter.Write((byte)({0}.g * byte.MaxValue));", colNames[colIdx]);
                                        strBuilder.AppendLineFormat("\t\t\tbinaryWriter.Write((byte)({0}.b * byte.MaxValue));", colNames[colIdx]);
                                        strBuilder.AppendLineFormat("\t\t\tbinaryWriter.Write((byte)({0}.a * byte.MaxValue));", colNames[colIdx]);
                                        break;
                                }
                            }
                        }
                        strBuilder.AppendLine("\t\t}");
                        strBuilder.AppendLine("\t}");
                        strBuilder.AppendLine("}");
                        System.IO.File.WriteAllText(System.IO.Path.Combine(m_ExportCodeTempPath, $"{table.tableName}.cs"), strBuilder.ToString());
                    }
                    catch (Exception e)
                    {
                        lock (m_GeneratedTableCodeList)
                        {
                            if (m_GeneratedTableCodeList.Contains(table))
                                Debug.LogError("????");
                            else
                                m_GeneratedTableCodeList.Add(table);
                        }
                        Debug.LogError($"Table Generate Error - {table.tableName} Struct 코드 저장 실패\n{e}");
                        return;
                    }

                    // Generate Table Load, Export Code
                    try
                    {
                        strBuilder.Clear();
                        strBuilder.AppendLine("using System.Collections;");
                        strBuilder.AppendLine("using System.Collections.Generic;");
                        strBuilder.AppendLine("using System.Linq;");
                        strBuilder.AppendLine("namespace GoogleSheetsTable");
                        strBuilder.AppendLine("{");
                        strBuilder.AppendLine("\tpublic partial class TableManager");
                        strBuilder.AppendLine("\t{");
                        strBuilder.AppendLineFormat("\t\tprivate readonly Dictionary<{1}, {0}> m_Dic{0} = new Dictionary<int, {0}>();", table.tableName, colTypes[0]);
                        strBuilder.AppendLineFormat("\t\tpublic void LoadTable_{0}(System.Xml.XmlReader xmlReader)", table.tableName);
                        strBuilder.AppendLine("\t\t{");
                        strBuilder.AppendLineFormat("\t\t\tm_Dic{0}.Clear();", table.tableName);
                        strBuilder.AppendLine("\t\t\twhile (xmlReader.Read())");
                        strBuilder.AppendLine("\t\t\t{");
                        strBuilder.AppendLine("\t\t\t\tif (xmlReader.NodeType != System.Xml.XmlNodeType.Element) continue;");
                        strBuilder.AppendLineFormat("\t\t\t\tif (xmlReader.Name != \"{0}\") continue;", table.tableName);
                        strBuilder.AppendLineFormat("\t\t\t\tvar data = new {0}(xmlReader);", table.tableName);
                        strBuilder.AppendLineFormat("\t\t\t\tm_Dic{0}.Add(data.{1}, data);", table.tableName, colNames[0]);
                        strBuilder.AppendLine("\t\t\t}");
                        strBuilder.AppendLine("\t\t}");
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
                        strBuilder.AppendLineFormat("\t\tpublic void ExportBinary_{0}(System.IO.BinaryWriter binaryWriter)", table.tableName);
                        strBuilder.AppendLine("\t\t{");
                        strBuilder.AppendLineFormat("\t\t\tbinaryWriter.Write(m_Dic{0}.Count);", table.tableName);
                        strBuilder.AppendLineFormat("\t\t\tforeach (var data in m_Dic{0}.Values)", table.tableName);
                        strBuilder.AppendLine("\t\t\t{");
                        strBuilder.AppendLine("\t\t\t\tdata.ExportBinary(binaryWriter);");
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
                        System.IO.File.WriteAllText(System.IO.Path.Combine(m_ExportCodeTempPath, $"TableManager_{table.tableName}.cs"), strBuilder.ToString());
                    }
                    catch (Exception e)
                    {
                        lock (m_GeneratedTableCodeList)
                        {
                            if (m_GeneratedTableCodeList.Contains(table))
                                Debug.LogError("????");
                            else
                                m_GeneratedTableCodeList.Add(table);
                        }
                        Debug.LogError($"Table Generate Error - {table.tableName} TableManager 코드 저장 실패\n{e}");
                        return;
                    }

                    lock (m_GeneratedTableCodeList)
                    {
                        if (m_GeneratedTableCodeList.Contains(table))
                            Debug.LogError("????");
                        else
                            m_GeneratedTableCodeList.Add(table);
                    }
                });
        }

        private void _GenerateTable_Xml(GoogleSheetsSetting.Table table)
        {
            GoogleSheetsAPI.Instance.RequestTable(table.spreadsheetId,
                $"{table.sheetName}!{table.dataRange}",
                values =>
                {
                    if (values == null || values.Count == 0)
                    {
                        lock (m_GeneratedTableXmlList)
                        {
                            m_GeneratedTableXmlList.Add(table);
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
                        lock (m_GeneratedTableXmlList)
                        {
                            m_GeneratedTableXmlList.Add(table);
                        }
                        Debug.LogError($"Table Generate Error - {table.tableName} 열 갯수 부족");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(colNames[0]))
                    {
                        lock (m_GeneratedTableXmlList)
                        {
                            m_GeneratedTableXmlList.Add(table);
                        }
                        Debug.LogError($"Table Generate Error - {table.tableName} 첫번째 열 이름이 없음");
                        return;
                    }
                    
                    if (string.IsNullOrWhiteSpace(colTypes[0]))
                    {
                        lock (m_GeneratedTableXmlList)
                        {
                            m_GeneratedTableXmlList.Add(table);
                        }
                        Debug.LogError($"Table Generate Error - {table.tableName} 첫번째 열 타입이 없음");
                        return;
                    }

                    // Generate Xml
                    try
                    {
                        var fileStream = new System.IO.FileStream(System.IO.Path.Combine(m_ExportXmlTempPath, $"{table.tableName.ToLower()}.xml"), System.IO.FileMode.Create);
                        var xmlWriterSetting = new System.Xml.XmlWriterSettings();
                        xmlWriterSetting.Encoding = new System.Text.UTF8Encoding(false);
                        xmlWriterSetting.Indent = true;
                        xmlWriterSetting.NewLineChars = "\n";
                        var xmlWriter = System.Xml.XmlWriter.Create(fileStream, xmlWriterSetting);
                        xmlWriter.WriteStartDocument();
                        xmlWriter.WriteStartElement($"{table.tableName}Table");
                        for (var rowIdx = 2; rowIdx < values.Count; rowIdx ++)
                        {
                            xmlWriter.WriteStartElement($"{table.tableName}");
                            var row = values[rowIdx];
                            for (var colIdx = 0; colIdx < row.Count; colIdx ++)
                            {
                                if (string.IsNullOrWhiteSpace(colNames[colIdx])) continue;
                                if (string.IsNullOrWhiteSpace(colTypes[colIdx])) continue;

                                var value = row[colIdx];
                                var valueStr = value == null ? string.Empty : value.ToString();
                                if (colIdx == 0 && string.IsNullOrWhiteSpace(valueStr)) break;

                                xmlWriter.WriteAttributeString(colNames[colIdx], valueStr);
                            }
                            xmlWriter.WriteEndElement();
                        }
                        xmlWriter.WriteEndElement();
                        xmlWriter.WriteEndDocument();
                        xmlWriter.Close();
                        fileStream.Close();
                    }
                    catch (Exception e)
                    {
                        lock (m_GeneratedTableXmlList)
                        {
                            m_GeneratedTableXmlList.Add(table);
                        }
                        Debug.LogError($"Table Generate Error - {table.tableName} Xml 저장 실패\n{e}");
                        return;
                    }
                    
                    lock (m_GeneratedTableXmlList)
                    {
                        m_GeneratedTableXmlList.Add(table);
                    }
                });
        }
    }
}