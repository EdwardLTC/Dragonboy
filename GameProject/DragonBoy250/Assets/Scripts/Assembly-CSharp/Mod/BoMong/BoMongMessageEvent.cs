using JetBrains.Annotations;
using Mod;
using Mod.BoMong;
using Mod.BoMong.QuestHandler;

namespace Assembly_CSharp.Mod.BoMong
{
	public static class BoMongMessageEvent
	{
		public static bool EndNvBoMong { get; private set; }
		public static QuestType? CurrentQuestType { get; private set; }
		[CanBeNull]
		public static string CurrentQuestMessage { get; private set; }
		public static bool IsQuestCompleted { get; private set; }

		public static void Process(string message)
		{
			if (string.IsNullOrEmpty(message))
			{
				return;
			}

			string normalizedMessage = Normalize(message);

			if (normalizedMessage.Contains("hoan thanh") && normalizedMessage.Contains("bo mong"))
			{
				HandleQuestComplete();
				return;
			}

			if (normalizedMessage.Contains("het nhiem vu") || normalizedMessage.Contains("cho den ngay mai"))
			{
				HandleQuestEnded();
				return;
			}

			if (normalizedMessage.Contains("thoi gian nhan nhiem vu"))
			{
				if (BoMongController.gI != null && BoMongController.gI.IsHandlingQuest)
				{
					return;
				}

				QuestType? detected = DetectQuestType(normalizedMessage);
				if (detected != null)
				{
					CurrentQuestType = detected;
					CurrentQuestMessage = message;
					IsQuestCompleted = false;
				}
			}
		}

		static string Normalize(string message)
		{
			return Utils.RemoveVietnameseDiacritics(message).Replace("  ", " ").ToLower().Trim();
		}

		static void HandleQuestComplete()
		{
			CurrentQuestType = null;
			CurrentQuestMessage = null;
			IsQuestCompleted = true;
		}

		static void HandleQuestEnded()
		{
			EndNvBoMong = true;
			IsQuestCompleted = false;
			CurrentQuestMessage = null;
			CurrentQuestType = null;

			GameCanvas.menu.doCloseMenu();
		}

		public static void ResetState()
		{
			EndNvBoMong = false;
			IsQuestCompleted = false;
			CurrentQuestType = null;
			CurrentQuestMessage = null;
		}

		[CanBeNull]
		static QuestType? DetectQuestType(string normalizedMessage)
		{
			if (normalizedMessage.Contains("nguoi"))
			{
				return QuestType.KillPlayer;
			}

			if (normalizedMessage.Contains("an trom"))
			{
				return QuestType.KillLurcher;
			}

			if (normalizedMessage.Contains("dia diem"))
			{
				return QuestType.TrainMonster;
			}

			if (normalizedMessage.Contains("vang"))
			{
				return QuestType.TrainGold;
			}

			return null;
		}

		public static string ToSString()
		{
			return $"BoMongMessageEvent: EndNvBoMong={EndNvBoMong}, CurrentQuestType={CurrentQuestType}, CurrentQuestMessage={CurrentQuestMessage}, IsQuestCompleted={IsQuestCompleted}";
		}
	}
}
