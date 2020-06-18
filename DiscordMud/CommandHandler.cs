using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MyCouch;
using MyCouch.Requests;

namespace DiscordMud {
    [System.AttributeUsageAttribute(System.AttributeTargets.Parameter)]
    public class Rest : System.Attribute { }
    [System.AttributeUsageAttribute(System.AttributeTargets.Parameter)]
    public class Author : System.Attribute { }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method)]
    public class Help : System.Attribute {
        public string Description { get; }

        public Help(string description) {
            Description = description;
        }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method)]
    public class Secret : System.Attribute { }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method)]
    public class GeneralHandler : System.Attribute { }

    public abstract class CommandHandler {
        public async Task Handle(SocketUserMessage message) {
            ISocketMessageChannel channel = message.Channel;
            ulong authorId = message.Author.Id;

            var parts = message.Content
                .Split(" ")
                .Select(part => part.Trim())
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToList();

            var commandName = parts[0].Substring(1).ToLower();

            var type = this.GetType();
            var methods = type.GetMethods();

            bool showSecret = channel.Id == 598342241626488862;

            if (commandName == "help") {
                var sb = new StringBuilder();
                foreach (var method in methods) {
                    var isSecret = method.GetCustomAttribute(typeof(Secret)) != null;
                    var help = method.GetCustomAttribute(typeof(Help)) as Help;
                    if (help != null && (!isSecret || showSecret)) {
                        sb.AppendLine(help.Description);
                    }
                }
                var text = sb.ToString();
                if (!string.IsNullOrWhiteSpace(text)) {
                    await channel.SendMessageAsync(type.Name);
                    await channel.SendMessageAsync(text);
                }
                return;
            }

            var command = methods
                .Where(method => !method.IsStatic && !method.IsConstructor
                        && (showSecret || method.GetCustomAttribute(typeof(Secret)) == null))
                .FirstOrDefault(method => method.Name.ToLower() == commandName);


            var confirm = true;
            if (command != null) {
                // Remove command part
                parts.RemoveAt(0);
            } else {
                command = methods
                    .Where(method => !method.IsStatic && !method.IsConstructor
                            && method.GetCustomAttribute(typeof(GeneralHandler)) != null)
                    .FirstOrDefault();
                confirm = false;
            }

            if (command == null) return;

            try {
                using (var db = Utils.OpenDatabase(Constants.DATABASE_NAME)) {
                    var parameters = new List<object>();
                    foreach (ParameterInfo parameter in command.GetParameters()) {
                        if (parameter.ParameterType.Name == "String") {
                            if (parameter.CustomAttributes.Any(attr => attr.AttributeType.Name == "Rest")) {
                                parameters.Add(string.Join(" ", parts));
                                parts.Clear();
                            } else {
                                var part = parts[0];
                                parts.RemoveAt(0);
                                parameters.Add(part);
                            }
                        } else if (parameter.ParameterType.Name == "Member") {
                            if (parameter.CustomAttributes.Any(attr => attr.AttributeType.Name == "Author")) {
                                parameters.Add(await db.GetMember(authorId));
                            } else {
                                var part = parts[0];
                                parts.RemoveAt(0);
                                parameters.Add(await db.GetMember(part));
                            }
                        } else if (parameter.ParameterType.Name == "Int32") {
                            var part = parts[0];
                            parts.RemoveAt(0);
                            if (int.TryParse(part, out var number)) {
                                parameters.Add(number);
                            } else {
                                throw new CommandFailedException("Argument not a number.");
                            }
                        } else if (parameter.ParameterType.Name == "MyCouchStore") {
                            parameters.Add(db);
                        } else if (parameter.ParameterType.Name == "ISocketMessageChannel") {
                            parameters.Add(channel);
                        } else if (parameter.ParameterType.Name == "SocketUserMessage") {
                            parameters.Add(message);
                        } else {
                            Console.WriteLine("Could not fill all arguments. Stuck on " + parameter.Name);
                            throw new CommandFailedException("Invalid command. Maybe arguments incorrect?");
                        }
                    }

                    var result = command.Invoke(this, parameters.ToArray());
                    if (result is Task<string>) {
                        var resultText = await (result as Task<string>);
                        await channel.SendMessageAsync(resultText);
                    } else if (result is Task) {
                        await (result as Task);
                    } else if (result is string) {
                        await channel.SendMessageAsync(result as string);
                    }

                    if (confirm) await message.ConfirmReact();
                }
            } catch (CommandFailedException exception) {
                if (confirm) {
                    await channel.SendMessageAsync(exception.Reason);
                    await message.RejectReact();
                }
            } catch (ArgumentOutOfRangeException) {
                if (confirm) {
                    await channel.SendMessageAsync("Insufficient arguments.");
                    await message.RejectReact();
                }
            } catch (Exception exception) {
                Console.WriteLine(exception);
                Console.WriteLine(exception.StackTrace);
                if (confirm) {
                    await channel.SendMessageAsync("Something went wrong :(");
                    await message.RejectReact();
                }
            }
        }
    }
}
