using UnityEngine;

namespace Mod
{
	public class ModScript : MonoBehaviour
	{
		void Awake()
		{
			GameEvents.OnAwake();
		}

		void Start()
		{
			GameEvents.OnGameStart();
		}
	}
}