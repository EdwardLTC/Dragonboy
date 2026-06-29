using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mod.ModHelper.CommandMod.Chat;
namespace Mod
{
	public partial class Boss
	{
		static readonly List<Boss> listBosses = new List<Boss>();
		public static bool isEnabled;
		static readonly int MAX_BOSS = 100;
		static readonly List<string> strBossHasBeenKilled = new List<string>
		{
			" mọi người đều ngưỡng mộ.",
			" everyone admired.",
			" semua orang mengagumi.",
			" đã đánh bại và nhận được cải trang thành ",
			" killed and receive disguise of ",
			" membunuh Dan menerima disguise ",
			": Đã tiêu diệt được ",
			": defeated ",
			": mengalahkan "
		};
		static readonly List<string> strBossAppeared = new List<string>
		{
			"BOSS ",
			" vừa xuất hiện tại ",
			" appear at ",
			" muncul di ",
			" khu vực ",
			" zone ",
			" zona "
		};
		readonly DateTime AppearTime;
		readonly string name;
		bool isDied;
		string killer;
		string map;
		int mapId;
		int zoneId = -1;
		Boss(string name, string map)
		{
			this.name = name;
			this.map = map;
			GetMapId(name, map);
			AppearTime = DateTime.Now;
		}
		void GetMapId(string bossName, string bossMap)
		{
			if (bossMap == "Vách núi Aru")
				mapId = 42;
			else if (bossMap == "Vách núi Moori")
				mapId = 43;
			else if (bossMap == "Trạm tàu vũ trụ")
			{
				if (bossName.StartsWith("Số ") || bossName.StartsWith("Tiểu đội"))
					mapId = 25;
				else if (bossName.Contains("Bojack") || bossName.StartsWith("Bujin") || bossName.StartsWith("Bido") || bossName.StartsWith("Zangya") || bossName.StartsWith("Bido"))
					mapId = 24;
			}
			else
				mapId = GetMapID(bossMap);
		}
		static bool IsSpecialBossMap(int mapId)
		{
			return new[] { 79, 82, 83 }.Contains(mapId);
		}
		static bool IsSpecialBossName(string bossName)
		{
			return Regex.IsMatch(bossName, "(Tiểu đội trưởng|(Captain|Kapten) Ginyu|Số [1-4]|Jeice|Burter|Recoome|Guldo)");
		}
		public static void AddBoss(string chatVip)
		{
			if (!isEnabled)
				return;
			if (strBossHasBeenKilled.Any(chatVip.Contains))
			{
				strBossHasBeenKilled.ForEach(s => chatVip = chatVip.Replace(s, "|"));
				string[] array = chatVip.Split('|');
				Boss boss = null;
				try
				{
					boss = listBosses.Last(b =>
					{
						if (IsSpecialBossMap(b.mapId) && IsSpecialBossName(b.name))
							return false;
						return b.name == array[1] && string.IsNullOrEmpty(b.killer);
					});
				}
				catch (InvalidOperationException) { }
				if (boss == null)
				{
					boss = new Boss(array[1], "");
					listBosses.Add(boss);
				}
				boss.isDied = true;
				boss.killer = array[0];
				return;
			}
			if (!chatVip.StartsWith(strBossAppeared[0]))
				return;
			strBossAppeared.ForEach(s => chatVip = chatVip.Replace(s, "|"));
			string[] parts = chatVip.Split('|');
			Boss appearedBoss = null;
			try
			{
				int messageMapId = GetMapID(parts[2]);
				appearedBoss = listBosses.Last(b =>
				{
					if (IsSpecialBossMap(messageMapId) && IsSpecialBossName(parts[1]))
						return false;
					return string.IsNullOrEmpty(b.map) && b.name == parts[1];
				});
			}
			catch (InvalidOperationException) { }
			if (appearedBoss == null)
			{
				appearedBoss = new Boss(parts[1], parts[2]);
				listBosses.Add(appearedBoss);
			}
			else
			{
				appearedBoss.map = parts[2];
				appearedBoss.GetMapId(appearedBoss.name, appearedBoss.map);
			}
			if (parts.Length == 4)
				appearedBoss.zoneId = int.Parse(parts[3]);
			if (listBosses.Count > MAX_BOSS)
				listBosses.RemoveAt(0);
			if (listBosses.Count > MAX_BOSS_DISPLAY && offsetX == 0)
			{
				getScrollBar(out int scrollBarWidth, out _, out _);
				offsetX = scrollBarWidth;
			}
		}
		static int GetMapID(string mapName)
		{
			for (int i = 0; i < TileMap.mapNames.Length; i++)
			{
				if (TileMap.mapNames[i].Equals(mapName))
					return i;
			}
			return -1;
		}
		[ChatCommand("testboss")]
		public static void Test()
		{
			for (int i = 0; i < 10; i++)
				GameEvents.OnChatVip($"BOSS edward {i} vừa xuất hiện tại Đảo Kamê");
		}
		public override string ToString()
		{
			TimeSpan timeSpan = DateTime.Now.Subtract(AppearTime);
			string result = $"{name} - ";
			if (string.IsNullOrEmpty(map))
				result += "chưa biết";
			else
				result += $"{map} [{mapId}]";
			result += " - ";
			if (!isDied)
			{
				if (zoneId > -1)
					result += $"khu {zoneId} - ";
				int hours = (int)System.Math.Floor((decimal)timeSpan.TotalHours);
				if (hours > 0)
					result += $"{hours}h";
				if (timeSpan.Minutes > 0)
					result += $"{timeSpan.Minutes}m";
				result += $"{timeSpan.Seconds}s";
			}
			else
			{
				if (!string.IsNullOrEmpty(killer))
					result += $"Bị {killer} tiêu diệt";
				else
					result += "Đã chết";
			}
			return result;
		}
	}
}
