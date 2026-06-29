using System.Collections;

namespace Mod.PickMob
{
	internal interface ICskbFullBagAction
	{
		IEnumerator Execute(CskbFullBagActionContext context);
	}
}
