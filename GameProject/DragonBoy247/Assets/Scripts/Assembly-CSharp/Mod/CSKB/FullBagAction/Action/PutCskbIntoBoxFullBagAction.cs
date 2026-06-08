using System.Collections;
using Mod.Xmap;

namespace Mod.PickMob
{
	internal sealed class PutCskbIntoBoxFullBagAction : ICskbFullBagAction
	{
		public IEnumerator Execute(CskbFullBagActionContext context)
		{
			if (!XmapController.gI.IsActing && !Utils.IsMyCharHome() && Utils.CanNextMap())
			{
				XmapController.start(XmapUtils.getIdMapHome(Char.myCharz().cgender));
				yield return null;
			}

			if (Utils.IsMyCharHome())
			{
				Service.gI().getItem(1, Utils.getIndexItemBag(CskbConstants.IdCapsuleKb));
				yield return null;
			}
		}
	}
}
