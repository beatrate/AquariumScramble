using System;
using UnityEngine;

namespace AquariumScramble
{
	public class PlayerInterface : MonoBehaviour
	{
		[SerializeField]
		private PlayerHealthBar healthBar = null;
		[SerializeField]
		private GameStartInterface gameStartInterface = null;
		[SerializeField]
		private GameEndInterface gameEndInterface = null;

		private GameFlow gameFlow = null;

		private PlayerBoat player = null;

		public event Action StartPassed;

		public void Awake()
		{
			gameStartInterface.Completed += OnStartCompleted;
		}

		private void OnStartCompleted()
		{
			StartPassed?.Invoke();
		}

		public void Start()
		{
			gameFlow = GameFlow.Instance;

			if(gameFlow != null)
			{
				gameFlow.PlayerPossessed += OnPlayerPossessed;
				gameFlow.PlayerUnpossessed += OnPlayerUnpossessed;
			}
		}

		private void OnPlayerPossessed(PlayerBoat player)
		{
			this.player = player;
			player.Boat.Damaged += OnBoatDamaged;
			player.Boat.StartedDying += OnBoatStartedDying;

			healthBar.SetInitialHealth(player.Boat.CurrentHealth, player.Boat.StartHealth);
		}

		private void OnPlayerUnpossessed(PlayerBoat player)
		{
			player.Boat.Damaged -= OnBoatDamaged;
			player.Boat.StartedDying -= OnBoatStartedDying;
			this.player = null;
		}

		public void OnDestroy()
		{
			if(gameFlow != null)
			{
				gameFlow.PlayerPossessed -= OnPlayerPossessed;
				gameFlow.PlayerUnpossessed -= OnPlayerUnpossessed;
			}
		}

		public void Update()
		{
			
		}

		public void ShowStart()
		{
			gameStartInterface.Show();
		}

		public void ShowGameEnd(GameEndResult result)
		{
			gameEndInterface.Show(result);
		}

		private void OnBoatDamaged(DamageEvent e)
		{
			healthBar.UpdateWithDamage(e);
		}

		private void OnBoatStartedDying(DeathEvent e)
		{
			if(player == null)
			{
				return;
			}
			player.Boat.Damaged -= OnBoatDamaged;
			player.Boat.StartedDying -= OnBoatStartedDying;
		}
	}
}