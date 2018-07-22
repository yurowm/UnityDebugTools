using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Yurowm.DebugTools {
    public class DebugPanel : MonoBehaviour {

        static DebugPanel _Instance = null;
        public static DebugPanel Instance {
            get {
                if (!_Instance && Application.isPlaying) { 
                    _Instance = FindObjectOfType<DebugPanel>();
                    if (!_Instance) {
                        _Instance = Resources.Load<DebugPanel>("DebugPanel");
                        if (_Instance) {
                            _Instance = Instantiate(_Instance.gameObject).GetComponent<DebugPanel>();
                            _Instance.transform.localPosition = Vector3.zero;
                            _Instance.transform.localRotation = Quaternion.identity;
                            _Instance.transform.localScale = Vector3.one;
                            _Instance.name = "DebugPanel";
                            _Instance.gameObject.SetActive(false);
                            _Instance.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
                            if (Application.isEditor)
                                _Instance.gameObject.SetActive(false);
                        }
                    }
                }
                return _Instance;
            }
        }

        public bool lockOnStart = true;
        public bool ignoreDefLogOnStart = true;
        public bool hideNewCategories = true;
        public bool onlyInDebugMode = true;

        bool ignoreDefLog = false;
        bool locked = false;

        const string delegatesCategory = "Delegates";

        public GameObject textItemPrefab;
        public GameObject buttonItemPrefab;
        public GameObject categoryItemPrefab;

        public Transform logContent;
        public Transform categoryContent;
        public Transform controlPanel;

        public Button lockButton;
        public Button unlockButton;
        public Button clearButton;
        public Button deflogButton;
        public Button showAllButton;
        public Button hideAllButton;
        public Button closeButton;

        public CanvasGroup group;
        public Transform unlockPanel;


        public static Dictionary<string, Category> categories = new Dictionary<string, Category>();

        public static Dictionary<string, Message> messages = new Dictionary<string, Message>();
        public static Dictionary<string, Button> buttons = new Dictionary<string, Button>();

        // Colors
        static Color systemColor = new Color(0.4f, 1f, 0.6f, 1);
        static Color warningColor = new Color(0.9f, 0.9f, 0.4f, 1);
        static Color errorColor = new Color(1f, 0.4f, 0.4f, 1);

        // FPS
        float fpsUpdateDelay = 0.5f;
        float fpsTime = 0f;
        int FPSCounter = 0;
        public static bool isActive {
            get {
                return Instance && Instance.gameObject.activeSelf;
            }
        }

        // Use this for initialization
        void Awake () {
            if (onlyInDebugMode && !Debug.isDebugBuild) {
                Destroy(gameObject);
                return;
            }

            group.gameObject.SetActive(true);
            unlockPanel.gameObject.SetActive(true);

            lockButton.onClick.AddListener(() => {
                locked = true;
                controlPanel.gameObject.SetActive(false);
                categoryContent.gameObject.SetActive(false);
                unlockPanel.gameObject.SetActive(true);
                group.alpha = 0.5f;
                group.blocksRaycasts = false;
                foreach (Button button in buttons.Values.ToArray())
                    button.gameObject.SetActive(false);
            });

            unlockButton.onClick.AddListener(() => {
                locked = false;
                controlPanel.gameObject.SetActive(true);
                categoryContent.gameObject.SetActive(true);
                unlockPanel.gameObject.SetActive(false);
                group.alpha = 1f;
                group.blocksRaycasts = true;
                if (categories.ContainsKey(delegatesCategory))
                    foreach (Button button in buttons.Values.ToArray())
                        button.gameObject.SetActive(categories[delegatesCategory].state);
            });

            clearButton.onClick.AddListener(Clear);

            deflogButton.GetComponent<Image>().color = ignoreDefLog ? Color.green : Color.white;
            deflogButton.onClick.AddListener(() => {
                ignoreDefLog = !ignoreDefLog;
                deflogButton.GetComponent<Image>().color = ignoreDefLog ? Color.green : Color.white;
            });

            showAllButton.onClick.AddListener(() => {
                Image image;
                Color c;
                foreach (Category category in categories.Values.ToArray()) {
                    category.state = true;
                    image = category.button.GetComponent<Image>();
                    c = image.color;
                    c.a = category.state ? 1f : 0.5f;
                }
            });

            hideAllButton.onClick.AddListener(() => {
                Image image;
                Color c;
                foreach (Category category in categories.Values.ToArray()) {
                    category.state = false;
                    image = category.button.GetComponent<Image>();
                    c = image.color;
                    c.a = category.state ? 1f : 0.5f;
                }
            });

            closeButton.onClick.AddListener(() => {
                Instance.gameObject.SetActive(false);
            });

            if (lockOnStart)
                lockButton.onClick.Invoke();

            if (ignoreDefLogOnStart && Application.isEditor)
                deflogButton.onClick.Invoke();

            Application.logMessageReceived += HandleLog;
        }

        void Update() {
            if (!isActive)
                return;
            FPSCounter++;
            if (fpsTime + fpsUpdateDelay < Time.unscaledTime) {
                FPSCounter = Mathf.RoundToInt(1f * FPSCounter / (Time.unscaledTime - fpsTime));

                Log("FPS", "System", FPSCounter);
                fpsTime = Time.unscaledTime;
                FPSCounter = 0;

                Log("DeviceID", "System", SystemInfo.deviceUniqueIdentifier);
            }
        }

        void OnDestroy() { 
            Application.logMessageReceived -= HandleLog;
            buttons.Clear();
            messages.Clear();
            categories.Clear();
        }

        void HandleLog(string logString, string stackTrace, LogType type) {
            if (ignoreDefLog)
                return;


            switch (type) {
                case LogType.Exception:
                case LogType.Error:
                    Log((stackTrace + logString).GetHashCode().ToString(), "Error", stackTrace, logString);
                    break;
                case LogType.Warning:
                    Log((stackTrace + logString).GetHashCode().ToString(), "Warning", stackTrace, logString);
                    break;
                default:
                    Log((stackTrace + logString).GetHashCode().ToString(), "Log", logString);
                    break;
            }
        }

        public static void Clear() {
            foreach (Message message in messages.Values.ToArray())
                Destroy(message.display.gameObject);
            messages.Clear();
        }

        public static void Log(string _name, object _message) {
            Log(_name, "N/A", _message);
        }

        public static void Log(string _name, string _category, string _stacktrace, object _message) {
            Log(_name, _category, _message.ToString() + "\n" + _stacktrace);
        }

        public static void Log(string _name, string _category, object _message) {
            if (Instance == null)
                return;

            Message message;
            string key = _name + "_" + _category;

            if (!messages.ContainsKey(key)) {
                message = new Message();
                message.name = _name;
                message.category = _category;
                RegisterNewMessage(ref message);
            } else
                message = messages[key];

            message.text = _message.ToString();

            message.Update();
        }

        public static void AddDelegate(string _name, UnityEngine.Events.UnityAction action) {
            if (Instance == null)
                return;

            Button button;
            if (!buttons.ContainsKey(_name)) {
                button = RegisterNewDelegate(_name);
            } else
                button = buttons[_name];

            button.onClick.AddListener(action);
        }

        static Button RegisterNewDelegate(string _name) {
            GameObject buttonItem = Instantiate(Instance.buttonItemPrefab);
            buttonItem.transform.SetParent(Instance.logContent);
            buttonItem.transform.localScale = Vector3.one;

            buttonItem.GetComponentInChildren<Text>().text = _name;

            if (!categories.ContainsKey(delegatesCategory))
                RegisterNewCategory(delegatesCategory);

            Button button = buttonItem.GetComponent<Button>();
            buttons.Add(_name, button);

            button.gameObject.SetActive(!Instance.locked && categories[delegatesCategory].state);

            return button;
        }

        static void RegisterNewMessage(ref Message message) {
            GameObject textItem = Instantiate(Instance.textItemPrefab);
            textItem.transform.SetParent(Instance.logContent);
            textItem.transform.localScale = Vector3.one;

            message.display = textItem.GetComponent<Text>();

            Color color;
            switch (message.category) {
			    case "System": color = systemColor; break;
			    case "Warning": color = warningColor; break;
			    case "Error": color = errorColor; break;
			    default: color = Color.white; break;
		    }
            message.display.color = color;

            if (!categories.ContainsKey(message.category))
                RegisterNewCategory(message.category);

            messages.Add(message.name + "_" + message.category, message);
        }

        static void RegisterNewCategory(string _category) {
            if (categories.ContainsKey(_category))
                return;

            Category category = new Category();

            GameObject categoryItem = Instantiate(Instance.categoryItemPrefab);
            categoryItem.transform.SetParent(Instance.categoryContent);
            categoryItem.transform.localScale = Vector3.one;
            category.button = categoryItem.GetComponent<Button>();
            category.state = !Instance.hideNewCategories;
            category.name = _category;
            categoryItem.GetComponentInChildren<Text>().text = _category;

            Color color;
            switch (_category) {
                case "System":
                    color = systemColor;
                    break;
                case "Warning":
                    color = warningColor;
                    break;
                case "Error":
                    color = errorColor;
                    break;
                default:
                    color = Color.white;
                    break;
            }
            color.a = category.state ? 1f : 0.5f;
            categoryItem.GetComponent<Image>().color = color;

            category.button.onClick.AddListener(() => {
                category.state = !category.state;
                Image image = category.button.GetComponent<Image>();
                Color c = image.color;
                c.a = category.state ? 1f : 0.5f;
                image.color = c;
                category.Update();
            });

            categories.Add(_category, category);
        }

        public class Message {
            public string name;
            public string category;
            public string text;
            public Text display;

            public void Update() {
                display.text = name + ": " + text;
                display.gameObject.SetActive(categories[category].state);
            }
        }

        public class Category {
            public string name;
            public bool state;
            public Button button;

            public void Update() {
                if (name == delegatesCategory)
                    foreach (Button button in buttons.Values.ToArray())
                        button.gameObject.SetActive(state);
                else 
                    foreach (Message message in messages.Values.ToArray())
                        if (message.category == name)
                            message.display.gameObject.SetActive(state);
            }
        }
    }
}