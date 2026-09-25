using Mod.ModHelper.Menu;

namespace Mod.BoMong
{
	internal static class BoMongMenu
	{
		internal static void ShowMenu()
		{
			string desc = "Auto Nhiệm vụ Bò Mộng\n" + (BoMongController.gI.IsActing ? "[Đang chạy]" : "[Đang dừng]");

			MenuBuilder menuBuilder = new MenuBuilder().setChatPopup(desc);

			menuBuilder.addItem(
				$"Auto Bò Mộng: {(BoMongController.gI.IsActing ? "BẬT" : "TẮT")}",
				new MenuAction(() =>
				{
					BoMongController.gI.Toggle(!BoMongController.gI.IsActing);
					ShowMenu();
				}));

			menuBuilder.addItem(
				$"Mức độ: {BoMongSettings.Difficulty.ToUpper()}",
				new MenuAction(() =>
				{
					BoMongSettings.CycleDifficulty();
					GameScr.info1.addInfo($"[Bò Mộng] Mức độ nhiệm vụ: {BoMongSettings.Difficulty}", 0);
					ShowMenu();
				}));

			menuBuilder.addItem(
				$"Bỏ nv pem quái: {(BoMongSettings.SkipTrainMonster ? "BẬT" : "TẮT")}",
				new MenuAction(() =>
				{
					BoMongSettings.SkipTrainMonster = !BoMongSettings.SkipTrainMonster;
					BoMongSettings.Save();
					ShowMenu();
				}));

			menuBuilder.addItem(
				$"Bỏ nv nhặt vàng: {(BoMongSettings.SkipTrainGold ? "BẬT" : "TẮT")}",
				new MenuAction(() =>
				{
					BoMongSettings.SkipTrainGold = !BoMongSettings.SkipTrainGold;
					BoMongSettings.Save();
					ShowMenu();
				}));

			menuBuilder.addItem(
				$"Bỏ nv pem người: {(BoMongSettings.SkipKillPlayer ? "BẬT" : "TẮT")}",
				new MenuAction(() =>
				{
					BoMongSettings.SkipKillPlayer = !BoMongSettings.SkipKillPlayer;
					BoMongSettings.Save();
					ShowMenu();
				}));

			menuBuilder.addItem(
				$"Bỏ nv ăn trộm: {(BoMongSettings.SkipKillLurcher ? "BẬT" : "TẮT")}",
				new MenuAction(() =>
				{
					BoMongSettings.SkipKillLurcher = !BoMongSettings.SkipKillLurcher;
					BoMongSettings.Save();
					ShowMenu();
				}));

			menuBuilder.start();
		}
	}
}
