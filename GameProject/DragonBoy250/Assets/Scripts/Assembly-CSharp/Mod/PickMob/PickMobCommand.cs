using System.Linq;
using Mod.ModHelper.CommandMod.Chat;
using Mod.R;

namespace Mod.PickMob
{
	internal static class PickMobCommands
	{
		const int ID_ITEM_GEM = 77;
		const int ID_ITEM_GEM_LOCK = 861;
		static readonly sbyte[] IdSkillsBase =
		{
			2, 9, 17, 0, 4, 13, 19, 6
		};
		static readonly short[] IdItemBlockBase =
		{
			225, 353, 354, 355, 356, 357, 358, 359, 360, 362, 1229
		};

		[ChatCommand("afs")]
		internal static void ToggleAutoFusion()
		{
			PickMobConfig.SetAutoFusion(!PickMobConfig.IsAutoFusion);
			GameScr.info1.addInfo("Auto hợp thể: " + Strings.OnOffStatus(PickMobConfig.IsAutoFusion), 0);
		}

		[ChatCommand("kv")]
		internal static void ToggleAutoChangeZone()
		{
			PickMobConfig.SetAutoChangeZone(!PickMobConfig.IsAutoChangeZone);
			GameScr.info1.addInfo("Auto đổi khu: " + Strings.OnOffStatus(PickMobConfig.IsAutoChangeZone), 0);
		}

		[ChatCommand("ts")]
		internal static void ToggleSlaughter()
		{
			PickMobConfig.SetSlaughter(!PickMobConfig.IsTanSat);
			GameScr.info1.addInfo(Strings.pickMobTitle + ": " + Strings.OnOffStatus(PickMobConfig.IsTanSat), 0);
		}

		[ChatCommand("add")]
		internal static void ToggleSelectedMob()
		{
			Mob mob = Char.myCharz().mobFocus;
			ItemMap itemMap = Char.myCharz().itemFocus;
			if (mob != null)
				AddOrRemoveMonsterAutoAttack(mob.mobId);
			else if (itemMap != null)
				AddOrRemoveItemAutoPick(itemMap.template.id);
			else
				GameScr.info1.addInfo(Strings.pickMobPlsFocusOnMonsterOrItem + '!', 0);
		}

		[ChatCommand("addm")]
		internal static void AddOrRemoveMonsterAutoAttack(int mobId)
		{
			if (PickMobConfig.IdMobsTanSat.Contains(mobId))
			{
				PickMobConfig.IdMobsTanSat.Remove(mobId);
				GameScr.info1.addInfo(string.Format(Strings.pickMobMonsterRemoved, mobId) + '!', 0);
			}
			else
			{
				PickMobConfig.IdMobsTanSat.Add(mobId);
				GameScr.info1.addInfo(string.Format(Strings.pickMobMonsterAdded, mobId) + '!', 0);
			}
		}

		[ChatCommand("addi")]
		internal static void AddOrRemoveItemAutoPick(short id)
		{
			if (PickMobConfig.IdItemPicks.Contains(id))
			{
				PickMobConfig.IdItemPicks.Remove(id);
				GameScr.info1.addInfo(string.Format(Strings.pickMobAutoPickItemListRemoved, ItemTemplates.get(id).name, id) + '!', 0);
			}
			else
			{
				PickMobConfig.IdItemPicks.Add(id);
				GameScr.info1.addInfo(string.Format(Strings.pickMobAutoPickItemListAdded, ItemTemplates.get(id).name, id) + '!', 0);
			}
		}

		[ChatCommand("addt")]
		internal static void ToggleItemOrMobType()
		{
			Mob mob = Char.myCharz().mobFocus;
			ItemMap itemMap = Char.myCharz().itemFocus;
			if (mob != null)
				AddOrRemoveMonsterTypeAutoAttack(mob.getTemplate().mobTemplateId);
			else if (itemMap != null)
				AddOrRemoveItemTypeAutoPick(itemMap.template.type);
			else
				GameScr.info1.addInfo(Strings.pickMobPlsFocusOnMonsterOrItem + '!', 0);
		}

		[ChatCommand("addtm")]
		internal static void AddOrRemoveMonsterTypeAutoAttack(int type)
		{
			if (PickMobConfig.TypeMobsTanSat.Contains(type))
			{
				PickMobConfig.TypeMobsTanSat.Remove(type);
				GameScr.info1.addInfo(string.Format(Strings.pickMobMonsterTypeRemoved, Mob.arrMobTemplate[type].name, Mob.arrMobTemplate[type].mobTemplateId) + '!', 0);
			}
			else
			{
				PickMobConfig.TypeMobsTanSat.Add(type);
				GameScr.info1.addInfo(string.Format(Strings.pickMobMonsterTypeAdded, Mob.arrMobTemplate[type].name, Mob.arrMobTemplate[type].mobTemplateId) + '!', 0);
			}
		}

		[ChatCommand("addti")]
		internal static void AddOrRemoveItemTypeAutoPick(sbyte type)
		{
			if (PickMobConfig.TypeItemPicks.Contains(type))
			{
				PickMobConfig.TypeItemPicks.Remove(type);
				GameScr.info1.addInfo(string.Format(Strings.pickMobAutoPickItemTypesListRemoved, type) + '!', 0);
			}
			else
			{
				PickMobConfig.TypeItemPicks.Add(type);
				GameScr.info1.addInfo(string.Format(Strings.pickMobAutoPickItemTypesListAdded, type) + '!', 0);
			}
		}

		[ChatCommand("clrm")]
		internal static void ClearMonsterToFightList()
		{
			PickMobConfig.IdMobsTanSat.Clear();
			PickMobConfig.TypeMobsTanSat.Clear();
			GameScr.info1.addInfo(Strings.pickMobMonsterListCleared + '!', 0);
		}

		[ChatCommand("nsq")]
		internal static void ToggleAvoidSuperMonsters()
		{
			PickMobConfig.SetAvoidSuperMonster(!PickMobConfig.IsNeSieuQuai);
			GameScr.info1.addInfo(Strings.pickMobAvoidSuperMobTitle + ": " + Strings.OnOffStatus(PickMobConfig.IsNeSieuQuai), 0);
		}

		[ChatCommand("anhat")]
		internal static void ToggleAutoPickUpItems()
		{
			PickMobConfig.SetAutoPickItems(!PickMobConfig.IsAutoPickItems);
			GameScr.info1.addInfo(Strings.autoPickItemTitle + ": " + Strings.OnOffStatus(PickMobConfig.IsAutoPickItems), 0);
		}

		[ChatCommand("itm")]
		internal static void ToggleFilterOtherCharItems()
		{
			PickMobConfig.SetAutoPickItemsFromOthers(!PickMobConfig.IsItemMe);
			GameScr.info1.addInfo(Strings.pickMobPickMyItemOnlyTitle + ": " + Strings.OnOffStatus(PickMobConfig.IsItemMe), 0);
		}

		[ChatCommand("sln")]
		internal static void TogglePickUpLimit()
		{
			PickMobConfig.SetPickUpLimited(!PickMobConfig.IsLimitTimesPickItem);
			GameScr.info1.addInfo(Strings.pickMobLimitPickTimesTitle + ": " + Strings.OnOffStatus(PickMobConfig.IsLimitTimesPickItem) + (PickMobConfig.IsLimitTimesPickItem ? ", " + PickMobConfig.TimesAutoPickItemMax : ""), 0);
		}

		[ChatCommand("sln")]
		internal static void SetPickUpLimit(int limit)
		{
			PickMobConfig.TimesAutoPickItemMax = limit;
			GameScr.info1.addInfo(Strings.pickMobLimitPickTimesTitle + ": " + PickMobConfig.TimesAutoPickItemMax, 0);
		}

		[ChatCommand("clri")]
		internal static void ResetItemFilterToDefault()
		{
			PickMobConfig.IdItemPicks.Clear();
			PickMobConfig.TypeItemPicks.Clear();
			PickMobConfig.TypeItemBlocks.Clear();
			PickMobConfig.IdItemBlocks.Clear();
			PickMobConfig.IdItemBlocks.AddRange(IdItemBlockBase);
			GameScr.info1.addInfo(Strings.pickMobItemListResetToDefault + '!', 0);
		}

		[ChatCommand("cnn")]
		internal static void SetToPickOnlyGems()
		{
			PickMobConfig.IdItemPicks.Clear();
			PickMobConfig.TypeItemPicks.Clear();
			PickMobConfig.TypeItemBlocks.Clear();
			PickMobConfig.IdItemBlocks.Clear();
			PickMobConfig.IdItemBlocks.AddRange(IdItemBlockBase);
			PickMobConfig.IdItemPicks.Add(ID_ITEM_GEM);
			PickMobConfig.IdItemPicks.Add(ID_ITEM_GEM_LOCK);
			GameScr.info1.addInfo(Strings.pickMobConfiguredPickGemsOnly + '!', 0);
		}

		[ChatCommand("skill")]
		internal static void ToggleSelectedSkillForSlaughter()
		{
			SkillTemplate template = Char.myCharz().myskill.template;
			AddOrRemoveSkillAutoAttack(template.id);
		}

		[ChatCommand("skill")]
		internal static void AddOrRemoveSkillAutoAttack(int index)
		{
			SkillTemplate template = Char.myCharz().nClass.skillTemplates[index - 1];
			if (PickMobConfig.IdSkillsTanSat.Contains(template.id))
			{
				PickMobConfig.IdSkillsTanSat.Remove(template.id);
				GameScr.info1.addInfo(string.Format(Strings.pickMobSkillListRemoved, template.name, template.id) + '!', 0);
			}
			else
			{
				PickMobConfig.IdSkillsTanSat.Add(template.id);
				GameScr.info1.addInfo(string.Format(Strings.pickMobSkillListAdded, template.name, template.id) + '!', 0);
			}
		}

		[ChatCommand("skillid")]
		internal static void AddOrRemoveSkillAutoAttack(sbyte id)
		{
			SkillTemplate template = Char.myCharz().nClass.skillTemplates.FirstOrDefault(t => t.id == id);
			if (template == null)
				return;
			if (PickMobConfig.IdSkillsTanSat.Contains(id))
			{
				PickMobConfig.IdSkillsTanSat.Remove(id);
				GameScr.info1.addInfo(string.Format(Strings.pickMobSkillListRemoved, template.name, template.id) + '!', 0);
			}
			else
			{
				PickMobConfig.IdSkillsTanSat.Add(id);
				GameScr.info1.addInfo(string.Format(Strings.pickMobSkillListAdded, template.name, template.id) + '!', 0);
			}
		}

		[ChatCommand("clrs")]
		internal static void SaveCurrentSkillListAsDefault()
		{
			PickMobConfig.IdSkillsTanSat.Clear();
			PickMobConfig.IdSkillsTanSat.AddRange(IdSkillsBase);
			GameScr.info1.addInfo(Strings.pickMobSkillListResetToDefault + '!', 0);
		}

		[ChatCommand("blocki")]
		internal static void BlockFocusedItem()
		{
			ItemMap itemMap = Char.myCharz().itemFocus;
			if (itemMap != null)
				BlockItem(itemMap.template.id);
			else
				GameScr.info1.addInfo(Strings.pickMobPlsFocusOnItem + '!', 0);
		}

		[ChatCommand("blockti")]
		internal static void BlockFocusedItemType()
		{
			ItemMap itemMap = Char.myCharz().itemFocus;
			if (itemMap != null)
				BlockItemType(itemMap.template.type);
			else
				GameScr.info1.addInfo(Strings.pickMobPlsFocusOnItem + '!', 0);
		}

		[ChatCommand("blocki")]
		internal static void BlockItem(short id)
		{
			if (PickMobConfig.IdItemBlocks.Contains(id))
			{
				PickMobConfig.IdItemBlocks.Remove(id);
				GameScr.info1.addInfo(string.Format(Strings.pickMobDontPickItemListAdded, ItemTemplates.get(id).name, id) + '!', 0);
			}
			else
			{
				PickMobConfig.IdItemBlocks.Add(id);
				GameScr.info1.addInfo(string.Format(Strings.pickMobDontPickItemListRemoved, ItemTemplates.get(id).name, id) + '!', 0);
			}
		}

		[ChatCommand("blockti")]
		internal static void BlockItemType(sbyte type)
		{
			if (PickMobConfig.TypeItemBlocks.Contains(type))
			{
				PickMobConfig.TypeItemBlocks.Remove(type);
				GameScr.info1.addInfo(string.Format(Strings.pickMobDontPickItemTypeListRemoved, type) + '!', 0);
			}
			else
			{
				PickMobConfig.TypeItemBlocks.Add(type);
				GameScr.info1.addInfo(string.Format(Strings.pickMobDontPickItemTypeListAdded, type) + '!', 0);
			}
		}
	}
}
