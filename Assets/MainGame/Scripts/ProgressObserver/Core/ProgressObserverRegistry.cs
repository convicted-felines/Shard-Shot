using System;
using System.Collections.Generic;
using System.Reflection;
using UnityGameFramework.Runtime;

namespace MainGame
{
    public sealed class ProgressObserverDefinition
    {
        public string Id { get; set; }
        public Type LogicType { get; set; }
        public ProgressObserverSpawnMode SpawnMode { get; set; }
        public ProgressObserverSignal SpawnSignal { get; set; }
        public Type[] ActiveInProcedures { get; set; }
    }

    /// <summary>
    /// 扫描带 <see cref="ProgressObserverAttribute"/> 的 <see cref="ProgressObserverEntity"/> 子类。
    /// </summary>
    public static class ProgressObserverRegistry
    {
        private static List<ProgressObserverDefinition> s_Definitions;
        private static Dictionary<string, ProgressObserverDefinition> s_ById;
        private static bool s_Initialized;

        public static IReadOnlyList<ProgressObserverDefinition> All
        {
            get
            {
                EnsureInitialized();
                return s_Definitions;
            }
        }

        public static bool TryGetDefinition(string observerId, out ProgressObserverDefinition definition)
        {
            definition = null;
            if (string.IsNullOrEmpty(observerId))
            {
                return false;
            }

            EnsureInitialized();
            return s_ById != null && s_ById.TryGetValue(observerId.Trim(), out definition);
        }

        private static void EnsureInitialized()
        {
            if (s_Initialized)
            {
                return;
            }

            s_Initialized = true;
            s_Definitions = new List<ProgressObserverDefinition>();
            s_ById = new Dictionary<string, ProgressObserverDefinition>(StringComparer.Ordinal);

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }

                if (types == null)
                {
                    continue;
                }

                for (int i = 0; i < types.Length; i++)
                {
                    RegisterType(types[i]);
                }
            }
        }

        private static void RegisterType(Type type)
        {
            if (type == null || type.IsAbstract || !typeof(ProgressObserverEntity).IsAssignableFrom(type))
            {
                return;
            }

            object[] attrs = type.GetCustomAttributes(typeof(ProgressObserverAttribute), false);
            if (attrs == null || attrs.Length == 0)
            {
                return;
            }

            if (!(attrs[0] is ProgressObserverAttribute attr) || string.IsNullOrEmpty(attr.Id))
            {
                return;
            }

            if (attr.SpawnMode == ProgressObserverSpawnMode.OnSignal && attr.SpawnSignal == ProgressObserverSignal.None)
            {
                Log.Warning("ProgressObserverRegistry: '{0}' uses OnSignal but SpawnSignal is None.", type.Name);
            }

            string id = attr.Id.Trim();
            var definition = new ProgressObserverDefinition
            {
                Id = id,
                LogicType = type,
                SpawnMode = attr.SpawnMode,
                SpawnSignal = attr.SpawnSignal,
                ActiveInProcedures = attr.ActiveInProcedures,
            };

            if (!s_ById.ContainsKey(id))
            {
                s_ById[id] = definition;
                s_Definitions.Add(definition);
            }
            else
            {
                Log.Warning(
                    "ProgressObserverRegistry: duplicate id '{0}', keeping '{1}', ignoring '{2}'.",
                    id,
                    s_ById[id].LogicType.Name,
                    type.Name);
            }
        }
    }
}
