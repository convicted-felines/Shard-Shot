//------------------------------------------------------------
// MainGame
//------------------------------------------------------------

using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 全局 UI 小元素对象池：可按业务键跨界面复用（如通用按钮、列表行等）。
    /// 关闭单个 UI 窗体时不会自动销毁此处注册的池；需要时在切场景、卸载资源或退出前调用 <see cref="DestroyPool"/> / <see cref="DestroyAllPools"/>。
    /// </summary>
    public static class GlobalUIElementPool
    {
        private static readonly Dictionary<string, IUGuiElementPool> s_Pools = new Dictionary<string, IUGuiElementPool>();

        private static string BuildObjectPoolName(string poolKey)
        {
            return string.Format("GlobalUIElement_{0}", poolKey);
        }

        /// <summary>
        /// 按唯一键获取或创建对象池。首次创建时使用 <paramref name="template"/> 与 <paramref name="defaultParent"/>；
        /// 已存在时直接返回已有池（忽略本次传入的 template，调用方应保证键唯一且首次创建参数正确）。
        /// </summary>
        public static UGuiElementPool<T> GetOrCreate<T>(string poolKey, T template, Transform defaultParent, int preloadCount = 0, int capacity = 32)
            where T : Component
        {
            if (string.IsNullOrEmpty(poolKey))
            {
                Log.Error("GlobalUIElementPool: poolKey is invalid.");
                return null;
            }

            string fullName = BuildObjectPoolName(poolKey);
            if (s_Pools.TryGetValue(fullName, out IUGuiElementPool existing))
            {
                UGuiElementPool<T> typed = existing as UGuiElementPool<T>;
                if (typed == null)
                {
                    Log.Error("GlobalUIElementPool: pool '{0}' exists but component type mismatch.", poolKey);
                }

                return typed;
            }

            if (template == null || defaultParent == null)
            {
                Log.Error("GlobalUIElementPool: template or defaultParent is invalid for key '{0}'.", poolKey);
                return null;
            }

            UGuiElementPool<T> pool = new UGuiElementPool<T>(fullName, template, defaultParent, preloadCount, capacity);
            s_Pools.Add(fullName, pool);
            return pool;
        }

        /// <summary>
        /// 是否已存在该键的全局池。
        /// </summary>
        public static bool HasPool(string poolKey)
        {
            if (string.IsNullOrEmpty(poolKey))
            {
                return false;
            }

            return s_Pools.ContainsKey(BuildObjectPoolName(poolKey));
        }

        /// <summary>
        /// 销毁指定键的全局池（内部销毁池中仍存在的全部 GameObject）。
        /// </summary>
        public static bool DestroyPool(string poolKey)
        {
            if (string.IsNullOrEmpty(poolKey))
            {
                return false;
            }

            string fullName = BuildObjectPoolName(poolKey);
            if (!s_Pools.TryGetValue(fullName, out IUGuiElementPool pool))
            {
                return false;
            }

            pool.Release();
            s_Pools.Remove(fullName);
            return true;
        }

        /// <summary>
        /// 销毁当前登记的全部全局 UI 元素池（例如切换场景、退出游戏前调用）。
        /// </summary>
        public static void DestroyAllPools()
        {
            foreach (KeyValuePair<string, IUGuiElementPool> kv in s_Pools)
            {
                kv.Value.Release();
            }

            s_Pools.Clear();
        }
    }

    public interface IUGuiPooledElement
    {
        void OnElementSpawn();
        void OnElementRecycle();
    }

    /// <summary>
    /// UI 小元素对象池句柄；<see cref="UGuiElementPool{T}"/> 实现此接口，供 <see cref="GlobalUIElementPool"/> 统一管理。
    /// </summary>
    public interface IUGuiElementPool
    {
        void Release();
    }

    /// <summary>
    /// 轻量 UI 子元素对象池：用空闲栈管理实例，不依赖 Game Framework <c>ObjectPool.Unspawn</c>（避免与 ObjectBase 包装不匹配导致无法回池、反复 Instantiate）。
    /// </summary>
    public sealed class UGuiElementPool<T> : IUGuiElementPool where T : Component
    {
        private readonly string m_PoolName;
        private readonly T m_Template;
        private readonly Transform m_Parent;
        private readonly int m_CapacityHint;
        private readonly Stack<T> m_Free = new Stack<T>();
        private readonly HashSet<T> m_InUse = new HashSet<T>();
        private readonly List<T> m_AllInstances = new List<T>();

        public UGuiElementPool(string poolName, T template, Transform parent, int preloadCount, int capacity)
        {
            if (template == null)
            {
                Log.Error("UI element template is invalid.");
                return;
            }

            m_PoolName = poolName;
            m_Template = template;
            m_Parent = parent;
            m_CapacityHint = Mathf.Max(1, capacity);

            m_Template.gameObject.SetActive(false);

            for (int i = 0; i < preloadCount; i++)
            {
                T instance = CreateInstance();
                m_Free.Push(instance);
                m_AllInstances.Add(instance);
            }
        }

        public T Spawn(Transform parent = null)
        {
            T element = m_Free.Count > 0 ? m_Free.Pop() : CreateNewOrNull();
            if (element == null)
            {
                return null;
            }

            Transform targetParent = parent != null ? parent : m_Parent;
            if (targetParent != null)
            {
                element.transform.SetParent(targetParent, false);
            }

            element.gameObject.SetActive(true);

            IUGuiPooledElement pooledElement = element as IUGuiPooledElement;
            if (pooledElement != null)
            {
                pooledElement.OnElementSpawn();
            }

            m_InUse.Add(element);
            return element;
        }

        public void Recycle(T element)
        {
            if (element == null)
            {
                return;
            }

            if (!m_InUse.Remove(element))
            {
                Log.Warning("UGuiElementPool: recycle element not in use (already recycled or not from this pool).");
                return;
            }

            IUGuiPooledElement pooledElement = element as IUGuiPooledElement;
            if (pooledElement != null)
            {
                pooledElement.OnElementRecycle();
            }

            if (m_Parent != null)
            {
                element.transform.SetParent(m_Parent, false);
            }

            element.gameObject.SetActive(false);
            m_Free.Push(element);
        }

        public void RecycleAll()
        {
            if (m_InUse.Count == 0)
            {
                return;
            }

            T[] elements = new T[m_InUse.Count];
            m_InUse.CopyTo(elements);
            for (int i = 0; i < elements.Length; i++)
            {
                Recycle(elements[i]);
            }
        }

        public void Release()
        {
            RecycleAll();
            for (int i = 0; i < m_AllInstances.Count; i++)
            {
                T inst = m_AllInstances[i];
                if (inst != null)
                {
                    Object.Destroy(inst.gameObject);
                }
            }

            m_AllInstances.Clear();
            m_Free.Clear();
            m_InUse.Clear();
        }

        private T CreateNewOrNull()
        {
            if (m_AllInstances.Count >= m_CapacityHint)
            {
                Log.Warning(
                    "UGuiElementPool '{0}': instance count {1} reached capacity hint {2}; will still expand.",
                    m_PoolName,
                    m_AllInstances.Count,
                    m_CapacityHint);
            }

            T instance = CreateInstance();
            m_AllInstances.Add(instance);
            return instance;
        }

        private T CreateInstance()
        {
            T instance = Object.Instantiate(m_Template, m_Parent);
            instance.gameObject.SetActive(false);
            return instance;
        }
    }
}
