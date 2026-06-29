using System.Collections;
using Mod.ModHelper;
using Mod.PickMob;

namespace Mod.Auto
{
	public class AutoKillAll : CoroutineMainThreadAction<AutoKillAll>
	{
		Char currentTarget;
		protected override float Interval => 0.2f;

		bool IsEnemyCandidate(Char target)
		{
			return target != null
			       && !string.IsNullOrEmpty(target.cName)
			       && !target.isPet
			       && !target.isMiniPet
			       && !target.cName.StartsWith("#")
			       && !target.cName.StartsWith("$")
			       && target.cName != "Trọng tài"
			       && target.cFlag != 0
			       && !char.IsUpper(char.Parse(target.cName.Substring(0, 1)))
			       && target.cHP > 0;
		}

		bool IsTargetValid(Char target)
		{
			return IsEnemyCandidate(target)
			       && !target.meDead
			       && GameScr.findCharInMap(target.charID) == target
			       && target.charID > 0
			       && target.cx >= 0
			       && target.cy >= 0
			       && target.cx <= TileMap.pxw
			       && target.cy <= TileMap.pxh;
		}

		void ClearFocus()
		{
			Char.myCharz().mobFocus = null;
			Char.myCharz().npcFocus = null;
			Char.myCharz().itemFocus = null;
		}

		void AttackTarget(Char target)
		{
			ClearFocus();
			Char.myCharz().charFocus = target;

			Skill skill = SkillPicker.GetSkillAttack();

			if (skill == null || skill.paintCanNotUseSkill)
			{
				return;
			}

			GameScr.gI().doSelectSkill(skill, true);

			bool inRange = Utils.Distance(Char.myCharz(), target) <= 50 || System.Math.Abs(Char.myCharz().cx - target.cx) <= 70;

			if (!inRange)
			{
				Utils.TeleportMyChar(target.cx - 30, Utils.GetYGround(target.cx));
				return;
			}

			skill.lastTimeUseThisSkill = mSystem.currentTimeMillis();
			MyVector targets = new MyVector();
			targets.addElement(target);
			Service.gI().sendPlayerAttack(new MyVector(), targets, 2);
		}

		protected override IEnumerator OnUpdate()
		{
			if (Char.myCharz().cFlag == 0)
			{
				Service.gI().getFlag(1, 8);
				yield return null;
			}

			if (Char.myCharz().meDead)
			{
				yield break;
			}

			if (currentTarget != null)
			{
				if (!IsTargetValid(currentTarget))
				{
					currentTarget = null;
				}
				else
				{
					AttackTarget(currentTarget);
					yield break;
				}
			}

			for (int i = 0; i < GameScr.vCharInMap.size(); i++)
			{
				Char obj = (Char)GameScr.vCharInMap.elementAt(i);
				if (IsTargetValid(obj))
				{
					currentTarget = obj;
					AttackTarget(obj);
					break;
				}
			}

		}

		protected override void OnStart()
		{
			Pk9rPickMob.IsAutoPickItems = false;
			base.OnStart();
		}

		protected override void OnStop()
		{
			Pk9rPickMob.IsAutoPickItems = true;
			base.OnStop();
		}
	}
}
