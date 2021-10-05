using UnityEngine;

namespace AquariumScramble
{
	public struct ConsumableInput
	{
		public bool WasPressed;

		public bool Consume()
		{
			bool pressed = WasPressed;
			WasPressed = false;
			return pressed;
		}
	}
	
	public class PlayerBoat : MonoBehaviour
	{
		public Boat Boat => boat;
		private Boat boat = null;
		private PlayerInput input = null;

		private ConsumableInput jumpInput = new ConsumableInput();

		public bool IsPossessed { get; private set; } = false;

		public void Awake()
		{
			boat = GetComponent<Boat>();
			input = GetComponent<PlayerInput>();

			boat.FixedUpdated += OnBoatFixedUpdate;
		}

		public void Update()
		{
			if(!IsPossessed)
			{
				return;
			}

			if(input.Jump.WasPressedThisFrame)
			{
				jumpInput.WasPressed = true;
			}
		}

		public void Possess()
		{
			if(IsPossessed)
			{
				return;
			}
			IsPossessed = true;
		}

		public void Unpossess()
		{
			if(!IsPossessed)
			{
				return;
			}
			IsPossessed = false;

			boat.SetEngineMode(BoatEngineMode.Forward);
			boat.SetEnginePower(0.0f);
			boat.SetSteering(0.0f);
			boat.SetDashing(false);

			jumpInput.Consume();
		}

		private void OnBoatFixedUpdate()
		{
			if(!IsPossessed)
			{
				return;
			}

			float gas = input.GasAxis;
			float reverse = input.ReverseAxis;
			float steer = input.SteerAxis;

			BoatEngineMode mode;
			float enginePower;

			if(reverse > 0)
			{
				mode = BoatEngineMode.Reverse;
				enginePower = reverse;
			}
			else
			{
				mode = BoatEngineMode.Forward;
				enginePower = gas;
			}

			boat.SetEngineMode(mode);
			boat.SetEnginePower(enginePower);
			boat.SetSteering(steer);

			bool dashInput = input.Dash.IsPressed;
			boat.SetDashing(dashInput);
			
			if(jumpInput.Consume() && (boat.FloatingObject.IsGrounded || boat.FloatingObject.IsPartiallySubmerged))
			{
				boat.Jump();
			}

			jumpInput.Consume();
		}
	}
}