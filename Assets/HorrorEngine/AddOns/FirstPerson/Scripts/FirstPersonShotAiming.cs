using UnityEngine;

namespace HorrorEngine
{
    public class FirstPersonShotAiming : MonoBehaviour, IShotAttackAiming
    {
        [SerializeField] private LayerMask m_LayerMask;

        public Vector3 GetDirection(Vector3 origin, float maxDistance)
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0.0f));
            Vector3 direction = ray.direction.normalized;
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, maxDistance, m_LayerMask, QueryTriggerInteraction.Collide))
            {
                direction = (hitInfo.point - origin).normalized;
            }
            return direction;
        }
    }
}