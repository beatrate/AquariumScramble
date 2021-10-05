using UnityEngine;

namespace AquariumScramble
{
	public enum GameEndResult
	{
		None,
		GameOver,
		GameCompleted
	}

	public class GameEndInterface : MonoBehaviour
	{
		[SerializeField]
		private GameObject gameCompletedScreen = null;
		[SerializeField]
		private GameObject gameOverScreen = null;

		public void Show(GameEndResult result)
		{
			switch(result)
			{
				case GameEndResult.GameOver:
					gameOverScreen.SetActive(true);

					break;
				case GameEndResult.GameCompleted:
					gameCompletedScreen.SetActive(true);
					break;
			}
		}
	}
}