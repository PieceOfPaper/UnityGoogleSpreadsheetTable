using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GoogleSheetsTable
{
    public class GoogleSheetsSetting : ScriptableObject
    {
        public const string PATH = "Assets/GoogleSheetsTable/Setting.asset";
        
        public string googleClientSecretsPath;
        public Table[] tableSettings;

        [System.Serializable]
        public struct Table
        {
            public string spreadsheetId;
            public string sheetName;
            public string dataRange;
        }

#if UNITY_EDITOR

        [UnityEditor.MenuItem("Tools/Google Sheets Table/Create Setting")]
        private static void CreateSetting()
        {
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance(typeof(GoogleSheetsSetting)), PATH);
        }

#endif
    }
}
