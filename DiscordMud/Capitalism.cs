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
        public string Name { get; set; }
        public Wallet Wallet { get; set; }
        public List<Weapon> Inventory { get; set; }
        public ulong? Challenger { get; set; }
        public Weapon Equiped { get; set; }

        public Member() {}

        public Member(string name) { 
            Id = name.ToLower();
            Wallet = new Wallet();
            Inventory = new List<Weapon>();
            Challenger = null;
            Equiped = null;
        }

        public override string ToString() {
            return Name;
        }
    }

    public class Capitalism : CommandHandler {
        private static readonly Wallet ALLOWANCE = new Wallet { BBP = 100 };
        const int SECONDS_BETWEEN_DUEL_ROUNDS = 5;

        [Help("!balance: Prints your current wallet balance.")]
        public string Balance([Author]Member author) {
            return $"You have {author.Wallet}";
        }

        [Help("!give {reciever} {amount}: Gives the given person an amount of money. Example: !give keith 2 bbp 3 trips 1 quad Example: !give keith #2 pencil")]
        public async Task<string> Give([Author]Member author, Member reciever, [Rest]string amountText, MyCouchStore db) {
            if (author.Id == reciever.Id) throw new CommandFailedException("You can't give to yourself.");

            var gift = author.Inventory.FirstOrDefault(possibleGift => possibleGift.Id.ToLower() == amountText.ToLower().Trim());
            if (gift != null) {
                reciever.Inventory.Add(gift);
                author.Inventory.Remove(gift);
                await db.StoreAsync(author);
                await db.StoreAsync(reciever);
                return $"{author} has given {reciever} {gift.Id.IndefiniteArticle()} {gift.Id}";
            } else {
                var amount = new Wallet(amountText);
                author.Wallet.Subtract(amount);
                reciever.Wallet.Add(amount);
                await db.StoreAsync(author);
                await db.StoreAsync(reciever);
                return $"{author} has given {reciever} {amount.ToString()}.";
            }
        }

        [Help("!challenge: Challenges somebody to a duel.")]
        public async Task<string> Challenge([Author]Member attacker, Member defender, MyCouchStore db) {
            if (attacker.Id == defender.Id) return "You can't challenge yourself.";
            defender.Challenger = ulong.Parse(attacker.Id);
            await db.StoreAsync(defender);

            return $"{attacker} has challenged {defender} to a duel!";
        }

        [Help("!accept: Accepts a duel if somebody has challenged you.")]
        public async Task Accept([Author]Member defender, MyCouchStore db, ISocketMessageChannel channel)  {
            if (defender.Challenger != null) {
                var attacker = await db.GetMember(defender.Challenger.Value);

                await channel.SendMessageAsync($"{defender} accepts the duel from {attacker}!\n");
                var stages = Weapon.Duel(attacker.Equiped ?? Weapon.Fists, attacker.Name, defender.Equiped ?? Weapon.Fists, defender.Name);

                Result result = Result.Undecided;
                foreach (var state in stages) {
                    await channel.SendMessageAsync(state.Description);
                    await Task.Delay(TimeSpan.FromSeconds(SECONDS_BETWEEN_DUEL_ROUNDS));
                    result = state.Result;
                }

                defender.Challenger = null;

                defender.Equiped?.HandlePostDuelEffects(defender, attacker, channel, result == Result.Attacker);
                attacker.Equiped?.HandlePostDuelEffects(attacker, defender, channel, result == Result.Defender);

                await db.StoreAsync(defender);
                await db.StoreAsync(attacker);
            } else {
                await channel.SendMessageAsync($"You don't have a current challenger. Go challenge someone to a duel.");
            }
        }

        [Secret]
        [Help("!open {dubs token rank}: Opens a lootbox of the given rank. Example: !open quad")]
        public async Task Open([Author]Member author, string rankName, ISocketMessageChannel channel, MyCouchStore db) {
            var rank = Utils.DubsNameToRank(rankName);
            rankName = Utils.RankToDubsName(rank);
            if (rankName == "bbp") {
                throw new CommandFailedException("You can only open loot boxes for trips or higher.");
            }

            if (author.Wallet.Dubs[rank] <= 0) {
                throw new CommandFailedException("You don't have enough tokens for that.");
            }

            using (var lootDB = Utils.OpenDatabase($"{rankName}{Constants.LOOT_DATABASE_SUFFIX}")) {
                var possibleLoot = (await lootDB.GetAllAsync<Weapon>()).ToList();
                Random random = new Random();

                if (possibleLoot.Count == 0) {
                    await channel.SendMessageAsync($"Unfortunately that level of loot isn't available yet. Go bug Keith about it.");
                    return;
                }

                var loot = possibleLoot[random.Next(possibleLoot.Count)];
                await channel.SendMessageAsync($"Opening loot box!");
                for (int i = 3; i > 0; i--) {
                    await channel.SendMessageAsync($"{i}...");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
                await channel.SendMessageAsync($"You got {loot}.");
                author.Wallet.Dubs[rank]--;
                author.Inventory.Add(loot);
                await db.StoreAsync(author);
            }
        }

        [Secret]
        [Help("!equip {weapon name}: Equips the weapon with the given name from your inventory.")]
        public async Task<string> Equip([Author]Member author, [Rest]string weaponName, MyCouchStore db) {
            var weapon = author.Inventory.FirstOrDefault(possibleWeapon => possibleWeapon.Id.ToLower() == weaponName.ToLower().Trim());
            if (weapon == null) throw new CommandFailedException("You don't have a weapon like that.");
            if (author.Equiped != null) {
                author.Inventory.Add(author.Equiped);
            }
            author.Equiped = weapon;
            author.Inventory.Remove(weapon);
            await db.StoreAsync(author);
            return $"You equip {weapon}.";
        }

        [Secret]
        [Help("!unequip: Unequips your currently held weapon and puts it back in your inventory.")]
        public async Task<string> Unequip([Author]Member author, MyCouchStore db) {
            var weapon = author.Equiped;

            if (weapon != null) {
                author.Equiped = null;
                author.Inventory.Add(weapon);
                await db.StoreAsync(author);
                return $"You unequip {weapon}";
            } else {
                return $"You don't have anything equiped...";
            }
        }

        [Help("!inventory: Lists your the things in your inventory.")]
        public string Inventory([Author]Member author) {
            if (author.Inventory.Count == 0) {
                return "You don't have anything in your inventory.";
            } else {
                var sb = new StringBuilder();
                sb.AppendLine("You have:");
                foreach (var weapon in author.Inventory) {
                    sb.AppendLine($"    {weapon}");
                }
                return sb.ToString();
            }
        }

        [Secret]
        [Help("!inspect: Inspects your currently equiped item.")]
        public string Inspect([Author]Member author) {
            if (author.Equiped == null) {
                return Weapon.Fists.Description;
            } else {
                return author.Equiped.Description;
            }
        }

        [Secret]
        [Help("!drop: Drops your currently equiped item.")]
        public async Task<string> Drop([Author]Member author, MyCouchStore db) {
            if (author.Equiped == null) {
                throw new CommandFailedException("You can't drop something if you don't have anything equiped.");
            } else {
                var droppedItem = author.Equiped;
                author.Equiped = null;
                await db.StoreAsync(author) ;
                return $"You drop the {droppedItem.Id} off the edge of a cliff. Goodbye forever.";
            }
        }

        [Secret]
        [Help("!grant {amount}: Gives yourself an amount of currency.")]
        public async Task<string> Grant([Author]Member author, MyCouchStore db, [Rest]string amountText) {
            var amount = new Wallet(amountText);
            author.Wallet.Add(amount);
            await db.StoreAsync(author);
            return $"You gave yourself {amount}.";
        }

        [Secret]
        [Help("!forfeit {amount}: Gives up an amount of currency.")]
        public async Task<string> Forfeit([Author]Member author, MyCouchStore db, [Rest]string amountText) {
            var amount = new Wallet(amountText);
            author.Wallet.Subtract(amount);
            await db.StoreAsync(author);
            return $"You gave up {amount}.";
        }

        [GeneralHandler]
        public async Task Handle([Author]Member author, SocketUserMessage message) {
            if (author.Equiped != null) {
                await author.Equiped.HandleGeneralMessageEffects(message);
            }
        }

        public static async Task<bool> AddDubsToken(ulong id, int rank) {
            using (var db = Utils.OpenDatabase(Constants.DATABASE_NAME)) {
                if (await db.ExistsAsync(id.ToString())) {
                    var member = await db.GetMember(id.ToString());
                    member.Wallet.Dubs[rank]++;
                    await db.StoreAsync(member);
                    return true;
                } else {
                    return false;
                }
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

                await Task.Delay(timeTillFive);
                
                var channel = (IMessageChannel)client.GetChannel(598338172958670862);
                using (var db = Utils.OpenDatabase(Constants.DATABASE_NAME)) {
                    var members = await db.GetAllAsync<Member>();

                    foreach (var member in members) {
                        member.Wallet.Add(ALLOWANCE);
                        await db.StoreAsync(member);
                    }

                    await channel.SendMessageAsync($"Morning all. Everyone has been given {ALLOWANCE}. Spend it wisely");
                }
            }
        }
    }
}
