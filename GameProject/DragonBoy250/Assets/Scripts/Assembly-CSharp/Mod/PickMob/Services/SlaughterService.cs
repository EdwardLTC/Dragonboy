using System.Collections;
using Mod.Constants;
using UnityEngine;

namespace Mod.PickMob.Services
{
	public static class SlaughterService
	{
		const float ATTACK_DELAY = 0.1f;
		const long BOSS_CHECK_COOLDOWN_MS = 4000;

		static long _lastBossCheckMs;

		public static IEnumerator DoSlaughter(Char myChar, bool isUseTDLT)
		{
			if (myChar.isCharge || myChar.stone || myChar.isFreez)
			{
				yield return new WaitForSecondsRealtime(ATTACK_DELAY);
				yield break;
			}

			AutoFusionService.AutoFusionInSlaughter(myChar);

			if (HasBossInMap())
			{
				yield return ZoneService.ChangeToLowestPlayerZone();
				yield break;
			}

			if (myChar.mobFocus != null && !MobPicker.IsMobTanSat(myChar.mobFocus))
			{
				myChar.mobFocus = null;
			}

			if (myChar.mobFocus == null)
			{
				myChar.mobFocus = MobPicker.GetMobTanSat();
				if (myChar.mobFocus != null && isUseTDLT)
				{
					myChar.cx = myChar.mobFocus.xFirst - 24;
					myChar.cy = myChar.mobFocus.yFirst;
					Service.gI().charMove();
				}
			}

			if (myChar.mobFocus != null)
			{
				ZoneService.ResetNoMobTimer();
				if (myChar.skillInfoPaint() == null)
				{
					Skill skill = SkillPicker.GetSkillAttack();
					if (skill is not null && !skill.paintCanNotUseSkill)
					{
						AttackMob(myChar, skill);
					}
				}
			}
			else
			{
				if (ZoneService.ShouldChangeZone())
				{
					yield return ZoneService.ChangeToLowestPlayerZone();
					yield break;
				}

				if (!isUseTDLT)
				{
					Mob mob = MobPicker.GetMobNext();
					if (mob != null)
					{
						MovementService.Move(myChar, mob.xFirst - 24, mob.yFirst);
					}
				}
			}

			yield return new WaitForSecondsRealtime(ATTACK_DELAY);
		}

		static void AttackMob(Char myChar, Skill skill)
		{
			Mob mobFocus = myChar.mobFocus;
			mobFocus.x = mobFocus.xFirst;
			mobFocus.y = mobFocus.yFirst;

			if (PickMobConfig.IsAttackMonsterBySendCommand)
			{
				AttackMobBySendCommand(myChar, skill, mobFocus);
			}
			else
			{
				AttackMobByFocus(myChar, skill, mobFocus);
			}
		}

		static void AttackMobBySendCommand(Char myChar, Skill skill, Mob mobFocus)
		{
			if (myChar.myskill != skill)
			{
				Service.gI().selectSkill(skill.template.id);
				myChar.myskill = skill;
			}

			if (mobFocus.getTemplate().type == MonsterType.Fly)
			{
				if (Math.abs(myChar.cx - mobFocus.x) > 70)
				{
					MovementService.Move(myChar, mobFocus.x, Utils.GetYGround(mobFocus.x));
				}
				else
				{
					myChar.currentMovePoint = null;
					myChar.cx = mobFocus.x + Res.random(-5, 5);
					myChar.cy = mobFocus.y + Res.random(-5, 5);
					Service.gI().charMove();
				}
			}
			else
			{
				MovementService.Move(myChar, mobFocus.xFirst, mobFocus.yFirst);
			}

			bool inRange = Utils.Distance(myChar, mobFocus) <= 50 || mobFocus.getTemplate().type == MonsterType.Fly && Math.abs(myChar.cx - mobFocus.x) <= 70;

			if (inRange && mSystem.currentTimeMillis() - skill.lastTimeUseThisSkill > skill.coolDown + 100L)
			{
				myChar.mobFocus = mobFocus;
				skill.lastTimeUseThisSkill = mSystem.currentTimeMillis();
				MyVector targets = new MyVector();
				targets.addElement(mobFocus);
				Service.gI().sendPlayerAttack(targets, new MyVector(), -1);
			}
		}

		static void AttackMobByFocus(Char myChar, Skill skill, Mob mobFocus)
		{
			GameScr.gI().doSelectSkill(skill, true);
			if (Res.distance(mobFocus.xFirst, mobFocus.yFirst, myChar.cx, myChar.cy) <= 48)
			{
				myChar.focusManualTo(mobFocus);
				Utils.DoDoubleClickToObj(mobFocus);
			}
			else
			{
				MovementService.Move(myChar, mobFocus.xFirst, mobFocus.yFirst);
			}
		}

		static bool HasBossInMap()
		{
			long now = mSystem.currentTimeMillis();
			if (now - _lastBossCheckMs < BOSS_CHECK_COOLDOWN_MS)
			{
				return false;
			}

			_lastBossCheckMs = now;

			if (GameScr.vCharInMap == null)
			{
				return false;
			}

			for (int i = 0; i < GameScr.vCharInMap.size(); i++)
			{
				Char @char = (Char)GameScr.vCharInMap.elementAt(i);
				if (@char.cTypePk == 5)
				{
					return true;
				}
			}

			return false;
		}
	}
}
