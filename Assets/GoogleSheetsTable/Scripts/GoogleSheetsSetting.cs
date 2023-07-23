using System;
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
        public struct Table : IEquatable<Table>
        {
            public string spreadsheetId;
            public string sheetName;
            public string dataRange;
            
            public bool Equals(Table o)
            {
                if (this.spreadsheetId != o.spreadsheetId) return false;
                if (this.sheetName != o.sheetName) return false;
                if (this.dataRange != o.dataRange) return false;
                return true;
            }
        }

#if UNITY_EDITOR

        [UnityEditor.MenuItem("Tools/Google Sheets Table/Create Setting")]
        public static void CreateSetting()
        {
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance(typeof(GoogleSheetsSetting)), PATH);
        }

#endif
    }
}
