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
        public struct Table : IEquatable<Table>
        {
            public string tableName;
            public string spreadsheetId;
            public string sheetName;
            public string dataRange;
            public bool useNative;
            
            public bool Equals(Table o)
            {
                if (this.tableName != o.tableName) return false;
                if (this.spreadsheetId != o.spreadsheetId) return false;
                if (this.sheetName != o.sheetName) return false;
                if (this.dataRange != o.dataRange) return false;
                if (this.useNative != o.useNative) return false;
                return true;
            }
        }
    }
}
