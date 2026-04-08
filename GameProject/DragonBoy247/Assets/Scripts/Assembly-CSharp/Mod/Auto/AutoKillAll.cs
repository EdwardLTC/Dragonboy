using System;
using System.Collections;
using Mod.ModHelper;
using Mod.PickMob;
using UnityEngine;

namespace Mod.Auto
{
	public class AutoKillAll : CoroutineMainThreadAction<AutoKillAll>
	{
		protected override float Interval => 1f;

		protected override IEnumerator OnUpdate()
		{
			if (Char.myCharz().cFlag == 0)
			{
				Service.gI().getFlag(1, 8);
				yield return null;
			}

			for (int i = 0; i < GameScr.vCharInMap.size(); i++)
			{
				Char obj = (Char)GameScr.vCharInMap.elementAt(i);
				try
				{
					if (!string.IsNullOrEmpty(obj.cName) && !obj.isPet && !obj.isMiniPet && !obj.cName.StartsWith("#") && !obj.cName.StartsWith("$") && obj.cName != "Trọng tài" && obj.cFlag != 0 && !char.IsUpper(char.Parse(obj.cName.Substring(0, 1))) && obj.cHP > 0)
					{
						Char.myCharz().mobFocus = null;
						Char.myCharz().npcFocus = null;
						Char.myCharz().itemFocus = null;

						if (!obj.meDead && obj.cHP > 0 && obj.cFlag != 0 && obj.charID > 0)
						{
							Char.myCharz().charFocus = obj;

							Skill skill = PickMobController.GetSkillAttack();

							if (skill != null && !skill.paintCanNotUseSkill)
							{
								GameScr.gI().doSelectSkill(skill, true);
								if (Utils.isUsingTDLT())
								{
									Utils.TeleportMyChar(obj.cx, obj.cy);
								}
								Utils.DoDoubleClickToObj(obj);
							}
						}
					}
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}
		}
	}
}
