namespace Mod.PickMob
{
	internal sealed class CskbFullBagActionContext
	{
		internal CskbFullBagActionContext(Item capsuleInBag)
		{
			CapsuleInBag = capsuleInBag;
		}

		internal Item CapsuleInBag { get; }
	}
}
