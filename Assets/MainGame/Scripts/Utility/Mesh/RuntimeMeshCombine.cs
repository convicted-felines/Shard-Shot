using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MainGame
{
    [System.Serializable]
    public struct SpecialDuplicateStepSettings
    {
        public Vector3 translateIncrement;
        public Vector3 rotateIncrementEuler;
        public Vector3 scaleIncrement;
        public bool useLocalSpace;
        public bool applyScaleIncrement;

        public static SpecialDuplicateStepSettings Default => new SpecialDuplicateStepSettings
        {
            scaleIncrement = Vector3.one,
            useLocalSpace = true,
        };
    }

    /// <summary>
    /// Maya「特殊复制」运行时/编辑器共用的位移、旋转增量逻辑。
    /// </summary>
    public static class SpecialDuplicateUtility
    {
        public static Vector3 ApplyTranslateOffset(Transform basis, Vector3 translateStep, bool useLocalSpace)
        {
            if (translateStep == Vector3.zero)
            {
                return basis.position;
            }

            Vector3 worldOffset = useLocalSpace
                ? basis.TransformVector(translateStep)
                : translateStep;

            return basis.position + worldOffset;
        }

        public static Quaternion ApplyRotateOffset(Quaternion basisRotation, Vector3 rotateStepEuler, bool localSpace)
        {
            if (rotateStepEuler == Vector3.zero)
            {
                return basisRotation;
            }

            Quaternion delta = Quaternion.Euler(rotateStepEuler);
            return localSpace ? basisRotation * delta : delta * basisRotation;
        }

        public static Transform InstantiateAtStep(
            Transform duplicateFrom,
            Transform basis,
            Transform parent,
            int stepMultiplier,
            SpecialDuplicateStepSettings settings)
        {
            GameObject copy = Object.Instantiate(duplicateFrom.gameObject, parent);
            copy.name = duplicateFrom.gameObject.name;
            ApplyTransformToDuplicate(copy.transform, basis, duplicateFrom, stepMultiplier, settings);
            return copy.transform;
        }

        public static void ApplyTransformToDuplicate(
            Transform copyTransform,
            Transform basis,
            Transform duplicateFrom,
            int stepMultiplier,
            SpecialDuplicateStepSettings settings)
        {
            Vector3 translateStep = settings.translateIncrement * stepMultiplier;
            Vector3 rotateStep = settings.rotateIncrementEuler * stepMultiplier;

            Vector3 worldPosition = ApplyTranslateOffset(basis, translateStep, settings.useLocalSpace);
            Quaternion worldRotation = ApplyRotateOffset(basis.rotation, rotateStep, settings.useLocalSpace);

            copyTransform.SetPositionAndRotation(worldPosition, worldRotation);

            if (settings.applyScaleIncrement)
            {
                copyTransform.localScale = PowPerComponent(basis.localScale, settings.scaleIncrement, stepMultiplier);
            }
            else
            {
                copyTransform.localScale = duplicateFrom.localScale;
            }
        }

        static Vector3 PowPerComponent(Vector3 baseScale, Vector3 multiplierPerStep, int steps)
        {
            return new Vector3(
                baseScale.x * Mathf.Pow(multiplierPerStep.x, steps),
                baseScale.y * Mathf.Pow(multiplierPerStep.y, steps),
                baseScale.z * Mathf.Pow(multiplierPerStep.z, steps));
        }
    }

    /// <summary>
    /// 运行时按 Maya 特殊复制规则生成子物体，再按材质分组合并子 Mesh 以减少 Draw Call。
    /// 同材质的 SubMesh 会合成一个 SubMesh；最终材质槽数量等于独立材质种类数。
    /// 在 Start 时执行一次，合并成功后销毁本组件；实体 Show/Hide 无需参与该流程。
    /// </summary>
    [DisallowMultipleComponent]
    public class RuntimeMeshCombine : MonoBehaviour
    {
        [Header("合并")]
        [SerializeField]
        [Tooltip("是否在 Start 时自动执行复制与合并。")]
        private bool m_CombineOnStart = true;

        [SerializeField]
        [Tooltip("是否包含未激活的子物体。")]
        private bool m_IncludeInactiveChildren = false;

        [SerializeField]
        [Tooltip("合并后是否销毁已参与合并的子物体。关闭则仅禁用其 MeshRenderer。")]
        private bool m_DestroySourceObjects = true;

        [SerializeField]
        [Tooltip("复制与合并完成前隐藏子物体 Renderer，避免逐帧出现半成品。")]
        private bool m_HideChildrenUntilCombined = true;

        [Header("运行时特殊复制")]
        [SerializeField]
        [Tooltip("合并前先按增量复制源子物体（类似 Maya 批量特殊复制）。")]
        private bool m_EnableSpecialDuplicate;

        [SerializeField]
        [Tooltip("作为复制模板的子物体；复制结果挂在当前物体下。")]
        private Transform m_DuplicateSource;

        [SerializeField]
        [Tooltip("每步位移增量（批量时 step 从 1 累乘到复制数量）。")]
        private Vector3 m_TranslateIncrement;

        [SerializeField]
        [Tooltip("每步旋转增量（欧拉角，度）。")]
        private Vector3 m_RotateIncrementEuler;

        [SerializeField]
        [Tooltip("位移/旋转增量是否相对源物体局部空间。")]
        private bool m_UseLocalSpace = true;

        [SerializeField]
        [Tooltip("额外复制的份数（不含场景中已有的源物体）。")]
        private int m_DuplicateCount = 47;

        [SerializeField]
        [Tooltip("每帧最多创建多少份复制体，避免单帧卡顿。")]
        private int m_CopiesPerFrame = 4;

        [SerializeField]
        [Tooltip("是否对复制体应用缩放增量。")]
        private bool m_ApplyScaleIncrement;

        [SerializeField]
        private Vector3 m_ScaleIncrement = Vector3.one;

        private bool m_HasCombined;
        private Mesh m_CombinedMesh;
        private Coroutine m_ProcessCoroutine;

        private void Start()
        {
            if (!m_CombineOnStart || m_HasCombined || m_ProcessCoroutine != null)
            {
                return;
            }

            m_ProcessCoroutine = StartCoroutine(ProcessOnStart());
        }

        private void OnDestroy()
        {
            if (m_ProcessCoroutine != null)
            {
                StopCoroutine(m_ProcessCoroutine);
                m_ProcessCoroutine = null;
            }

            if (m_CombinedMesh == null)
            {
                return;
            }

            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh != m_CombinedMesh)
            {
                Destroy(m_CombinedMesh);
            }

            m_CombinedMesh = null;
        }

        [ContextMenu("Run Duplicate And Combine")]
        public void RunDuplicateAndCombine()
        {
            if (m_HasCombined)
            {
                return;
            }

            if (m_ProcessCoroutine != null)
            {
                StopCoroutine(m_ProcessCoroutine);
            }

            m_ProcessCoroutine = StartCoroutine(ProcessOnStart());
        }

        [ContextMenu("Combine Child Meshes")]
        public bool CombineChildMeshes()
        {
            if (m_HasCombined)
            {
                return true;
            }

            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(m_IncludeInactiveChildren);
            var sources = new List<MeshSource>(meshFilters.Length);

            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter meshFilter = meshFilters[i];
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    continue;
                }

                if (meshFilter.transform == transform)
                {
                    continue;
                }

                MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    continue;
                }

                sources.Add(new MeshSource(meshFilter, meshRenderer));
            }

            if (sources.Count == 0)
            {
                Debug.LogWarning($"[{nameof(RuntimeMeshCombine)}] 未在 '{name}' 下找到可合并的子 Mesh。", this);
                return false;
            }

            Matrix4x4 rootWorldToLocal = transform.worldToLocalMatrix;
            List<MaterialCombineGroup> materialGroups = BuildMaterialCombineGroups(sources, rootWorldToLocal);

            if (materialGroups.Count == 0)
            {
                Debug.LogError(
                    $"[{nameof(RuntimeMeshCombine)}] '{name}' 没有可合并的 Mesh。" +
                    "请在 FBX 导入设置中勾选 Read/Write Enabled，或确认子物体含 MeshFilter。",
                    this);
                RestoreChildVisibilityAfterFailedCombine();
                return false;
            }

            var intermediateMeshes = new List<Mesh>(materialGroups.Count);
            try
            {
                m_CombinedMesh = CombineByMaterialGroups(materialGroups, intermediateMeshes);
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"[{nameof(RuntimeMeshCombine)}] '{name}' 合并失败: {exception.Message}\n" +
                    "请在对应 FBX 的 Model 导入设置中勾选 Read/Write Enabled。",
                    this);
                DestroyMesh(m_CombinedMesh);
                m_CombinedMesh = null;
                DestroyMeshes(intermediateMeshes);
                RestoreChildVisibilityAfterFailedCombine();
                return false;
            }

            DestroyMeshes(intermediateMeshes);

            if (m_CombinedMesh == null || m_CombinedMesh.vertexCount == 0)
            {
                Debug.LogError($"[{nameof(RuntimeMeshCombine)}] '{name}' 合并结果为空。", this);
                DestroyMesh(m_CombinedMesh);
                m_CombinedMesh = null;
                RestoreChildVisibilityAfterFailedCombine();
                return false;
            }

            m_CombinedMesh.RecalculateBounds();
            m_CombinedMesh.RecalculateNormals();

            MeshFilter targetFilter = GetComponent<MeshFilter>();
            if (targetFilter == null)
            {
                targetFilter = gameObject.AddComponent<MeshFilter>();
            }

            MeshRenderer targetRenderer = GetComponent<MeshRenderer>();
            if (targetRenderer == null)
            {
                targetRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            var uniqueMaterials = new Material[materialGroups.Count];
            for (int i = 0; i < materialGroups.Count; i++)
            {
                uniqueMaterials[i] = materialGroups[i].Material;
            }

            targetFilter.sharedMesh = m_CombinedMesh;
            targetRenderer.sharedMaterials = uniqueMaterials;
            targetRenderer.enabled = true;
            CopyRendererSettings(sources[0].MeshRenderer, targetRenderer);

            CleanupSources(sources);

            m_HasCombined = true;
            m_CombinedMesh = null;

            Destroy(this);
            return true;
        }

        /// <summary>
        /// 按材质引用分组：同材质的 SubMesh 归为一组，组顺序为首次出现顺序。
        /// </summary>
        List<MaterialCombineGroup> BuildMaterialCombineGroups(List<MeshSource> sources, Matrix4x4 rootWorldToLocal)
        {
            var groups = new List<MaterialCombineGroup>();
            var groupIndexByMaterial = new Dictionary<Material, int>();

            for (int i = 0; i < sources.Count; i++)
            {
                MeshSource source = sources[i];
                Mesh mesh = GetReadableMesh(source.MeshFilter);
                if (mesh == null)
                {
                    continue;
                }

                Material[] sharedMaterials = source.MeshRenderer.sharedMaterials;
                if (sharedMaterials == null || sharedMaterials.Length == 0)
                {
                    continue;
                }

                Matrix4x4 localTransform = rootWorldToLocal * source.MeshFilter.transform.localToWorldMatrix;

                for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
                {
                    Material material = subMeshIndex < sharedMaterials.Length
                        ? sharedMaterials[subMeshIndex]
                        : sharedMaterials[0];
                    if (material == null)
                    {
                        continue;
                    }

                    if (!groupIndexByMaterial.TryGetValue(material, out int groupIndex))
                    {
                        groupIndex = groups.Count;
                        groupIndexByMaterial.Add(material, groupIndex);
                        groups.Add(new MaterialCombineGroup(material));
                    }

                    groups[groupIndex].Instances.Add(new CombineInstance
                    {
                        mesh = mesh,
                        subMeshIndex = subMeshIndex,
                        transform = localTransform,
                    });
                }
            }

            return groups;
        }

        /// <summary>
        /// 两阶段合并：先将同材质实例合成一张网格，再拼成多 SubMesh 的最终网格。
        /// </summary>
        Mesh CombineByMaterialGroups(List<MaterialCombineGroup> materialGroups, List<Mesh> intermediateMeshes)
        {
            if (materialGroups.Count == 1)
            {
                MaterialCombineGroup onlyGroup = materialGroups[0];
                Mesh singleMesh = CreateCombinedMesh($"{name}_Combined", onlyGroup.Instances, true, true);
                return singleMesh;
            }

            var finalInstances = new List<CombineInstance>(materialGroups.Count);
            for (int i = 0; i < materialGroups.Count; i++)
            {
                MaterialCombineGroup group = materialGroups[i];
                Mesh groupMesh = CreateCombinedMesh(
                    $"{name}_Combined_{group.Material.name}_{i}",
                    group.Instances,
                    true,
                    true);
                intermediateMeshes.Add(groupMesh);

                finalInstances.Add(new CombineInstance
                {
                    mesh = groupMesh,
                    subMeshIndex = 0,
                    transform = Matrix4x4.identity,
                });
            }

            return CreateCombinedMesh($"{name}_Combined", finalInstances, false, false);
        }

        static Mesh CreateCombinedMesh(
            string meshName,
            List<CombineInstance> instances,
            bool mergeSubMeshes,
            bool useTransforms)
        {
            var mesh = new Mesh
            {
                name = meshName,
            };

            if (GetTotalVertexCount(instances) > 65535)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.CombineMeshes(instances.ToArray(), mergeSubMeshes, useTransforms);
            return mesh;
        }

        static void DestroyMesh(Mesh mesh)
        {
            if (mesh != null)
            {
                Destroy(mesh);
            }
        }

        static void DestroyMeshes(List<Mesh> meshes)
        {
            for (int i = 0; i < meshes.Count; i++)
            {
                DestroyMesh(meshes[i]);
            }

            meshes.Clear();
        }

        void FinishProcessCoroutine()
        {
            m_ProcessCoroutine = null;
        }

        void RestoreChildVisibilityAfterFailedCombine()
        {
            if (m_HideChildrenUntilCombined)
            {
                SetChildRenderersEnabled(true);
            }
        }

        /// <summary>
        /// 使用 MeshFilter.mesh 获取可在 CPU 上访问的实例（sharedMesh 导入时通常不可读）。
        /// </summary>
        static Mesh GetReadableMesh(MeshFilter meshFilter)
        {
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                return null;
            }

            Mesh mesh = meshFilter.mesh;
            if (mesh != null && mesh.isReadable)
            {
                return mesh;
            }

            Mesh sharedMesh = meshFilter.sharedMesh;
            if (sharedMesh.isReadable)
            {
                return sharedMesh;
            }

            Mesh instantiated = Object.Instantiate(sharedMesh);
            instantiated.name = sharedMesh.name + "_Readable";
            if (instantiated.isReadable)
            {
                return instantiated;
            }

            Debug.LogWarning(
                $"[{nameof(RuntimeMeshCombine)}] Mesh '{sharedMesh.name}' 不可读，请在 FBX Import 中勾选 Read/Write Enabled。",
                meshFilter);
            return null;
        }

        IEnumerator ProcessOnStart()
        {
            if (m_HideChildrenUntilCombined)
            {
                SetChildRenderersEnabled(false);
            }

            if (m_EnableSpecialDuplicate)
            {
                yield return SpecialDuplicateRoutine();
            }

            CombineChildMeshes();
            FinishProcessCoroutine();
        }

        IEnumerator SpecialDuplicateRoutine()
        {
            if (m_DuplicateSource == null)
            {
                Debug.LogWarning($"[{nameof(RuntimeMeshCombine)}] '{name}' 启用了特殊复制但未指定源子物体。", this);
                yield break;
            }

            if (!m_DuplicateSource.IsChildOf(transform))
            {
                Debug.LogWarning(
                    $"[{nameof(RuntimeMeshCombine)}] 源子物体 '{m_DuplicateSource.name}' 必须是 '{name}' 的子物体。",
                    this);
                yield break;
            }

            int duplicateCount = Mathf.Max(0, m_DuplicateCount);
            if (duplicateCount == 0)
            {
                yield break;
            }

            int copiesPerFrame = Mathf.Max(1, m_CopiesPerFrame);
            SpecialDuplicateStepSettings settings = BuildDuplicateSettings();
            int createdThisFrame = 0;

            for (int step = 1; step <= duplicateCount; step++)
            {
                Transform created = SpecialDuplicateUtility.InstantiateAtStep(
                    m_DuplicateSource,
                    m_DuplicateSource,
                    transform,
                    step,
                    settings);

                if (m_HideChildrenUntilCombined)
                {
                    SetRenderersEnabled(created, false);
                }

                createdThisFrame++;
                if (createdThisFrame >= copiesPerFrame)
                {
                    createdThisFrame = 0;
                    yield return null;
                }
            }

            if (createdThisFrame > 0)
            {
                yield return null;
            }
        }

        SpecialDuplicateStepSettings BuildDuplicateSettings()
        {
            return new SpecialDuplicateStepSettings
            {
                translateIncrement = m_TranslateIncrement,
                rotateIncrementEuler = m_RotateIncrementEuler,
                scaleIncrement = m_ScaleIncrement,
                useLocalSpace = m_UseLocalSpace,
                applyScaleIncrement = m_ApplyScaleIncrement,
            };
        }

        void SetChildRenderersEnabled(bool enabled)
        {
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(m_IncludeInactiveChildren);
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (renderer == null || renderer.transform == transform)
                {
                    continue;
                }

                renderer.enabled = enabled;
            }
        }

        static void SetRenderersEnabled(Transform root, bool enabled)
        {
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = enabled;
                }
            }
        }

        static int GetTotalVertexCount(List<CombineInstance> combineInstances)
        {
            int total = 0;
            for (int i = 0; i < combineInstances.Count; i++)
            {
                CombineInstance instance = combineInstances[i];
                if (instance.mesh == null)
                {
                    continue;
                }

                total += instance.mesh.vertexCount;
            }

            return total;
        }

        static void CopyRendererSettings(MeshRenderer source, MeshRenderer target)
        {
            target.shadowCastingMode = source.shadowCastingMode;
            target.receiveShadows = source.receiveShadows;
            target.lightProbeUsage = source.lightProbeUsage;
            target.reflectionProbeUsage = source.reflectionProbeUsage;
            target.motionVectorGenerationMode = source.motionVectorGenerationMode;
            target.renderingLayerMask = source.renderingLayerMask;
            target.allowOcclusionWhenDynamic = source.allowOcclusionWhenDynamic;
        }

        void CleanupSources(List<MeshSource> sources)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                MeshSource source = sources[i];
                if (source.MeshRenderer == null)
                {
                    continue;
                }

                if (m_DestroySourceObjects)
                {
                    Destroy(source.MeshFilter.gameObject);
                }
                else
                {
                    source.MeshRenderer.enabled = false;
                }
            }
        }

        readonly struct MeshSource
        {
            public MeshSource(MeshFilter meshFilter, MeshRenderer meshRenderer)
            {
                MeshFilter = meshFilter;
                MeshRenderer = meshRenderer;
            }

            public MeshFilter MeshFilter { get; }
            public MeshRenderer MeshRenderer { get; }
        }

        sealed class MaterialCombineGroup
        {
            public MaterialCombineGroup(Material material)
            {
                Material = material;
                Instances = new List<CombineInstance>();
            }

            public Material Material { get; }
            public List<CombineInstance> Instances { get; }
        }
    }
}
