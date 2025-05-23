using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Rotation speed of the character")]
        public float RotationSpeed = 1.0f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;
        
        public AudioClip LandingAudioClip;
        public AudioClip[] DefaultClips;
        public AudioClip[] WoodClips;
        public AudioClip[] TileClips;
        public AudioClip[] CarpetClips;        
        public AudioClip[] ConcreteClips;               
        public AudioClip[] WetClips;
        public AudioClip[] FragileClips;

        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _animationSpeedX;
        private float _animationSpeedZ;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        private float _lastFootstep;
        private float _bodyDirection;
        private float _differenceAngle;
        private Vector3 movementVector;        

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDVelocityX;
        private int _animIDVelocityZ;

        // others
        private float smoothRotation;
        private bool _isLeft;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }


        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;            
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();
            Move();
            FootstepAudio();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDVelocityX = Animator.StringToHash("VelocityX");
            _animIDVelocityZ = Animator.StringToHash("VelocityZ");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier * RotationSpeed;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier * RotationSpeed;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);

            //if (_input.look.sqrMagnitude >= _threshold)
            //{
            //    //Don't multiply mouse input by Time.deltaTime
            //    float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            //    _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
            //    _rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

            //    // clamp our pitch rotation
            //    _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            //    // Update Cinemachine camera target pitch
            //    CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

            //    // rotate the player left and right
            //    transform.Rotate(Vector3.up * _rotationVelocity);
            //}
        }

        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero)
            {
                targetSpeed = 0.0f;                
                //_animationSpeedX = Mathf.Lerp(_animationSpeedX, targetSpeed, Time.deltaTime * SpeedChangeRate);
                //_animationSpeedZ = Mathf.Lerp(_animationSpeedZ, targetSpeed, Time.deltaTime * SpeedChangeRate);
            }                

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;            

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;
            targetSpeed *= inputMagnitude;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                if (currentHorizontalSpeed < targetSpeed - speedOffset) 
                    _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed, Time.deltaTime * SpeedChangeRate);
                else if (currentHorizontalSpeed > targetSpeed + speedOffset)
                    _speed = Mathf.Lerp(targetSpeed, currentHorizontalSpeed, Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
                _speed = _speed < 0.1f ? 0.0f : _speed;
            }

            _animationSpeedX = Mathf.Lerp(_animationSpeedX, _input.move.x != 0 ? _speed : 0.0f, Time.deltaTime * SpeedChangeRate);
            _animationSpeedZ = Mathf.Lerp(_animationSpeedZ, _input.move.y != 0 ? _speed : 0.0f, Time.deltaTime * SpeedChangeRate);

            if (_animationBlend < 0.01f) _animationBlend = 0f;
            if (_animationSpeedX < 0.01f) _animationSpeedX = 0f;
            if (_animationSpeedZ < 0.01f) _animationSpeedZ = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            _bodyDirection = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
            //Debug.Log(_bodyDirection);
            // rotate to face input direction relative to camera position                                
            // transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            //_differenceAngle = Mathf.Abs(_bodyDirection - _cinemachineTargetYaw);
            //_differenceAngle = _differenceAngle > 180 ? Modulus((-1) * _differenceAngle, 360) : _differenceAngle;
            _differenceAngle = LoopAround(_bodyDirection - _cinemachineTargetYaw);
            if (_input.move == Vector2.zero)
            {
                smoothRotation = Mathf.Lerp(_bodyDirection, _cinemachineTargetYaw, Time.deltaTime * -15.0f);
                transform.rotation = Quaternion.Euler(0.0f, smoothRotation, 0.0f);                
                //if (_differenceAngle < -30.0f)
                //{
                //    _animator.SetTrigger("TurnLeft");
                //}
                //else if (_differenceAngle > 30.0f)
                //{
                //    _animator.SetTrigger("TurnRight");
                //}
                //else
                //{
                //    _animator.SetTrigger("Reset");
                //}
            }
            else
            {
                //_animator.SetTrigger("Reset");
                transform.rotation = Quaternion.Euler(0.0f, _cinemachineTargetYaw, 0.0f);
                Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;                

                // move the playere
                Vector3 movementVector = (targetDirection.normalized * (_speed * Time.deltaTime) +
                                         new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

                _controller.Move(movementVector);

            }
            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _speed);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
                _animator.SetFloat(_animIDVelocityX, movementVector.x * 
                                   _cinemachineTargetYaw % 180 > 45 && _cinemachineTargetYaw % 180 < 135 ? 
                                   _animationSpeedZ : _animationSpeedX * 10);
                _animator.SetFloat(_animIDVelocityZ, movementVector.z * 
                                   _cinemachineTargetYaw % 180 > 45 && _cinemachineTargetYaw % 180 < 135 ?
                                   _animationSpeedX : _animationSpeedZ * 10);

            }
        }
        private float LoopAround(float a)
        {
            return a <= 180 ? a : 360 - a;            
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                //if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                //{
                //    // the square root of H * -2 * G = how much velocity needed to reach desired height
                //    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                //    // update animator if using character
                //    if (_hasAnimator)
                //    {
                //        _animator.SetBool(_animIDJump, true);
                //    }
                //}

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump                
            }
            _input.jump = false;

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            //Debug.Log(animationEvent.animatorClipInfo.weight);
            //if (animationEvent.animatorClipInfo.weight >= 0.5f)
            //{
            //    if (FootstepAudioClips.Length > 0)
            //    {
            //        var index = Random.Range(0, FootstepAudioClips.Length);
            //        AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
            //    }
            //}
        }

        private void FootstepAudio()
        {
            var footstep = _animator.GetFloat("Footstep");
            footstep = Mathf.Abs(footstep) < 0.0001f ? 0 : footstep;

            if ((_lastFootstep > 0 && footstep < 0) || (_lastFootstep < 0 && footstep > 0))
            {
                var FootstepAudioClips = GetClipsForSurface();                
                var index = UnityEngine.Random.Range(0, FootstepAudioClips.Length - 1);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume * _input.move.magnitude);                
                //AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.position, FootstepAudioVolume);
            }            
            _lastFootstep = footstep;

        }

        private AudioClip[] GetClipsForSurface()
        {
            var isHit = Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 0.5f, GroundLayers);
            if (isHit)
            {                
                var surface = hit.collider.GetComponent<SurfaceDefinition>();
                if (surface)
                {                    
                    switch (surface.SurfaceType)
                    {
                        case SurfaceType.Wood: return WoodClips;
                        case SurfaceType.Tile: return TileClips;
                        case SurfaceType.Carpet: return CarpetClips;                        
                        case SurfaceType.Concrete: return ConcreteClips;                                                
                        case SurfaceType.Wet: return WetClips;
                        case SurfaceType.Fragile: return FragileClips;
                    }

                }
            }            
            return DefaultClips;
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        public void LockPlayer(bool state)
        {
            LockCameraPosition = state;      
            _input.canMove = !state;
        }
    }
}