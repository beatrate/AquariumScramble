using Beatrate.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AquariumScramble
{
	public struct PIDChannel
	{
		public float Proportional;
		public float Integral;
		public float Differential;

		public bool IsAngular;

		private float previousError;
		private float integralError;
		private bool isFresh;

		public void Clear()
		{
			previousError = 0.0f;
			integralError = 0.0f;
			isFresh = true;
		}

		public float Process(float current, float desired, float deltaTime)
		{
			if(deltaTime <= 0.0f)
			{
				return 0.0f;
			}

			float error = desired - current;
			if(IsAngular)
			{
				error = error % 360.0f;
				if(error > 180.0f)
				{
					error -= 360.0f;
				}
				else if(error < -180.0f)
				{
					error += 360.0f;
				}
			}
			//Debug.Log($"Current:{current} Desired:{desired} Error:{error}");

			integralError = isFresh ? error : integralError;
			previousError = isFresh ? error : previousError;
			integralError = (1.0f - deltaTime) * integralError + deltaTime * error;

			float derivativeError = (error - previousError) / deltaTime;
			previousError = error;
			isFresh = false;

			return Proportional * error + Integral * integralError + Differential * derivativeError;
		}
	}

	[Serializable]
	public struct PIDParameters
	{
		public float Proportional;
		public float Integral;
		public float Differential;
	}

	public struct AvoidanceCastHit
	{
		public RaycastHit RayHit;
		public Boat Boat;
	}

	public struct DamageRecord
	{
		public DamageEvent Event;
		public float Time;
	}

	public class EnemyBoat : MonoBehaviour
	{
		private const int AvoidanceRayCount = 6;
		private const float StuckDuration = 5.0f;
		private const int MaxEnemyCountTargetingPlayerWithImmediatePriority = 6;
		private const int SoftMinTargetCandidateCount = 6;
		private const float MaxDamageRevengeDistance = 20.0f;
		private const int MaxRandomTargetPickOffset = 3;

		[SerializeField]
		private PIDParameters steeringParameters = default;
		[SerializeField]
		private PIDParameters gasParameters = default;

		[SerializeField]
		[Min(0.0f)]
		private float avoidanceRadius = 10.0f;
		[SerializeField]
		[Min(0.0f)]
		private float avoidanceCastDistance = 12.0f;
		[SerializeField]
		[Min(0.0f)]
		private float avoidanceCastHeight = 0.5f;
		[SerializeField]
		private float avoidanceMultiplier = 1.0f;

		[SerializeField]
		[Min(0.0f)]
		private float minSingleTargetAgressionDuration = 0.5f;
		[SerializeField]
		[Min(0.0f)]
		private float damageRecordDuration = 1.0f;

		private Boat boat = null;
		private Boat targetBoat = null;
		private AIDirector aiDirector = null;

		private float aggressionTime = 0.0f;
		private float stuckTime = 0.0f;
		private bool isStuck = false;
		private System.Random random = null;
		private List<DamageRecord> damageRecords = new List<DamageRecord>();

		private PIDChannel steeringChannel = new PIDChannel
		{
			IsAngular = true
		};

		private PIDChannel gasChannel = new PIDChannel
		{
			IsAngular = false
		};

		public void Awake()
		{
			boat = GetComponent<Boat>();

			steeringChannel.Clear();
			gasChannel.Clear();
			random = new System.Random(System.Environment.TickCount + GetInstanceID());
			isStuck = false;
			stuckTime = 0.0f;

			boat.FixedUpdated += OnBoatFixedUpdate;
			boat.Damaged += OnBoatDamaged;
			boat.StartedDying += OnBoatStartedDying;
		}

		private void OnBoatStartedDying(DeathEvent e)
		{
			Destroy(this);
		}

		private void OnBoatDamaged(DamageEvent e)
		{
			var record = new DamageRecord
			{
				Event = e,
				Time = Time.time
			};

			damageRecords.Add(record);

			if(e.InstigatorBoat != null && e.InstigatorBoat == targetBoat)
			{
				aggressionTime = 0.0f;
			}
			else
			{
				aggressionTime = Mathf.MoveTowards(aggressionTime, minSingleTargetAgressionDuration, minSingleTargetAgressionDuration * ((float)e.Damage / boat.StartHealth));
			}
		}

		public void Start()
		{
			aiDirector = AIDirector.Instance;
		}

		public void OnDestroy()
		{
			if(boat != null)
			{
				boat.FixedUpdated -= OnBoatFixedUpdate;
				boat.Damaged -= OnBoatDamaged;
				boat.StartedDying -= OnBoatStartedDying;
			}
		}

		public void Update()
		{
			UpdatePIDChannelParameters(ref steeringChannel, steeringParameters);
			UpdatePIDChannelParameters(ref gasChannel, gasParameters);

			PruneStaleDamageRecords();

			if(targetBoat != null)
			{
				aggressionTime += Time.deltaTime;
			}
		}

		private void PruneStaleDamageRecords()
		{
			for(int i = damageRecords.Count - 1; i >= 0; --i)
			{
				var record = damageRecords[i];
				if(record.Event.InstigatorBoat == null || (Time.time - record.Time) >= damageRecordDuration)
				{
					damageRecords.RemoveAt(i);
				}
			}
		}

		private void UpdatePIDChannelParameters(ref PIDChannel channel, PIDParameters parameters)
		{
			channel.Proportional = parameters.Proportional;
			channel.Integral = parameters.Integral;
			channel.Differential = parameters.Differential;
		}

		private void OnBoatFixedUpdate()
		{
			if(boat.AliveState != AliveState.Alive)
			{
				return;
			}

			if(!aiDirector.RunAI)
			{
				return;
			}

			if(targetBoat == null || aggressionTime > minSingleTargetAgressionDuration)
			{
				//aggressionTime = Mathf.Lerp(0.0f, 0.5f * minSingleTargetAgressionDuration, (float)random.NextDouble());
				aggressionTime = 0.0f;

				var hits = ListPool<BoatCastHit>.Get();

				aiDirector.FindClosestBoats(boat, hits);
				if(hits.Count != 0)
				{
					int maxCheckIndex = Mathf.Min(Mathf.Max(hits.Count / 2, SoftMinTargetCandidateCount), hits.Count);
					Boat playerBoat = null;

					for(int i = 0; i < maxCheckIndex; ++i)
					{
						var hit = hits[i];
						if(hit.Boat.GetComponent<PlayerBoat>() != null)
						{
							playerBoat = hit.Boat;
						}
					}

					Boat candidateBoat = null;

					if(playerBoat != null && playerBoat.AliveState == AliveState.Alive && aiDirector.GetTargetInfo(playerBoat).AttackerCount < MaxEnemyCountTargetingPlayerWithImmediatePriority)
					{
						candidateBoat = playerBoat;
					}
					
					if(candidateBoat == null && damageRecords.Count != 0)
					{
						for(int i = 0; i < damageRecords.Count; ++i)
						{
							var record = damageRecords[i];
							if(record.Event.InstigatorBoat != null && Vector3.Distance(record.Event.InstigatorBoat.transform.position, boat.transform.position) < MaxDamageRevengeDistance && record.Event.InstigatorBoat.AliveState == AliveState.Alive)
							{
								candidateBoat = record.Event.InstigatorBoat;
							}
						}
					}

					if(candidateBoat == null)
					{
						var hit = hits[Mathf.Min(0 + random.Next(0, MaxRandomTargetPickOffset + 1), hits.Count - 1)];
						candidateBoat = hit.Boat;
					}
					
					Boat oldTargetBoat = targetBoat;
					targetBoat = candidateBoat;

					if(oldTargetBoat != targetBoat)
					{
						if(oldTargetBoat != null)
						{
							var info = aiDirector.GetTargetInfo(oldTargetBoat);
							--info.AttackerCount;
							aiDirector.SetTargetInfo(oldTargetBoat, info);
						}

						if(targetBoat != null)
						{
							var info = aiDirector.GetTargetInfo(targetBoat);
							++info.AttackerCount;
							aiDirector.SetTargetInfo(targetBoat, info);
						}
					}
				}

				ListPool<BoatCastHit>.Return(hits);
			}

			Vector3 targetPosition = transform.position;
			targetPosition.y = 0.0f;

			if(targetBoat != null)
			{
				targetPosition = targetBoat.transform.position;
				targetPosition.y = 0.0f;
			}

			var avoidanceHits = ListPool<AvoidanceCastHit>.Get();
			Vector3 avoidanceVector = Vector3.zero;
			CastAvoidance(avoidanceHits);

			for(int i = 0; i < avoidanceHits.Count; ++i)
			{
				var avoidanceHit = avoidanceHits[i];
				float avoidanceWeight = Mathf.Clamp01(avoidanceHit.RayHit.distance / avoidanceRadius);
				if(avoidanceHit.Boat == null)
				{
					avoidanceVector += avoidanceHit.RayHit.normal * avoidanceWeight * avoidanceMultiplier;
				}
			}

			if(avoidanceHits.Count != 0)
			{
				avoidanceVector /= avoidanceHits.Count;
			}
			
			//targetPosition += avoidanceVector;

			ListPool<AvoidanceCastHit>.Return(avoidanceHits);

			Vector3 currentPosition = transform.position;
			currentPosition.y = 0.0f;

			Vector3 toTarget = targetPosition - currentPosition;
			Vector3 desiredFacing;

			if(toTarget.magnitude > 0.01f)
			{
				desiredFacing = toTarget.normalized;
			}
			else
			{
				desiredFacing = Vector3.ProjectOnPlane(boat.transform.forward, Vector3.zero);
			}

			desiredFacing += avoidanceVector;

			Quaternion desiredRotation = Quaternion.LookRotation(desiredFacing, Vector3.up);
			float desiredYaw = MathUtility.SignedAngle(Quaternion.identity, desiredRotation, 1);

			Vector3 currentFacing = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
			Quaternion currentRotation = Quaternion.LookRotation(currentFacing, Vector3.up);
			float currentYaw = MathUtility.SignedAngle(Quaternion.identity, currentRotation, 1); ;

			float steeringYaw = steeringChannel.Process(currentYaw, desiredYaw, Time.deltaTime);
			float steeringControlValue = Mathf.Clamp(steeringYaw / 90.0f, -1.0f, 1.0f);
			boat.SetSteering(steeringControlValue);

			float currentSpeed = Vector3.Dot(transform.forward, boat.FloatingObject.Rigidbody.velocity);
			float desiredSpeed = 0.0f;

			if(toTarget.magnitude > 0.5f)
			{
				desiredSpeed = boat.TopSpeed * Mathf.Clamp01(toTarget.magnitude / boat.StopDistance);
			}
			if(avoidanceVector.magnitude > 0.01f)
			{
				desiredSpeed += avoidanceVector.magnitude;
			}

			float gasSpeed = gasChannel.Process(currentSpeed, desiredSpeed, Time.deltaTime);
			float gasControlValue = Mathf.Clamp(gasSpeed / boat.TopSpeed, -1.0f, 1.0f);
			if(gasControlValue >= 0.0f)
			{
				boat.SetEngineMode(BoatEngineMode.Forward);
				boat.SetEnginePower(Mathf.Abs(gasControlValue));
			}
			else
			{
				boat.SetEngineMode(BoatEngineMode.Reverse);
				boat.SetEnginePower(Mathf.Abs(gasControlValue));
			}


			if(!isStuck)
			{
				if(boat.FloatingObject.IsGrounded && boat.FloatingObject.Rigidbody.velocity.y < 0.0f)
				{
					isStuck = true;
					stuckTime = 0.0f;
				}
			}
			else
			{
				if(!boat.FloatingObject.IsGrounded || boat.FloatingObject.Rigidbody.velocity.y >= 0.0f)
				{
					isStuck = false;
					stuckTime = 0.0f;
				}
				else if(stuckTime > StuckDuration)
				{
					boat.Jump();
					isStuck = false;
					stuckTime = 0.0f;
				}
				else
				{
					stuckTime += Time.deltaTime;
				}
			}
		}

		private void CastAvoidance(List<AvoidanceCastHit> hits)
		{
			float obstacleHitRadius = avoidanceRadius / AvoidanceRayCount * 0.5f;
			hits.Clear();

			for(int i = 0; i < AvoidanceRayCount; ++i)
			{
				float t = (float)i / AvoidanceRayCount;
				Quaternion rotation = Quaternion.Euler(0.0f, 360.0f * t, 0.0f);
				Vector3 rayDirection = rotation * Vector3.forward;
				Vector3 rayStart = transform.position + Vector3.up * avoidanceCastHeight;
				bool wasObstacleHit = false;

				if(Physics.SphereCast(rayStart, obstacleHitRadius, rayDirection, out RaycastHit hit, avoidanceCastDistance, 1 << LayerMask.NameToLayer("Default"), QueryTriggerInteraction.Ignore))
				{
					if(hit.distance != 0.0f && hit.transform.root != gameObject.transform.root && hit.distance < avoidanceRadius)
					{
						wasObstacleHit = true;
						var avoidanceHit = new AvoidanceCastHit
						{
							RayHit = hit,
							Boat = hit.collider.GetComponentInParent<Boat>()
						};
						hits.Add(avoidanceHit);
					}
				}

				Vector3 rayEnd = rayStart + rayDirection * avoidanceCastDistance;
				Color color = Color.grey;

				if(wasObstacleHit)
				{
					rayEnd = rayStart + rayDirection * hit.distance;
					color = Color.green;
				}

				Debug.DrawLine(rayStart, rayEnd, color);
			}
		}
	}
}