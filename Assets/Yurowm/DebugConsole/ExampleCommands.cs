using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Yurowm.DebugTools {
    public class HelloWorld : ICommand {
        public override IEnumerator Execute(params string[] args) {
            yield return "Hello buddy! :)";
            foreach (string arg in args) {
                yield return arg;
                yield return new WaitForSeconds(1f);
            }
        }

        public override string GetName() {
            return "hello";
        }
    }

    public class SceneResearch : ICommand {

        Dictionary<string, Func<string[], IEnumerator>> sublogics;

        public SceneResearch() {
            sublogics = new Dictionary<string, Func<string[], IEnumerator>>();
            sublogics.Add("list", ListOfChilds);
            sublogics.Add("select", SelectChild);
            sublogics.Add("details", PrintDetails);
            sublogics.Add("destroy", DestroySelected);
        }

        public override string Help() {
            StringBuilder builder = new StringBuilder();
            string format = GetName() + " {0} - {1}";
            builder.AppendLine(string.Format(format, "list", "show list of child objects"));
            builder.AppendLine(string.Format(format, "select @3", "select third child object"));
            builder.AppendLine(string.Format(format, "select @root", "select root of the scene"));
            builder.AppendLine(string.Format(format, "select @parent", "select parent object"));
            builder.AppendLine(string.Format(format, "select ABC", "select child object with ABC name"));
            builder.AppendLine(string.Format(format, "details", "show details of selected object"));
            builder.AppendLine(string.Format(format, "destroy", "destroy selected object"));
            return builder.ToString();
        }

        public override IEnumerator Execute(params string[] args) {
            IEnumerator sublogic = null;

            if (args.Length > 0 && sublogics.ContainsKey(args[0]))
                sublogic = sublogics[args[0]](args.Skip(1).ToArray());

            if (sublogic == null)
                sublogic = sublogics["help"](args.Skip(1).ToArray());

            while (sublogic.MoveNext())
                yield return sublogic.Current;
        }

        GameObject currentObject = null;
        IEnumerator ListOfChilds(params string[] args) {
            yield return DebugConsole.ColorizeText(string.Format("Childs of {0}", currentObject ? currentObject.name : "@Root"), Color.green, true);

            var childs = Childs(currentObject);
            if (childs.Length == 0)
                yield return "None...";
            else {
                for (int i = 0; i < childs.Length; i++)
                    yield return i + ". " + childs[i].name;
            }
        }

        IEnumerator DestroySelected(params string[] args) {
            if (currentObject) {
                Transform parent = currentObject.transform.parent;
                MonoBehaviour.Destroy(currentObject);
                yield return DebugConsole.Success(currentObject.name + " is removed");
                currentObject = parent ? parent.gameObject : null;
				yield return DebugConsole.Success((currentObject ? currentObject.name : "@Root") + " is selected");
            } else 
                yield return DebugConsole.Error("@Root can't be removed");
        }

        IEnumerator SelectChild(params string[] args) {
            if (args.Length == 0) {
                yield return DebugConsole.Error(
                    "scene select @1 - to select a child with index # 1"
                    + "\nscene select @root - to select the root"
                    + "\nscene select @parent - to select a parent"
                    + "\nscene select ABC - to select a child with ABC name");
                yield break;
            }

            foreach (var arg in args) {
				var childs = Childs(currentObject);
				
                if (childs.Length == 0) {
					yield return DebugConsole.Error("Current object doesn't have any childs");
                    yield break;  
                }

				if (arg.StartsWith("@")) {
					string substring = arg.Substring(1).ToLower();
					switch (substring) {
						case "root": {
								currentObject = null;
								yield return DebugConsole.Success((currentObject ? currentObject.name : "@Root") + " is selected");
							} break;
						case "parent": {
								if (currentObject && currentObject.transform.parent) {
									currentObject = currentObject.transform.parent.gameObject;
									yield return DebugConsole.Success((currentObject ? currentObject.name : "@Root") + " is selected");
                                } else {
									yield return DebugConsole.Error("@Root is already selected");
                                    yield break;                              
                                }
							} break;
						default: {
								int index = -1;
								if (int.TryParse(substring, out index)) {
									if (index >= 0 && index < childs.Length) {
										currentObject = childs[index].gameObject;
										yield return DebugConsole.Success(currentObject.name + " is selected");
                                    } else {
										yield return DebugConsole.Error("Out or range!");
										yield break;
                                    }
                                } else {
									yield return DebugConsole.Error("Wrong format!");
                                    yield break;                              
                                }
							} break;
					}
				} else {
					string name = arg;
					GameObject newChild = childs.FirstOrDefault(c => c.name == name);
					
					if (newChild) {
						currentObject = newChild;
						yield return DebugConsole.Success(currentObject.name + " is selected");
                    } else {
						yield return DebugConsole.Error("The child is not found");
                        yield break;
					}
				}
            }
        }

        IEnumerator PrintDetails(params string[] args) {
            if (currentObject == null)
                yield return DebugConsole.Error("Can't show details of @Root");
            else {
                yield return DebugConsole.Success("Details of " + currentObject.name);
                Type type;
                var components = currentObject.GetComponents<Component>();
                List<string> lines = new List<string>();
                foreach (var component in components) {
                    type = component.GetType();
                    yield return DebugConsole.Alias(type.Name);
                    foreach (FieldInfo info in type.GetFields()) {
                        var value = info.GetValue(component);
                        lines.Add("   " + info.Name + ": " + (value == null ? "null" : value.ToString()));
                    }
                    foreach (PropertyInfo info in type.GetProperties()) {
                        if (info.GetIndexParameters().Length == 0)
                            try {
                                var value = info.GetValue(component, new object[0]);
                                lines.Add("   " + info.Name + ": " + (value == null ? "null" : value.ToString()));
                            } catch (Exception) {}
                    }
                    yield return string.Join("\n", lines.ToArray());
                    lines.Clear();
                }
            }
        }

        GameObject[] Childs(GameObject gameObject) {
            if (gameObject == null)
                return SceneManager.GetActiveScene().GetRootGameObjects();
            else {
                Transform transform = gameObject.transform;
                GameObject[] result = new GameObject[transform.childCount];
                for (int i = 0; i < transform.childCount; i++)
                    result[i] = transform.GetChild(i).gameObject;
                return result;
            }
        }

        public override string GetName() {
            return "scene";
        }
    }

    public class SetCommands : ICommand {

        Dictionary<string, Func<string[], IEnumerator>> sublogics;

        Vector2Int defaultResolution;

        public override string Help() {
            StringBuilder builder = new StringBuilder();
            string format = GetName() + " {0} - {1}";
            builder.AppendLine(string.Format(format, "resolution default", "change screen resolution to default"));
            builder.AppendLine(string.Format(format, "resolution 480 600", "change screen resolution to 480x600"));
            builder.AppendLine(string.Format(format, "framerate default", "change target FPS to default (60)"));
            builder.AppendLine(string.Format(format, "framerate 20", "change target FPS to 20"));
            return builder.ToString();
        }

        public SetCommands() {
            sublogics = new Dictionary<string, Func<string[], IEnumerator>>();
            sublogics.Add("resolution", SetResolution);
            sublogics.Add("framerate", SetFramerate);
            defaultResolution = new Vector2Int(Screen.width, Screen.height);
        }

        public override IEnumerator Execute(params string[] args) {
            IEnumerator sublogic = null;

            if (args.Length > 0 && sublogics.ContainsKey(args[0]))
                sublogic = sublogics[args[0]](args.Skip(1).ToArray());

            if (sublogic != null)
                while (sublogic.MoveNext())
                    yield return sublogic.Current;
        }

        IEnumerator SetResolution(params string[] args) {
            if (args.Length > 0 && args[0] == "default") {
                Screen.SetResolution(defaultResolution.x, defaultResolution.y, true);
                yield return DebugConsole.Success(string.Format("Resolution is set to {0}x{1}", defaultResolution.x, defaultResolution.y));
            }

            if (args.Length != 2) {
                yield return DebugConsole.Error("set resolution 480 600 (example)");
                yield break;
            }

            int width;
            int height;

            if (int.TryParse(args[0], out width) && int.TryParse(args[1], out height)) {
                Screen.SetResolution(width, height, true);
                yield return DebugConsole.Success(string.Format("Resolution is set to {0}x{1}", width, height));
            } else
                yield return DebugConsole.Error("Error of parsing. Use only integer values.");
        }

        IEnumerator SetFramerate(params string[] args) {
            if (args.Length > 0 && args[0] == "default") {
                Application.targetFrameRate = 60;
                yield return DebugConsole.Success(string.Format("Frame rate is set to {0}", Application.targetFrameRate));
            }

            if (args.Length != 1) {
                yield return DebugConsole.Error("set framerate 30 (example)");
                yield break;
            }

            int target;

            if (int.TryParse(args[0], out target)) {
                Application.targetFrameRate = target;
                yield return DebugConsole.Success(string.Format("Frame rate is set to {0}", Application.targetFrameRate));
            } else
                yield return DebugConsole.Error("Error of parsing. Use only integer values.");
        }

        public override string GetName() {
            return "set";
        }
    }
}