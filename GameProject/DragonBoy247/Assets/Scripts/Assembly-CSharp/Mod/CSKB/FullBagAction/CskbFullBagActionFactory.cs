namespace Mod.PickMob
{
	internal static class CskbFullBagActionFactory
	{
		static readonly ICskbFullBagAction putToBoxAction = new PutCskbIntoBoxFullBagAction();
		static readonly ICskbFullBagAction depositAction = new DepositCskbFullBagAction();
		static readonly ICskbFullBagAction sellAction = new SellCskbFullBagAction();

		internal static ICskbFullBagAction Create(ActionOnFullBag action)
		{
			return action switch
			{
				ActionOnFullBag.PutToBox => putToBoxAction,
				ActionOnFullBag.Deposit => depositAction,
				ActionOnFullBag.Sell => sellAction,
				_ => depositAction
			};
		}
	}
}
