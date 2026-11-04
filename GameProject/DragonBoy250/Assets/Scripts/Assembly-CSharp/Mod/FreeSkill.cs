using System.Collections.Generic;
using Mod.ModHelper.CommandMod.Chat;
using Mod.ModHelper.CommandMod.Hotkey;

namespace Mod
{
	public static class FreeSkill
	{

		static readonly Dictionary<int, FrozenSkillData> _frozenSkills = new Dictionary<int, FrozenSkillData>();

		[HotkeyCommand('d')]
		internal static void FreeCurrentSkill()
		{
			int currentSkillIndex = GetCurrentSkillIndex();
			if (currentSkillIndex == -1)
			{
				return;
			}

			Skill skill = GameScr.onScreenSkill[currentSkillIndex];
			int id = skill.template.id;

			if (_frozenSkills.TryGetValue(id, out FrozenSkillData data))
			{
				if (ReferenceEquals(data.Skill, skill))
				{
					skill.coolDown = data.CoolDown;
					skill.manaUse = data.ManaUse;
					_frozenSkills.Remove(id);
					GameScr.info1.addInfo("Đã gỡ free skill " + skill.template.name, 0);
					return;
				}
				_frozenSkills.Remove(id);
			}

			_frozenSkills[id] = new FrozenSkillData
			{
				Skill = skill,
				CoolDown = skill.coolDown,
				ManaUse = skill.manaUse
			};

			skill.coolDown = 0;
			skill.manaUse = 0;
			GameScr.info1.addInfo("Đã free skill " + skill.template.name, 0);
		}

		[ChatCommand("unfreezeall")]
		internal static void UnfreezeAllSkills()
		{
			foreach (FrozenSkillData data in _frozenSkills.Values)
			{
				data.Skill.coolDown = data.CoolDown;
				data.Skill.manaUse = data.ManaUse;
			}

			_frozenSkills.Clear();
		}

		static int GetCurrentSkillIndex()
		{
			Skill current = Char.myCharz().myskill;
			if (current == null || GameScr.onScreenSkill == null)
			{
				return -1;
			}

			for (int i = 0; i < GameScr.onScreenSkill.Length; i++)
			{
				if (GameScr.onScreenSkill[i] != null && GameScr.onScreenSkill[i].template.id == current.template.id)
				{
					return i;
				}
			}

			return -1;
		}

		class FrozenSkillData
		{
			public int CoolDown;
			public int ManaUse;
			public Skill Skill;
		}
	}
}
