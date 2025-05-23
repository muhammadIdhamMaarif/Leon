using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[SerializeField] private InputActionAsset inputActionAsset;

		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
		public bool interact;
		public bool canMove = true;

		[Header("Movement Settings")]
		public bool analogMovement;
		[SerializeField] private List<string> actions;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM

        public void OnInteraction(InputValue value)
		{
			InteractionInput(value.isPressed);
		}
		public void OnMove(InputValue value)
		{		
            MoveInput(value.Get<Vector2>());            
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}

        private void Awake()
        {
            inputActionAsset.Enable();
            foreach (string action in actions)
            {
                inputActionAsset.FindAction(action).performed += DoTestAction;
            }
        }

        private void OnDestroy()
        {
            foreach (string action in actions)
            {
                inputActionAsset.FindAction(action).performed -= DoTestAction;
            }
        }

        private void DoTestAction(InputAction.CallbackContext ctx)
        {
            string deviceName = ctx.action.activeControl.device.name;
			analogMovement = deviceName.Equals("XInputControllerWindows") || deviceName.Equals("DualShock4GamepadHID");
        }
#endif
        private void Update()
        {
            if (!canMove)
            {
				MoveInput(new Vector2());
            }
        }
        public void InteractionInput(bool newInteractionState)
		{
			interact = newInteractionState;
			if (Input.GetKeyDown(KeyCode.P)) interact = !interact;
		}
        public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;			
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}
		
		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}