using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Mod.ModHelper
{
	public static class DelayedAction
	{
		struct ScheduledEntry
		{
			public float executeAtRealtime;
			public float intervalSeconds;
			public Action action;
			public int id;
		}
		
		static readonly ConcurrentQueue<ScheduledEntry> _pending = new ConcurrentQueue<ScheduledEntry>();
		static readonly List<ScheduledEntry> _scheduled = new List<ScheduledEntry>();
		static readonly HashSet<int> _cancelledIds = new HashSet<int>();
		static int _nextId;
		
		public static int Schedule(float delaySeconds, Action action)
		{
			int id = System.Threading.Interlocked.Increment(ref _nextId);
			_pending.Enqueue(new ScheduledEntry
			{
				executeAtRealtime = Time.realtimeSinceStartup + delaySeconds,
				intervalSeconds = -1f,
				action = action,
				id = id
			});
			return id;
		}
		
		public static int ScheduleRepeating(float intervalSeconds, Action action)
		{
			int id = System.Threading.Interlocked.Increment(ref _nextId);
			_pending.Enqueue(new ScheduledEntry
			{
				executeAtRealtime = Time.realtimeSinceStartup + intervalSeconds,
				intervalSeconds = intervalSeconds,
				action = action,
				id = id
			});
			return id;
		}
		
		public static void Cancel(int id)
		{
			_cancelledIds.Add(id);
		}
		
		internal static void Tick()
		{
			while (_pending.TryDequeue(out ScheduledEntry entry))
			{
				_scheduled.Add(entry);
			}

			if (_scheduled.Count == 0)
				return;

			float now = Time.realtimeSinceStartup;

			for (int i = _scheduled.Count - 1; i >= 0; i--)
			{
				ScheduledEntry entry = _scheduled[i];

				if (_cancelledIds.Remove(entry.id))
				{
					_scheduled.RemoveAt(i);
					continue;
				}

				if (now >= entry.executeAtRealtime)
				{
					try
					{
						entry.action?.Invoke();
					}
					catch (Exception ex)
					{
						Debug.LogError("[DelayedAction] Error executing scheduled action: " + ex);
					}

					if (entry.intervalSeconds > 0f)
					{
						// Reschedule for next interval
						entry.executeAtRealtime = now + entry.intervalSeconds;
						_scheduled[i] = entry;
					}
					else
					{
						_scheduled.RemoveAt(i);
					}
				}
			}
		}
	}
}