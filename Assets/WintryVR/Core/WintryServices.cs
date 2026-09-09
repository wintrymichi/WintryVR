using System;
using System.Collections.Generic;

namespace WintryVR.Core
{
    /// <summary>
    /// Minimal service locator. Every layer registers its implementation here during bootstrap;
    /// consumers only ever depend on interfaces.
    /// </summary>
    public static class WintryServices
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public static void Register<T>(T instance) where T : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            _services[typeof(T)] = instance;
            WintryLog.V("Services", "Registered " + typeof(T).Name + " -> " + instance.GetType().Name);
        }

        public static T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var s)) return (T)s;
            throw new InvalidOperationException("Service not registered: " + typeof(T).Name);
        }

        public static T TryGet<T>() where T : class
        {
            return _services.TryGetValue(typeof(T), out var s) ? (T)s : null;
        }

        public static bool Has<T>() where T : class => _services.ContainsKey(typeof(T));

        public static void Clear() => _services.Clear();
    }

    /// <summary>
    /// Platform-specific assemblies (e.g. WintryVR.Meta) register factories here at load time.
    /// The bootstrap picks the best available implementation and falls back to generic/mock ones.
    /// </summary>
    public static class ProviderRegistry
    {
        private class Entry { public string Name; public int Priority; public Func<object> Factory; }
        private static readonly Dictionary<Type, List<Entry>> _entries = new Dictionary<Type, List<Entry>>();

        public static void Register<T>(string name, int priority, Func<T> factory) where T : class
        {
            if (!_entries.TryGetValue(typeof(T), out var list)) { list = new List<Entry>(); _entries[typeof(T)] = list; }
            list.Add(new Entry { Name = name, Priority = priority, Factory = () => factory() });
            list.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            WintryLog.V("Registry", "Provider registered: " + typeof(T).Name + " <- " + name + " (p" + priority + ")");
        }

        /// <summary>Creates the highest-priority provider whose factory succeeds and passes the validator.</summary>
        public static T Resolve<T>(Func<T, bool> validator, Func<T> fallback) where T : class
        {
            if (_entries.TryGetValue(typeof(T), out var list))
            {
                foreach (var e in list)
                {
                    try
                    {
                        var instance = e.Factory() as T;
                        if (instance != null && (validator == null || validator(instance)))
                        {
                            WintryLog.I("Registry", typeof(T).Name + " -> " + e.Name);
                            return instance;
                        }
                    }
                    catch (Exception ex)
                    {
                        WintryLog.W("Registry", "Provider " + e.Name + " failed to initialise: " + ex.Message);
                    }
                }
            }
            var fb = fallback != null ? fallback() : null;
            if (fb != null) WintryLog.I("Registry", typeof(T).Name + " -> fallback " + fb.GetType().Name);
            return fb;
        }

        /// <summary>Creates every registered provider of a type (used for input sources: hands + controllers).</summary>
        public static List<T> ResolveAll<T>() where T : class
        {
            var result = new List<T>();
            if (_entries.TryGetValue(typeof(T), out var list))
                foreach (var e in list)
                {
                    try { var inst = e.Factory() as T; if (inst != null) result.Add(inst); }
                    catch (Exception ex) { WintryLog.W("Registry", "Provider " + e.Name + " failed: " + ex.Message); }
                }
            return result;
        }

        public static IEnumerable<string> Names<T>()
        {
            if (_entries.TryGetValue(typeof(T), out var list)) foreach (var e in list) yield return e.Name;
        }
    }
}
