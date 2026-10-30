using UnityEngine;

namespace HorrorEngine
{
    public class PlayerMovementFirstPerson : MonoBehaviour, IPlayerMovementSettings
    {
        [SerializeField] float m_MinInputMovementThreshold = 0.5f;

        private Transform m_RotationRef;

        public void SetRotationReference(Transform refT)
        {
            m_RotationRef = refT;
        }

        public PlayerMovementType GetMovementType() { return PlayerMovementType.Alternate; }

        public float GetFwdRate(PlayerMovement movement)
        {
            if (Mathf.Abs(movement.InputAxis.y) < m_MinInputMovementThreshold)
                return 0f;

            return movement.InputAxis.y;
        }

        public float GetRightRate(PlayerMovement movement)
        {
            if (Mathf.Abs(movement.InputAxis.x) < m_MinInputMovementThreshold)
                return 0f;

            return movement.InputAxis.x;
        }

        public void GetRotation(PlayerMovement movement, out float sign, out float rate)
        {
            if (m_RotationRef)
            {
                float signedAngle = Vector2.SignedAngle(m_RotationRef.forward.ToXZ(), transform.forward.ToXZ());
                sign = Mathf.Sign(signedAngle);
                float unsignedAngle = Mathf.Abs(signedAngle);
                rate = unsignedAngle / Time.deltaTime;
            }
            else
            {
                sign = 1;
                rate = 0;
            }
        }

        public void ResetMovement()
        {
            // Do Nothing
        }
    }
}