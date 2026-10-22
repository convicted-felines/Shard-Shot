using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 俯视角角色刚体配置与平面移动辅助。
    /// </summary>
    public static class CharacterRigidbodyUtility
    {
        public static void SyncRigidbodyToTransform(Rigidbody rigidbody)
        {
            if (rigidbody == null)
            {
                return;
            }

            Transform transform = rigidbody.transform;
            rigidbody.position = transform.position;
            rigidbody.rotation = transform.rotation;
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }

        public static void SetupTopDownMovement(Rigidbody rigidbody)
        {
            if (rigidbody == null)
            {
                return;
            }

            SyncRigidbodyToTransform(rigidbody);
            rigidbody.isKinematic = false;
            rigidbody.useGravity = false;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public static void ApplyPlanarVelocity(Rigidbody rigidbody, Vector3 planarVelocity)
        {
            if (rigidbody == null)
            {
                return;
            }

            planarVelocity.y = 0f;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.velocity = planarVelocity;
        }

        public static void StopPlanarMovement(Rigidbody rigidbody)
        {
            if (rigidbody == null)
            {
                return;
            }

            rigidbody.angularVelocity = Vector3.zero;
            Vector3 velocity = rigidbody.velocity;
            velocity.x = 0f;
            velocity.y = 0f;
            velocity.z = 0f;
            rigidbody.velocity = velocity;
        }

        public static float GetPlanarSpeed(Rigidbody rigidbody)
        {
            if (rigidbody == null)
            {
                return 0f;
            }

            return new Vector3(rigidbody.velocity.x, 0f, rigidbody.velocity.z).magnitude;
        }
    }
}
