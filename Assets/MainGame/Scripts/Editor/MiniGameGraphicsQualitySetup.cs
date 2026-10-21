using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MainGame.Editor
{
    /// <summary>
    /// 为抖音 / 微信 / 安卓注册独立 URP 管线与 QualitySettings 质量层。
    /// 管线 .asset 可先入库；Unity 生成 .meta 后执行本菜单完成 Renderer 绑定与质量层写入。
    /// </summary>
    public static class MiniGameGraphicsQualitySetup
    {
        private const string WebGlPipelinePath = "Assets/Settings/webgl_PipelineAsset.asset";
        private const string WebGlForwardRendererPath = "Assets/Settings/webgl_PipelineAsset_ForwardRenderer.asset";
        private const string QualitySettingsPath = "ProjectSettings/QualitySettings.asset";

        [MenuItem("Tools/Graphics/Ensure MiniGame Quality Levels", false, 200)]
        public static void EnsureMiniGameQualityLevels()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                EnsureForwardRenderer(Constant.Graphics.DouYinForwardRendererPath, "douyin_PipelineAsset_ForwardRenderer");
                EnsureForwardRenderer(Constant.Graphics.WeChatForwardRendererPath, "wechat_PipelineAsset_ForwardRenderer");
                EnsureForwardRenderer(Constant.Graphics.AndroidForwardRendererPath, "android_PipelineAsset_ForwardRenderer");
                EnsurePipelineAsset(
                    Constant.Graphics.DouYinPipelineAssetPath,
                    Constant.Graphics.DouYinForwardRendererPath,
                    "douyin_PipelineAsset",
                    shadowDistance: 40f,
                    renderScale: 1f,
                    mainLightShadowmapResolution: 512,
                    shadowCascadeCount: 2,
                    colorGradingLutSize: 32,
                    additionalLightShadowsSupported: true);
                EnsurePipelineAsset(
                    Constant.Graphics.WeChatPipelineAssetPath,
                    Constant.Graphics.WeChatForwardRendererPath,
                    "wechat_PipelineAsset",
                    shadowDistance: 50f,
                    renderScale: 1f,
                    mainLightShadowmapResolution: 512,
                    shadowCascadeCount: 2,
                    colorGradingLutSize: 32,
                    additionalLightShadowsSupported: true);
                EnsurePipelineAsset(
                    Constant.Graphics.AndroidPipelineAssetPath,
                    Constant.Graphics.AndroidForwardRendererPath,
                    "android_PipelineAsset",
                    shadowDistance: 40f,
                    renderScale: 0.85f,
                    mainLightShadowmapResolution: 1024,
                    shadowCascadeCount: 1,
                    colorGradingLutSize: 16,
                    additionalLightShadowsSupported: false);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            UniversalRenderPipelineAsset douyinPipeline =
                AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Constant.Graphics.DouYinPipelineAssetPath);
            UniversalRenderPipelineAsset wechatPipeline =
                AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Constant.Graphics.WeChatPipelineAssetPath);
            UniversalRenderPipelineAsset androidPipeline =
                AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Constant.Graphics.AndroidPipelineAssetPath);

            if (douyinPipeline == null || wechatPipeline == null || androidPipeline == null)
            {
                EditorUtility.DisplayDialog(
                    "Graphics Quality",
                    "未能加载抖音/微信/安卓 PipelineAsset。请确认 Assets/Settings 下资源已导入后再执行一次。",
                    "OK");
                return;
            }

            int templateIndex = FindQualityIndex("webgl");
            if (templateIndex < 0)
            {
                templateIndex = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, QualitySettings.names.Length - 1);
            }

            EnsureQualityLevel(Constant.Graphics.DouYinQualityName, douyinPipeline, templateIndex, shadowDistance: 40f, shadowCascades: 2);
            EnsureQualityLevel(Constant.Graphics.WeChatQualityName, wechatPipeline, templateIndex, shadowDistance: 50f, shadowCascades: 2);
            EnsureQualityLevel(
                Constant.Graphics.AndroidQualityName,
                androidPipeline,
                templateIndex,
                shadowDistance: 40f,
                shadowCascades: 1,
                pixelLightCount: 1,
                skinWeights: 2,
                lodBias: 0.7f,
                particleRaycastBudget: 256,
                softParticles: false,
                softVegetation: false);

            SetPerPlatformDefaultQuality("Android", Constant.Graphics.AndroidQualityName);

            AssetDatabase.SaveAssets();

            Debug.Log(
                "[MiniGameGraphicsQualitySetup] Quality levels ready: " +
                Constant.Graphics.DouYinQualityName + ", " +
                Constant.Graphics.WeChatQualityName + ", " +
                Constant.Graphics.AndroidQualityName +
                ". Commit QualitySettings.asset and Settings/*.meta after Unity import.");
            EditorUtility.DisplayDialog(
                "Graphics Quality",
                "已注册质量层 DouYin / WeChat / Android，并绑定对应 URP 管线。\n安卓平台默认质量已指向 Android。",
                "OK");
        }

        private static void EnsureForwardRenderer(string assetPath, string objectName)
        {
            if (!File.Exists(ToFullPath(assetPath)))
            {
                if (!File.Exists(ToFullPath(WebGlForwardRendererPath)))
                {
                    Debug.LogError("[MiniGameGraphicsQualitySetup] Missing source renderer: " + WebGlForwardRendererPath);
                    return;
                }

                AssetDatabase.CopyAsset(WebGlForwardRendererPath, assetPath);
            }

            ScriptableRendererData renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(assetPath);
            if (renderer == null)
            {
                return;
            }

            renderer.name = objectName;
            SerializedObject so = new SerializedObject(renderer);
            SerializedProperty receive = so.FindProperty("m_ShadowTransparentReceive");
            if (receive != null)
            {
                receive.boolValue = false;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer);
        }

        private static void EnsurePipelineAsset(
            string pipelinePath,
            string forwardRendererPath,
            string objectName,
            float shadowDistance,
            float renderScale,
            int mainLightShadowmapResolution,
            int shadowCascadeCount,
            int colorGradingLutSize,
            bool additionalLightShadowsSupported)
        {
            if (!File.Exists(ToFullPath(pipelinePath)))
            {
                if (!File.Exists(ToFullPath(WebGlPipelinePath)))
                {
                    Debug.LogError("[MiniGameGraphicsQualitySetup] Missing source pipeline: " + WebGlPipelinePath);
                    return;
                }

                AssetDatabase.CopyAsset(WebGlPipelinePath, pipelinePath);
            }

            UniversalRenderPipelineAsset pipeline =
                AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            ScriptableRendererData renderer =
                AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(forwardRendererPath);
            if (pipeline == null)
            {
                return;
            }

            pipeline.name = objectName;
            SerializedObject so = new SerializedObject(pipeline);

            SerializedProperty rendererList = so.FindProperty("m_RendererDataList");
            if (rendererList != null && renderer != null)
            {
                rendererList.arraySize = 1;
                rendererList.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            }

            SetBool(so, "m_RequireDepthTexture", false);
            SetBool(so, "m_RequireOpaqueTexture", false);
            SetBool(so, "m_SupportsTerrainHoles", false);
            SetBool(so, "m_SupportsHDR", false);
            SetInt(so, "m_MSAA", 1);
            SetFloat(so, "m_RenderScale", renderScale);
            SetBool(so, "m_EnableLODCrossFade", false);
            SetBool(so, "m_MainLightShadowsSupported", true);
            SetInt(so, "m_MainLightShadowmapResolution", mainLightShadowmapResolution);
            SetInt(so, "m_AdditionalLightsRenderingMode", 0);
            SetBool(so, "m_AdditionalLightShadowsSupported", additionalLightShadowsSupported);
            SetInt(so, "m_AdditionalLightsShadowmapResolution", 512);
            SetBool(so, "m_ReflectionProbeBlending", false);
            SetBool(so, "m_ReflectionProbeBoxProjection", false);
            SetFloat(so, "m_ShadowDistance", shadowDistance);
            SetInt(so, "m_ShadowCascadeCount", shadowCascadeCount);
            SetBool(so, "m_SoftShadowsSupported", false);
            SetInt(so, "m_SoftShadowQuality", 0);
            SetInt(so, "m_ColorGradingLutSize", colorGradingLutSize);
            SetBool(so, "m_SupportDataDrivenLensFlare", false);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
        }

        private static void EnsureQualityLevel(
            string qualityName,
            RenderPipelineAsset pipelineAsset,
            int templateIndex,
            float shadowDistance,
            int shadowCascades,
            int pixelLightCount = 4,
            int skinWeights = 4,
            float lodBias = 1f,
            int particleRaycastBudget = 2048,
            bool softParticles = true,
            bool softVegetation = true)
        {
            Object qualityAsset = AssetDatabase.LoadMainAssetAtPath(QualitySettingsPath);
            if (qualityAsset == null)
            {
                Debug.LogError("[MiniGameGraphicsQualitySetup] Cannot load " + QualitySettingsPath);
                return;
            }

            SerializedObject qualitySo = new SerializedObject(qualityAsset);
            SerializedProperty settings = qualitySo.FindProperty("m_QualitySettings");
            if (settings == null || !settings.isArray)
            {
                Debug.LogError("[MiniGameGraphicsQualitySetup] m_QualitySettings missing.");
                return;
            }

            int index = FindQualityIndexInSerialized(settings, qualityName);
            if (index < 0)
            {
                int copyFrom = Mathf.Clamp(templateIndex, 0, settings.arraySize - 1);
                settings.arraySize++;
                index = settings.arraySize - 1;
                CopyQualityElement(settings, copyFrom, index);
            }

            SerializedProperty level = settings.GetArrayElementAtIndex(index);
            level.FindPropertyRelative("name").stringValue = qualityName;
            SetRelativeInt(level, "pixelLightCount", pixelLightCount);
            SetRelativeInt(level, "vSyncCount", 0);
            SetRelativeInt(level, "antiAliasing", 0);
            SetRelativeInt(level, "shadowCascades", shadowCascades);
            SetRelativeFloat(level, "shadowDistance", shadowDistance);
            SetRelativeInt(level, "skinWeights", skinWeights);
            SetRelativeFloat(level, "lodBias", lodBias);
            SetRelativeInt(level, "particleRaycastBudget", particleRaycastBudget);
            SetRelativeBool(level, "softParticles", softParticles);
            SetRelativeBool(level, "softVegetation", softVegetation);
            SetRelativeBool(level, "realtimeReflectionProbes", false);
            SetRelativeBool(level, "enableLODCrossFade", false);

            SerializedProperty pipelineProp = level.FindPropertyRelative("customRenderPipeline");
            if (pipelineProp != null)
            {
                pipelineProp.objectReferenceValue = pipelineAsset;
            }

            qualitySo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(qualityAsset);

            Debug.Log("[MiniGameGraphicsQualitySetup] Quality '" + qualityName + "' -> " + pipelineAsset.name);
        }

        private static void SetPerPlatformDefaultQuality(string platformName, string qualityName)
        {
            Object qualityAsset = AssetDatabase.LoadMainAssetAtPath(QualitySettingsPath);
            if (qualityAsset == null)
            {
                return;
            }

            SerializedObject qualitySo = new SerializedObject(qualityAsset);
            SerializedProperty settings = qualitySo.FindProperty("m_QualitySettings");
            int qualityIndex = FindQualityIndexInSerialized(settings, qualityName);
            if (qualityIndex < 0)
            {
                Debug.LogWarning("[MiniGameGraphicsQualitySetup] Cannot set platform default, missing quality: " + qualityName);
                return;
            }

            SerializedProperty perPlatform = qualitySo.FindProperty("m_PerPlatformDefaultQuality");
            if (perPlatform == null)
            {
                return;
            }

            for (int i = 0; i < perPlatform.arraySize; i++)
            {
                SerializedProperty entry = perPlatform.GetArrayElementAtIndex(i);
                SerializedProperty first = entry.FindPropertyRelative("first");
                SerializedProperty second = entry.FindPropertyRelative("second");
                if (first != null && second != null && first.stringValue == platformName)
                {
                    second.intValue = qualityIndex;
                    qualitySo.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(qualityAsset);
                    Debug.Log("[MiniGameGraphicsQualitySetup] Platform '" + platformName + "' default quality -> " + qualityName + " (" + qualityIndex + ")");
                    return;
                }
            }

            // Unity 有时把 PerPlatformDefaultQuality 存成命名字段而非数组；回退到直接改 YAML 已由手工写入。
            Debug.LogWarning(
                "[MiniGameGraphicsQualitySetup] m_PerPlatformDefaultQuality entry for '" +
                platformName +
                "' not found as array; keep ProjectSettings/QualitySettings.asset Android mapping.");
        }

        private static int FindQualityIndex(string qualityName)
        {
            string[] names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] == qualityName)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int FindQualityIndexInSerialized(SerializedProperty settings, string qualityName)
        {
            for (int i = 0; i < settings.arraySize; i++)
            {
                SerializedProperty nameProp = settings.GetArrayElementAtIndex(i).FindPropertyRelative("name");
                if (nameProp != null && nameProp.stringValue == qualityName)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void CopyQualityElement(SerializedProperty settings, int from, int to)
        {
            if (from == to || from < 0 || to < 0 || from >= settings.arraySize || to >= settings.arraySize)
            {
                return;
            }

            SerializedProperty src = settings.GetArrayElementAtIndex(from);
            SerializedProperty dst = settings.GetArrayElementAtIndex(to);
            string[] paths =
            {
                "pixelLightCount", "shadows", "shadowResolution", "shadowProjection", "shadowCascades",
                "shadowDistance", "shadowNearPlaneOffset", "shadowmaskMode", "skinWeights",
                "anisotropicTextures", "antiAliasing", "softParticles", "softVegetation",
                "realtimeReflectionProbes", "billboardsFaceCameraPosition", "vSyncCount",
                "lodBias", "maximumLODLevel", "enableLODCrossFade", "particleRaycastBudget",
                "customRenderPipeline", "realtimeGICPUUsage", "streamingMipmapsActive"
            };

            for (int i = 0; i < paths.Length; i++)
            {
                SerializedProperty a = src.FindPropertyRelative(paths[i]);
                SerializedProperty b = dst.FindPropertyRelative(paths[i]);
                if (a == null || b == null)
                {
                    continue;
                }

                switch (a.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        b.intValue = a.intValue;
                        break;
                    case SerializedPropertyType.Boolean:
                        b.boolValue = a.boolValue;
                        break;
                    case SerializedPropertyType.Float:
                        b.floatValue = a.floatValue;
                        break;
                    case SerializedPropertyType.Enum:
                        b.enumValueIndex = a.enumValueIndex;
                        break;
                    case SerializedPropertyType.ObjectReference:
                        b.objectReferenceValue = a.objectReferenceValue;
                        break;
                }
            }
        }

        private static string ToFullPath(string assetPath)
        {
            if (assetPath.StartsWith("Assets/") || assetPath.StartsWith("Assets\\"))
            {
                return Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            }

            return assetPath;
        }

        private static void SetBool(SerializedObject so, string propertyName, bool value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void SetInt(SerializedObject so, string propertyName, int value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static void SetFloat(SerializedObject so, string propertyName, float value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetRelativeInt(SerializedProperty level, string name, int value)
        {
            SerializedProperty property = level.FindPropertyRelative(name);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static void SetRelativeFloat(SerializedProperty level, string name, float value)
        {
            SerializedProperty property = level.FindPropertyRelative(name);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetRelativeBool(SerializedProperty level, string name, bool value)
        {
            SerializedProperty property = level.FindPropertyRelative(name);
            if (property != null)
            {
                property.boolValue = value;
            }
        }
    }
}
