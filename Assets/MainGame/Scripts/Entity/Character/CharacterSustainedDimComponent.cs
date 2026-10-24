using System.Collections.Generic;
using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 持续压暗角色渲染（MaterialPropertyBlock），用于击飞/起身等阶段。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterSustainedDimComponent : MonoBehaviour
    {
        public const float DefaultDimmedMultiplier = 0.6f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

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
        private bool m_IsActive;
        private bool m_SlotsCached;
        private float m_DimmedMultiplier = DefaultDimmedMultiplier;

        public bool IsActive => m_IsActive;

        public static CharacterSustainedDimComponent Ensure(GameObject owner)
        {
            if (owner == null)
            {
                return null;
            }

            CharacterSustainedDimComponent dim = owner.GetComponent<CharacterSustainedDimComponent>();
            if (dim == null)
            {
                dim = owner.AddComponent<CharacterSustainedDimComponent>();
            }

            return dim;
        }

        public void ApplyDim(float dimmedMultiplier = DefaultDimmedMultiplier)
        {
            m_DimmedMultiplier = dimmedMultiplier;
            EnsureSlotsCached();
            if (m_Slots.Count == 0)
            {
                return;
            }

            m_IsActive = true;
            ApplyDimColors();
        }

        public void Clear()
        {
            if (m_IsActive)
            {
                RestoreOriginalColors();
            }

            m_IsActive = false;
            m_Slots.Clear();
            m_SlotsCached = false;
        }

        private void ApplyDimColors()
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

                Color color = slot.OriginalColor * m_DimmedMultiplier;
                slot.Renderer.GetPropertyBlock(m_PropertyBlock, slot.MaterialIndex);
                m_PropertyBlock.SetColor(slot.ColorPropertyId, color);
                if (slot.HasEmission)
                {
                    m_PropertyBlock.SetColor(EmissionColorId, Color.black);
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
