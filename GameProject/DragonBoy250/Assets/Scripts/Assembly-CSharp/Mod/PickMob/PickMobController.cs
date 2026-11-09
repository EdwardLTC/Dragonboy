using System.Collections;
using Mod.ModHelper;
using Mod.PickMob.Services;
using Mod.Xmap;

namespace Mod.PickMob
{
	public class PickMobController : CoroutineMainThreadAction<PickMobController>
	{
		const int ID_ICON_ITEM_TDLT = 4387;

		protected override float Interval => 0f;

		protected override IEnumerator OnUpdate()
		{
			if (XmapController.gI.IsActing)
			{
				yield break;
			}

			Char myChar = Char.myCharz();

			if (myChar.statusMe == 14 || myChar.cHP <= 0)
			{
				yield break;
			}

			MovementService.EnsureGroundCache();

			bool isUseTDLT = ItemTime.isExistItem(ID_ICON_ITEM_TDLT);
			bool isTanSatTDLT = PickMobConfig.IsTanSat && isUseTDLT;

			if (PickMobConfig.IsAutoPickItems && !isTanSatTDLT)
			{
				IEnumerator pickResult = ItemPickService.TryPickItem(myChar);
				if (pickResult != null)
				{
					yield return pickResult;
					yield break;
				}
			}

			if (PickMobConfig.IsTanSat)
			{
				yield return SlaughterService.DoSlaughter(myChar, isUseTDLT);
			}
		}
	}
}
