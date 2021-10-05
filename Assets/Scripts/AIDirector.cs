using Beatrate.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AquariumScramble
{
	public struct BoatCastHit
	{
		public float Distance;
		public Boat Boat;
	}

	public struct BoatCastHitDistanceComparer : IComparer<BoatCastHit>
	{
		public int Compare(BoatCastHit x, BoatCastHit y)
		{
			return x.Distance.CompareTo(y.Distance);
		}
	}

	public struct TargetableInfo
	{
		public Boat Boat;
		public int AttackerCount;
	}

	public class AIDirector : MonoBehaviour
	{
		public static AIDirector Instance { get; private set; }

		public List<Boat> Boats => boats;
		private List<Boat> boats = new List<Boat>();

		public bool RunAI { get; private set; } = false;

		private List<TargetableInfo> targetableInfos = new List<TargetableInfo>();

		public event Action<Boat> BoatAttached;
		public event Action<Boat> BoatDetached;

		public void Awake()
		{
			if(Instance != null)
			{
				Destroy(this);
			}

			Instance = this;
		}

		public void AllowAI(bool allow)
		{
			RunAI = allow;
		}

		public void Attach(Boat boat)
		{
			boats.Add(boat);
			targetableInfos.Add(new TargetableInfo
			{
				Boat = boat,
				AttackerCount = 0
			});

			BoatAttached?.Invoke(boat);
		}

		public void Detach(Boat boat)
		{
			BoatDetached?.Invoke(boat);

			boats.Remove(boat);
			for(int i = 0; i < targetableInfos.Count; ++i)
			{
				if(targetableInfos[i].Boat == boat)
				{
					targetableInfos.RemoveAt(i);
				}
			}
		}

		public TargetableInfo GetTargetInfo(Boat boat)
		{
			for(int i = 0; i < targetableInfos.Count; ++i)
			{
				if(targetableInfos[i].Boat == boat)
				{
					return targetableInfos[i];
				}
			}

			Debug.LogWarning($"Missing targetable info for {boat.name}");
			return default;
		}

		public void SetTargetInfo(Boat boat, TargetableInfo info)
		{
			for(int i = 0; i < targetableInfos.Count; ++i)
			{
				if(targetableInfos[i].Boat == boat)
				{
					targetableInfos[i] = info;
					return;
				}
			}
		}

		public void FindClosestBoats(Boat boat, List<BoatCastHit> hits)
		{
			hits.Clear();

			Vector3 boatPlanarPosition = boat.transform.position;
			boatPlanarPosition.y = 0.0f;

			for(int i = 0; i < boats.Count; ++i)
			{
				Boat otherBoat = boats[i];
				if(otherBoat == null || otherBoat == boat || otherBoat.AliveState != AliveState.Alive)
				{
					continue;
				}

				Vector3 otherBoatPlanarPosition = otherBoat.transform.position;
				otherBoatPlanarPosition.y = 0.0f;

				var hit = new BoatCastHit
				{
					Distance = Vector3.Distance(boatPlanarPosition, otherBoatPlanarPosition),
					Boat = otherBoat
				};
				hits.Add(hit);
			}

			CollectionUtility.Sort(hits, new BoatCastHitDistanceComparer());
		}

		public int GetAliveBoatCount(bool includePlayer = false)
		{
			int count = 0;

			for(int i = 0; i < boats.Count; ++i)
			{
				var boat = boats[i];
				if(boat.GetComponent<PlayerBoat>() == null || includePlayer)
				{
					++count;
				}
			}

			return count;
		}
	}
}