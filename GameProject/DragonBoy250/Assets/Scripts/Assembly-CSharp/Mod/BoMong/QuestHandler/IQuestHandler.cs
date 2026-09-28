using System.Collections;

namespace Mod.BoMong.QuestHandler
{
	public interface IQuestHandler
	{
		public float intervalHandleQuest => 1f;

		public int MapIDForThisQuest(string questMessage);

		public void PreHandleQuest();

		public IEnumerator HandleQuest(int mapId);

		public void OnQuestCompleted();
	}
}
