namespace Mod.Xmap
{
	internal sealed class CapsuleService
	{
		readonly MapLookupService mapLookup;
		readonly XmapSettings settings;

		internal CapsuleService(MapLookupService mapLookup, XmapSettings settings)
		{
			this.mapLookup = mapLookup;
			this.settings = settings;
		}

		bool CanUseVip()
		{
			return settings.UseCapsuleVip && !Char.myCharz().IsCharDead() && mapLookup.HasCapsuleVipInBag();
		}

		bool CanUseNormal()
		{
			return settings.UseCapsuleNormal && !Char.myCharz().IsCharDead() && mapLookup.HasCapsuleNormalInBag();
		}

		internal bool CanUseAny()
		{
			return CanUseVip() || CanUseNormal();
		}

		internal void UseProbeCapsule()
		{
			if (CanUseVip())
			{
				Service.gI().useItem(0, 1, -1, MapLookupService.IdItemCapsuleVip);
			}
			else if (CanUseNormal())
			{
				Service.gI().useItem(0, 1, -1, MapLookupService.IdItemCapsuleNormal);
			}
		}
	}

}
