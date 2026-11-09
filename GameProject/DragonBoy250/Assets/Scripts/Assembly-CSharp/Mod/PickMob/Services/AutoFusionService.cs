using System;

namespace Mod.PickMob.Services
{
	public static class AutoFusionService
	{
		const int AUTO_FUSION_ITEM_1 = 921;
		const int AUTO_FUSION_ITEM_2 = 454;
		const long AUTO_FUSION_REUSE_DELAY_MS = 3000;

		static long _nextAutoFusionTicks;

		public static void AutoFusionInSlaughter(Char myChar)
		{
			if (!PickMobConfig.IsAutoFusion)
			{
				return;
			}

			if (!myChar.havePet || myChar.isNhapThe || myChar.isFusion)
			{
				return;
			}

			long nowTicks = DateTime.Now.Ticks;
			if (nowTicks < _nextAutoFusionTicks)
			{
				return;
			}

			sbyte index = Utils.getIndexItemBag(AUTO_FUSION_ITEM_1, AUTO_FUSION_ITEM_2);
			if (index == -1)
			{
				return;
			}

			Service.gI().useItem(0, 1, index, -1);
			_nextAutoFusionTicks = nowTicks + AUTO_FUSION_REUSE_DELAY_MS * TimeSpan.TicksPerMillisecond;
		}
	}
}
