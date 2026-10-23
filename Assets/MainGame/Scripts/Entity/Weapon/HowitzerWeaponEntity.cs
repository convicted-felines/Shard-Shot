using System.Collections;
using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 榴弹炮：正常双管齐射后，有概率在短时间内以随机偏角追加连发榴弹。
    /// </summary>
    public sealed class HowitzerWeaponEntity : RangedWeaponEntity
    {
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("每次开火后触发连发的概率")]
        private float m_BurstChance = 0.15f;

        [SerializeField]
        [Min(1)]
        [Tooltip("连发额外弹数下限（含）")]
        private int m_BurstExtraCountMin = 2;

        [SerializeField]
        [Min(1)]
        [Tooltip("连发额外弹数上限（含）")]
        private int m_BurstExtraCountMax = 4;

        [SerializeField]
        [Tooltip("连发各弹之间的间隔（秒）")]
        private float m_BurstInterval = 0.08f;

        [SerializeField]
        [Tooltip("连发单发水平偏角上限（度），实际偏移为 ±该值")]
        private float m_BurstSpreadAngle = 15f;

        private Coroutine m_BurstCoroutine;

        public override void FireBullet()
        {
            base.FireBullet();
            TryStartBurst();
        }

        protected override void OnUnequipped()
        {
            CancelBurst();
            base.OnUnequipped();
        }

        private void TryStartBurst()
        {
            if (m_BurstChance <= 0f || m_BurstExtraCountMax <= 0 || Random.value >= m_BurstChance)
            {
                return;
            }

            int minCount = Mathf.Max(1, m_BurstExtraCountMin);
            int maxCount = Mathf.Max(minCount, m_BurstExtraCountMax);
            int extraCount = Random.Range(minCount, maxCount + 1);

            CancelBurst();
            m_BurstCoroutine = StartCoroutine(BurstFireCoroutine(extraCount));
        }

        private IEnumerator BurstFireCoroutine(int extraCount)
        {
            for (int i = 0; i < extraCount; i++)
            {
                if (i > 0 && m_BurstInterval > 0f)
                {
                    yield return new WaitForSeconds(m_BurstInterval);
                }

                if (Owner == null || WeaponConfig == null)
                {
                    m_BurstCoroutine = null;
                    yield break;
                }

                FireBurstShot();
            }

            m_BurstCoroutine = null;
        }

        private void FireBurstShot()
        {
            Vector3 baseDirection = GetOwnerPlanarForward();
            if (m_BurstSpreadAngle > 0f)
            {
                float angle = Random.Range(-m_BurstSpreadAngle, m_BurstSpreadAngle);
                baseDirection = Quaternion.AngleAxis(angle, Vector3.up) * baseDirection;
            }

            FireFromMuzzle(baseDirection, PickRandomMuzzle());
        }

        private Transform PickRandomMuzzle()
        {
            Transform[] muzzles = Muzzles;
            if (muzzles == null || muzzles.Length == 0)
            {
                return null;
            }

            int validCount = 0;
            for (int i = 0; i < muzzles.Length; i++)
            {
                if (muzzles[i] != null)
                {
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                return null;
            }

            int pick = Random.Range(0, validCount);
            for (int i = 0; i < muzzles.Length; i++)
            {
                Transform muzzle = muzzles[i];
                if (muzzle == null)
                {
                    continue;
                }

                if (pick == 0)
                {
                    return muzzle;
                }

                pick--;
            }

            return null;
        }

        private void CancelBurst()
        {
            if (m_BurstCoroutine == null)
            {
                return;
            }

            StopCoroutine(m_BurstCoroutine);
            m_BurstCoroutine = null;
        }
    }
}
