using System.Collections;
using Mod.Constants;
using Mod.ModHelper;
using Mod.R;

namespace Mod.Auto
{
	internal class AutoSkill : CoroutineMainThreadAction<AutoSkill>
	{
		internal static TargetMode targetMode { get; private set; } = TargetMode.None;
		static bool shouldReviveDeadChars => targetMode != TargetMode.None;
		internal static bool isUseCurrentSkill { get; set; }

		protected override float Interval => 0.3f;

		protected override IEnumerator OnUpdate()
		{
			if (shouldReviveDeadChars)
			{
				Char deadChar = getDeadCharInMap();
				Skill skillRescue = Char.myCharz().getSkill(Char.myCharz().nClass.skillTemplates[2]);
				if (deadChar == null || !skillRescue.CanUse())
				{
					yield break;
				}
				if (canHealChar(deadChar) && skillRescue.point <= 1)
				{
					useSkillOn(deadChar, skillRescue);
				}
				else
				{
					Utils.buffMe();
				}
			}

			if (isUseCurrentSkill && Char.myCharz().myskill != null && Char.myCharz().myskill.CanUse())
			{
				GameScr.gI().doSelectSkill(Char.myCharz().myskill, false);
			}
		}

		internal static void setReviveTargetMode(int target)
		{
			targetMode = (TargetMode)target;

			if (shouldReviveDeadChars && Char.myCharz().cgender != CharGender.Namekian)
			{
				targetMode = TargetMode.None;
				GameScr.info1.addInfo(Strings.youAreNotNamekian + '!', 0);
			}
		}

		static bool isValidTarget(Char target)
		{
			switch (targetMode)
			{
			case TargetMode.Everyone:
				return true;
			case TargetMode.OnlyClanMembers:
				return target.IsFromMyClan();
			case TargetMode.OnlyPet:
				return target.IsPet();
			case TargetMode.OnlyMyPet:
				return target.IsPet() && Char.myCharz().GetPetId() == target.charID;
			default:
				return false;
			}
		}

		static Char getDeadCharInMap()
		{
			int i = 0;
			for (; i < GameScr.vCharInMap.size(); i++)
			{
				Char ch = (Char)GameScr.vCharInMap.elementAt(i);
				if (isValidTarget(ch) && ch.IsCharDead())
					return ch;
			}
			if (i == GameScr.vCharInMap.size() && isValidTarget(Char.myCharz()) && Char.myCharz().IsCharDead())
				return Char.myCharz();
			return null;
		}

		static bool canHealChar(Char ch)
		{
			return ch.cFlag == Char.myCharz().cFlag;
		}

		static void useSkillOn(Char c, Skill skill)
		{
			Service.gI().selectSkill(skill.template.id);
			Service.gI().sendPlayerAttack(new MyVector(), new MyVector(new ArrayList
			{
				c
			}), -1);
			skill.lastTimeUseThisSkill = mSystem.currentTimeMillis();
		}

		internal enum TargetMode
		{
			None,
			Everyone,
			OnlyClanMembers,
			OnlyPet,
			OnlyMyPet
		}
	}
}
