using System.Collections.Generic;
using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 起身后无敌闪白：随无敌剩余时间缩短，闪烁频率逐渐加快。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterInvincibilityFlashComponent : MonoBehaviour
    {
        private const float MinBlinkFrequency = 2.5f;
        private const float MaxBlinkFrequency = 12f;
        private const float DimmedMultiplier = 0.45f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly Color FlashColor = Color.white;

        private struct Slot
        {
            public Renderer Renderer;
            public int MaterialIndex;
            public int ColorPropertyId;
            public bool HasEmission;
            public Color OriginalColor;
        }

        private readonly List<Slot> m_Slots = new List<Slot>(16);
        private MaterialPropertyBlock m_PropertyBlock;
        private CharacterKnockdownComponent m_Knockdown;
        private float m_BlinkPhase;
        private bool m_SlotsCached;
        private bool m_WasInvincible;

        public static CharacterInvincibilityFlashComponent Ensure(GameObject owner)
        {
            if (owner == null)
            {
                return null;
            }

            CharacterInvincibilityFlashComponent flash = owner.GetComponent<CharacterInvincibilityFlashComponent>();
            if (flash == null)
            {
                flash = owner.AddComponent<CharacterInvincibilityFlashComponent>();
            }

            return flash;
        }

        public static CharacterInvincibilityFlashComponent Ensure(CharacterEntity owner)
        {
            return owner == null ? null : Ensure(owner.gameObject);
        }

        public void Clear()
        {
            RestoreOriginalColors();
            m_BlinkPhase = 0f;
            m_WasInvincible = false;
            m_Slots.Clear();
            m_SlotsCached = false;
        }

        public void OnPostGetUpInvincibilityBegan()
        {
            m_BlinkPhase = 0f;
            m_WasInvincible = true;
            m_Slots.Clear();
            m_SlotsCached = false;
            EnsureSlotsCached();
            ApplyColors(true);
        }

        public void Tick(float elapseSeconds)
        {
            if (m_Knockdown == null)
            {
                m_Knockdown = GetComponent<CharacterKnockdownComponent>();
            }

            if (m_Knockdown == null || !m_Knockdown.IsInPostGetUpInvincibility)
            {
                if (m_WasInvincible)
                {
                    Clear();
                }

                return;
            }

            if (!m_WasInvincible)
            {
                OnPostGetUpInvincibilityBegan();
            }

            TickFlash(elapseSeconds);
        }

        private void TickFlash(float elapseSeconds)
        {
            if (m_Slots.Count == 0)
            {
                return;
            }

            float remaining = m_Knockdown.PostGetUpInvincibleRemaining;
            float total = m_Knockdown.PostGetUpInvincibleTotal;
            if (remaining <= 0f || total <= 0f)
            {
                Clear();
                return;
            }

            float elapsedRatio = 1f - remaining / total;
            float frequency = Mathf.Lerp(MinBlinkFrequency, MaxBlinkFrequency, elapsedRatio);
            m_BlinkPhase += elapseSeconds * frequency;
            bool showFlash = (Mathf.FloorToInt(m_BlinkPhase) % 2) == 0;
            ApplyColors(showFlash);
        }

        private void ApplyColors(bool showFlash)
        {
            if (m_PropertyBlock == null)
            {
                m_PropertyBlock = new MaterialPropertyBlock();
            }

            for (int i = 0; i < m_Slots.Count; i++)
            {
                Slot slot = m_Slots[i];
                if (slot.Renderer == null)
                {
                    continue;
                }

                Color color = showFlash
                    ? FlashColor
                    : slot.OriginalColor * DimmedMultiplier;
                slot.Renderer.GetPropertyBlock(m_PropertyBlock, slot.MaterialIndex);
                m_PropertyBlock.SetColor(slot.ColorPropertyId, color);
                if (slot.HasEmission)
                {
                    m_PropertyBlock.SetColor(
                        EmissionColorId,
                        showFlash ? FlashColor * 1.5f : Color.black);
                }

                slot.Renderer.SetPropertyBlock(m_PropertyBlock, slot.MaterialIndex);
            }
        }

        private void RestoreOriginalColors()
        {
            for (int i = 0; i < m_Slots.Count; i++)
            {
                Slot slot = m_Slots[i];
                if (slot.Renderer == null)
                {
                    continue;
                }

                slot.Renderer.SetPropertyBlock(null, slot.MaterialIndex);
            }
        }

        private void EnsureSlotsCached()
        {
            if (m_SlotsCached)
            {
                return;
            }

            m_Slots.Clear();
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                if (renderer == null)
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material == null)
                    {
                        continue;
                    }

                    int colorPropertyId = ResolveColorPropertyId(material);
                    if (colorPropertyId < 0)
                    {
                        continue;
                    }

                    m_Slots.Add(new Slot
                    {
                        Renderer = renderer,
                        MaterialIndex = materialIndex,
                        ColorPropertyId = colorPropertyId,
                        HasEmission = material.HasProperty(EmissionColorId),
                        OriginalColor = material.GetColor(colorPropertyId),
                    });
                }
            }

            m_SlotsCached = true;
        }

        private static int ResolveColorPropertyId(Material material)
        {
            if (material.HasProperty(BaseColorId))
            {
                return BaseColorId;
            }

            if (material.HasProperty(ColorId))
            {
                return ColorId;
            }

            return -1;
        }
    }
}
