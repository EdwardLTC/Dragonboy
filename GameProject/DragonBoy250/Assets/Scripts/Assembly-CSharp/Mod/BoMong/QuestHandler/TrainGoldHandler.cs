using System.Collections;
using Mod.Auto;
using Mod.Xmap;

namespace Mod.BoMong.QuestHandler
{
	public class TrainGoldHandler : IQuestHandler
	{
		public float intervalHandleQuest => 0.5f;
		public int MapIDForThisQuest(string questMessage)
		{
			return XmapContext.MapLookup.GetVillageMapId(Char.myCharz().cgender);
		}

		public void PreHandleQuest()
		{
			AutoKillSelfAndPickGold.gI.Toggle(true);
		}

		public IEnumerator HandleQuest()
		{
			if (TileMap.mapID != XmapContext.MapLookup.GetVillageMapId(Char.myCharz().cgender) && !AutoGoback.IsGoingBack && !XmapController.gI.IsActing)
			{
				XmapController.start(XmapContext.MapLookup.GetVillageMapId(Char.myCharz().cgender));
			}

			yield return null;
		}

		public void OnQuestCompleted()
		{
			AutoKillSelfAndPickGold.gI.Toggle(false);
		}
	}
}
