using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GoogleSheetsTable
{
    public class GoogleSheetsSetting : ScriptableObject
    {
        public string exportCodePath = "Assets/Scripts/TableGenerated";
        public string exportXmlPath = "Assets/Resources/Data";
        public Table[] tableSettings;
        
        
        [Serializable]
        public struct KeyField
        {
            [Serializable]
            public enum Type
            {
                Key = 0,
                Index = 1,
            }
            
            public Type type;
            public string fieldName;
        }

        [Serializable]
        public struct Table
        {
            public string tableName;
            public string spreadsheetId;
            public string sheetName;
            public string dataRange;
            public bool useNative;
            public KeyField mainKeyField;
            public KeyField subKeyField;
        }
    }
}
