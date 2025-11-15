using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace SmallScaleInc.TopDownPixelCharactersPack1.EditorTools
{
    public static class IsometricAssetSetup
    {
        private static readonly string[] SpriteSourceFolders =
        {
            "Assets/Tiles/NewisometricDungeon/Isometric",
            "Assets/Tiles/isometricDungeon/Isometric"
        };
        private const float TargetPixelsPerUnit = 256f;
        private static readonly Vector2 PivotBottomCenter = new Vector2(0.5f, 0f);

        private const string PrefabRootFolder = "Assets/Prefabs/Environment";
        private const string PaletteFolder = "Assets/TilePalettes/NewisometricDungeon";

        private static readonly SpriteDataProviderFactories DataProviderFactories;

        private static readonly PrefabRule[] PrefabRules =
        {
            new PrefabRule(
                category: "Ground",
                sortingLayer: "Ground",
                sortingOrder: 0,
                addCollider: false,
                tokens: new[] { "floor", "tile", "dirt", "plank" }),
            new PrefabRule(
                category: "Walls",
                sortingLayer: "Walls",
                sortingOrder: 2,
                addCollider: true,
                tokens: new[] { "wall", "column", "pillar", "gate", "door", "support" }),
            new PrefabRule(
                category: "Decor",
                sortingLayer: "Decor",
                sortingOrder: 1,
                addCollider: true,
                tokens: new[] { "barrel", "crate", "chest", "chair", "table", "stairs", "step", "bridge", "pile" })
        };

        static IsometricAssetSetup()
        {
            DataProviderFactories = new SpriteDataProviderFactories();
            DataProviderFactories.Init();
        }

        [MenuItem("Tools/Isometric/1. Normalize Sprites")]        
        public static void NormalizeSprites()
        {
            var sourceFolder = ResolveSourceFolder();
            if (sourceFolder == null)
            {
                Debug.LogError("No valid sprite source folder found.");
                return;
            }

            var textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { sourceFolder });
            if (textureGuids.Length == 0)
            {
                Debug.LogWarning($"No textures found in '{sourceFolder}'.");
                return;
            }

            int processed = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var guid in textureGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null)
                    {
                        continue;
                    }

                    bool changed = NormalizeSpriteImporter(importer);
                    if (changed)
                    {
                        importer.SaveAndReimport();
                        processed++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Debug.Log($"Normalized {processed} texture(s) to pivot {PivotBottomCenter} and PPU {TargetPixelsPerUnit}.");
        }

        [MenuItem("Tools/Isometric/2. Generate Prefabs")]        
        public static void GeneratePrefabs()
        {
            var sourceFolder = ResolveSourceFolder();
            if (sourceFolder == null)
            {
                Debug.LogError("No valid sprite source folder found.");
                return;
            }

            EnsureFolder(PrefabRootFolder);

            var spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { sourceFolder });
            if (spriteGuids.Length == 0)
            {
                Debug.LogWarning($"No sprites found in '{sourceFolder}'.");
                return;
            }

            var sprites = spriteGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .SelectMany(path => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
                .Where(sprite => sprite != null)
                .OrderBy(sprite => sprite.texture != null ? sprite.texture.name : sprite.name)
                .ThenBy(sprite => sprite.name)
                .ToArray();

            int created = 0;
            foreach (var sprite in sprites)
            {
                var rule = MatchRule(sprite.name);
                if (rule == null)
                {
                    continue;
                }

                string prefabFolder = Path.Combine(PrefabRootFolder, rule.Value.Category).Replace('\\', '/');
                EnsureFolder(prefabFolder);

                string safeName = sprite.name.Replace(' ', '_');
                string prefabPath = Path.Combine(prefabFolder, safeName + ".prefab").Replace('\\', '/');

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    prefab = CreatePrefab(sprite, rule.Value);
                    PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                    UnityEngine.Object.DestroyImmediate(prefab);
                    created++;
                }
                else
                {
                    UpdatePrefab(prefab, sprite, rule.Value);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Prefabs generated/updated from '{sourceFolder}'. Created {created} new prefab(s) under '{PrefabRootFolder}'.");
        }

        [MenuItem("Tools/Isometric/3. Create Level Root")]        
        public static void CreateLevelRoot()
        {
            var root = GameObject.Find("LevelRoot") ?? new GameObject("LevelRoot");
            Undo.RegisterCreatedObjectUndo(root, "Create LevelRoot");

            EnsureChild(root.transform, "Ground");
            EnsureChild(root.transform, "Walls");
            EnsureChild(root.transform, "Props");
            EnsureChild(root.transform, "Colliders");

            Selection.activeGameObject = root;
            Debug.Log("LevelRoot created with child containers Ground/Walls/Props/Colliders.");
        }

        private static bool NormalizeSpriteImporter(TextureImporter importer)
        {
            bool changed = false;

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, TargetPixelsPerUnit))
            {
                importer.spritePixelsPerUnit = TargetPixelsPerUnit;
                changed = true;
            }

            var dataProvider = DataProviderFactories.GetSpriteEditorDataProviderFromObject(importer) as ISpriteEditorDataProvider;
            if (dataProvider == null)
            {
                return changed;
            }

            dataProvider.InitSpriteEditorDataProvider();
            var spriteRects = dataProvider.GetSpriteRects();
            bool rectsChanged = false;

            for (int i = 0; i < spriteRects.Length; i++)
            {
                var rect = spriteRects[i];
                if (rect.alignment != SpriteAlignment.Custom || rect.pivot != PivotBottomCenter)
                {
                    rect.alignment = SpriteAlignment.Custom;
                    rect.pivot = PivotBottomCenter;
                    spriteRects[i] = rect;
                    rectsChanged = true;
                }
            }

            if (rectsChanged)
            {
                dataProvider.SetSpriteRects(spriteRects);
                dataProvider.Apply();
                changed = true;
            }

            return changed;
        }

        private static GameObject CreatePrefab(Sprite sprite, PrefabRule rule)
        {
            var go = new GameObject(sprite.name);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = rule.SortingLayer;
            renderer.sortingOrder = rule.SortingOrder;

            if (rule.AddCollider)
            {
                var collider = go.AddComponent<BoxCollider2D>();
                ApplyCollider(collider, sprite);
            }

            return go;
        }

        private static void UpdatePrefab(GameObject prefab, Sprite sprite, PrefabRule rule)
        {
            var renderer = prefab.GetComponent<SpriteRenderer>() ?? prefab.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = rule.SortingLayer;
            renderer.sortingOrder = rule.SortingOrder;

            var collider = prefab.GetComponent<BoxCollider2D>();
            if (rule.AddCollider)
            {
                if (collider == null)
                {
                    collider = prefab.AddComponent<BoxCollider2D>();
                }

                ApplyCollider(collider, sprite);
            }
            else if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider, true);
            }

            EditorUtility.SetDirty(prefab);
        }

        private static void ApplyCollider(BoxCollider2D collider, Sprite sprite)
        {
            if (collider == null || sprite == null)
            {
                return;
            }

            var bounds = sprite.bounds;
            collider.offset = bounds.center;
            collider.size = bounds.size;
        }

        private static PrefabRule? MatchRule(string spriteName)
        {
            string lower = spriteName.ToLowerInvariant();
            foreach (var rule in PrefabRules)
            {
                if (rule.Tokens.Any(token => lower.Contains(token)))
                {
                    return rule;
                }
            }

            return null;
        }

        private static string ResolveSourceFolder()
        {
            foreach (var folder in SpriteSourceFolders)
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    return folder;
                }
            }

            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            string leaf = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent ?? "Assets", leaf);
        }

        private static void EnsureChild(Transform parent, string childName)
        {
            if (parent.Find(childName) != null)
            {
                return;
            }

            var child = new GameObject(childName);
            child.transform.SetParent(parent, false);
        }

        private readonly struct PrefabRule
        {
            public PrefabRule(string category, string sortingLayer, int sortingOrder, bool addCollider, string[] tokens)
            {
                Category = category;
                SortingLayer = sortingLayer;
                SortingOrder = sortingOrder;
                AddCollider = addCollider;
                Tokens = tokens ?? Array.Empty<string>();
            }

            public string Category { get; }
            public string SortingLayer { get; }
            public int SortingOrder { get; }
            public bool AddCollider { get; }
            public string[] Tokens { get; }
        }
    }
}
