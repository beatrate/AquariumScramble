using System.Collections.Generic;
using UnityEngine;

namespace AquariumScramble
{
	public class NpcInterface : MonoBehaviour
	{
		private struct NpcInterfaceBinding
		{
			public Boat Boat;
			public NpcHealthBar HealthBar;
		}

		[SerializeField]
		private NpcHealthBar healthBarPrefab = null;
		[SerializeField]
		private Transform healthBarsParent = null;

		private AIDirector aiDirector = null;
		private Camera mainCamera = null;
		private Vector3[] corners = new Vector3[4];

		private List<NpcInterfaceBinding> bindings = new List<NpcInterfaceBinding>();
		private Canvas canvas = null;

		public void Awake()
		{
			canvas = GetComponentInParent<Canvas>();
			mainCamera = Camera.main;
		}

		public void Start()
		{
			aiDirector = AIDirector.Instance;
			
			if(aiDirector != null)
			{
				for(int i = 0; i < aiDirector.Boats.Count; ++i)
				{
					OnBoatAttached(aiDirector.Boats[i]);
				}
				aiDirector.BoatAttached += OnBoatAttached;
				aiDirector.BoatDetached += OnBoatDetached;
			}
		}

		public void OnDestroy()
		{
			if(aiDirector != null)
			{
				aiDirector.BoatAttached -= OnBoatAttached;
				aiDirector.BoatDetached -= OnBoatDetached;
			}
		}

		public void Update()
		{
			for(int i = 0; i < bindings.Count; ++i)
			{
				var binding = bindings[i];
				binding.HealthBar.transform.localPosition = WorldToCanvas(binding.Boat.transform.position + GetHealthBarWorldOffset(binding.Boat));
				if(ShouldCull(binding.HealthBar.transform as RectTransform))
				{
					binding.HealthBar.gameObject.SetActive(false);
				}
				else
				{
					binding.HealthBar.gameObject.SetActive(true);
				}
			}
		}

		private void OnBoatAttached(Boat boat)
		{
			if(boat.GetComponent<PlayerBoat>() == null)
			{
				BindBoat(boat);
			}
		}

		private void OnBoatDetached(Boat boat)
		{
			UnbindBoat(boat);
		}

		private void BindBoat(Boat boat)
		{
			var healthBar = Instantiate(healthBarPrefab, Vector3.zero, Quaternion.identity, healthBarsParent);
			healthBar.transform.localPosition = WorldToCanvas(boat.transform.position + GetHealthBarWorldOffset(boat));
			if(ShouldCull(healthBar.transform as RectTransform))
			{
				healthBar.gameObject.SetActive(false);
			}
			else
			{
				healthBar.gameObject.SetActive(true);
			}

			var binding = new NpcInterfaceBinding
			{
				Boat = boat,
				HealthBar = healthBar
			};

			bindings.Add(binding);

			binding.HealthBar.SetInitialHealth(boat.CurrentHealth, boat.StartHealth);

			boat.Damaged += OnBoatDamaged;
			boat.Died += OnBoatDied;
		}

		private void OnBoatDied(DeathEvent e)
		{
			int index = FindBindingIndex(e.Boat);
			if(index == -1)
			{
				return;
			}

			var binding = bindings[index];
		}

		private void OnBoatDamaged(DamageEvent e)
		{
			int index = FindBindingIndex(e.Boat);
			if(index == -1)
			{
				return;
			}

			var binding = bindings[index];
			binding.HealthBar.UpdateWithDamage(e);
		}

		private void UnbindBoat(Boat boat)
		{
			int index = FindBindingIndex(boat);
			if(index == -1)
			{
				return;
			}

			var binding = bindings[index];
			bindings.RemoveAt(index);
			Destroy(binding.HealthBar.gameObject);

			boat.Damaged -= OnBoatDamaged;
			boat.Died -= OnBoatDied;
		}

		private Vector3 GetHealthBarWorldOffset(Boat boat)
		{
			return boat.HealthBarOffset;
		}

		private Vector2 WorldToCanvas(Vector3 worldPosition)
		{
			Vector2 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, screenPosition, canvas.worldCamera, out Vector2 localPosition);
			return localPosition;
		}

		private bool ShouldCull(RectTransform rect)
		{
			return !CheckOverlap(canvas.transform as RectTransform, rect);
		}

		private bool CheckOverlap(RectTransform transform1, RectTransform transform2)
		{
			return GetWorldRect(transform1).Overlaps(GetWorldRect(transform2));
		}

		private Rect GetWorldRect(RectTransform rectTransform)
		{
			rectTransform.GetWorldCorners(corners);
			Vector2 min = new Vector2(corners[0].x, corners[0].y);
			Vector2 max = new Vector2(corners[2].x, corners[2].y);
			return new Rect(min, max - min);
		}

		private int FindBindingIndex(Boat boat)
		{
			for(int i = 0; i < bindings.Count; ++i)
			{
				if(bindings[i].Boat == boat)
				{
					return i;
				}
			}

			return -1;
		}
	}
}