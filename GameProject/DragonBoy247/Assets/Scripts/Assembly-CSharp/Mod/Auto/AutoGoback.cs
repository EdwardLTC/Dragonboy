using System.Collections;
using Mod.ModHelper;
using Mod.Xmap;
using UnityEngine;

namespace Mod.Auto
{
    internal class AutoGoback :CoroutineMainThreadAction<AutoGoback>
    {
        static int? mapGoBackId;
        static int? zoneIdTrain;
        
        protected override float Interval => 1f;

        protected override IEnumerator OnUpdate()
        {
            if (Char.myCharz().IsCharDead())
            {
                mapGoBackId = TileMap.mapID;
                zoneIdTrain = TileMap.zoneID;
                yield return new WaitForSecondsRealtime(1f);
                ReviveWhenDead();
            }
            
            if (Utils.IsMyCharHome() && Char.myCharz().cHP < 1000)
            {
                yield return new WaitForSecondsRealtime(1f);
                RegenHpWhenInHome();
            }
            
            yield return ReturnToTrainMapIfNeeded();
            yield return ChangeToTrainZoneIfNeeded();
        }
        
        static void ReviveWhenDead()
        {
            Service.gI().returnTownFromDead();
        }
        
        static void RegenHpWhenInHome()
        {
            Service.gI().pickItem(-1);
        }
        
        static IEnumerator ChangeToTrainZoneIfNeeded()
        {
            if (TileMap.mapID != mapGoBackId || zoneIdTrain == null || TileMap.zoneID == zoneIdTrain)
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(1f);
            Service.gI().requestChangeZone(zoneIdTrain.Value, 0);
        }

        static IEnumerator ReturnToTrainMapIfNeeded()
        {
            if (mapGoBackId == null || XmapController.gI.IsActing || TileMap.mapID == mapGoBackId)
            {
                yield break;
            }
            XmapController.start(mapGoBackId.Value);
            yield return null;
        }
    }
}
