using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AstraSyndicate.PlayerControl
{
    /// <summary>
    /// Player flight bindings for keyboard/mouse and gamepad, defined in code so they live in one reviewable
    /// place. Touch controls can be added later as extra bindings without touching gameplay code.
    /// </summary>
    /// <remarks>
    /// Keyboard/mouse: mouse aim, W/S throttle, A/D turn, Shift boost, LMB fire, T or RMB target,
    /// F attack my target, G form up, hold Left Alt to free the cursor.
    /// Gamepad: right stick aim, left stick throttle/turn, LB boost, RT fire, RB target, D-pad up/down orders.
    /// </remarks>
    public sealed class FlightControls : IDisposable
    {
        private readonly InputActionMap _map = new InputActionMap("Flight");
        private readonly InputAction _aimPointer;
        private readonly InputAction _aimStick;
        private readonly InputAction _turn;
        private readonly InputAction _throttle;
        private readonly InputAction _boost;
        private readonly InputAction _fire;
        private readonly InputAction _target;
        private readonly InputAction _orderAttack;
        private readonly InputAction _orderFormUp;
        private readonly InputAction _freeCursor;

        public FlightControls()
        {
            _aimPointer = _map.AddAction("AimPointer", InputActionType.Value, "<Mouse>/delta", expectedControlLayout: "Vector2");
            _aimStick = _map.AddAction("AimStick", InputActionType.Value, "<Gamepad>/rightStick", expectedControlLayout: "Vector2");
            _turn = AddAxis("Turn", "<Keyboard>/a", "<Keyboard>/d", "<Gamepad>/leftStick/x");
            _throttle = AddAxis("Throttle", "<Keyboard>/s", "<Keyboard>/w", "<Gamepad>/leftStick/y");
            _boost = AddButton("Boost", "<Keyboard>/leftShift", "<Gamepad>/leftShoulder");
            _fire = AddButton("Fire", "<Mouse>/leftButton", "<Gamepad>/rightTrigger");
            _target = AddButton("Target", "<Keyboard>/t", "<Mouse>/rightButton", "<Gamepad>/rightShoulder");
            _orderAttack = AddButton("OrderAttack", "<Keyboard>/f", "<Gamepad>/dpad/up");
            _orderFormUp = AddButton("OrderFormUp", "<Keyboard>/g", "<Gamepad>/dpad/down");
            _freeCursor = AddButton("FreeCursor", "<Keyboard>/leftAlt");
        }

        /// <summary>Mouse movement this frame, in pixels.</summary>
        public Vector2 AimPointerDelta => _aimPointer.ReadValue<Vector2>();

        /// <summary>Aim stick deflection, -1..1 per axis.</summary>
        public Vector2 AimStick => _aimStick.ReadValue<Vector2>();

        public float Turn => _turn.ReadValue<float>();

        /// <summary>-1 (slow down) .. 1 (full throttle); 0 means cruise.</summary>
        public float Throttle => _throttle.ReadValue<float>();

        public bool Boost => _boost.IsPressed();

        public bool Fire => _fire.IsPressed();

        public bool TargetPressed => _target.WasPressedThisFrame();

        public bool OrderAttackPressed => _orderAttack.WasPressedThisFrame();

        public bool OrderFormUpPressed => _orderFormUp.WasPressedThisFrame();

        public bool FreeCursorHeld => _freeCursor.IsPressed();

        public void Enable()
        {
            _map.Enable();
        }

        public void Disable()
        {
            _map.Disable();
        }

        public void Dispose()
        {
            _map.Dispose();
        }

        private InputAction AddAxis(string name, string negative, string positive, string gamepadAxis)
        {
            InputAction action = _map.AddAction(name, InputActionType.Value, expectedControlLayout: "Axis");
            action.AddCompositeBinding("1DAxis").With("Negative", negative).With("Positive", positive);
            action.AddBinding(gamepadAxis).WithProcessor("axisDeadzone");
            return action;
        }

        private InputAction AddButton(string name, params string[] bindings)
        {
            InputAction action = _map.AddAction(name, InputActionType.Button);
            foreach (string binding in bindings) action.AddBinding(binding);
            return action;
        }
    }
}
