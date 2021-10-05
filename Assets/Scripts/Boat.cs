using Beatrate.ExecutionOrder;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AquariumScramble
{
	public enum BoatEngineMode
	{
		Forward,
		Reverse
	}

	public enum AliveState
	{
		Alive,
		Dying,
		Dead
	}

	public struct DamageEvent
	{
		public Boat Boat { get; set; }
		public Boat InstigatorBoat { get; set; }
		public int HealthBeforeDamage { get; set; }
		public int Damage { get; set; }
		public int HealthAfterDamage => HealthBeforeDamage - Damage;
	}

	public struct DeathEvent
	{
		public Boat Boat { get; set; }
	}

	public enum DamageResult
	{
		None,
		Killed
	}

	[ExecuteBefore(typeof(FloatingObject))]
	public class Boat : MonoBehaviour
	{
		public const float KillHeight = -100.0f;
		private const float MaxDeathDuration = 60.0f;
		private const float ExplosionImpulse = 20.0f;

		public Vector3 HealthBarOffset => healthBarOffset;
		[SerializeField]
		private Vector3 healthBarOffset = Vector3.zero;

		public int StartHealth => startHealth;
		[SerializeField]
		[Min(0)]
		private int startHealth = 200;

		[SerializeField]
		private List<Renderer> bodyRenderers = new List<Renderer>();
		[SerializeField]
		private Material deadMaterial = null;
		[SerializeField]
		private Renderer waterBlockerRenderer = null;
		[SerializeField]
		private ParticleSystem deathParticleSystemPrefab = null;
		[SerializeField]
		[Min(0.0f)]
		private float deathParticleSystemScale = 1.0f;
		[SerializeField]
		private Vector3 deathParticleSystemLocalOffset = Vector3.zero;

		[SerializeField]
		[Min(0)]
		private int lowHitDamage = 10;

		[SerializeField]
		[Min(0)]
		private int highHitDamage = 10;

		[SerializeField]
		[Min(0.0f)]
		private float lowHitDamageThresholdSpeed = 8.0f;

		[SerializeField]
		[Min(0.0f)]
		private float highHitDamageThresholdSpeed = 8.0f;

		public int CurrentHealth { get; private set; } = 0;
		public AliveState AliveState { get; private set; } = AliveState.Alive;

		public float ForwardForce => forwardForce;
		[SerializeField]
		[Min(0.0f)]
		private float forwardForce = 1.0f;

		public float TurnTorque => turnTorque;
		[SerializeField]
		[Min(0.0f)]
		private float turnTorque = 1.0f;

		public float JumpForce => jumpForce;
		[SerializeField]
		[Min(0.0f)]
		private float jumpForce = 10.0f;

		[SerializeField]
		[Min(0.0f)]
		private float minDashDuration = 0.2f;
		[SerializeField]
		[Min(0.0f)]
		private float dashForce = 1.0f;

		private float dashTime = 0.0f;

		public float TopSpeed => 2.0f * (ForwardForce / FloatingObject.DragInWater - Time.fixedDeltaTime * ForwardForce) / FloatingObject.Rigidbody.mass;
		public float StopDuration => TopSpeed / (ForwardForce / FloatingObject.Rigidbody.mass);
		public float StopDistance => (ForwardForce / FloatingObject.Rigidbody.mass) * Time.fixedDeltaTime * Time.fixedDeltaTime / 2.0f;

		public bool IsDashing => isDashing;
		private bool isDashing = false;

		private bool isDashingInputActive = false;

		public FloatingObject FloatingObject => floatingObject;
		private FloatingObject floatingObject = null;
		private AIDirector aiDirector = null;
		private float deathTime = 0.0f;

		public BoatEngineMode EngineMode { get; private set; }
		public float EnginePower { get; private set; }
		public float Steering { get; private set; }

		public event Action FixedUpdated;

		public event Action<DamageEvent> Damaged;
		public event Action<DeathEvent> StartedDying;
		public event Action<DeathEvent> Died;

		public void Awake()
		{
			floatingObject = GetComponent<FloatingObject>();

			dashTime = 0.0f;
			isDashing = false;
			isDashingInputActive = false;
			CurrentHealth = StartHealth;

			SetEngineMode(BoatEngineMode.Forward);
			SetEnginePower(0);
			SetSteering(0);
		}

		public void Start()
		{
			aiDirector = AIDirector.Instance;
			aiDirector.Attach(this);
		}

		public void OnDestroy()
		{
			if(aiDirector != null && AliveState == AliveState.Alive)
			{
				aiDirector.Detach(this);
			}
		}

		public void Update()
		{
			if(AliveState == AliveState.Dying)
			{
				OnKeepDying();
			}
		}

		public void FixedUpdate()
		{
			FixedUpdated?.Invoke();

			if(AliveState != AliveState.Alive)
			{
				return;
			}

			if(floatingObject != null)
			{
				float forwardForceWithPower = forwardForce * EnginePower * (EngineMode == BoatEngineMode.Reverse ? -1 : 1);
				float torqueForceWithSteering = turnTorque * Steering * (EngineMode == BoatEngineMode.Reverse ? -1 : 1);

				if(floatingObject.IsPartiallySubmerged)
				{
					floatingObject.Rigidbody.AddRelativeForce(Vector3.forward * forwardForceWithPower);
					floatingObject.Rigidbody.AddRelativeTorque(Vector3.up * torqueForceWithSteering);
				}
				else
				{
					floatingObject.Rigidbody.AddForce(Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized * forwardForceWithPower);
					floatingObject.Rigidbody.AddTorque(Vector3.up * torqueForceWithSteering);
				}

				if(!isDashing && isDashingInputActive)
				{
					isDashing = true;
				}

				if(isDashing)
				{
					if(dashTime > minDashDuration && !isDashingInputActive)
					{
						isDashing = false;
						dashTime = 0.0f;
					}
					else
					{
						if(floatingObject.IsPartiallySubmerged)
						{
							floatingObject.Rigidbody.AddRelativeForce(Vector3.forward * dashForce);
						}
						else
						{
							floatingObject.Rigidbody.AddForce(Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized * dashForce);
						}

						dashTime += Time.deltaTime;
					}
				}
			}
		}

		public void OnCollisionEnter(Collision collision)
		{
			var otherBoat = collision.gameObject.GetComponent<Boat>();
			if(otherBoat != null)
			{
				if(Vector3.Dot(collision.relativeVelocity, FloatingObject.Rigidbody.velocity) < 0.0f)
				{
					//Debug.Log($"{gameObject.name} hitting {otherBoat.gameObject.name} with speed {collision.relativeVelocity.magnitude}");

					float speed = collision.relativeVelocity.magnitude;
					if(speed >= highHitDamageThresholdSpeed)
					{
						otherBoat.Damage(highHitDamage, this);
					}
					else if(speed >= lowHitDamageThresholdSpeed)
					{
						otherBoat.Damage(lowHitDamage, this);
					}
				}
			}
		}

		public void SetEngineMode(BoatEngineMode mode)
		{
			if(EngineMode != mode)
			{
				SetEnginePower(0);
			}
			EngineMode = mode;
		}

		public void SetEnginePower(float power)
		{
			EnginePower = Mathf.Clamp01(power);
		}

		public void SetSteering(float steering)
		{
			Steering = Mathf.Clamp(steering, -1.0f, 1.0f);
		}

		public void SetDashing(bool dashing)
		{
			isDashingInputActive = dashing;
		}

		public void Jump()
		{
			floatingObject.Rigidbody.AddForce(Vector3.up * jumpForce);
		}

		public DamageResult Damage(int points, Boat instigatorBoat)
		{
			if(AliveState != AliveState.Alive)
			{
				return DamageResult.None;
			}

			int oldHealth = CurrentHealth;
			int newHealth = Mathf.Max(oldHealth - points, 0);
			points = oldHealth - newHealth;

			if(oldHealth != newHealth)
			{
				CurrentHealth = newHealth;
			}

			bool resultsInDeath = false;
			if(oldHealth != newHealth && CurrentHealth == 0)
			{
				resultsInDeath = true;
			}

			var a = new DamageEvent
			{
				Boat = this,
				InstigatorBoat = instigatorBoat,
				HealthBeforeDamage = oldHealth,
				Damage = points
			};

			// Raise event anyway even if we took 0 damage.
			Damaged?.Invoke(a);

			if(resultsInDeath)
			{
				StartDying();
				return DamageResult.Killed;
			}

			return DamageResult.None;
		}

		private void StartDying()
		{
			if(AliveState != AliveState.Alive)
			{
				return;
			}

			AliveState = AliveState.Dying;
			OnStartDying();
			StartedDying?.Invoke(new DeathEvent
			{
				Boat = this
			});
		}

		private void OnStartDying()
		{
			if(aiDirector != null)
			{
				aiDirector.Detach(this);
			}

			floatingObject.Float = false;
			floatingObject.Rigidbody.AddForce(Vector3.up * ExplosionImpulse, ForceMode.VelocityChange);

			if(deathParticleSystemPrefab != null)
			{
				ParticleSystem deathParticleSystem = Instantiate(deathParticleSystemPrefab, transform.TransformPoint(deathParticleSystemLocalOffset), Quaternion.identity);
				deathParticleSystem.transform.parent = null;
				deathParticleSystem.transform.localScale = Vector3.one * deathParticleSystemScale;

				var main = deathParticleSystem.main;
				main.stopAction = ParticleSystemStopAction.Destroy;
				deathParticleSystem.Play(withChildren: true);
			}

			if(waterBlockerRenderer != null)
			{
				waterBlockerRenderer.enabled = false;
			}

			for(int bodyRendererIndex = 0; bodyRendererIndex < bodyRenderers.Count; ++bodyRendererIndex)
			{
				var bodyRenderer = bodyRenderers[bodyRendererIndex];

				if(deadMaterial != null)
				{
					var materials = new Material[bodyRenderer.sharedMaterials.Length];

					for(int materialIndex = 0; materialIndex < materials.Length; ++materialIndex)
					{
						materials[materialIndex] = deadMaterial;
					}

					bodyRenderer.sharedMaterials = materials;
				}
				else
				{
					// Just in case.
					for(int materialIndex = 0; materialIndex < bodyRenderer.materials.Length; ++materialIndex)
					{
						var material = bodyRenderer.materials[materialIndex];
						material.SetColor("_Color", Color.black);
					}
				}
			}
		}

		private void OnKeepDying()
		{
			if(deathTime >= MaxDeathDuration || transform.position.y < KillHeight)
			{
				EndDying();
			}
			else
			{
				deathTime += Time.deltaTime;
			}
		}

		public void EndDying()
		{
			if(AliveState != AliveState.Dying)
			{
				return;
			}

			AliveState = AliveState.Dead;
			OnDied();

			var e = new DeathEvent
			{
				Boat = this
			};

			Died?.Invoke(e);

			Destroy(gameObject);
		}

		private void OnDied()
		{

		}
	}
}