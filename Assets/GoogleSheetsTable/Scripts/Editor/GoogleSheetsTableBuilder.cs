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


        private GoogleSheetsSetting m_Setting;
        private bool m_IsSettingModified;

        private void OnGUI()
        {
            if (m_Setting == null)
            {
                m_Setting = AssetDatabase.LoadAssetAtPath<GoogleSheetsSetting>(GoogleSheetsSetting.PATH);
            }

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
            EditorGUILayout.Space();

            if (m_IsSettingModified == true)
            {
                m_IsSettingModified = false;
                EditorUtility.SetDirty(m_Setting);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
}