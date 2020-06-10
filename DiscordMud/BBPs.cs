using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using LiteDB;
using Discord;
using Discord.WebSocket;

namespace DiscordMud {
    public class Member {
        public int Id { get; set; }
        public string Name { get; set; }
        public int BBPBalance { get; set; }
        public Dictionary<int, int> DubsBalances { get; set;}

        public Member() {}

        public Member(string name) { 
            Name = name;
            BBPBalance = 0;
            DubsBalances = new Dictionary<int, int> {
                [3] = 0,
                [4] = 0,
                [5] = 0,
                [6] = 0,
                [7] = 0,
                [8] = 0,
                [9] = 0
            }
        }
    }

    public static class BBPs {
        public static int GetBBPBalance(string name) {
            using (var db = new LiteDatabase(@".\bootyboys.db")) {
                var members = db.GetCollection<Member>("members");
                var user = members.FindOne(x => x.Name == name);
                if (user != null) {
                    return user.Balance;
                } else {
                    return 10;
                }
            }
        }

        public static int AddDubs(string name, int rank) {
            int pointsToAdd = Math.Pow(10, rank - 3);
            using (var db = new LiteDatabase(@".\bootyboys.db")) {
                var members = db.GetCollection<Member>("members");
                var user = members.FindOne(x => x.Name == name);
                if (user == null) user = new Member(name);
                user.Balance += pointsToAdd;
                members.Insert(user);
            }
        }

        public static string Transfer(string fromName, string toName, int amount) {
            using (var db = new LiteDatabase(@".\bootyboys.db")) {
                var members = db.GetCollection<Member>("members");
                var fromUser = members.FindOne(x => x.Name.ToLower() == fromName.ToLower());
                if (fromUser == null) fromUser = new Member(fromName);
                var toUser = members.FindOne(x => x.Name.ToLower() == toName.ToLower());

                if (toUser == null) {
                    return $"Booty Boy with username {toName} does not exist. Maybe spelling is slightly wrong?";
                }

                if (fromUser.Balance < amount) {
                    return "You do not have enough BBPs to give that amount. Shame on you";
                }

                fromUser.Balance -= amount;
                toUser.Balance += amount;

                members.Insert(fromUser);
                members.Insert(toUser);

                return $"{fromUser.Name} has given {toUser.Name} {amount} BBps.";
            }
        }

        public static async void Handle(SocketMessage message) {
            ISocketMessageChannel channel = message.Channel;

            string author = message.Author.Username;
            message.Auth
            string content = message.Content;

            if (content == "!balance") {
                var balance = GetBalance(author.Nickname);
                await channel.SendMessageAsync($"{author.Nickname} has {balance} BBPs.");
            }

            if (content.StartsWith("!give")) {
                try {
                    var args = content.Substring(5).Split(" ").Select(arg => arg.Trim());
                    var toName = args[0];
                    var amount = int.Parse(args[1]);
                    await channel.SendMessageAsync(Transfer(author.Nickname, toName, amount));
                } catch (Exception e) {
                    Console.WriteLine(e);
                    await channel.SendMessageAsync("Unknown error... Probably format is wrong. Try something like \"!give {to} {amount}\"");
                }

            }
        }
    }
}
