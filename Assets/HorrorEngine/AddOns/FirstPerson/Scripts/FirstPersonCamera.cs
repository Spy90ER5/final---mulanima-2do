using System;
using UnityEngine;
using Cinemachine;

namespace HorrorEngine
{
    [Serializable]
    public class FirstPersonCameraConfig
    {
        public float BlendSpeed = 1f;
        public bool ChangeFOV;
        public float FOV;
        [Tooltip("Determines if the camera matches the socket rotation during the state")]
        public bool MatchAnimationLook;
        [Tooltip("Increase this value to make the camera match more closely the socket rotation during the state animation")]
        public float MatchStrength = 10f;
        [Tooltip("Determines if an offset is applied to the socket position during the state")]
        public bool ChangeOffset;
        [Tooltip("Socket offset in local space")]
        public Vector3 Offset;

        public float SensitivityScalar = 1f;
    }

    [Serializable]
    public class FirstPersonCameraStateEntry
    {
        public string StateTag;
        public FirstPersonCameraConfig Config;
    }

    [Serializable]
    public class FirstPersonCameraSensitivitySetup
    {
        public InputSchemeHandle InputScheme;
        public AnimationCurve SensitivityProgressionX;
        public AnimationCurve SensitivityProgressionY;
        public float RotationInterpolationSpeed = 1;
        public float Accumulation = 0.1f;
        public bool ApplyDeltaTime;
    }

    public class FirstPersonCamera : MonoBehaviour
    {
        [SerializeField] GameObject m_Rotator;
        [SerializeField] Transform m_AimingTarget;
        [SerializeField] SocketAttachment m_SocketAttachment;
        [SerializeField] FirstPersonCameraStateEntry[] m_StateCameraConfig;
        [SerializeField] FirstPersonCameraConfig m_DefaultConfig;
        [SerializeField] FirstPersonCameraSensitivitySetup[] m_SensitivitySetups;
        [SerializeField] float m_LookUpMaxAngle = 270f;
        [SerializeField] float m_LookDownMaxAngle = 70f;
        [SerializeField] private float m_UnpauseBlendTime = 1f;

        [Header("Settings")]
        [SerializeField] SettingsElementContent m_HorizontalSensitivitySetting;
        [SerializeField] SettingsElementContent m_VerticalSensitivitySetting;
        [SerializeField] SettingsElementContent m_InvertedYSetting;

        private GameObject m_RotatorInstance;
        private Transform m_RotatorTransform;
        private IPlayerInput m_Input;
        private Transform m_PlayerT;
        private Transform m_SocketT;
        private CinemachineVirtualCamera m_VirtualCam;
        private Transform m_VCamT;
        private FirstPersonCameraConfig m_CurrentConfig;
        private Vector2 m_Accumulated;
        private Quaternion m_DesiredRotation;
        private Vector3 m_Offset;
        private float m_UnpauseTime;
        
        private InputSchemeHandle m_CurrentScheme;
        private FirstPersonCameraSensitivitySetup m_CurrentSensitivity;
        
        // --------------------------------------------------------------------

        private void Awake()
        {
            m_CurrentConfig = m_DefaultConfig;
            m_CurrentSensitivity = m_SensitivitySetups[0];
            MessageBuffer<DoorTransitionMidWayPostRoutineMessage>.Subscribe(OnDoorTransition);
            MessageBuffer<GameUnpausedMessage>.Subscribe(OnGameUnpaused);
        }

        // --------------------------------------------------------------------

        private void OnGameUnpaused(GameUnpausedMessage msg)
        {
            if (m_RotatorTransform)
                m_RotatorTransform.rotation = m_DesiredRotation;

            m_UnpauseTime = Time.time;
        }

        // --------------------------------------------------------------------

        void OnDoorTransition(DoorTransitionMidWayPostRoutineMessage msg)
        {
            Recenter(true);
        }

        // --------------------------------------------------------------------

        private void Recenter(bool immediate)
        {
            m_DesiredRotation = m_PlayerT.rotation;
            
            if (immediate)
            {
                m_RotatorTransform.rotation = m_DesiredRotation;
            }
        }

        // --------------------------------------------------------------------

        void Start()
        {
            enabled = false;
        }

        // --------------------------------------------------------------------

        public void Update()
        {
            if (!m_RotatorTransform)
                return;

            if (m_Input.GetControlScheme() != m_CurrentScheme)
            {
                UpdateControlScheme();
            }

            if (PauseController.Instance.IsPaused)
                return;

            float unpauseBlending = GetUnpauseBlending();

            if (!m_CurrentConfig.MatchAnimationLook)
            {
                Vector2 look = m_Input.GetSecondaryAxis();
                look.x = Math.Sign(look.x) * m_CurrentSensitivity.SensitivityProgressionX.Evaluate(Math.Abs(look.x)) * unpauseBlending * m_CurrentConfig.SensitivityScalar;
                look.y = Math.Sign(look.y) * m_CurrentSensitivity.SensitivityProgressionY.Evaluate(Math.Abs(look.y)) * unpauseBlending * m_CurrentConfig.SensitivityScalar;

                if (m_HorizontalSensitivitySetting)
                    look.x *= m_HorizontalSensitivitySetting.GetAsFloat();

                if (m_VerticalSensitivitySetting)
                    look.y *= m_VerticalSensitivitySetting.GetAsFloat();

                if (m_InvertedYSetting)
                    look.y *= m_InvertedYSetting.GetAsBool() ? -1 : 1;

                float speedX = look.x * (m_CurrentSensitivity.ApplyDeltaTime ? Time.deltaTime : 1f) + m_Accumulated.x;
                float speedY = look.y * (m_CurrentSensitivity.ApplyDeltaTime ? Time.deltaTime : 1f) + m_Accumulated.y;

                m_DesiredRotation *= Quaternion.AngleAxis(speedX, Vector3.up);
                m_DesiredRotation *= Quaternion.AngleAxis(speedY, Vector3.right);

                var angles = m_DesiredRotation.eulerAngles;
                angles.z = 0;

                var angle = m_DesiredRotation.eulerAngles.x;

                if (angle > 180 && angle < m_LookUpMaxAngle)
                {
                    angles.x = m_LookUpMaxAngle;
                }
                else if (angle < 180 && angle > m_LookDownMaxAngle)
                {
                    angles.x = m_LookDownMaxAngle;
                }

                m_DesiredRotation.eulerAngles = angles;
                m_Accumulated = new Vector2(speedX, speedY) * m_CurrentSensitivity.Accumulation;

            }
            else
            {
                m_DesiredRotation = Quaternion.Slerp(m_DesiredRotation, m_SocketT.rotation, Time.deltaTime * m_CurrentConfig.MatchStrength);
            }
        }

        // --------------------------------------------------------------------

        private void FixedUpdate()
        {
            if (!m_RotatorTransform)
                return;

            if (PauseController.Instance.IsPaused)
                return;

            float unpauseBlending = GetUnpauseBlending();
            if (!m_CurrentConfig.MatchAnimationLook)
            {
                m_RotatorTransform.rotation = Quaternion.Slerp(m_RotatorTransform.rotation, m_DesiredRotation, Mathf.Clamp01(Time.deltaTime * m_CurrentSensitivity.RotationInterpolationSpeed) * unpauseBlending);
            }
            else
            {
                m_RotatorTransform.rotation = m_DesiredRotation;
            }

            TweenToConfig();
        }

        // --------------------------------------------------------------------

        private void LateUpdate()
        {
            if (!m_RotatorTransform)
                return;

            if (PauseController.Instance.IsPaused)
                return;

            float unpauseBlending = GetUnpauseBlending();
            if (unpauseBlending < 1f)
                m_RotatorTransform.position = Vector3.Lerp(m_RotatorTransform.position, m_SocketT.position + m_Offset, unpauseBlending);
            else
                m_RotatorTransform.position = m_SocketT.position + m_Offset;

            
        }

        // --------------------------------------------------------------------

        private float GetUnpauseBlending()
        {
            return m_UnpauseBlendTime > 0 ? Mathf.Clamp01((Time.time - m_UnpauseTime) / m_UnpauseBlendTime) : 1f;
        }

        // --------------------------------------------------------------------

        void UpdateControlScheme()
        {
            m_CurrentScheme = m_Input.GetControlScheme();

            foreach (var s in m_SensitivitySetups)
            {
                if (s.InputScheme == m_CurrentScheme)
                {
                    m_CurrentSensitivity = s;
                    break;
                }
            }
        }

        // --------------------------------------------------------------------

        void TweenToConfig()
        {
            if (m_CurrentConfig.ChangeFOV)
                m_VirtualCam.m_Lens.FieldOfView = Mathf.Lerp(m_VirtualCam.m_Lens.FieldOfView, m_CurrentConfig.FOV, Time.deltaTime * m_CurrentConfig.BlendSpeed);

            if (m_CurrentConfig.ChangeOffset)
            {
                Vector3 worldOffset = m_SocketT.TransformDirection(m_CurrentConfig.Offset);
                m_Offset = Vector3.Lerp(m_Offset, worldOffset, Time.deltaTime * m_CurrentConfig.BlendSpeed);
            }
        }

        // --------------------------------------------------------------------

        public void OnPlayerRegistered(PlayerActor player)
        {
            SetPlayer(player.transform);
        }

        // --------------------------------------------------------------------

        void SetPlayer(Transform playerT)
        {
            this.InvokeActionNextFrame(() => // Give one frame for all component to get activated
            {
                m_PlayerT = playerT;
                m_Input = playerT.GetComponentInChildren<IPlayerInput>();

                m_RotatorInstance = Instantiate(m_Rotator, transform.parent);
                m_RotatorTransform = m_RotatorInstance.transform;
                m_RotatorTransform.localPosition = Vector3.zero;
                m_RotatorTransform.rotation = m_PlayerT.rotation;
                m_DesiredRotation = m_PlayerT.rotation;

                PlayerMovementFirstPerson tpMovement = playerT.GetComponentInChildren<PlayerMovementFirstPerson>();
                tpMovement.SetRotationReference(m_RotatorTransform);

                PlayerVerticalAimingFromTransform tpAiming = playerT.GetComponentInChildren<PlayerVerticalAimingFromTransform>();
                tpAiming.SetTransform(m_AimingTarget);

                ActorStateController stateController = playerT.GetComponentInChildren<ActorStateController>();
                stateController.OnStateChanged.AddListener(OnStateChanged);

                m_VirtualCam = GetComponentInChildren<CinemachineVirtualCamera>();
                m_VCamT = m_VirtualCam.transform;
                m_VirtualCam.Follow = m_RotatorTransform;
                m_VirtualCam.LookAt = m_RotatorTransform.transform; // TODO - Maybe the child

                if (m_SocketAttachment.Socket)
                {
                    SocketController socketCtrl = playerT.GetComponentInChildren<SocketController>();
                    var socket = socketCtrl.GetSocket(m_SocketAttachment.Socket).transform;
                    if (socket)
                        m_SocketT = socket.transform;
                    else
                        Debug.LogWarning("First person camera couldn't find the expected socket");
                }

                InteractionDetector interactor = playerT.GetComponentInChildren<InteractionDetector>();
                interactor.transform.SetParent(m_VCamT);
                interactor.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                m_CurrentScheme = m_Input.GetControlScheme();

                enabled = true;

                m_UnpauseTime = Time.time;
            });
        }

        // --------------------------------------------------------------------

        private void OnDestroy()
        {
            Destroy(m_RotatorInstance);

            var stateController = m_PlayerT.GetComponentInChildren<ActorStateController>();
            stateController.OnStateChanged.RemoveListener(OnStateChanged);

            MessageBuffer<DoorTransitionMidWayPostRoutineMessage>.Unsubscribe(OnDoorTransition);
            MessageBuffer<GameUnpausedMessage>.Unsubscribe(OnGameUnpaused);
        }

        // --------------------------------------------------------------------

        void OnStateChanged(IActorState fromstate, IActorState toState)
        {
            SetConfigForState(toState);
        }

        // --------------------------------------------------------------------

        void SetConfigForState(IActorState state)
        {
            foreach (var entry in m_StateCameraConfig)
            {
                if (state.HasTag(entry.StateTag))
                {
                    m_CurrentConfig = entry.Config;
                    return;
                }
            }

            m_CurrentConfig = m_DefaultConfig;
        }

    }
}