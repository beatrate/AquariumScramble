using Cinemachine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AquariumScramble
{
	[Serializable]
	public class EnemySet
	{
		public EnemyBoat EnemyPrefab = null;
		[Min(0)]
		public int MinCount;
		[Min(0)]
		public int MaxCount;
	}

	[Serializable]
	public class GameStage
	{
		public PlayerBoat PlayerPrefab = null;
		public List<EnemySet> EnemySets = new List<EnemySet>();
	}

	public class GameFlow : MonoBehaviour
	{
		private const float GroundHeight = 0.0f;
		private const float SpawnHeightOffset = 5.0f;

		private enum GameFlowState
		{
			None,
			Start,
			StageInProgress,
			GameOver,
			GameCompleted
		}

		public static GameFlow Instance { get; private set; }

		[SerializeField]
		private List<GameStage> stages = new List<GameStage>();

		[SerializeField]
		private CinemachineVirtualCamera playerCamera = null;

		private PlayerInterface playerInteface = null;
		private AIDirector aiDirector = null;
		private GameFlowState currentState = GameFlowState.None;
		private PlayerSpawn initialPlayerSpawn = null;

		private int currentStageIndex = -1;
		private Vector3 lastPossessedPlayerPosition = Vector3.zero;
		private Quaternion lastPossessedPlayerRotation = Quaternion.identity;
		private bool isReloading = false;
		private List<EnemySpawn> enemySpawners = new List<EnemySpawn>();
		private int currentStageEnemyCount = 0;
		private System.Random random = null;

		public PlayerBoat PossessedPlayer { get; private set; } = null;

		public event Action<PlayerBoat> PlayerPossessed;
		public event Action<PlayerBoat> PlayerUnpossessed;

		public void Awake()
		{
			if(Instance != null)
			{
				Destroy(this);
				return;
			}

			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;

			Instance = this;
			random = new System.Random(System.Environment.TickCount);
		}

		public void Start()
		{
			aiDirector = AIDirector.Instance;
			initialPlayerSpawn = FindObjectOfType<PlayerSpawn>(includeInactive: true);
			enemySpawners = FindObjectsOfType<EnemySpawn>().ToList();
			playerInteface = FindObjectOfType<PlayerInterface>(includeInactive: true);
			playerCamera.m_Follow = null;
			playerCamera.m_LookAt = null;
			currentStageIndex = -1;

			TransitionTo(GameFlowState.Start);
		}

		public void Update()
		{
			if(isReloading)
			{
				return;
			}

			if(Keyboard.current != null)
			{
				Keyboard keyboard = Keyboard.current;
				
				if(keyboard.rKey.wasPressedThisFrame)
				{
					isReloading = true;
					SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
					return;
				}
			}

			UpdateState(currentState);
		}

		private void TransitionTo(GameFlowState state)
		{
			if(currentState != GameFlowState.None)
			{
				ExitState(currentState, state);
			}

			if(state != GameFlowState.None)
			{
				EnterState(state, currentState);
			}

			currentState = state;
		}

		private void EnterState(GameFlowState state, GameFlowState previousState)
		{
			switch(state)
			{
				case GameFlowState.Start:
					aiDirector.AllowAI(false);

					playerInteface.ShowStart();
					playerInteface.StartPassed += OnPlayerInterfaceStartPassed;
					break;
				case GameFlowState.StageInProgress:
					aiDirector.AllowAI(true);
					Vector3 spawnPosition;
					Quaternion spawnRotation;

					if(currentStageIndex == 0)
					{
						spawnPosition = initialPlayerSpawn == null ? Vector3.zero : initialPlayerSpawn.transform.position;
						spawnRotation = initialPlayerSpawn == null ? Quaternion.identity : initialPlayerSpawn.transform.rotation;
					}
					else
					{
						spawnPosition = lastPossessedPlayerPosition;
						spawnRotation = lastPossessedPlayerRotation;
					}

					var currentStage = stages[currentStageIndex];
					spawnPosition.y = GroundHeight + SpawnHeightOffset;
					PossessedPlayer = Instantiate(currentStage.PlayerPrefab, spawnPosition, spawnRotation);

					playerCamera.m_LookAt = PossessedPlayer.transform;
					playerCamera.m_Follow = PossessedPlayer.transform;
					PossessedPlayer.Boat.StartedDying += OnPlayerStartedDying;
					PossessedPlayer.Possess();
					PlayerPossessed?.Invoke(PossessedPlayer);
					currentStageEnemyCount = 0;

					int spawnerIndex = 0;
					for(int enemySetIndex = 0; enemySetIndex < currentStage.EnemySets.Count; ++enemySetIndex)
					{
						var enemySet = currentStage.EnemySets[enemySetIndex];
						if(enemySet.EnemyPrefab != null)
						{
							int count = random.Next(enemySet.MinCount, enemySet.MaxCount + 1);
							for(int i = 0; i < count; ++i)
							{
								var spawner = enemySpawners[spawnerIndex];
								spawnerIndex = (spawnerIndex + 1) % enemySpawners.Count;

								Vector3 enemySpawnPosition = spawner.transform.position;
								enemySpawnPosition.y = GroundHeight + SpawnHeightOffset;
								Quaternion enemySpawnRotation = spawner.transform.rotation;

								Instantiate(enemySet.EnemyPrefab, enemySpawnPosition, enemySpawnRotation);
								++currentStageEnemyCount;
							}
						}
					}

					break;
				case GameFlowState.GameOver:
					playerInteface.ShowGameEnd(GameEndResult.GameOver);
					break;
				case GameFlowState.GameCompleted:
					playerInteface.ShowGameEnd(GameEndResult.GameCompleted);
					break;
			}
		}

		private void OnPlayerInterfaceStartPassed()
		{
			TransitionTo(GameFlowState.StageInProgress);
		}

		private void ExitState(GameFlowState state, GameFlowState nextState)
		{
			switch(state)
			{
				case GameFlowState.Start:
					playerInteface.StartPassed -= OnPlayerInterfaceStartPassed;
					currentStageIndex = 0;
					break;
				case GameFlowState.StageInProgress:
				{
					aiDirector.AllowAI(false);
					PossessedPlayer.Unpossess();
					PossessedPlayer.Boat.StartedDying -= OnPlayerStartedDying;
					PlayerUnpossessed?.Invoke(PossessedPlayer);

					playerCamera.m_LookAt = null;
					playerCamera.m_Follow = null;

					if(nextState == GameFlowState.StageInProgress)
					{
						PossessedPlayer.Boat.Damage(PossessedPlayer.Boat.CurrentHealth + 9999, null);
					}
					else
					{

					}

					PossessedPlayer = null;

					for(int i = 0; i < aiDirector.Boats.Count; ++i)
					{
						var boat = aiDirector.Boats[i];
						if(boat.AliveState == AliveState.Alive && boat.GetComponent<PlayerBoat>() == null)
						{
							boat.Damage(boat.CurrentHealth + 9999, null);
						}
					}

					break;
				}
			}
		}

		private void UpdateState(GameFlowState state)
		{
			switch(state)
			{
				case GameFlowState.StageInProgress:
					if(aiDirector.GetAliveBoatCount(includePlayer: false) == 0)
					{
						if(currentStageIndex == stages.Count - 1)
						{
							TransitionTo(GameFlowState.GameCompleted);
						}
						else
						{
							++currentStageIndex;
							TransitionTo(GameFlowState.StageInProgress);
						}
					}
					break;
			}
		}

		private void OnPlayerStartedDying(DeathEvent e)
		{
			TransitionTo(GameFlowState.GameOver);
		}
	}
}