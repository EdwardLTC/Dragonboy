using System.Collections;

namespace Mod.CSKB
{
	internal interface ICskbFullBagAction
	{
		IEnumerator Execute(CskbFullBagActionContext context);
	}
}
