using GameFramework;
using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 加特林：子弹命中敌人时叠射速，射速随时间逐渐回落到表内攻速。
    /// </summary>
    public sealed class GatlingWeaponEntity : RangedWeaponEntity
    {
        [SerializeField]
        [Tooltip("每次命中敌人增加的攻速，相对表内 AttackSpeed 的倍率（如 0.15 = +15% 表攻速）")]
        private float m_BoostPerHit = 0.15f;

        [SerializeField]
        [Tooltip("每秒回落的攻速，相对表内 AttackSpeed 的倍率（如 0.4 = 每秒 -40% 表攻速）")]
        private float m_DecayPerSecond = 0.4f;

        [SerializeField]
        [Tooltip("相对表内 AttackSpeed 的最大倍率")]
        private float m_MaxAttackSpeedMultiplier = 3f;

        [SerializeField]
        [Tooltip("水平偏角上限（度），叠满射速时单发最大偏移")]
        private float m_MaxSpreadAngle = 12f;

        [SerializeField]
        [Tooltip("相对表攻速达到该倍率后开始产生散布（如 1.2 = 叠到 120% 攻速起偏）")]
        private float m_SpreadStartSpeedRatio = 1.2f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("每枪口独立判定，发射铅弹（巨型弹）的概率")]
        private float m_LeadBulletChance = 0.08f;

        [SerializeField]
        [Tooltip("铅弹 Prefab 资源名（与 Weapon 目录下 Prefab 同名）")]
        private string m_LeadBulletAssetName = "加特林巨型子弹";

        [SerializeField]
        [Tooltip("铅弹伤害 = 表内 Damage × 该倍率")]
        private float m_LeadDamageMultiplier = 3f;

        [SerializeField]
        [Tooltip("铅弹命中定身时长（秒）")]
        private float m_LeadStunDuration = 1.5f;

        [SerializeField]
        [Tooltip("铅弹命中后扇形分裂的小弹 Prefab 资源名")]
        private string m_ScatterBulletAssetName = "加特林巨型子弹分裂子弹";

        [SerializeField]
        [Min(1)]
        [Tooltip("铅弹命中后分裂小弹数量")]
        private int m_ScatterCount = 6;

        [SerializeField]
        [Tooltip("分裂扇形总角度（度），以入射方向为中心")]
        private float m_ScatterArcDegrees = 90f;

        [SerializeField]
        [Tooltip("分裂小弹伤害 = 表内 Damage × 该倍率")]
        private float m_ScatterDamageMultiplier = 0.5f;

        [SerializeField]
        [Tooltip("分裂小弹命中定身时长（秒）")]
        private float m_ScatterStunDuration = 0.4f;

        [SerializeField]
        [Tooltip("普通子弹命中击退强度（沿子弹飞行方向的平面冲量）")]
        private float m_KnockbackForce = 4f;

        [SerializeField]
        [Tooltip("铅弹命中击退强度倍率（相对普通子弹）")]
        private float m_LeadKnockbackMultiplier = 2f;

        [SerializeField]
        [Tooltip("攻速加成飘字相对枪体的世界偏移")]
        private Vector3 m_AttackSpeedBoostUIOffset = new Vector3(0f, 0.35f, 0f);

        [SerializeField]
        [Tooltip("攻速加成飘字上浮时长（秒）")]
        private float m_AttackSpeedBoostFloatDuration = 0.9f;

        [SerializeField]
        [Tooltip("攻速加成飘字上浮距离（世界单位）")]
        private float m_AttackSpeedBoostFloatRiseDistance = 0.45f;

        private float m_CurrentAttackSpeed;

        public float GetEffectiveAttackSpeed()
        {
            return m_CurrentAttackSpeed;
        }

        public void OnEnemyHit()
        {
            if (WeaponConfig == null || m_BoostPerHit <= 0f)
            {
                return;
            }

            float baseSpeed = WeaponConfig.AttackSpeed;
            float maxSpeed = baseSpeed * m_MaxAttackSpeedMultiplier;
            float previousSpeed = m_CurrentAttackSpeed;
            if (previousSpeed >= maxSpeed)
            {
                m_CurrentAttackSpeed = maxSpeed;
                return;
            }

            m_CurrentAttackSpeed += baseSpeed * m_BoostPerHit;
            if (m_CurrentAttackSpeed > maxSpeed)
            {
                m_CurrentAttackSpeed = maxSpeed;
            }

            int displayBoost = Mathf.RoundToInt(m_CurrentAttackSpeed - previousSpeed);
            if (displayBoost <= 0)
            {
                return;
            }

            ShowAttackSpeedBoostWorldUI(displayBoost);
        }

        private void ShowAttackSpeedBoostWorldUI(int boostAmount)
        {
            GameEntry.Entity.ShowWorldUIFloatingTextAttached(
                Id,
                m_AttackSpeedBoostUIOffset,
                Utility.Text.Format("攻速+{0}", boostAmount),
                WorldUIEnum.普通字体蓝灰,
                m_AttackSpeedBoostFloatDuration,
                m_AttackSpeedBoostFloatRiseDistance);
        }

        public void TickDecay(float elapseSeconds)
        {
            if (WeaponConfig == null || elapseSeconds <= 0f || m_DecayPerSecond <= 0f)
            {
                return;
            }

            float baseSpeed = WeaponConfig.AttackSpeed;
            if (m_CurrentAttackSpeed <= baseSpeed)
            {
                m_CurrentAttackSpeed = baseSpeed;
                return;
            }

            m_CurrentAttackSpeed -= baseSpeed * m_DecayPerSecond * elapseSeconds;
            if (m_CurrentAttackSpeed < baseSpeed)
            {
                m_CurrentAttackSpeed = baseSpeed;
            }
        }

        public void ResetAttackSpeed()
        {
            m_CurrentAttackSpeed = WeaponConfig != null ? WeaponConfig.AttackSpeed : 0f;
        }

        protected override void OnBulletSpawned(BulletData data, Transform muzzle)
        {
            if (data == null)
            {
                return;
            }

            if (muzzle != null)
            {
                string muzzlePath = TransformUtility.GetRelativeTransformPath(CachedTransform, muzzle);
                if (!string.IsNullOrEmpty(muzzlePath))
                {
                    GameEntry.Juice?.PlayEffectAttachedToTransform(
                        EffectEnum.加特林开火, Id, muzzlePath, data.Rotation);
                    return;
                }
            }

            GameEntry.Juice?.PlayEffect(EffectEnum.加特林开火, data.Position, data.Rotation);
        }

        protected override BulletData CreateBulletData(Vector3 direction, Quaternion rotation, Transform muzzle)
        {
            if (ShouldSpawnLeadBullet())
            {
                Vector3 spawnPosition = GetBulletSpawnPosition(direction, muzzle);
                BulletData leadData = BulletData.Create(
                    m_LeadBulletAssetName,
                    Owner.Id,
                    spawnPosition,
                    rotation,
                    direction,
                    WeaponConfig.Damage * m_LeadDamageMultiplier,
                    WeaponConfig.BulletSpeed,
                    WeaponConfig.DamageCountdown,
                    m_LeadStunDuration,
                    BulletKind.Lead);
                ConfigureLeadScatter(leadData);
                leadData.KnockbackForce = m_KnockbackForce * m_LeadKnockbackMultiplier;
                ApplyGatlingHitEffect(leadData);
                return leadData;
            }

            BulletData data = base.CreateBulletData(direction, rotation, muzzle);
            if (data != null)
            {
                data.KnockbackForce = m_KnockbackForce;
                ApplyGatlingHitEffect(data);
            }

            return data;
        }

        private static void ApplyGatlingHitEffect(BulletData data)
        {
            if (data != null)
            {
                data.HitEffect = EffectEnum.加特林击中;
                data.GatlingAttackSpeedBoost = true;
            }
        }

        private bool ShouldSpawnLeadBullet()
        {
            return m_LeadBulletChance > 0f
                && !string.IsNullOrEmpty(m_LeadBulletAssetName)
                && Random.value < m_LeadBulletChance;
        }

        private void ConfigureLeadScatter(BulletData leadData)
        {
            if (leadData == null || WeaponConfig == null)
            {
                return;
            }

            leadData.ScatterBulletAssetName = m_ScatterBulletAssetName;
            leadData.ScatterCount = m_ScatterCount;
            leadData.ScatterArcDegrees = m_ScatterArcDegrees;
            leadData.ScatterDamage = WeaponConfig.Damage * m_ScatterDamageMultiplier;
            leadData.ScatterStunDuration = m_ScatterStunDuration;
        }

        protected override Vector3 ApplySpawnDirectionVariation(Vector3 baseDirection)
        {
            if (WeaponConfig == null || m_MaxSpreadAngle <= 0f)
            {
                return baseDirection;
            }

            float baseSpeed = WeaponConfig.AttackSpeed;
            if (baseSpeed <= 0f)
            {
                return baseDirection;
            }

            float ratio = m_CurrentAttackSpeed / baseSpeed;
            float spreadProb = Mathf.InverseLerp(m_SpreadStartSpeedRatio, m_MaxAttackSpeedMultiplier, ratio);
            if (spreadProb <= 0f || Random.value > spreadProb)
            {
                return baseDirection;
            }

            float angle = Random.Range(-m_MaxSpreadAngle, m_MaxSpreadAngle);
            return Quaternion.AngleAxis(angle, Vector3.up) * baseDirection;
        }

        protected override void OnEquipped()
        {
            base.OnEquipped();
            ResetAttackSpeed();
        }
    }
}
