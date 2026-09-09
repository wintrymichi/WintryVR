using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WintryVR.Core;
using WintryVR.Networking;

namespace WintryVR.AssetGeneration
{
    /// <summary>
    /// Persistent cache of generated assets so nothing is regenerated unnecessarily. Stores a manifest
    /// (asset id, version, prompt, date, model, texture files, material params, LOD info, optimisation status)
    /// plus PNG textures under persistentDataPath/WintryAssetCache.
    /// </summary>
    public class AssetCache
    {
        [Serializable]
        public class Entry
        {
            public string AssetId;
            public int Version;
            public string GenerationPrompt;
            public string GenerationDate;
            public string Model;
            public string LookJson;
            public List<string> Textures = new List<string>();
            public string MaterialName;
            public int LodCount;
            public string LodReport;
            public string OptimizationStatus;
            public string Stage;
        }

        [Serializable] private class Manifest { public List<Entry> Entries = new List<Entry>(); }

        private readonly string _root;
        private readonly string _manifestPath;
        private Manifest _manifest = new Manifest();

        public AssetCache(string root = null)
        {
            _root = root ?? Path.Combine(Application.persistentDataPath, "WintryAssetCache");
            Directory.CreateDirectory(_root);
            _manifestPath = Path.Combine(_root, "manifest.json");
            Load();
        }

        public string Root => _root;
        public IReadOnlyList<Entry> Entries => _manifest.Entries;

        public static string IdFor(string prompt, string model)
        {
            using (var sha = System.Security.Cryptography.SHA1.Create())
            {
                var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes((prompt ?? "") + "|" + (model ?? "")));
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < 8; i++) sb.Append(bytes[i].ToString("x2"));
                return "asset_" + sb;
            }
        }

        public Entry Find(string assetId) => _manifest.Entries.Find(e => e.AssetId == assetId);
        public bool Has(string assetId) => Find(assetId) != null;

        public Entry Store(GeneratedAsset asset)
        {
            var entry = Find(asset.AssetId) ?? new Entry { AssetId = asset.AssetId };
            if (!_manifest.Entries.Contains(entry)) _manifest.Entries.Add(entry);
            entry.Version = asset.Version;
            entry.GenerationPrompt = asset.Prompt;
            entry.GenerationDate = asset.GeneratedAt;
            entry.Model = asset.Model;
            entry.LookJson = JsonUtility.ToJson(asset.Look);
            entry.MaterialName = asset.Material != null ? asset.Material.name : "";
            entry.LodCount = asset.Lods != null ? asset.Lods.Length : 0;
            entry.LodReport = asset.Lods != null ? MeshOptimizer.Report(asset.Lods) : "";
            entry.OptimizationStatus = asset.Optimized ? "optimized" : "raw";
            entry.Stage = asset.Stage.ToString();
            entry.Textures.Clear();
            if (asset.Textures != null)
            {
                string dir = Path.Combine(_root, asset.AssetId); Directory.CreateDirectory(dir);
                SaveTex(dir, "basecolor", asset.Textures.BaseColor, entry);
                SaveTex(dir, "normal", asset.Textures.Normal, entry);
                SaveTex(dir, "roughness", asset.Textures.Roughness, entry);
                SaveTex(dir, "metallic", asset.Textures.Metallic, entry);
                SaveTex(dir, "emission", asset.Textures.Emission, entry);
                SaveTex(dir, "detail", asset.Textures.Detail, entry);
            }
            Save();
            return entry;
        }

        private void SaveTex(string dir, string name, Texture2D t, Entry e)
        {
            if (t == null) return;
            try
            {
                string path = Path.Combine(dir, name + ".png");
                File.WriteAllBytes(path, t.EncodeToPNG());
                e.Textures.Add(path);
            }
            catch (Exception ex) { WintryLog.W("AssetCache", "Could not save " + name + ": " + ex.Message); }
        }

        public GeneratedTextureSet LoadTextures(Entry e)
        {
            if (e == null || e.Textures.Count == 0) return null;
            var set = new GeneratedTextureSet();
            foreach (var path in e.Textures)
            {
                if (!File.Exists(path)) continue;
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, true, !path.Contains("basecolor") && !path.Contains("emission"));
                t.LoadImage(File.ReadAllBytes(path));
                t.wrapMode = TextureWrapMode.Repeat;
                string n = Path.GetFileNameWithoutExtension(path);
                switch (n)
                {
                    case "basecolor": set.BaseColor = t; break;
                    case "normal": set.Normal = t; break;
                    case "roughness": set.Roughness = t; break;
                    case "metallic": set.Metallic = t; break;
                    case "emission": set.Emission = t; break;
                    case "detail": set.Detail = t; break;
                }
                set.Resolution = t.width;
            }
            return set;
        }

        public WintryLookDefinition LoadLook(Entry e)
        {
            var look = new WintryLookDefinition();
            try { if (!string.IsNullOrEmpty(e.LookJson)) JsonUtility.FromJsonOverwrite(e.LookJson, look); } catch { }
            return look;
        }

        public void Remove(string assetId)
        {
            var e = Find(assetId);
            if (e == null) return;
            _manifest.Entries.Remove(e);
            try { string dir = Path.Combine(_root, assetId); if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { }
            Save();
        }

        public void Clear()
        {
            foreach (var e in new List<Entry>(_manifest.Entries)) Remove(e.AssetId);
        }

        private void Load()
        {
            try { if (File.Exists(_manifestPath)) _manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(_manifestPath)) ?? new Manifest(); }
            catch (Exception ex) { WintryLog.W("AssetCache", "Manifest unreadable: " + ex.Message); _manifest = new Manifest(); }
        }

        private void Save()
        {
            try { File.WriteAllText(_manifestPath, JsonUtility.ToJson(_manifest, true)); }
            catch (Exception ex) { WintryLog.W("AssetCache", "Manifest save failed: " + ex.Message); }
        }
    }
}
