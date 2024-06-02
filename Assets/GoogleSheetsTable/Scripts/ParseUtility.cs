using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace GoogleSheetsTable
{
    public static class ParseUtility
    {
        private static Dictionary<System.Type, Dictionary<string, object>> m_CachedEnumValues = new Dictionary<System.Type, Dictionary<string, object>>();

        public static bool TryParseEnum<T>(string value, out T output) where T : struct
        {
            object objOutput;
            if (TryParseEnum(typeof(T), value, out objOutput))
            {
                output = (T)objOutput;
                return true;
            }
            output = default;
            return false;
        }

        public static bool TryParseEnum(System.Type type, string str, out object output)
        {
            if (m_CachedEnumValues.ContainsKey(type) == false)
            {
                m_CachedEnumValues.Add(type, new Dictionary<string, object>());
                var names = Enum.GetNames(type);
                var values = Enum.GetValues(type);
                for (int i = 0; i < names.Length; i ++)
                {
                    m_CachedEnumValues[type].Add(names[i], values.GetValue(i));
                }
            }
            
            if (m_CachedEnumValues[type].ContainsKey(str) == false)
            {
                output = null;
                return false;
            }

            output = m_CachedEnumValues[type][str];
            return true;
        }

        public static bool TryParseInt2(string str, out int2 output)
        {
            output = default;
            if (string.IsNullOrWhiteSpace(str))
                return false;
            
            var split = str.Split(',');
            if (split.Length > 0) int.TryParse(split[0].Trim(), out output.x);
            if (split.Length > 1) int.TryParse(split[1].Trim(), out output.y);
            return true;
        }
        
        public static bool TryParseInt3(string str, out int3 output)
        {
            output = default;
            if (string.IsNullOrWhiteSpace(str))
                return false;
            
            var split = str.Split(',');
            if (split.Length > 0) int.TryParse(split[0].Trim(), out output.x);
            if (split.Length > 1) int.TryParse(split[1].Trim(), out output.y);
            if (split.Length > 2) int.TryParse(split[2].Trim(), out output.z);
            return true;
        }
        
        public static bool TryParseInt4(string str, out int4 output)
        {
            output = default;
            if (string.IsNullOrWhiteSpace(str))
                return false;
            
            var split = str.Split(',');
            if (split.Length > 0) int.TryParse(split[0].Trim(), out output.x);
            if (split.Length > 1) int.TryParse(split[1].Trim(), out output.y);
            if (split.Length > 2) int.TryParse(split[2].Trim(), out output.z);
            if (split.Length > 3) int.TryParse(split[3].Trim(), out output.w);
            return true;
        }

        public static bool TryParseFloat2(string str, out float2 output)
        {
            output = default;
            if (string.IsNullOrWhiteSpace(str))
                return false;
            
            var split = str.Split(',');
            if (split.Length > 0) float.TryParse(split[0].Trim(), out output.x);
            if (split.Length > 1) float.TryParse(split[1].Trim(), out output.y);
            return true;
        }
        
        public static bool TryParseFloat3(string str, out float3 output)
        {
            output = default;
            if (string.IsNullOrWhiteSpace(str))
                return false;
            
            var split = str.Split(',');
            if (split.Length > 0) float.TryParse(split[0].Trim(), out output.x);
            if (split.Length > 1) float.TryParse(split[1].Trim(), out output.y);
            if (split.Length > 2) float.TryParse(split[2].Trim(), out output.z);
            return true;
        }
        
        public static bool TryParseFloat4(string str, out float4 output)
        {
            output = default;
            if (string.IsNullOrWhiteSpace(str))
                return false;
            
            var split = str.Split(',');
            if (split.Length > 0) float.TryParse(split[0].Trim(), out output.x);
            if (split.Length > 1) float.TryParse(split[1].Trim(), out output.y);
            if (split.Length > 2) float.TryParse(split[2].Trim(), out output.z);
            if (split.Length > 3) float.TryParse(split[3].Trim(), out output.w);
            return true;
        }
        
        
        public static bool TryParseColor(string str, out Color output)
        {
            float4 value = default;
            if (TryParseFloat4(str, out value))
            {
                output = new Color(value.x, value.y, value.z, value.w);
                return true;
            }
            output = default;
            return false;
        }
        
        public static bool TryParseVector2Int(string str, out Vector2Int output)
        {
            int2 value = default;
            if (TryParseInt2(str, out value))
            {
                output = new Vector2Int(value.x, value.y);
                return true;
            }
            output = default;
            return false;
        }
        
        public static bool TryParseVector3Int(string str, out Vector3Int output)
        {
            int3 value = default;
            if (TryParseInt3(str, out value))
            {
                output = new Vector3Int(value.x, value.y);
                return true;
            }
            output = default;
            return false;
        }

        public static bool TryParseVector2(string str, out Vector2 output)
        {
            float2 value = default;
            if (TryParseFloat2(str, out value))
            {
                output = value;
                return true;
            }
            output = default;
            return false;
        }

        public static bool TryParseVector3(string str, out Vector3 output)
        {
            float3 value = default;
            if (TryParseFloat3(str, out value))
            {
                output = value;
                return true;
            }
            output = default;
            return false;
        }

        public static bool TryParseVector4(string str, out Vector4 output)
        {
            float4 value = default;
            if (TryParseFloat4(str, out value))
            {
                output = value;
                return true;
            }
            output = default;
            return false;
        }
    }
}