using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yurowm.DebugTools {
    public class DebugConsole : MonoBehaviour {
        static DebugConsole _Instance = null;
        public static DebugConsole Instance {
            get {
                if (!_Instance && Application.isPlaying) {
                    _Instance = FindObjectOfType<DebugConsole>();
                    if (!_Instance) {
                        _Instance = Resources.Load<DebugConsole>("DebugConsole");
                        if (_Instance) {
                            _Instance = Instantiate(_Instance.gameObject).GetComponent<DebugConsole>();
                            _Instance.transform.localPosition = Vector3.zero;
                            _Instance.transform.localRotation = Quaternion.identity;
                            _Instance.transform.localScale = Vector3.one;
                            _Instance.gameObject.SetActive(false);
                            _Instance.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
                            _Instance.name = "DebugConsole";
                        } 
                    }
                }
                return _Instance;
            }
        }

        static DebugConsoleUpdater _Updater = null;
        static DebugConsoleUpdater Updater {
            get {
                if (!_Updater && Application.isPlaying) {
                    _Updater = FindObjectOfType<DebugConsoleUpdater>();
                    if (!_Updater) {
                        _Updater = new GameObject("DebugConsoleUpdater")
                            .AddComponent<DebugConsoleUpdater>();
                        _Updater.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
                    }
                }
                return _Updater;
            }
        }

        public StringBuilder builder = new StringBuilder();

        public Text output;
        public InputField input;
        public Button enter;
        public Button cancel;

        [RuntimeInitializeOnLoadMethod]
        public static void InitializeOnLoad() {
            DebugPanel.AddDelegate("Debug Console", () => {
                DebugPanel.Instance.lockButton.onClick.Invoke();
                Instance.gameObject.SetActive(true);
            });
        }

        void Awake() {
            if (!_Instance) _Instance = this;

            enter.onClick.AddListener(OnSubmit);
            cancel.onClick.AddListener(OnCancel);
            output.text = "";
            input.text = "";
        }

        void OnEnable() {
			enter.gameObject.SetActive(true);
			cancel.gameObject.SetActive(false);
            cancelRequest = false;

            StopAllCoroutines();
            Hello();
        }

        public void Hello() {
            WriteLine(ColorizeText("Write 'help' to see the command list.", Color.gray));
            WriteLine(ColorizeText("Write 'hide' to close the console.", Color.gray));
        }

		bool wasFocused = false;
        void Update() {
            if (wasFocused && Input.GetKeyDown(KeyCode.Return))
                OnSubmit();
            wasFocused = input.isFocused;
        }

        public void OnSubmit() {
            string command = input.text;
            input.text = "";
            input.Select();
            input.ActivateInputField();
            OnSubmit(command);
        }

        public void OnSubmit(string command) {
            command = command.Trim();
            if (string.IsNullOrEmpty(command))
                return;
            WriteLine("<i>> " + command + "</i>");
            Updater.StartCoroutine(Execute(command));
        }

        bool cancelRequest = false;
        void OnCancel() {
            cancelRequest = true;
        }

        IEnumerator Execute(string command) {
            enter.gameObject.SetActive(false);
            cancel.gameObject.SetActive(true);

            cancelRequest = false;

            var logic = Commands.Execute(command, WriteLine);

            while (logic.MoveNext() && !cancelRequest)
                yield return logic.Current;
   
            cancelRequest = false;

            enter.gameObject.SetActive(true);
            cancel.gameObject.SetActive(false);
        }

        public void WriteLine(string command) {
            builder.AppendLine(command);
            output.text = builder.ToString().Trim();
        }

        public static string Error(string text) {
            return ColorizeText(text, Color.red, false, true);
        }

        public static string Success(string text) {
            return ColorizeText(text, Color.green, true);
        }

        public static string Alias(string text) {
            return ColorizeText(text, Color.cyan);
        }

        public static string Warning(string text) {
            return ColorizeText(text, Color.yellow, false, true);
        }

        public static string ColorizeText(string text, Color? color = null, bool bold = false, bool italic = false) {
            StringBuilder builder = new StringBuilder();
            if (color.HasValue)
                builder.Append(string.Format("<color=#{0:X2}{1:X2}{2:X2}{3:X2}>",
                    (byte) (255 * color.Value.r),
                    (byte) (255 * color.Value.g),
                    (byte) (255 * color.Value.b),
                    (byte) (255 * color.Value.a)));
            if (bold) builder.Append("<b>");
            if (italic) builder.Append("<i>");

            builder.Append(text);

            if (italic) builder.Append("</i>");
            if (bold) builder.Append("</b>");
            if (color.HasValue) builder.Append("</color>");

            return builder.ToString();
        }
    }

    class DebugConsoleUpdater : MonoBehaviour {

    }
}