using System.Collections.Generic;
using UnityEngine;

namespace AquariumScramble
{
	public class WaterSystem : MonoBehaviour
	{
		public static WaterSystem Instance { get; private set; } = null;

		private List<Water> waterComponents = new List<Water>();

		public void Awake()
		{
			if(Instance != null)
			{
				Destroy(this);
			}

			Instance = this;
		}

		public void Attach(Water water)
		{
			waterComponents.Add(water);
		}

		public void Detach(Water water)
		{
			waterComponents.Remove(water);
		}

		public bool TryGetSurfaceInfo(Vector3 position, out float height, out Vector3 normal, out float density)
		{
			for(int i = 0; i < waterComponents.Count; ++i)
			{
				Water currentWater = waterComponents[i];
				if(currentWater == null)
				{
					continue;
				}

				if(currentWater.TryGetSurfaceInfo(position, out float currentHeight, out Vector3 currentNormal))
				{
					height = currentHeight;
					normal = currentNormal;
					density = currentWater.Density;
					return true;
				}
			}

			height = 0.0f;
			normal = Vector3.up;
			density = 0.0f;
			return false;
		}
	}

	
}