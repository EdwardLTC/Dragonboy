using System.Collections;
using Assembly_CSharp.Mod.BoMong;
using Mod.Auto;
using Mod.BoMong.QuestHandler;
using Mod.ModHelper;
using Mod.PickMob;
using Mod.Xmap;
using UnityEngine;

namespace Mod.BoMong
{
	internal class BoMongController : CoroutineMainThreadAction<BoMongController>
	{
		const int BOMONG_MAP_ID = 47;
		const int BOMONG_NPC_ID = 17;

		Coroutine questCoroutine;
		IQuestHandler questHandler;

		public bool IsHandlingQuest { get; private set; }
		bool IsWaitingReward { get; set; }

		protected override float Interval => 1f;

		protected override void OnStart()
		{
			BoMongSettings.Load();
			BoMongMessageEvent.ResetState();
			IsWaitingReward = false;

			GameScr.info1.addInfo("[Auto Bò Mộng] BẮT ĐẦU", 0);
		}

		protected override void OnStop()
		{
			StopCurrentQuest();
			BoMongMessageEvent.ResetState();
			IsWaitingReward = false;

			Pk9rPickMob.SetSlaughter(false);
			AutoKillAll.gI.Toggle(false);
			AutoGoback.gI.Toggle(false);
			AutoKillSelfAndPickGold.gI.Toggle(false);

			GameCanvas.menu.doCloseMenu();
			Char.chatPopup = null;

			GameScr.info1.addInfo("[Auto Bò Mộng] ĐÃ DỪNG", 0);
		}

		protected override IEnumerator OnUpdate()
		{
			if (!Utils.isUsingTDLT())
			{
				Utils.useItem(521);
			}

			if (Char.myCharz().IsCharDead())
			{
				Service.gI().returnTownFromDead();
				yield return new WaitForSecondsRealtime(1.5f);
				yield break;
			}

			// ==========================================
			// KẾT THÚC TOÀN BỘ BÒ MỘNG TRONG NGÀY
			// ==========================================
			if (BoMongMessageEvent.EndNvBoMong)
			{
				StopCurrentQuest();
				BoMongMessageEvent.ResetState();
				GameScr.info1.addInfo("[Auto Bò Mộng] Đã hết nhiệm vụ hôm nay!", 0);
				Toggle(false);
				yield break;
			}

			// ==========================================
			// ĐANG XỬ LÝ QUEST
			// ==========================================
			if (IsHandlingQuest)
			{
				if (BoMongMessageEvent.IsQuestCompleted)
				{
					CompleteCurrentQuest();
					IsWaitingReward = true;
				}

				yield break;
			}

			// ==========================================
			// CẦN NHẬN THƯỞNG KHI HOÀN THÀNH QUEST
			// ==========================================
			if (IsWaitingReward)
			{
				if (TileMap.mapID != BOMONG_MAP_ID)
				{
					yield return XmapController.StartAndWait(BOMONG_MAP_ID);
					yield return new WaitForSecondsRealtime(0.5f);
					yield break;
				}

				yield return InteractClaimReward();
				yield break;
			}

			// ==========================================
			// ĐÃ CÓ QUEST MỚI
			// ==========================================
			if (BoMongMessageEvent.CurrentQuestType.HasValue)
			{
				QuestType questType = BoMongMessageEvent.CurrentQuestType.Value;

				// Kiểm tra nếu nhiệm vụ nằm trong danh sách BỎ QUA
				if (BoMongSettings.IsQuestSkipped(questType))
				{
					if (TileMap.mapID != BOMONG_MAP_ID)
					{
						yield return XmapController.StartAndWait(BOMONG_MAP_ID);
						yield return new WaitForSecondsRealtime(0.5f);
						yield break;
					}

					yield return InteractCancelQuest();
					yield break;
				}

				questHandler = QuestHandlerFactory.Create(questType);
				if (questHandler == null)
				{
					yield break;
				}

				int mapId = questHandler.MapIDForThisQuest(BoMongMessageEvent.CurrentQuestMessage);
				if (mapId >= 0 && mapId != TileMap.mapID)
				{
					yield return XmapController.StartAndWait(mapId);
				}

				questHandler.PreHandleQuest();
				IsHandlingQuest = true;
				questCoroutine = StartCoroutine(RunQuest(questHandler));
				yield break;
			}

			// ==========================================
			// CHƯA CÓ QUEST -> ĐI NHẬN QUEST MỚI
			// ==========================================
			if (TileMap.mapID != BOMONG_MAP_ID)
			{
				yield return XmapController.StartAndWait(BOMONG_MAP_ID);
				yield return new WaitForSecondsRealtime(0.5f);
				yield break;
			}

			yield return InteractRequestQuest();
		}

		static IEnumerator RunQuest(IQuestHandler handler)
		{
			while (!BoMongMessageEvent.IsQuestCompleted && gI != null && gI.IsActing)
			{
				yield return handler.HandleQuest();
				yield return new WaitForSecondsRealtime(handler.intervalHandleQuest);
			}
		}

		void CompleteCurrentQuest()
		{
			if (questCoroutine != null)
			{
				StopCoroutine(questCoroutine);
				questCoroutine = null;
			}

			questHandler?.OnQuestCompleted();
			questHandler = null;
			IsHandlingQuest = false;
		}

		void StopCurrentQuest()
		{
			if (questCoroutine != null)
			{
				StopCoroutine(questCoroutine);
				questCoroutine = null;
			}

			questHandler?.OnQuestCompleted();
			questHandler = null;
			IsHandlingQuest = false;
		}

		IEnumerator InteractClaimReward()
		{
			if (TileMap.mapID != BOMONG_MAP_ID)
			{
				yield break;
			}

			Utils.TeleportToNPC(BOMONG_NPC_ID);
			yield return new WaitForSecondsRealtime(0.4f);

			Service.gI().openMenu(BOMONG_NPC_ID);
			yield return WaitForMenu(2.5f);

			if (!GameCanvas.menu.showMenu || GameCanvas.menu.menuItems == null)
			{
				yield break;
			}

			// Menu cấp 1: Chọn "Nhiệm vụ hàng ngày"
			int dailyQuestIdx = FindMenuItemIndex("hang ngay");
			if (dailyQuestIdx != -1)
			{
				ConfirmNPCMenuOption(dailyQuestIdx);

				yield return new WaitForSecondsRealtime(0.8f);
				yield return WaitForMenu(2.5f);

				if (GameCanvas.menu.showMenu && GameCanvas.menu.menuItems != null)
				{
					// Menu cấp 2: Chọn "Nhận thưởng"
					int rewardIdx = FindMenuItemIndex("nhan thuong");
					if (rewardIdx != -1)
					{
						IsWaitingReward = false;
						BoMongMessageEvent.ResetState();
						yield return new WaitForSecondsRealtime(1f);
						ConfirmNPCMenuOption(rewardIdx);
						GameScr.info1.addInfo("[Auto Bò Mộng] Đã nhận thưởng thành công!", 0);
						yield return new WaitForSecondsRealtime(1f);
					}
					else
					{
						GameCanvas.menu.doCloseMenu();
						Char.chatPopup = null;
					}
				}
			}
			else
			{
				// Kiểm tra nếu mục Nhận thưởng nằm ngay ở menu ngoài
				int directRewardIdx = FindMenuItemIndex("nhan thuong");
				if (directRewardIdx != -1)
				{
					IsWaitingReward = false;
					BoMongMessageEvent.ResetState();
					yield return new WaitForSecondsRealtime(1f);
					ConfirmNPCMenuOption(directRewardIdx);
					GameScr.info1.addInfo("[Auto Bò Mộng] Đã nhận thưởng thành công!", 0);
					yield return new WaitForSecondsRealtime(1f);
				}
				else
				{
					GameCanvas.menu.doCloseMenu();
					Char.chatPopup = null;
				}
			}
		}

		IEnumerator InteractRequestQuest()
		{
			if (TileMap.mapID != BOMONG_MAP_ID)
			{
				yield break;
			}

			Utils.TeleportToNPC(BOMONG_NPC_ID);
			yield return new WaitForSecondsRealtime(0.4f);

			Service.gI().openMenu(BOMONG_NPC_ID);
			yield return WaitForMenu(2.5f);

			if (!GameCanvas.menu.showMenu || GameCanvas.menu.menuItems == null)
			{
				yield break;
			}

			// Menu cấp 1: Chọn "Nhiệm vụ hàng ngày"
			int dailyQuestIdx = FindMenuItemIndex("hang ngay");
			if (dailyQuestIdx != -1)
			{
				ConfirmNPCMenuOption(dailyQuestIdx);

				yield return new WaitForSecondsRealtime(1f);
				yield return WaitForMenu(2.5f);

				if (GameCanvas.menu.showMenu && GameCanvas.menu.menuItems != null)
				{
					int alreadyHaveQuest = FindMenuItemIndex("chi tiet");
					if (alreadyHaveQuest != -1)
					{
						IsWaitingReward = false;
						BoMongMessageEvent.ResetState();
						yield return new WaitForSecondsRealtime(1f);
						ConfirmNPCMenuOption(alreadyHaveQuest);
						yield return new WaitForSecondsRealtime(1f);
						yield break;
					}

					int rewardIdx = FindMenuItemIndex("nhan thuong");
					if (rewardIdx != -1)
					{
						IsWaitingReward = false;
						BoMongMessageEvent.ResetState();
						yield return new WaitForSecondsRealtime(1f);
						ConfirmNPCMenuOption(rewardIdx);
						yield return new WaitForSecondsRealtime(1f);
						yield break;
					}

					string diffKeyword = BoMongSettings.Difficulty.ToLower().Trim();
					int diffIdx = FindMenuItemIndex(diffKeyword, true);
					if (diffIdx != -1)
					{
						ConfirmNPCMenuOption(diffIdx);
						GameScr.info1.addInfo($"[Auto Bò Mộng] Đã chọn nhiệm vụ: {BoMongSettings.Difficulty}", 0);
						yield return new WaitForSecondsRealtime(1.5f);
					}
					else
					{
						GameCanvas.menu.doCloseMenu();
						Char.chatPopup = null;
					}
				}
			}
			else
			{
				GameCanvas.menu.doCloseMenu();
				Char.chatPopup = null;
			}
		}

		static IEnumerator InteractCancelQuest()
		{
			if (TileMap.mapID != BOMONG_MAP_ID)
			{
				yield break;
			}

			Utils.TeleportToNPC(BOMONG_NPC_ID);
			yield return new WaitForSecondsRealtime(0.4f);

			Service.gI().openMenu(BOMONG_NPC_ID);
			yield return WaitForMenu(2.5f);

			if (!GameCanvas.menu.showMenu || GameCanvas.menu.menuItems == null)
			{
				yield break;
			}

			int dailyQuestIdx = FindMenuItemIndex("hang ngay");
			if (dailyQuestIdx != -1)
			{
				ConfirmNPCMenuOption(dailyQuestIdx);

				yield return new WaitForSecondsRealtime(0.8f);
				yield return WaitForMenu(2.5f);

				if (GameCanvas.menu.showMenu && GameCanvas.menu.menuItems != null)
				{
					int cancelIdx = FindMenuItemIndex("huy");
					if (cancelIdx != -1)
					{
						ConfirmNPCMenuOption(cancelIdx);
						yield return new WaitForSecondsRealtime(0.8f);

						if (GameCanvas.currentDialog != null && GameCanvas.currentDialog.left != null)
						{
							GameCanvas.currentDialog.left.performAction();
							GameCanvas.currentDialog = null;
						}
						else if (GameCanvas.menu.showMenu && GameCanvas.menu.menuItems != null)
						{
							int confirmIdx = FindMenuItemIndex("dong y");
							if (confirmIdx == -1) confirmIdx = 0;
							ConfirmNPCMenuOption(confirmIdx);
						}

						BoMongMessageEvent.ResetState();
						GameScr.info1.addInfo("[Auto Bò Mộng] Đã hủy nhiệm vụ bỏ qua thành công!", 0);
						yield return new WaitForSecondsRealtime(1.5f);
					}
					else
					{
						GameCanvas.menu.doCloseMenu();
						Char.chatPopup = null;
					}
				}
			}
			else
			{
				GameCanvas.menu.doCloseMenu();
				Char.chatPopup = null;
			}
		}

		static IEnumerator WaitForMenu(float timeout)
		{
			float elapsed = 0;
			while (!GameCanvas.menu.showMenu && elapsed < timeout)
			{
				yield return new WaitForSecondsRealtime(0.2f);
				elapsed += 0.2f;
			}
		}

		static int FindMenuItemIndex(string keyword, bool forceExact = false)
		{
			if (GameCanvas.menu.menuItems == null)
			{
				return -1;
			}

			string normKeyword = NormalizeText(keyword);

			for (int i = 0; i < GameCanvas.menu.menuItems.size(); i++)
			{
				Command cmd = (Command)GameCanvas.menu.menuItems.elementAt(i);

				if (cmd == null || string.IsNullOrEmpty(cmd.caption))
				{
					continue;
				}

				string normCaption = NormalizeText(cmd.caption);

				bool matched = forceExact
					? normCaption.Equals(normKeyword)
					: normCaption.Contains(normKeyword);

				if (matched)
				{
					return i;
				}
			}

			return -1;
		}

		static string NormalizeText(string text)
		{
			return string.IsNullOrEmpty(text) ? string.Empty : Utils.RemoveVietnameseDiacritics(text).Replace("\n", " ").ToLowerInvariant().Trim();
		}

		static void ConfirmNPCMenuOption(int index)
		{
			Service.gI().confirmMenu(BOMONG_NPC_ID, (sbyte)index);
			GameCanvas.menu.doCloseMenu();
			Char.chatPopup = null;
		}
	}
}
