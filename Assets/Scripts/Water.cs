using UnityEngine;

namespace AquariumScramble
{
	public class Water : MonoBehaviour
	{
		public float Density => density;
		[SerializeField]
		[Min(0.0f)]
		private float density = 1;

		private WaterSystem waterSystem = null;

		public void Start()
		{
			waterSystem = WaterSystem.Instance;
			waterSystem.Attach(this);
		}

		public void OnDestroy()
		{
			if(waterSystem != null)
			{
				waterSystem.Detach(this);
			}
		}

		public bool TryGetSurfaceInfo(Vector3 position, out float height, out Vector3 normal)
		{
			float waterHeight = transform.position.y;
			if(position.y > waterHeight)
			{
				height = 0.0f;
				normal = Vector3.up;
				return false;
			}

			height = waterHeight;
			normal = Vector3.up;
			return true;
		}
	}
}