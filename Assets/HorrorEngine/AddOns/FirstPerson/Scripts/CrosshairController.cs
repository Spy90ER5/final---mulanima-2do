using System;
using UnityEngine;

namespace HorrorEngine
{
    [Serializable]
    public class CrosshairStateConfig
    {
        public string StateTag;
        public bool Visible;
        public string AnimTrigger;
    }

    public class CrosshairController : MonoBehaviour
    {
        private static readonly int k_VisibleHash = Animator.StringToHash("Visible");

        [SerializeField] GameObject m_Crosshair;
        [SerializeField] CrosshairStateConfig[] m_CrosshairStateConfig;
        [SerializeField] CrosshairStateConfig m_DefaultCrosshairStateConfig;

        private PlayerActor m_Player;
        private GameObject m_CrosshairInstance;
        private Animator m_CrosshairAnimator;

        private void Awake()
        {
            MessageBuffer<GamePausedMessage>.Subscribe(OnGamePaused);
            MessageBuffer<GameUnpausedMessage>.Subscribe(OnGameUnpaused);
        }
        

        private void OnGamePaused(GamePausedMessage msg) 
        {
            m_CrosshairInstance.SetActive(false);
        }

        private void OnGameUnpaused(GameUnpausedMessage msg) 
        {
            m_CrosshairInstance.SetActive(true);
        }

        public void OnPlayerRegistered(PlayerActor player)
        {
            m_Player = player;

            ActorStateController stateController = m_Player.GetComponentInChildren<ActorStateController>();
            stateController.OnStateChanged.AddListener(OnStateChanged);

            m_CrosshairInstance = Instantiate(m_Crosshair, transform.parent);
            m_CrosshairAnimator = m_CrosshairInstance.GetComponent<Animator>();
        }

        private void OnDestroy()
        {
            ActorStateController stateController = m_Player.GetComponentInChildren<ActorStateController>();
            stateController.OnStateChanged.RemoveListener(OnStateChanged);

            MessageBuffer<GamePausedMessage>.Unsubscribe(OnGamePaused);
            MessageBuffer<GameUnpausedMessage>.Unsubscribe(OnGameUnpaused);
        }

        void OnStateChanged(IActorState fromstate, IActorState toState)
        {
            SetCrosshairForState(toState);
        }

        void SetCrosshairForState(IActorState state)
        {

            foreach (var entry in m_CrosshairStateConfig)
            {
                if (state.HasTag(entry.StateTag))
                {
                    SetCrosshairConfig(entry);
                    return;
                }
            }

            SetCrosshairConfig(m_DefaultCrosshairStateConfig);
        }

        void SetCrosshairConfig(CrosshairStateConfig config)
        {
            m_CrosshairAnimator.SetBool(k_VisibleHash, config.Visible);
            if (!string.IsNullOrEmpty(config.AnimTrigger))
                m_CrosshairAnimator.SetTrigger(config.AnimTrigger);
        }

    }
}