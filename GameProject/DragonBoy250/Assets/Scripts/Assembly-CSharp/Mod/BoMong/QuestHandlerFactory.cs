using System;

namespace Mod.BoMong.QuestHandler
{
	public static class QuestHandlerFactory
	{
		static readonly IQuestHandler TrainMonsterHandler = new TrainMonsterHandler();
		static readonly IQuestHandler TrainGoldHandler = new TrainGoldHandler();
		static readonly IQuestHandler KillPlayerHandler = new KillPlayerHandler();
		static readonly IQuestHandler KillLurcherHandler = new KillLurcherHandler();

		public static IQuestHandler Create(QuestType quest)
		{
			return quest switch
			{
				QuestType.TrainMonster => TrainMonsterHandler,
				QuestType.TrainGold => TrainGoldHandler,
				QuestType.KillPlayer => KillPlayerHandler,
				QuestType.KillLurcher => KillLurcherHandler,
				_ => throw new NotImplementedException($"Quest handler for {quest} is not implemented yet.")
			};
		}
	}
}
