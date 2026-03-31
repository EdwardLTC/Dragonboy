using UnityEngine;

namespace Mod
{
	public class ModScript : MonoBehaviour
	{
		void Awake()
		{
			GameHarmony.Initialize();
			GameEvents.OnAwake();
		}

		void Start()
		{
			GameEvents.OnGameStart();
		}
	}
}
