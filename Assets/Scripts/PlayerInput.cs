using Beatrate.ExecutionOrder;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace AquariumScramble
{
	public struct ButtonInput
	{
		public bool IsPressed;
		public bool WasPressedThisFrame;
		public bool WasReleasedThisFrame;
	}

	[ExecutionOrder(-100)]
	public class PlayerInput : MonoBehaviour
	{
		public float GasAxis { get; private set; }
		public float ReverseAxis { get; private set; }
		public float SteerAxis { get; private set; }
		public ButtonInput Dash { get; private set; }
		public ButtonInput Jump { get; private set; }

		public void Update()
		{
			Keyboard keyboard = Keyboard.current;
			if(keyboard == null)
			{
				GasAxis = 0.0f;
				ReverseAxis = 0.0f;
				SteerAxis = 0.0f;
				Dash = default;
				return;
			}

			GasAxis = keyboard.wKey.isPressed ? 1.0f : 0.0f;
			ReverseAxis = keyboard.sKey.isPressed ? 1.0f : 0.0f;
			SteerAxis = (keyboard.aKey.isPressed ? -1.0f : 0.0f) + (keyboard.dKey.isPressed ? 1.0f : 0.0f);

			Dash = ToButtonInput(keyboard.shiftKey);
			Jump = ToButtonInput(keyboard.spaceKey);
		}

		private ButtonInput ToButtonInput(ButtonControl button)
		{
			return new ButtonInput
			{
				IsPressed = button.isPressed,
				WasPressedThisFrame = button.wasPressedThisFrame,
				WasReleasedThisFrame = button.wasReleasedThisFrame
			};
		}
	}
}