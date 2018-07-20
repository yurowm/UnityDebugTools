using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Yurowm.DebugTools {
    public abstract class ICommand {
        public abstract string GetName();
        public abstract IEnumerator Execute(params string[] args);
        public virtual string Help() {
            return null;
        }
    }

    public static class Commands {
        public readonly static Dictionary<string, ICommand> commands;
        static Commands() {
            commands = new Dictionary<string, ICommand>();
            foreach (Type type in typeof(ICommand).FindInheritorTypes()) {
                ICommand command = (ICommand) Activator.CreateInstance(type);
                commands[command.GetName().ToLower()] = command;
            }
        }

        readonly static Regex wordSplitter = new Regex(@"\s+");
        public static IEnumerator Execute(string command, Action<string> output) {
            string[] words = wordSplitter.Split(command);
            if (words.Length > 0) {
                if (commands.ContainsKey(words[0].ToLower())) {
                    ICommand c = commands[words[0].ToLower()];
                    var logic = c.Execute(words.Skip(1).ToArray());

					Exception exception = null;
                               
                    while (true) {
                        try {
                            if (!logic.MoveNext())
                                break;              
                        } catch (Exception e) {
                            exception = e;
                            break;
                        }

						if (logic.Current is string) output(logic.Current as string);
						yield return logic.Current;
                    }

                    if (exception != null)
                        output(DebugConsole.Error(exception.ToString()));


                } else {
                    output.Invoke("This command is not found");
                    yield break;
                }
            }
        }

        static List<Type> FindInheritorTypes(this Type type) {
            return type.Assembly.GetTypes().Where(x => type != x && type.IsAssignableFrom(x)).ToList();
        }
    }

    public class ClearCommand : ICommand {
        public override IEnumerator Execute(params string[] args) {
            DebugConsole.Instance.builder = new System.Text.StringBuilder();
            DebugConsole.Instance.output.text = "";
            DebugConsole.Instance.Hello();
            yield return null;
        }

        public override string GetName() {
            return "clear";
        }

        public override string Help() {
            return GetName() + " - clear the console";
        }
    }

    public class HideCommand : ICommand {
        public override IEnumerator Execute(params string[] args) {
            DebugConsole.Instance.gameObject.SetActive(false);
            yield return null;
        }

        public override string GetName() {
            return "hide";
        }

        public override string Help() {
            return GetName() + " - close the console";
        }
    }

    public class HelpCommand : ICommand {
        public override IEnumerator Execute(params string[] args) {
            StringBuilder builder = new StringBuilder();
            foreach (string help in Commands.commands.Values
                .Select(c => c.Help())
                .Where(h => !string.IsNullOrEmpty(h))
                .OrderBy(h => h)) {
                builder.AppendLine(help.Trim());
            }
            yield return builder.ToString();
        }

        public override string GetName() {
            return "help";
        }

        public override string Help() {
            return GetName() + " - show the list of commands";
        }
    }
}
