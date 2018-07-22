using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Yurowm.DebugTools {
    public class DebugPanelView : EditorWindow {
        static DebugPanelView instance = null;

        [MenuItem("Window/DebugTools/Debug Panel")]
        public static DebugPanelView CreateBerryPanel() {
            DebugPanelView window;
            if (instance == null) {
                window = GetWindow<DebugPanelView>();
                window.Show();
                window.OnEnable();
            } else {
                window = instance;
                window.Show();
            }
            return window;
        }

        float messageHeight = 20;

        void OnEnable() {
            instance = this;
            titleContent.text = "Debug Panel";
        }

        GUIStyle messageNameStyle = null;
        GUIStyle messageValueStyle = null;
        GUIStyle categoryStyle = null;
        GUIStyle outputBackgroundStyle = null;
        Texture2D blackTexture = null;

        void InitializeStyles() {
            blackTexture = new Texture2D(1, 1);
            blackTexture.SetPixel(0, 0, Color.black);
            blackTexture.Apply();

            messageNameStyle = new GUIStyle(EditorStyles.label);
            messageNameStyle.normal.textColor = Color.white;
            messageNameStyle.alignment = TextAnchor.MiddleLeft;
            messageNameStyle.clipping = TextClipping.Clip;
            messageNameStyle.hover = messageNameStyle.normal;
            messageNameStyle.active = messageNameStyle.normal;
            messageNameStyle.focused = messageNameStyle.normal;
            messageNameStyle.fontSize = 12;

            messageValueStyle = new GUIStyle(messageNameStyle);
            messageValueStyle.fontStyle = FontStyle.Bold;

            categoryStyle = new GUIStyle(EditorStyles.miniButton);
            categoryStyle.normal.background = Texture2D.whiteTexture;
            categoryStyle.normal.textColor = Color.black;
            categoryStyle.alignment = TextAnchor.MiddleLeft;
            categoryStyle.clipping = TextClipping.Clip;
            categoryStyle.hover = categoryStyle.normal;
            categoryStyle.active = categoryStyle.normal;
            categoryStyle.focused = categoryStyle.normal;
            categoryStyle.fontSize = 12;

            outputBackgroundStyle = new GUIStyle(EditorStyles.textArea);
            outputBackgroundStyle.normal.background = blackTexture;
            outputBackgroundStyle.border = new RectOffset();
            outputBackgroundStyle.margin = new RectOffset();
            outputBackgroundStyle.padding = new RectOffset();

            messageHeight = messageNameStyle.CalcHeight(new GUIContent("Test"), 100);
        }

        void OnGUI() {
            if (blackTexture == null)
                InitializeStyles();

            DrawToolbar();
            DrawLog();
        }

        void DrawToolbar() {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));

            if (EditorApplication.isPlaying) {
                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50))) DebugPanel.Clear();
                if (GUILayout.Button("Show all", EditorStyles.toolbarButton, GUILayout.Width(60))) DebugPanel.Instance.showAllButton.onClick.Invoke();
                if (GUILayout.Button("Hide all", EditorStyles.toolbarButton, GUILayout.Width(60))) DebugPanel.Instance.hideAllButton.onClick.Invoke();
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        const string delegatesCategory = "Delegates";
        Vector2 scrollPosition = new Vector2();
        void DrawLog() {

            EditorGUILayout.BeginVertical(outputBackgroundStyle, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            if (EditorApplication.isPlaying) {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                foreach (var category in DebugPanel.categories.Values) {
                    GUI.color = CategoryColor(category.name);
                    if (GUILayout.Button(CategoryToggle(category.state) + category.name, categoryStyle, GUILayout.ExpandWidth(true)))
                        category.state = !category.state;
                    if (category.state) {
                        if (category.name.Equals(delegatesCategory)) {
                            foreach (var button in DebugPanel.buttons)
                                DrawDelegateMessage(button);
                        } else
                            foreach (var message in DebugPanel.messages.Values) {
                                if (message.category.Equals(category.name))
                                    DrawMessage(message);
                            }
                    }
                }
                GUI.color = Color.white;
                EditorGUILayout.EndScrollView();
                Repaint();
            } else 
                EditorGUILayout.HelpBox("Debug Panel works only in Play mode yet.", MessageType.Warning);
            EditorGUILayout.EndVertical();
        }

        Color errorColor = new Color(1f, .5f, .5f);
        Color systemColor = new Color(.5f, 1f, .5f);
        Color delegateColor = new Color(.5f, 1f, 1f);
        Color warningColor = new Color(1f, 1f, .3f);
        Color logColor = new Color(.5f, .5f, .5f);
        Color defaultColor = Color.white;

        Color CategoryColor(string category) {
            switch (category) {
                case "Error": return errorColor;
                case "System": return systemColor;
                case "Delegates": return delegateColor;
                case "Warning": return delegateColor;
                case "Log": return logColor;
                default: return defaultColor;
            }
        }

        const string categoryMinus = "− ";
        const string categoryPlus = "+ ";
        string CategoryToggle(bool value) {
            return value ? categoryMinus : categoryPlus;
        }

        void DrawMessage(DebugPanel.Message message) {
            var mRect = GetMessageRect();

            var rect = new Rect(mRect.x, mRect.y, 120, mRect.height);
            GUI.Label(rect, message.name, messageNameStyle);

            rect.x += rect.width;
            rect.width = mRect.width - rect.width;

            GUI.Label(rect, message.text, messageValueStyle);
        }

        void DrawDelegateMessage(KeyValuePair<string, Button> pair) {
            var mRect = GetMessageRect();

            var rect = new Rect(mRect.x, mRect.y, 120, mRect.height);
            GUI.Label(rect, pair.Key, messageNameStyle);

            rect.x += rect.width;
            rect.width = 70;

            if (GUI.Button(rect, "> Invoke", categoryStyle))
                pair.Value.onClick.Invoke();
        }

        Rect GetMessageRect() {
            var rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(messageHeight));
            rect.x += messageHeight;
            rect.width -= messageHeight;
            return rect;
        }
    }
}