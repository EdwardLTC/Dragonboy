using System;
using System.Linq;
using Mod.ModHelper;
using UnityEngine;
using Object = UnityEngine.Object;

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
		
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		static void Init()
		{
			var types = AppDomain.CurrentDomain
				.GetAssemblies()
				.SelectMany(a => a.GetTypes())
				.Where(t =>
					!t.IsAbstract &&
					t.BaseType != null &&
					t.BaseType.IsGenericType &&
					t.BaseType.GetGenericTypeDefinition() == typeof(CoroutineMainThreadAction<>));

			foreach (var type in types)
			{
				var obj = new GameObject(type.Name);
				obj.AddComponent(type);
				Object.DontDestroyOnLoad(obj);
			}
		}
	}
}