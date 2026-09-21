namespace Mod.CharEffect
{
	internal class CharEffectTime
	{
		internal bool hasBlackStarDragonBall;

		internal bool hasHuytSao;

		internal bool hasMobMe;

		internal bool hasMonkey;

		internal bool hasNamekianDragonBall;

		internal bool hasShield;

		internal bool isChocolate;

		internal bool isHypnotized;
		internal bool isHypnotizedByMe;

		internal bool isQCKK;

		internal bool isSelfExplode;

		internal bool isStone;

		internal bool isTDHS;

		internal bool isTeleported;

		internal bool isTied;
		internal bool isTiedByMe;
		internal long lastTimeChocolated;
		long lastTimeHoldingBlackStarDragonBall;
		internal long lastTimeHuytSao;
		internal long lastTimeHypnotized;
		internal long lastTimeMobMe;
		internal long lastTimeMonkey;
		internal long lastTimeQCKK;
		internal long lastTimeSelfExplode;
		internal long lastTimeShield;
		internal long lastTimeStoned;
		internal long lastTimeTDHS;
		internal long lastTimeTeleported;
		long lastTimeTied;
		internal int timeChocolate;
		internal int timeHoldingBlackStarDragonBall;
		internal int timeHuytSao;
		internal int timeHypnotized;
		internal int timeMobMe;
		internal int timeMonkey;
		internal int timeQCKK;
		internal int timeSelfExplode;
		internal int timeShield;
		internal int timeStone;
		internal int timeTDHS;
		internal int timeTeleported;
		internal int timeTied;

		internal void Update()
		{
			if (timeHoldingBlackStarDragonBall > 0 && mSystem.currentTimeMillis() - lastTimeHoldingBlackStarDragonBall >= 1000)
			{
				timeHoldingBlackStarDragonBall--;
				lastTimeHoldingBlackStarDragonBall = mSystem.currentTimeMillis();
			}
			if (timeHypnotized > 0 && mSystem.currentTimeMillis() - lastTimeHypnotized >= 1000)
			{
				timeHypnotized--;
				if (timeHypnotized == 0)
					isHypnotizedByMe = false;
				lastTimeHypnotized = mSystem.currentTimeMillis();
			}
			if (timeMonkey > 0 && mSystem.currentTimeMillis() - lastTimeMonkey >= 1000)
			{
				timeMonkey--;
				lastTimeMonkey = mSystem.currentTimeMillis();
			}
			if (timeHuytSao > 0 && mSystem.currentTimeMillis() - lastTimeHuytSao >= 1000)
			{
				timeHuytSao--;
				if (timeHuytSao <= 0)
					hasHuytSao = false;
				lastTimeHuytSao = mSystem.currentTimeMillis();
			}
			if (timeShield > 0 && mSystem.currentTimeMillis() - lastTimeShield >= 1000)
			{
				timeShield--;
				lastTimeShield = mSystem.currentTimeMillis();
			}
			if (timeTeleported > 0 && mSystem.currentTimeMillis() - lastTimeTeleported >= 1000)
			{
				timeTeleported--;
				lastTimeTeleported = mSystem.currentTimeMillis();
			}
			if (timeTied > 0 && mSystem.currentTimeMillis() - lastTimeTied >= 1000)
			{
				timeTied--;
				if (timeTied == 0)
					isTiedByMe = false;
				lastTimeTied = mSystem.currentTimeMillis();
			}
			if (timeMobMe > 0 && mSystem.currentTimeMillis() - lastTimeMobMe >= 1000)
			{
				timeMobMe--;
				lastTimeMobMe = mSystem.currentTimeMillis();
			}
			if (timeTDHS > 0 && mSystem.currentTimeMillis() - lastTimeTDHS >= 1000)
			{
				timeTDHS--;
				lastTimeTDHS = mSystem.currentTimeMillis();
			}
			if (timeStone > 0 && mSystem.currentTimeMillis() - lastTimeStoned >= 1000)
			{
				timeStone--;
				lastTimeStoned = mSystem.currentTimeMillis();
			}
			if (timeChocolate > 0 && mSystem.currentTimeMillis() - lastTimeChocolated >= 1000)
			{
				timeChocolate--;
				lastTimeChocolated = mSystem.currentTimeMillis();
			}

			if (timeSelfExplode > 0 && mSystem.currentTimeMillis() - lastTimeSelfExplode >= 1000)
			{
				timeSelfExplode--;
				lastTimeSelfExplode = mSystem.currentTimeMillis();
			}
			if (timeQCKK > 0 && mSystem.currentTimeMillis() - lastTimeQCKK >= 1000)
			{
				timeQCKK--;
				lastTimeQCKK = mSystem.currentTimeMillis();
			}
		}

		internal bool HasAnyEffect()
		{
			return timeTeleported + timeTied + timeHoldingBlackStarDragonBall + timeHuytSao + timeMobMe + timeMonkey + timeShield + timeHypnotized + timeTDHS + timeStone + timeChocolate + timeSelfExplode + timeQCKK > 0 || hasNamekianDragonBall;
		}
	}
}
