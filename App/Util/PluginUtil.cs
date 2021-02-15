using Advanced_Combat_Tracker;
using MognetPlugin.Enum;
using MognetPlugin.Model;
using MognetPlugin.Properties;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Linq;

namespace MognetPlugin.Util
{
    internal class PluginUtil
    {
        private static JavaScriptSerializer Serializer = new JavaScriptSerializer();
        private static int MaxPlayers;

        public static string ToJson(object obj)
        {
            return Serializer.Serialize(obj);
        }

        public static T FromJson<T>(string json)
        {
            return Serializer.Deserialize<T>(json);
        }

        public static bool IsPluginEnabled()
        {
            bool enabled = PluginSettings.GetSetting<bool>("Enabled");
            string token = PluginSettings.GetSetting<string>("Token");
            return enabled && token != null;
        }

        public static Log ACTEncounterToModel(EncounterData encounter)
        {
            Log Log = new Log();
            Log.successLevel = SuccessLevelEnum.GetByCode(encounter.GetEncounterSuccessLevel()).Name;
            Log.startTime = encounter.StartTime.TimeOfDay.ToString();
            Log.duration = encounter.Duration.ToString();
            Log.maxHit = ValidateAndFill("MaxHitParty", encounter.GetMaxHit(false));
            Log.totalHealing = ValidateAndFill("TotalHealing", encounter.Healed.ToString());
            Log.targetName = encounter.GetStrongestEnemy(null);
            Log.mapName = ValidateAndFill("MapName", encounter.ZoneName);
            Log.sortBy = PluginSettings.GetSetting<string>("SortBy");
            encounter.GetAllies().ForEach(combatant =>
            {
                Player Player = new Player();

                if (IsLimitBreak(combatant.Name))
                {
                    Player.playerName = combatant.Name;
                    Player.maxHit = combatant.GetMaxHit(true);

                    Log.players.Add(Player);
                }
                else if (!IsLimitBreak(combatant.Name) && GetCustomColumnData(combatant, "Job") != "")
                {
                    Player.playerJob = GetCustomColumnData(combatant, "Job").ToUpper();
                    Player.playerName = FormatName(combatant.Name);
                    Player.damagePercentage = ValidateAndFill("DamagePerc", combatant.DamagePercent);
                    Player.dps = Math.Round(combatant.EncDPS).ToString();
                    Player.maxHit = FormatSkill(ValidateAndFill("MaxHitIndividual", combatant.GetMaxHit(true)));
                    Player.healingPercentage = ValidateAndFill("HealingPerc", combatant.HealedPercent);
                    Player.hps = ValidateAndFill("HPS", Math.Round(combatant.EncHPS).ToString());
                    Player.maxHeal = FormatSkill(ValidateAndFill("MaxHeal", combatant.GetMaxHeal(true, false)));
                    Player.overhealPercentage = ValidateAndFill("OverHealPerc", GetCustomColumnData(combatant, "OverHealPct"));
                    Player.deaths = ValidateAndFill("Deaths", combatant.Deaths.ToString());
                    Player.crit = ValidateAndFill("Crit", Math.Round(combatant.CritDamPerc).ToString());
                    Player.dh = ValidateAndFill("DirectHit", GetCustomColumnData(combatant, "DirectHitPct"));
                    Player.dhCrit = ValidateAndFill("DirectHitCrit", GetCustomColumnData(combatant, "CritDirectHitPct"));
                    Player.critHealPercentage = ValidateAndFill("CritHealPerc", Math.Round(combatant.CritHealPerc).ToString());

                    Log.players.Add(Player);
                }
            });

            if (Log.players.Count == 0 || Log.duration == "00:00:00")
            {
                return null;
            }

            return Log;
        }


        public static List<Log> ACTEncounterToModelList(EncounterData encounter)
        {
            int i = 0;

            try
            {
                MaxPlayers = Int32.Parse(PluginSettings.GetSetting<string>("MaxPlayers"));
            }
            catch (Exception ee)
            {
                MaxPlayers = 8;
            }

            List<Log> LogList = new List<Log>();
            List<Player> PlayerList = new List<Player>();
            encounter.GetAllies().ForEach(combatant =>
            {
                Player Player = new Player();

                if (IsLimitBreak(combatant.Name))
                {
                    Player.playerName = combatant.Name;
                    Player.maxHit = combatant.GetMaxHit(true);

                    PlayerList.Add(Player);
                }
                else if (!IsLimitBreak(combatant.Name) && GetCustomColumnData(combatant, "Job") != "")
                {
                    Player.playerJob = GetCustomColumnData(combatant, "Job").ToUpper();
                    Player.playerName = FormatName(combatant.Name);
                    Player.damagePercentage = ValidateAndFill("DamagePerc", combatant.DamagePercent);
                    Player.dps = Math.Round(combatant.EncDPS).ToString();
                    Player.idps = (int)Math.Round(combatant.EncDPS);
                    Player.maxHit = FormatSkill(ValidateAndFill("MaxHitIndividual", combatant.GetMaxHit(true)));
                    Player.healingPercentage = ValidateAndFill("HealingPerc", combatant.HealedPercent);
                    Player.hps = ValidateAndFill("HPS", Math.Round(combatant.EncHPS).ToString());
                    Player.ihps = (int)Math.Round(combatant.EncHPS);
                    Player.maxHeal = FormatSkill(ValidateAndFill("MaxHeal", combatant.GetMaxHeal(true, false)));
                    Player.overhealPercentage = ValidateAndFill("OverHealPerc", GetCustomColumnData(combatant, "OverHealPct"));
                    Player.deaths = ValidateAndFill("Deaths", combatant.Deaths.ToString());
                    Player.crit = ValidateAndFill("Crit", Math.Round(combatant.CritDamPerc).ToString());
                    Player.dh = ValidateAndFill("DirectHit", GetCustomColumnData(combatant, "DirectHitPct"));
                    Player.dhCrit = ValidateAndFill("DirectHitCrit", GetCustomColumnData(combatant, "CritDirectHitPct"));
                    Player.critHealPercentage = ValidateAndFill("CritHealPerc", Math.Round(combatant.CritHealPerc).ToString());

                    PlayerList.Add(Player);
                }
            });

            if (PluginSettings.GetSetting<string>("SortBy") == "DPS")
            {
                PlayerList.Sort((x, y) => y.idps.CompareTo(x.idps));
            }
            else if (PluginSettings.GetSetting<string>("SortBy") == "HPS")
            {
                PlayerList.Sort((x, y) => y.ihps.CompareTo(x.ihps));
            }
            
            PlayerList.Where(Player => Player.playerJob != "" && Player.playerJob != null).ToList().ForEach(Player =>
            {
                i += 1;
                Player.number = i;
                Player.playerJob = i.ToString() + "-" + Player.playerJob;
                if (Player.playerName.Length > 18)
                {
                    if (i <= 9)
                    {
                        Player.playerName = Player.playerName.Substring(0, 18);
                    }
                    else 
                    {
                        Player.playerName = Player.playerName.Substring(0, 17);
                    }
                }
            });
            

            if (i > MaxPlayers)
            {

                int totaltimes = (int)Math.Ceiling((decimal)i / MaxPlayers);
                int begin = 0;
                int end = 0;

                for (int ix = 1; ix <= totaltimes; ix++)
                {
                    end = ix * MaxPlayers;
                    begin = end - MaxPlayers + 1;
                    LogList.Add(CreateLog(begin, end, PlayerList, encounter, "[Part " + ix + " of " + totaltimes + "]"));
                }
            }
            else
            {
                LogList.Add(CreateLog(0, i, PlayerList, encounter, ""));
            }

            PlayerList.Where(Player => Player.playerName == "Limit Break" ).ToList().ForEach(Player =>
            {
                LogList.Last().players.Add(Player);
            });

            return LogList;
        }

        private static Log CreateLog(int start, int end, List<Player> PlayerList, EncounterData encounter, string append) 
        {
            try
            {
                Log Log = new Log();
                Log.successLevel = SuccessLevelEnum.GetByCode(encounter.GetEncounterSuccessLevel()).Name;
                Log.startTime = encounter.StartTime.TimeOfDay.ToString() + " " + append;
                Log.duration = encounter.Duration.ToString();
                Log.maxHit = ValidateAndFill("MaxHitParty", encounter.GetMaxHit(false));
                Log.totalHealing = ValidateAndFill("TotalHealing", encounter.Healed.ToString());
                Log.targetName = encounter.GetStrongestEnemy(null);
                Log.mapName = ValidateAndFill("MapName", encounter.ZoneName);
                Log.sortBy = PluginSettings.GetSetting<string>("SortBy");
                PlayerList.Where(Player => Player.number >= start && Player.number <= end).ToList().ForEach(Player =>
                {
                    Log.players.Add(Player);
                });

                if (Log.players.Count > 0)
                {
                    return Log;
                }

                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private static String ValidateAndFill(string setting, string data)
        {
            if (PluginSettings.GetSetting<bool>(setting))
            {
                return data;
            }

            return "";
        }

        private static String GetCustomColumnData(CombatantData combatant, String column)
        {
            String data = combatant.GetColumnByName(column);
            if (data != null)
            {
                return data;
            }

            return "";
        }

        private static bool IsLimitBreak(string charName)
        {
            return "Limit Break".Equals(charName);
        }

        private static string FormatSkill(string skillHit)
        {
            if (skillHit != "")
            {
                string skill = MatchOne(skillHit, @".*(?=-.)").Replace(" ", "");
                string damage = MatchOne(skillHit, @"[^-]+$");

                if (skill.Length > 10)
                {
                    skill = skill.Substring(0, 10);
                }

                if (skill.Contains("(*)") == true)
                {
                    skill = skill.Replace("(*)", "");
                }

                return skill + "-" + damage;
            }

            return "-";
        }

        private static string FormatName(string name)
        {
            if (name.Length > 20)
            {
                return name.Substring(0, 20);
            }

            return name;
        }

        private static string MatchOne(string text, string regex)
        {
            Regex r = new Regex(regex, RegexOptions.IgnoreCase);
            Match m = r.Match(text);
            return m.Groups[0].ToString().Trim();
        }

        public static bool TimeBetween(DateTime datetime, TimeSpan start, TimeSpan end)
        {
            // convert datetime to a TimeSpan
            TimeSpan now = datetime.TimeOfDay;
            // see if start comes before end
            if (start < end)
                return start <= now && now <= end;
            // start is after end, so do the inverse comparison
            return !(end < now && now < start);
        }
    }
}