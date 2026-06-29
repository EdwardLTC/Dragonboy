using System;
using System.Collections.Generic;

namespace Mod
{
	internal static class ClanUtils
	{
		const sbyte PeanRequestInterval = 5;
		static DateTime lastRequestedPean = DateTime.MinValue;

		internal static bool CanAskForPeans()
		{
			return (DateTime.Now - lastRequestedPean).TotalMinutes >= PeanRequestInterval;
		}

		internal static bool CanDonatePeans()
		{
			for (int i = 0; i < ClanMessage.vMessage.size(); i++)
			{
				ClanMessage msg = (ClanMessage)ClanMessage.vMessage.elementAt(i);
				if (msg.type == 1 && msg.recieve < msg.maxCap && msg.playerId != Char.myCharz().charID)
					return true;
			}
			return false;
		}

		internal static void RequestPeans()
		{
			Service.gI().clanMessage(1, null, -1);
			lastRequestedPean = DateTime.Now;
		}

		internal static void DonatePeans()
		{
			foreach (ClanMessage msg in GetDonationMessages())
			{
				Service.gI().clanDonate(msg.id);
			}
		}

		static List<ClanMessage> GetDonationMessages()
		{
			List<ClanMessage> clanMessages = new List<ClanMessage>();
			for (int i = 0; i < ClanMessage.vMessage.size(); i++)
			{
				ClanMessage msg = (ClanMessage)ClanMessage.vMessage.elementAt(i);
				if (msg.type == 1 && msg.recieve < msg.maxCap && msg.playerId != Char.myCharz().charID)
					clanMessages.Add(msg);
			}
			return clanMessages;
		}
	}
}
