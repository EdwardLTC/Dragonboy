namespace Mod.Auto
{
	public static class AutoUseGrape
	{
		const short GREEN_GRAPE_ID = 212;
		const short PURPLE_GRAPE_ID = 211;
		
		public static void doUseGrape()
		{
			Item green = Utils.getItemInBag(GREEN_GRAPE_ID);
			Item purple = Utils.getItemInBag(PURPLE_GRAPE_ID);

			short? itemId = green != null ? GREEN_GRAPE_ID : purple != null ? PURPLE_GRAPE_ID : null;

			if (itemId.HasValue)
			{
				Utils.useItem(itemId.Value);
			}
		}
	}
}
