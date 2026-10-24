using System.Collections.Generic;
using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 角色受击闪红：通过 MaterialPropertyBlock 临时修改材质颜色，避免实例化材质。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterHitFlashComponent : MonoBehaviour
    {
        private const float FlashDuration = 0.12f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly Color FlashColor = new Color(1f, 0.15f, 0.15f, 1f);

        private struct Slot
        {
            public Renderer Renderer;
            public int MaterialIndex;
            public int ColorPropertyId;
            public Color OriginalColor;
        }

        private readonly List<Slot> m_Slots = new List<Slot>(16);
        private MaterialPropertyBlock m_PropertyBlock;
        private float m_RemainingSeconds;
        private bool m_IsFlashing;
        private bool m_SlotsCached;

        public static CharacterHitFlashComponent Ensure(GameObject owner)
        {
            if (owner == null)
            {
                return null;
            }

            CharacterHitFlashComponent flash = owner.GetComponent<CharacterHitFlashComponent>();
            if (flash == null)
            {
                flash = owner.AddComponent<CharacterHitFlashComponent>();
            }

            return flash;
        }

        public static CharacterHitFlashComponent Ensure(CharacterEntity owner)
        {
            return owner == null ? null : Ensure(owner.gameObject);
        }

        public void Flash()
        {
            EnsureSlotsCached();
            if (m_Slots.Count == 0)
            {
                return;
            }

            m_RemainingSeconds = FlashDuration;
            m_IsFlashing = true;
            UpdateFlashColors();
        }

        public void Clear()
        {
            if (m_IsFlashing)
            {
                RestoreOriginalColors();
            }

            m_IsFlashing = false;
            m_RemainingSeconds = 0f;
            m_Slots.Clear();
            m_SlotsCached = false;
        }

        private void Update()
        {
            if (!m_IsFlashing)
            {
                return;
            }

            m_RemainingSeconds -= Time.deltaTime;
            if (m_RemainingSeconds <= 0f)
            {
                RestoreOriginalColors();
                m_IsFlashing = false;
                return;
            }

            UpdateFlashColors();
        }

        private void UpdateFlashColors()
        {
            if (m_PropertyBlock == null)
            {
                m_PropertyBlock = new MaterialPropertyBlock();
            }

            float blend = m_RemainingSeconds / FlashDuration;
            for (int i = 0; i < m_Slots.Count; i++)
            {
                Slot slot = m_Slots[i];
                if (slot.Renderer == null)
                {
                    continue;
                }

                Color color = Color.Lerp(slot.OriginalColor, FlashColor, blend);
                slot.Renderer.GetPropertyBlock(m_PropertyBlock, slot.MaterialIndex);
                m_PropertyBlock.SetColor(slot.ColorPropertyId, color);
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
