using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace HorrorEngine
{
    public class ThreeItemDoor : DoorLock
    {
        [SerializeField] private DialogData m_OnUnlockedDialog;
        [SerializeField] private AudioClip m_OnUnlockedSound;
        [SerializeField] private DialogData m_OnLockedOtherSideDialog;

        [Tooltip("Sonido que se reproduce cuando se encuentra una llave, siempre y cuando esa llave no complete el set (si completa las tres, solo suena el sonido de desbloqueo).")]
        [SerializeField] private AudioClip m_OnKeyFoundSound;

        [Space]
        [SerializeField] private ItemData m_KeyA;
        [Tooltip("Objeto de la jerarquía (normalmente desactivado) que se activa cuando se consigue la llave A.")]
        [SerializeField] private GameObject m_KeyAIndicator;
        [HideInInspector][SerializeField] private bool m_KeyACollected;

        [Space]
        [SerializeField] private ItemData m_KeyB;
        [Tooltip("Objeto de la jerarquía (normalmente desactivado) que se activa cuando se consigue la llave B.")]
        [SerializeField] private GameObject m_KeyBIndicator;
        [HideInInspector][SerializeField] private bool m_KeyBCollected;

        [Space]
        [SerializeField] private ItemData m_KeyC;
        [Tooltip("Objeto de la jerarquía (normalmente desactivado) que se activa cuando se consigue la llave C.")]
        [SerializeField] private GameObject m_KeyCIndicator;
        [HideInInspector][SerializeField] private bool m_KeyCCollected;

        [Space]
        [SerializeField] private bool m_ConsumesItem = true;
        [SerializeField] private bool m_UseObjectAutomatically = true;
        [SerializeField] private bool m_UseKeyBothSides = true;

        [HideInInspector]
        [FormerlySerializedAs("m_LockedOtherSideDialog")]
        [SerializeField] private string[] m_LockedOtherSideDialog_DEPRECATED = { "The door is locked from the other side" };
        [HideInInspector]
        [FormerlySerializedAs("m_OnUnlockDialog")]
        [SerializeField] private string[] m_OnUnlockDialog_DEPRECATED = { "You unlocked the door with the key" };

        protected override void Awake()
        {
            base.Awake();

            Debug.Assert(m_KeyA, "ThreeItemDoor requires key A to work", gameObject);
            Debug.Assert(m_KeyB, "ThreeItemDoor requires key B to work", gameObject);
            Debug.Assert(m_KeyC, "ThreeItemDoor requires key C to work", gameObject);
        }

        public override void OnTryToUnlock(out bool openImmediately)
        {
            if (!TryUnlock())
            {
                if (m_LockedSound)
                    AudioSource.PlayOneShot(m_LockedSound);

                if (m_OnLockedDialog.IsValid())
                    UIManager.Get<UIDialog>().Show(m_OnLockedDialog);
            }

            openImmediately = false;
        }

        public override void OnTryToUnlockFromExit(out bool openImmediately)
        {
            openImmediately = false;

            if (!m_UseKeyBothSides)
            {
                if (m_LockedSound)
                    AudioSource.PlayOneShot(m_LockedSound);

                if (m_OnLockedOtherSideDialog.IsValid())
                    UIManager.Get<UIDialog>().Show(m_OnLockedOtherSideDialog);
                return;
            }

            if (!TryUnlock() && m_OnLockedDialog.IsValid())
                UIManager.Get<UIDialog>().Show(m_OnLockedDialog);
        }

        public bool TryUnlock()
        {
            if (!m_UseObjectAutomatically)
                return false;

            // Revisa cada llave todavía no recolectada; si está en el inventario
            // la marca como conseguida y prende su indicador. Pasa aunque falten
            // las otras dos, para dar feedback de progreso.
            bool foundAny = false;
            foundAny |= CheckKey(m_KeyA, m_KeyAIndicator, ref m_KeyACollected);
            foundAny |= CheckKey(m_KeyB, m_KeyBIndicator, ref m_KeyBCollected);
            foundAny |= CheckKey(m_KeyC, m_KeyCIndicator, ref m_KeyCCollected);

            if (!AllKeysCollected())
            {
                // Si se encontró alguna llave nueva pero todavía no se completó el
                // set, se reproduce el sonido de progreso. Si ya se completaron las
                // tres en este mismo llamado, no suena acá: solo suena el de desbloqueo.
                if (foundAny && m_OnKeyFoundSound)
                    AudioSource.PlayOneShot(m_OnKeyFoundSound);

                return false;
            }

            if (m_UseKeyBothSides)
            {
                // TODO - Support CrossSceneDoors
                SceneDoor sceneDoor = Door as SceneDoor;
                if (sceneDoor)
                {
                    // Si el otro lado tiene las mismas tres llaves, se desbloquea también
                    var exitLock = sceneDoor.Exit.GetComponent<ThreeItemDoor>();
                    if (exitLock && exitLock.IsLocked && exitLock.SharesKeysWith(this))
                    {
                        exitLock.MarkAllKeysCollected();
                        exitLock.IsLocked = false;
                        exitLock.OnUnlock?.Invoke();
                    }
                }
            }

            IsLocked = false;
            OnUnlock?.Invoke();

            if (m_OnUnlockedSound)
                AudioSource.PlayOneShot(m_OnUnlockedSound);

            if (m_OnUnlockedDialog.IsValid())
                UIManager.Get<UIDialog>().Show(m_OnUnlockedDialog);

            return true;
        }

        private bool CheckKey(ItemData key, GameObject indicator, ref bool collected)
        {
            if (collected || !key)
                return false;

            if (GameManager.Instance.Inventory.Contains(key))
            {
                collected = true;

                if (indicator)
                    indicator.SetActive(true);

                if (m_ConsumesItem)
                    GameManager.Instance.Inventory.Remove(key);

                return true;
            }

            return false;
        }

        private bool AllKeysCollected()
        {
            return m_KeyACollected && m_KeyBCollected && m_KeyCCollected;
        }

        private void MarkAllKeysCollected()
        {
            m_KeyACollected = true;
            m_KeyBCollected = true;
            m_KeyCCollected = true;

            if (m_KeyAIndicator) m_KeyAIndicator.SetActive(true);
            if (m_KeyBIndicator) m_KeyBIndicator.SetActive(true);
            if (m_KeyCIndicator) m_KeyCIndicator.SetActive(true);
        }

        private bool SharesKeysWith(ThreeItemDoor other)
        {
            return m_KeyA == other.m_KeyA && m_KeyB == other.m_KeyB && m_KeyC == other.m_KeyC;
        }
    }
}
