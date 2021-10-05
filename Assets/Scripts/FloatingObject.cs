using System.Collections.Generic;
using UnityEngine;

namespace AquariumScramble
{
	public class FloatingObject : MonoBehaviour
	{
		private const float MaxGroundedDistance = 1.0f;

		[SerializeField]
		[Range(0.0f, 1.0f)]
		private float normalizedVoxelSize = 0.5f;
		[SerializeField]
		[Min(0.0f)]
		private float density = 1;

		[SerializeField]
		[Min(0.0f)]
		private float dragOutsideWater = 1;
		[SerializeField]
		[Min(0.0f)]
		private float angularDragOutsideWater = 0.1f;

		public float DragInWater => dragInWater;
		[SerializeField]
		[Min(0.0f)]
		private float dragInWater = 1;

		[SerializeField]
		[Min(0.0f)]
		private float angularDragInWater = 0.1f;

		public Collider Collider => collider;
		[SerializeField]
		private new Collider collider = null;
		[SerializeField]
		private Transform centerOfMass = null;

		private float voxelSize = 0;
		private List<Vector3> voxels = new List<Vector3>();
		private WaterSystem waterSystem = null;

		public Rigidbody Rigidbody
		{
			get
			{
				if(rigidbody == null)
				{
					rigidbody = GetComponent<Rigidbody>();
				}
				return rigidbody;
			}
		}
		private new Rigidbody rigidbody = null;

		public bool IsPartiallySubmerged { get; private set; }
		public bool IsGrounded { get; private set; }

		public bool Float { get; set; } = true;

		public void Awake()
		{
			GenerateVoxels();
			IsPartiallySubmerged = false;
			IsGrounded = false;
		}

		public void Start()
		{
			waterSystem = WaterSystem.Instance;
		}

		public void OnDrawGizmosSelected()
		{
			if(Application.IsPlaying(this))
			{
				Gizmos.color = Color.white;
				for(int i = 0; i < voxels.Count; ++i)
				{
					Gizmos.DrawWireCube(transform.TransformPoint(voxels[i]), new Vector3(voxelSize, voxelSize, voxelSize) * 0.95f);
				}
			}
		}

		public void FixedUpdate()
		{
			if(centerOfMass != null)
			{
				Rigidbody.centerOfMass = transform.InverseTransformPoint(centerOfMass.position);
			}
			else
			{
				Rigidbody.centerOfMass = Vector3.zero;
			}

			if(!Float)
			{
				Rigidbody.drag = dragOutsideWater;
				Rigidbody.angularDrag = angularDragOutsideWater;
				return;
			}

			if(waterSystem != null && voxels.Count > 0)
			{
				float submergedVolume = 0;
				float volume = rigidbody.mass / density;

				for(int i = 0; i < voxels.Count; ++i)
				{
					Vector3 voxel = voxels[i];
					Vector3 voxelWorldSpace = transform.TransformPoint(voxel);

					if(waterSystem.TryGetSurfaceInfo(voxelWorldSpace, out float waterHeight, out Vector3 waterNormal, out float waterDensity))
					{
						Vector3 maxBuoyancyForce = (volume * waterDensity * -Physics.gravity) / voxels.Count;

						float submergedFactor = Mathf.Clamp01((waterHeight - voxelWorldSpace.y + 0.5f * voxelSize) / voxelSize);
						submergedVolume += submergedFactor;

						Quaternion surfaceRotation = Quaternion.FromToRotation(Vector3.up, waterNormal);
						surfaceRotation = Quaternion.Slerp(surfaceRotation, Quaternion.identity, submergedFactor);

						Vector3 voxelBuoyancyForce = surfaceRotation * maxBuoyancyForce * submergedFactor;
						Rigidbody.AddForceAtPosition(voxelBuoyancyForce, voxelWorldSpace);
					}
				}

				submergedVolume /= voxels.Count;
				IsPartiallySubmerged = submergedVolume > 0.01f;

				Rigidbody.drag = Mathf.Lerp(dragOutsideWater, dragInWater, submergedVolume);
				Rigidbody.angularDrag = Mathf.Lerp(angularDragOutsideWater, angularDragInWater, submergedVolume);

				if(Rigidbody.SweepTest(Vector3.down, out RaycastHit hit, MaxGroundedDistance, QueryTriggerInteraction.Ignore))
				{
					IsGrounded = true;
				}
				else
				{
					IsGrounded = false;
				}
			}
		}

		private void GenerateVoxels()
		{
			voxels.Clear();
			if(collider == null)
			{
				return;
			}

			Rigidbody.isKinematic = true;
			Quaternion startRotation = transform.rotation;
			transform.rotation = Quaternion.identity;

			Bounds bounds = collider.bounds;
			voxelSize = Mathf.Min(Mathf.Min(bounds.size.x, bounds.size.y), bounds.size.z) * normalizedVoxelSize;

			Vector3Int voxelsPerAxis = Vector3Int.RoundToInt(bounds.size / voxelSize);

			for(int x = 0; x < voxelsPerAxis.x; ++x)
			{
				for(int y = 0; y < voxelsPerAxis.y; ++y)
				{
					for(int z = 0; z < voxelsPerAxis.z; ++z)
					{
						Vector3 point = bounds.min + Vector3.Scale(new Vector3(voxelSize, voxelSize, voxelSize), new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
						if(IsPointInsideCollider(point, collider))
						{
							voxels.Add(transform.InverseTransformPoint(point));
						}
					}
				}
			}

			transform.rotation = startRotation;
			Rigidbody.isKinematic = false;
		}

		private static bool IsPointInsideCollider(Vector3 point, Collider collider)
		{
			Bounds bounds = collider.bounds;
			float rayLength = bounds.size.magnitude;
			Ray ray = new Ray(point, collider.transform.position - point);

			if(Physics.Raycast(ray, out RaycastHit hit, rayLength))
			{
				if(hit.collider == collider)
				{
					return false;
				}
			}

			return true;
		}
	}

	
}