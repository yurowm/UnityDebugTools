using System;
using System.Collections;
using System.Collections.Generic;
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

        float messageHeight = -1;

        void OnEnable() {
            instance = this;
            titleContent.text = "Debug Panel";
        }

        void OnGUI() {
            if (messageHeight < 0)
                messageHeight = EditorStyles.label.CalcHeight(new GUIContent("Test"), 100);

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
        void DrawLog() {
            if (EditorApplication.isPlaying) {
                foreach (var category in DebugPanel.categories.Values) {
                    category.state = GUILayout.Toggle(category.state, category.name, EditorStyles.foldout, GUILayout.ExpandWidth(true));
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
                Repaint();
            } else {
                EditorGUILayout.HelpBox("Debug Panel work's only in Play mode yet.", MessageType.Warning);
            }
        }

        void DrawMessage(DebugPanel.Message message) {
            var mRect = GetMessageRect();

            var rect = new Rect(mRect.x, mRect.y, 120, mRect.height);
            GUI.Label(rect, message.name);

            rect.x += rect.width;
            rect.width = mRect.width - rect.width;

            GUI.Label(rect, message.text, EditorStyles.boldLabel);
        }

        void DrawDelegateMessage(KeyValuePair<string, Button> pair) {
            var mRect = GetMessageRect();

            var rect = new Rect(mRect.x, mRect.y, 120, mRect.height);
            GUI.Label(rect, pair.Key);

            rect.x += rect.width;
            rect.width = 100;

            if (GUI.Button(rect, "Execute", EditorStyles.miniButton))
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