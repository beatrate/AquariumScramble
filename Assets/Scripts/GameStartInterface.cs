using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AquariumScramble
{
	public class GameStartInterface : MonoBehaviour
	{
		[SerializeField]
		private GameObject screen = null;

		public event Action Completed;

		public bool IsRunning { get; private set; }

		public void Update()
		{
			if(IsRunning)
			{
				if(Keyboard.current != null)
				{
					var keyboard = Keyboard.current;
					if(keyboard.anyKey.wasPressedThisFrame)
					{
						IsRunning = false;
						screen.SetActive(false);
						Completed?.Invoke();
					}
				}
			}
		}

		public void Show()
		{
			if(IsRunning)
			{
				return;
			}

			IsRunning = true;
			screen.SetActive(true);
		}
	}
}