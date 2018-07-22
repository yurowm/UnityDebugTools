using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Yurowm.DebugTools {
    public class DebugConsoleView : EditorWindow {
        static DebugConsoleView instance = null;

        [MenuItem("Window/DebugTools/Debug Console")]
        public static DebugConsoleView CreateBerryPanel() {
            DebugConsoleView window;
            if (instance == null) {
                window = GetWindow<DebugConsoleView>();
                window.Show();
                window.OnEnable();
            } else {
                window = instance;
                window.Show();
            }
            return window;
        }

        void OnEnable() {
            instance = this;
            blackTexture = null;
            titleContent.text = "Debug Console";
        }

        GUIStyle outputStyle = null;
        GUIStyle outputBackgroundStyle = null;
        Texture2D blackTexture = null;

        void InitializeStyles() {
            blackTexture = new Texture2D(1, 1);
            blackTexture.SetPixel(0, 0, Color.black);
            blackTexture.Apply();

            outputStyle = new GUIStyle(EditorStyles.label);
            outputStyle.normal.textColor = Color.white;
            outputStyle.richText = true;
            outputStyle.wordWrap = true;
            outputStyle.alignment = TextAnchor.LowerLeft;
            outputStyle.clipping = TextClipping.Clip;
            outputStyle.hover = outputStyle.normal;
            outputStyle.active = outputStyle.normal;
            outputStyle.focused = outputStyle.normal;
            outputStyle.fontSize = 14;

            outputBackgroundStyle = new GUIStyle(EditorStyles.textArea);
            outputBackgroundStyle.normal.background = blackTexture;
            outputBackgroundStyle.border = new RectOffset();
            outputBackgroundStyle.margin = new RectOffset();
            outputBackgroundStyle.padding = new RectOffset();
        }

        string offlineOutput = DebugConsole.Warning("The console works only in Play mode.");
        const string controlName = "Command Line";
        Vector2 scrollPosition = new Vector2();
        List<string> commandsHistory = new List<string>();
        int commandsHistoryIndex = 0;
        string command = "";
        bool updateInput = false;
        void OnGUI() {
            if (blackTexture == null)
                InitializeStyles();

            EditorGUILayout.BeginVertical(outputBackgroundStyle, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            GUILayout.TextArea(EditorApplication.isPlaying ?
                DebugConsole.Instance.output.text : offlineOutput,
                outputStyle, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            EditorGUILayout.EndScrollView();

            if (EditorApplication.isPlaying) {
                GUI.SetNextControlName(controlName);
                if (updateInput) {
                    UpdateInput();
                    updateInput = false;
                }
                command = EditorGUILayout.TextField(command, GUILayout.ExpandWidth(true));
                GUI.SetNextControlName("");

                if (GUI.GetNameOfFocusedControl() == controlName) {
                    EditorGUI.FocusTextInControl(controlName);
                    if (Event.current.keyCode == KeyCode.Return) {
                        command = command.Trim();
                        if (!string.IsNullOrEmpty(command)) {
                            try {
                                DebugConsole.Instance.OnSubmit(command);
                                if (commandsHistory.Count == 0 || command != commandsHistory.Last()) {
                                    commandsHistory.Add(command);
                                    commandsHistoryIndex = commandsHistory.Count;
                                }
                            } catch (Exception e) {
                                DebugConsole.Instance.WriteLine(DebugConsole.Error(e.ToString()));
                            }
                            scrollPosition = new Vector2(0, float.MaxValue);
                            command = "";
                            updateInput = true;
                            Repaint();
                        }
                    }
                    else if(Event.current.type == EventType.KeyUp) {
                        if (Event.current.keyCode == KeyCode.DownArrow) {
                            if (commandsHistory.Count > 0) {
                                commandsHistoryIndex++;
                                commandsHistoryIndex = Mathf.Min(commandsHistoryIndex, commandsHistory.Count - 1);
                                command = commandsHistory[commandsHistoryIndex];
                                updateInput = true;
                                Repaint();
                            }
                        } else if (Event.current.keyCode == KeyCode.UpArrow) {
                            if (commandsHistory.Count > 0) {
                                commandsHistoryIndex--;
                                commandsHistoryIndex = Mathf.Max(commandsHistoryIndex, 0);
                                command = commandsHistory[commandsHistoryIndex];
                                updateInput = true;
                                Repaint();
                            }
                        }
                    }
                    EditorGUI.FocusTextInControl(controlName);
                }
            }
            EditorGUILayout.EndVertical();
            Repaint();
        }

        FieldInfo textEditorProvider = null;
        void UpdateInput() {

            if (textEditorProvider == null)
                textEditorProvider = typeof(EditorGUI)
              .GetField("activeEditor", BindingFlags.Static | BindingFlags.NonPublic);

            TextEditor textEditor = textEditorProvider.GetValue(null) as TextEditor;

            if (textEditor == null)
                return;

            //textEditor.text = command;
            textEditor.SelectAll();
            textEditor.Delete();
            textEditor.ReplaceSelection(command);
            textEditor.SelectTextEnd();
        }
    }
}