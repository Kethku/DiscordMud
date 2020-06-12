using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MyCouch;
using MyCouch.Requests;

namespace DiscordMud {
    public class Member {
        public string Rev { get; set; }

        public string Id { get; set; }
        public Wallet Wallet { get; set; }
        public List<Weapon> Inventory { get; set; }
        public string Challenger { get; set; }
        public Weapon Equiped { get; set; }

        public Member() {}

        public Member(string name) { 
            Id = name.ToLower();
            Wallet = new Wallet();
            Inventory = new List<Weapon>();
            Challenger = null;
            Equiped = null;
        }
    }

    public static class Capitalism {
        const string COUCHDB_URL = "http://02credits.ddns.net:5984";
        const string DATABASE_NAME = "booty_boy_capitalism";
        private static readonly Wallet ALLOWANCE = new Wallet { BBP = 100 };
        const int SECONDS_BETWEEN_DUEL_ROUNDS = 5;

        public static async Task<string> GetBalance(string name) {
            using (var db = new MyCouchStore(COUCHDB_URL, DATABASE_NAME)) {
                return (await db.GetMember(name)).Wallet.ToString();
            }
        }

        public static async Task AddDubsToken(string name, int rank) {
            using (var db = new MyCouchStore(COUCHDB_URL, DATABASE_NAME)) {
                var member = await db.GetMember(name);
                member.Wallet.Dubs[rank]++;
                await db.StoreAsync(member);
            }
        }

        public static async Task<string> Transfer(string fromName, string toName, string amountText) {
            using (var db = new MyCouchStore(COUCHDB_URL, DATABASE_NAME)) {
                var fromMember = await db.GetMember(fromName);
                var toMember = await db.GetMember(toName);

                if (fromMember.Id == toMember.Id) throw new CommandFailedException("You can't give to yourself.");

                var amount = new Wallet(amountText);
                Console.WriteLine(amount.ToString());

                fromMember.Wallet.Subtract(amount);
                toMember.Wallet.Add(amount);

                await db.StoreAsync(fromMember);
                await db.StoreAsync(toMember);

                return $"{fromMember.Id} has given {toMember.Id} {amount.ToString()}.";
            }
        }

        public static async Task<string> Challenge(string attackerName, string defenderName) {
            using (var db = new MyCouchStore(COUCHDB_URL, DATABASE_NAME)) {
                var attacker = await db.GetMember(attackerName);
                var defender = await db.GetMember(defenderName);

                // if (attacker.Id == defender.Id) throw new CommandFailedException("You can't challenge yourself.");
                defender.Challenger = defender.Id;

                await db.StoreAsync(defender);

                return $"{attacker.Id} has challenged {defender.Id} to a duel!";
            }
        }

        public static async Task Accept(string name, ISocketMessageChannel channel)  {
            using (var db = new MyCouchStore(COUCHDB_URL, DATABASE_NAME)) {
                var defender = await db.GetMember(name);
                if (defender.Challenger != null) {
                    var attacker = await db.GetMember(defender.Challenger);

                    await channel.SendMessageAsync($"{defender.Id} accepts the duel from {attacker.Id}!");
                    await Duel(channel, attacker.Id, attacker.Equiped ?? Weapon.Fists, defender.Id, defender.Equiped ?? Weapon.Fists);

                    attacker.Challenger = null;
                    await db.StoreAsync(attacker);
                } else {
                    await channel.SendMessageAsync($"You don't have a current challenger. Go challenge someone to a duel.");
                }
            }
        }

        public static async Task Duel(ISocketMessageChannel channel, string attackerName, Weapon attackerWeapon, string defenderName, Weapon defenderWeapon) {
            var stages = Weapon.Duel(attackerWeapon, attackerName, defenderWeapon, defenderName);

            foreach (var state in stages) {
                await channel.SendMessageAsync(state.Description);
                await Task.Delay(TimeSpan.FromSeconds(SECONDS_BETWEEN_DUEL_ROUNDS));
            }
        }

        public static async Task ManageAllowances(DiscordSocketClient client) {
            while (true) {
                var now = DateTime.Now;
                var midnight = (now - TimeSpan.FromHours(now.Hour)) - TimeSpan.FromMinutes(now.Minute);
                var fiveAm = midnight + TimeSpan.FromHours(5);
                if (fiveAm < DateTime.Now) {
                    fiveAm = fiveAm + TimeSpan.FromDays(1);
                }
                var timeTillFive = fiveAm - now;

                Console.WriteLine(timeTillFive);

                await Task.Delay(timeTillFive);
                
                var channel = (IMessageChannel)client.GetChannel(598338172958670862);
                using (var db = new MyCouchStore(COUCHDB_URL, DATABASE_NAME)) {
                    var query = new Query(SystemViewIdentity.AllDocs).Configure(config => config.IncludeDocs(true));
                    var members = (await db.QueryAsync<dynamic, Member>(query)).Select(row => row.IncludedDoc);

                    foreach (var member in members) {
                        member.Wallet.Add(ALLOWANCE);
                        db.StoreAsync(member);
                    }

                    await channel.SendMessageAsync($"Morning all. Everyone has been given {ALLOWANCE}. Spend it wisely");
                }
            }
        }

        public static async void Handle(SocketUserMessage message) {
            ISocketMessageChannel channel = message.Channel;

            var user = ((IGuildUser)message.Author);
            string author = user.Nickname;
            if (author == null) {
                author = user.Username;
            }
            string content = message.Content;

            Console.WriteLine(author);

            try {
                var parts = content
                    .Split(" ")
                    .Select(part => part.Trim())
                    .Where(part => !string.IsNullOrWhiteSpace(part))
                    .ToArray();

                if (parts[0] == "!balance") {
                    var balance = await GetBalance(author);
                    await channel.SendMessageAsync($"You have {balance}.");
                    await message.ConfirmReact();
                }

                if (parts[0] == "!give") {
                    var toName = parts[1];
                    var rest = string.Join(" ", parts.Skip(2));
                    await channel.SendMessageAsync(await Transfer(author, toName, rest));
                    await message.ConfirmReact();
                }

                if (parts[0] == "!challenge") {
                    var defenderName = parts[1];
                    await channel.SendMessageAsync(await Challenge(author, defenderName));
                }

                /* if (parts[0] == "!equip") { */
                /*     var weaponName = parts[1]; */
                /*     await channel.SendMessageAsync(await Equip(author, weaponName)); */
                /*     await message.ConfirmReact(); */
                /* } */

                if (parts[0] == "!accept") {
                    await Accept(author, channel);
                }
            } catch (CommandFailedException exception) {
                await channel.SendMessageAsync(exception.Reason);
                await message.RejectReact();
            }
        }
    }
}
