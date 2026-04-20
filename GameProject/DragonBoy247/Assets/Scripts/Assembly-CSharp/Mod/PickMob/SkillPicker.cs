using System.Linq;
using JetBrains.Annotations;

namespace Mod.PickMob
{
	public static class SkillPicker
	{
		static readonly sbyte[] IdSkillsMelee =
		{
			0, 9, 2, 17, 4
		};
		static readonly sbyte[] IdSkillsCanNotAttack =
		{
			10, 11, 14, 23, 7
		};

		[CanBeNull]
		public static Skill GetSkillAttack()
		{
			Skill best = null;
			SkillTemplate template = new SkillTemplate();
			foreach (sbyte id in Pk9rPickMob.IdSkillsTanSat)
			{
				template.id = id;
				Skill candidate = Char.myCharz().getSkill(template);
				if (IsSkillBetter(candidate, best))
				{
					best = candidate;
				}
			}

			return best;
		}

		static bool IsSkillBetter(Skill candidate, Skill current)
		{
			if (candidate == null || !CanUseSkill(candidate))
				return false;

			if (current == null)
				return true;

			bool isPrioritize = candidate.template.id == 17 && current.template.id == 2 ||
			                    candidate.template.id == 9 && current.template.id == 0;

			return current.coolDown < candidate.coolDown || isPrioritize;
		}

		static bool CanUseSkill(Skill skill)
		{
			if (mSystem.currentTimeMillis() - skill.lastTimeUseThisSkill > skill.coolDown)
				skill.paintCanNotUseSkill = false;

			if (skill.paintCanNotUseSkill && !IdSkillsMelee.Contains(skill.template.id))
				return false;

			if (IdSkillsCanNotAttack.Contains(skill.template.id))
				return false;

			if (mSystem.currentTimeMillis() - skill.lastTimeUseThisSkill < skill.coolDown)
				return false;

			if (Char.myCharz().cMP < GetManaUseSkill(skill))
				return false;

			return true;
		}

		static int GetManaUseSkill(Skill skill)
		{
			if (skill.template.manaUseType == 2)
				return 1;
			if (skill.template.manaUseType == 1)
				return (int)(skill.manaUse * Char.myCharz().cMPFull / 100);
			return skill.manaUse;
		}
	}
}
