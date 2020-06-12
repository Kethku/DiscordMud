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
    public enum WeaponClass {
        Ranged,
        Heavy,
        Light
    }

    public enum AttackSpeed {
        Slow,
        Medium,
        Fast
    }

    public enum Modifier {
        None,
        Water,
        Fire,
        Ice,
        Earth,
        Electric
    }

    public enum Result {
        Undecided,
        Attacker,
        Defender,
        Draw
    }

    public class DuelEvent {
        public string Description { get; set; }
        public Result Result { get; set; }

        public DuelEvent(string description) {
            Description = description;
            Result = Result.Undecided;
        }

        public DuelEvent(string description, Result result) {
            Description = description;
            Result = result;
        }
    }

    public class Weapon {
        public const double EffectiveFactor = 1.5;
        public const double MinorFactor = 1.2;
        public const double InefficientFactor = 0.5;

        public static readonly Weapon Fists = new Weapon {
            Name = "fists",
            Description = "their own two fists",
            AttackDescriptions = new List<string> {
                "upper cuts",
                "swipes",
                "jabs",
                "chops",
                "punches",
            },
            Class = WeaponClass.Light,
            AttackSpeed = AttackSpeed.Fast,
            Modifier = Modifier.None,
            MaxDamage = 5,
            MinDamage = 1,
            WiffChance = 0.25
        };

        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> AttackDescriptions { get; set; }

        public WeaponClass Class { get; set; }
        public AttackSpeed AttackSpeed { get; set; }
        public Modifier Modifier { get; set; }
        public double MaxDamage { get; set; }
        public double MinDamage { get; set; }
        public double WiffChance { get; set; }

        public bool ParticipatesInRound(int round) {
            if (AttackSpeed == AttackSpeed.Fast) return true;

            if (round == 1) {
                return AttackSpeed == AttackSpeed.Medium;
            } else if (round == 2) {
                return AttackSpeed == AttackSpeed.Slow;
            } else if (round == 3) {
                return AttackSpeed == AttackSpeed.Medium;
            }

            return false;
        }

        public double WeaponClassDamageFactor(Weapon other) {
            if (Class == WeaponClass.Ranged) {
                if (other.Class == WeaponClass.Heavy) {
                    return EffectiveFactor;
                } else if (other.Class == WeaponClass.Light) {
                    return InefficientFactor;
                }
            } else if (Class == WeaponClass.Heavy) {
                if (other.Class == WeaponClass.Light) {
                    return EffectiveFactor;
                } else if (other.Class == WeaponClass.Ranged) {
                    return InefficientFactor;
                }
            } else if (Class == WeaponClass.Light) {
                if (other.Class == WeaponClass.Ranged) {
                    return EffectiveFactor;
                } else if (other.Class == WeaponClass.Heavy) {
                    return InefficientFactor;
                }
            }

            return 1.0;
        }

        public double ModifierDamageFactor(Weapon other) {
            if (Modifier == Modifier.None) {
                return 1.0;
            }

            if (other.Modifier == Modifier.None) {
                return MinorFactor;
            }

            if (Modifier == Modifier.Water) {
                if (other.Modifier == Modifier.Fire) {
                    return EffectiveFactor;
                } else if (other.Modifier == Modifier.Electric) {
                    return InefficientFactor;
                }
            } else if (Modifier == Modifier.Fire) {
                if (other.Modifier == Modifier.Ice) {
                    return EffectiveFactor;
                } else if (other.Modifier == Modifier.Water) {
                    return InefficientFactor;
                }
            } else if (Modifier == Modifier.Ice) {
                if (other.Modifier == Modifier.Earth) {
                    return EffectiveFactor;
                } else if (other.Modifier == Modifier.Fire) {
                    return InefficientFactor;
                }
            } else if (Modifier == Modifier.Earth) {
                if (other.Modifier == Modifier.Electric) {
                    return EffectiveFactor;
                } else if (other.Modifier == Modifier.Ice) {
                    return InefficientFactor;
                }
            } else if (Modifier == Modifier.Electric) {
                if (other.Modifier == Modifier.Water) {
                    return EffectiveFactor;
                } else if (other.Modifier == Modifier.Earth) {
                    return InefficientFactor;
                }
            }

            return 1.0;
        }

        public double DamageFactor(Weapon other) {
            return
                WeaponClassDamageFactor(other) *
                ModifierDamageFactor(other);
        }
        
        public string Wiffs(Random random, string name) {
            if (random.Next(100) <= WiffChance * 100) {
                return $"{name} wiffs the attack. How embaressing!";
            }
            return null;
        }

        public (string, double) CalculateAttack(string name, int round, Weapon other) {
            if (ParticipatesInRound(round)) {
                Random random = new Random();
                string wiffMessage = Wiffs(random, name);
                if (wiffMessage != null) {
                    return (wiffMessage, 0.0);
                } else {
                    double damageFactor = DamageFactor(other);
                    double baseDamage = random.NextDouble() * (MaxDamage - MinDamage) + MinDamage;
                    double damage = baseDamage * damageFactor;
                    string attackDescription = AttackDescriptions[random.Next(AttackDescriptions.Count)];
                    return ($"{name} {attackDescription} dealing {damage:#.##} damage.", damage);
                }
            } else {
                return ($"{name} readies their next attack.", 0.0);
            }
        }

        public static IEnumerable<DuelEvent> Duel(Weapon attacker, string attackerName, Weapon defender, string defenderName) {
            double attackerHealth = 100.0;
            double defenderHealth = 100.0;

            // Intro
            
            yield return new DuelEvent($"{attackerName} and {defenderName} line up on the dueling ground.\n{attackerName} is using {attacker.Description}.\n{defenderName} is using {defender.Description}.\nTension fills the air.");

            // Round 1
            {
                var sb = new StringBuilder();
                (string attackerResult, double attackerDamageDelt) = attacker.CalculateAttack(attackerName, 1, defender);
                sb.AppendLine(attackerResult);
                defenderHealth -= attackerDamageDelt;
                (string defenderResult, double defenderDamageDelt) = defender.CalculateAttack(defenderName, 1, attacker);
                sb.AppendLine(defenderResult);
                attackerHealth -= defenderDamageDelt;

                if (attackerHealth <= 0 && defenderHealth <= 0) {
                    sb.AppendLine("Both contestants break away feeling confident that neither would have won. It is a draw.");
                    yield return new DuelEvent(sb.ToString(), Result.Draw);
                    yield break;
                } else if (attackerHealth <= 0) {
                    sb.AppendLine($"{defenderName} exhausts {attackerName}. {defenderName} is victorious!");
                    yield return new DuelEvent(sb.ToString(), Result.Defender);
                    yield break;
                } else if (defenderHealth <= 0) {
                    sb.AppendLine($"{attackerName} exhausts {defenderName}. {attackerName} is victorious!");
                    yield return new DuelEvent(sb.ToString(), Result.Attacker);
                    yield break;
                } else {
                    yield return new DuelEvent(sb.ToString());
                }
            }

            {
                var sb = new StringBuilder();
                (string attackerResult, double attackerDamageDelt) = attacker.CalculateAttack(attackerName, 2, defender);
                sb.AppendLine(attackerResult);
                defenderHealth -= attackerDamageDelt;
                (string defenderResult, double defenderDamageDelt) = defender.CalculateAttack(defenderName, 2, attacker);
                sb.AppendLine(defenderResult);
                attackerHealth -= defenderDamageDelt;

                if (attackerHealth <= 0 && defenderHealth <= 0) {
                    sb.AppendLine("Both contestants break away feeling confident that neither would have won. It is a draw.");
                    yield return new DuelEvent(sb.ToString(), Result.Draw);
                    yield break;
                } else if (attackerHealth <= 0) {
                    sb.AppendLine($"{defenderName} exhausts {attackerName}. {defenderName} is victorious!");
                    yield return new DuelEvent(sb.ToString(), Result.Defender);
                    yield break;
                } else if (defenderHealth <= 0) {
                    sb.AppendLine($"{attackerName} exhausts {defenderName}. {attackerName} is victorious!");
                    yield return new DuelEvent(sb.ToString(), Result.Attacker);
                    yield break;
                } else {
                    yield return new DuelEvent(sb.ToString());
                }
            }

            {
                var sb = new StringBuilder();
                (string attackerResult, double attackerDamageDelt) = attacker.CalculateAttack(attackerName, 3, defender);
                sb.AppendLine(attackerResult);
                defenderHealth -= attackerDamageDelt;
                (string defenderResult, double defenderDamageDelt) = defender.CalculateAttack(defenderName, 3, attacker);
                sb.AppendLine(defenderResult);
                attackerHealth -= defenderDamageDelt;

                if ((attackerHealth <= 0 && defenderHealth <= 0) || attackerHealth == defenderHealth) {
                    sb.AppendLine("Both contestants break away feeling confident that neither would have won. It is a draw.");
                    yield return new DuelEvent(sb.ToString(), Result.Draw);
                } else if (attackerHealth < defenderHealth) {
                    sb.AppendLine($"{defenderName} exhausts {attackerName}. {defenderName} is victorious!");
                    yield return new DuelEvent(sb.ToString(), Result.Defender);
                } else{
                    sb.AppendLine($"{attackerName} exhausts {defenderName}. {attackerName} is victorious!");
                    yield return new DuelEvent(sb.ToString(), Result.Attacker);
                }
            }
        }
    }

}
