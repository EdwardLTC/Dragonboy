using Mod.ModHelper;
using Mod.Xmap;

namespace Mod.PickMob
{
	internal class UpCSKBController : ThreadActionUpdate<UpCSKBController>
	{

		internal override int Interval => 100;

		static int ID_ICON_MD = 2758;
		static short ID_CAPSULE_MD = 379;
		static short ID_CAPSULE_KB = 380;

		long notUsingMDTime;

		protected override void update()
		{
			if (!ItemTime.isExistItem(ID_CAPSULE_MD))
			{
				notUsingMDTime = System.DateTime.Now.Ticks;
			}

			if (System.DateTime.Now.Ticks - notUsingMDTime > 3 * 60 * 10000000L)
			{
				bool isUseMDSuccess = Utils.useItem(ID_CAPSULE_MD);

				if (!isUseMDSuccess)
				{
					toggle(false);
					return;
				}

				notUsingMDTime = 0L;
			}

			Item capsuleKB = Utils.getItemInBag(ID_CAPSULE_KB);

			if (capsuleKB != null)
			{

				if (capsuleKB.quantity == 99)
				{
					// go home and put in storage
				}
			}

			throw new System.NotImplementedException();
		}

		internal static void Start()
		{
			Pk9rPickMob.SetAutoPickItems(true);
			Pk9rPickMob.SetAvoidSuperMonster(true);
			Pk9rPickMob.SetSlaughter(true);
			toggle(true);
		}
	}
}
