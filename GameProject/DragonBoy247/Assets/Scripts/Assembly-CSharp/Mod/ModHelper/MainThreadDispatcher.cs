using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace Mod.ModHelper
{
	public class MainThreadDispatcher : MonoBehaviour
	{
		static readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();

		void Update()
		{
			while (queue.TryDequeue(out Action action))
			{
				action();
			}
		}

		public static void Dispatch(Action action)
		{
			queue.Enqueue(action);
		}
	}
}
