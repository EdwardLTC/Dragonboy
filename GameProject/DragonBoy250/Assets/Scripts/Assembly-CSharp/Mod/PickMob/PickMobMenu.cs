using Mod.ModHelper.Menu;
using Mod.R;

namespace Mod.PickMob
{
	internal static class PickMobMenu
	{
		public static void ShowMenu()
		{
			string menuDesc = string.Empty;
			Mob mobFocus = Char.myCharz().mobFocus;
			ItemMap itemFocus = Char.myCharz().itemFocus;
			string cpDesc = "PickMob NRO by Edward\n";
			if (mobFocus != null || itemFocus != null)
			{
				if (mobFocus != null)
					cpDesc += string.Format(Strings.pickMobFocusedMob, mobFocus.getTemplate().name, mobFocus.mobId, mobFocus.getTemplate().mobTemplateId);
				else
					cpDesc += string.Format(Strings.pickMobFocusedItem, itemFocus.template.name, itemFocus.template.id, itemFocus.template.type);
			}
			MenuBuilder menuBuilder = new MenuBuilder().setChatPopup(cpDesc);
			if (mobFocus != null || itemFocus != null)
			{
				if (mobFocus != null)
				{
					if (PickMobConfig.IdMobsTanSat.Contains(mobFocus.mobId))
						menuDesc = string.Format(Strings.pickMobRemoveMobIdFromList, mobFocus.mobId);
					else
						menuDesc = string.Format(Strings.pickMobAddMobIdToList, mobFocus.mobId);
				}
				else if (itemFocus != null)
				{
					if (PickMobConfig.IdItemPicks.Contains(itemFocus.template.id))
						menuDesc = string.Format(Strings.pickMobRemoveFromList, itemFocus.template.name);
					else
						menuDesc = string.Format(Strings.pickMobAddToList, itemFocus.template.name);
				}
				menuBuilder.addItem(menuDesc, new MenuAction(PickMobCommands.ToggleSelectedMob));
				if (mobFocus != null)
				{
					if (PickMobConfig.TypeMobsTanSat.Contains(mobFocus.getTemplate().mobTemplateId))
						menuDesc = string.Format(Strings.pickMobRemoveFromList, mobFocus.getTemplate().name);
					else
						menuDesc = string.Format(Strings.pickMobAddToList, mobFocus.getTemplate().name);
				}
				else if (itemFocus != null)
				{
					if (PickMobConfig.TypeItemPicks.Contains(itemFocus.template.type))
						menuDesc = string.Format(Strings.pickMobRemoveItemTypeFromList, itemFocus.template.type);
					else
						menuDesc = string.Format(Strings.pickMobAddItemTypeToList, itemFocus.template.type);
				}
				menuBuilder.addItem(menuDesc, new MenuAction(PickMobCommands.ToggleItemOrMobType));
				if (itemFocus != null)
				{
					if (PickMobConfig.IdItemBlocks.Contains(itemFocus.template.id))
						menuDesc = string.Format(Strings.pickMobRemoveFromDontPickList, itemFocus.template.name);
					else
						menuDesc = string.Format(Strings.pickMobAddToDontPickList, itemFocus.template.name);
					menuBuilder.addItem(menuDesc, new MenuAction(() =>
					{
						PickMobCommands.BlockItem(itemFocus.template.id);
					}));
					if (PickMobConfig.TypeItemBlocks.Contains(itemFocus.template.type))
						menuDesc = string.Format(Strings.pickMobRemoveItemTypeFromDontPickList, itemFocus.template.type);
					else
						menuDesc = string.Format(Strings.pickMobAddItemTypeToDontPickList, itemFocus.template.type);
					menuBuilder.addItem(menuDesc, new MenuAction(() =>
					{
						PickMobCommands.BlockItemType(itemFocus.template.type);
					}));
				}
			}
			if (PickMobConfig.IdMobsTanSat.Count + PickMobConfig.TypeMobsTanSat.Count > 0)
			{
				menuBuilder.addItem(Strings.pickMobClearMonsterList, new MenuAction(PickMobCommands.ClearMonsterToFightList));
			}
			SkillTemplate mySkillTemplate = Char.myCharz().myskill.template;
			if (PickMobConfig.IdSkillsTanSat.Contains(mySkillTemplate.id))
				menuDesc = string.Format(Strings.pickMobRemoveFromSkillList, mySkillTemplate.name);
			else
				menuDesc = string.Format(Strings.pickMobAddToSkillList, mySkillTemplate.name);
			menuBuilder.addItem(menuDesc, new MenuAction(PickMobCommands.ToggleSelectedSkillForSlaughter));
			menuBuilder.addItem(Strings.pickMobResetSkillListToDefault, new MenuAction(PickMobCommands.SaveCurrentSkillListAsDefault));
			menuBuilder.addItem(Strings.pickMobResetItemListToDefault, new MenuAction(PickMobCommands.ResetItemFilterToDefault));
			if (PickMobConfig.IdMobsTanSat.Count + PickMobConfig.TypeMobsTanSat.Count > 0)
				menuBuilder.addItem(Strings.pickMobViewMonsterList, new MenuAction(() =>
				{
					string infoMob = Strings.pickMobMonsterIdList + ": ";
					if (PickMobConfig.IdMobsTanSat.Count > 0)
					{
						foreach (int mobId in PickMobConfig.IdMobsTanSat)
							infoMob += mobId + ", ";
						infoMob = infoMob.Remove(infoMob.LastIndexOf(','), 2);
					}
					else
						infoMob += Strings.empty;
					infoMob += $"\n{Strings.pickMobMonsterTypeList}: ";
					if (PickMobConfig.TypeMobsTanSat.Count > 0)
					{
						foreach (int mobType in PickMobConfig.TypeMobsTanSat)
							infoMob += $"{Mob.arrMobTemplate[mobType].name} [{mobType}], ";
						infoMob = infoMob.Remove(infoMob.LastIndexOf(','), 2);
					}
					else
						infoMob += Strings.empty;
					GameCanvas.startOKDlg(infoMob);
				}));
			if (PickMobConfig.IdItemPicks.Count + PickMobConfig.TypeItemPicks.Count + PickMobConfig.IdItemBlocks.Count + PickMobConfig.TypeItemBlocks.Count > 0)
				menuBuilder.addItem(Strings.pickMobViewItemList, new MenuAction(() =>
				{
					string infoItem = Strings.pickMobAutoPickItemList + ": ";
					if (PickMobConfig.IdItemPicks.Count > 0)
					{
						foreach (short itemIdPick in PickMobConfig.IdItemPicks)
							infoItem += $"{ItemTemplates.get(itemIdPick).name} [{itemIdPick}], ";
						infoItem = infoItem.Remove(infoItem.LastIndexOf(','), 2);
					}
					else
						infoItem += Strings.empty;
					infoItem += $"\n{Strings.pickMobAutoPickItemTypeList}: ";
					if (PickMobConfig.TypeItemPicks.Count > 0)
					{
						foreach (sbyte itemTypePick in PickMobConfig.TypeItemPicks)
							infoItem += itemTypePick + ", ";
						infoItem = infoItem.Remove(infoItem.LastIndexOf(','), 2);
					}
					else
						infoItem += Strings.empty;
					infoItem += $"\n{Strings.pickMobDontPickItemList}: ";
					if (PickMobConfig.IdItemBlocks.Count > 0)
					{
						foreach (short itemIdBlock in PickMobConfig.IdItemBlocks)
							infoItem += $"{ItemTemplates.get(itemIdBlock).name} [{itemIdBlock}], ";
						infoItem = infoItem.Remove(infoItem.LastIndexOf(','), 2);
					}
					else
						infoItem += Strings.empty;
					infoItem += $"\n{Strings.pickMobDontPickItemTypeList}: ";
					if (PickMobConfig.TypeItemBlocks.Count > 0)
					{
						foreach (sbyte itemTypeBlock in PickMobConfig.TypeItemBlocks)
							infoItem += itemTypeBlock + ", ";
						infoItem = infoItem.Remove(infoItem.LastIndexOf(','), 2);
					}
					else
						infoItem += Strings.empty;
					GameCanvas.startOKDlg(infoItem);
				}));
			menuBuilder.addItem(Strings.pickMobViewSkillList, new MenuAction(() =>
			{
				string infoskill = Strings.pickMobSkillList + ": ";
				foreach (sbyte skillid in PickMobConfig.IdSkillsTanSat)
				{
					for (int i = 0; i < 8; i++)
					{
						SkillTemplate template = Char.myCharz().nClass.skillTemplates[i];
						if (template.id == skillid)
						{
							infoskill += $"{template.name} [{skillid}]";
							goto here;
						}
					}
				}
				here:
				GameCanvas.startOKDlg(infoskill);
			}));
			menuBuilder.addItem("Auto đổi khu: " + Strings.OnOffStatus(PickMobConfig.IsAutoChangeZone), new MenuAction(PickMobCommands.ToggleAutoChangeZone));
			menuBuilder.addItem("Auto hợp thể: " + Strings.OnOffStatus(PickMobConfig.IsAutoFusion), new MenuAction(PickMobCommands.ToggleAutoFusion));
			menuBuilder.start();
		}
	}
}
