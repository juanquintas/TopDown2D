using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SmallScaleInc.TopDownPixelCharactersPack1.EditorTools
{
    public static class KnightPlayerPrefabBuilder
    {
        private const string PrefabFolder = "Assets/Prefabs";
        private const string PrefabPath = PrefabFolder + "/KnightPlayer.prefab";
        private const string SpriteSheetPath = "Assets/SmallScaleInt/TopDown 2D pixel Characters pack 1/Spritesheets/1Knight/Idle.png";
        private const string AnimatorControllerPath = "Assets/SmallScaleInt/TopDown 2D pixel Characters pack 1/Animations/1Knight.controller";

        [MenuItem("Tools/Create Knight Player Prefab", priority = 10)]
        public static void CreateKnightPlayerPrefab()
        {
            EnsurePrefabFolder();

            var baseSprite = LoadIdleSprite();
            if (baseSprite == null)
            {
                Debug.LogError($"KnightPlayerPrefabBuilder: No sprite frames found at {SpriteSheetPath}. Check the importer settings.");
                return;
            }

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            if (controller == null)
            {
                Debug.LogError($"KnightPlayerPrefabBuilder: Could not load animator controller at {AnimatorControllerPath}.");
                return;
            }

            var tempObject = new GameObject("KnightPlayer");
            try
            {
                var spriteRenderer = tempObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = baseSprite;

                var rigidbody = tempObject.AddComponent<Rigidbody2D>();
                rigidbody.gravityScale = 0f;
                rigidbody.freezeRotation = true;
                rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
                rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                tempObject.AddComponent<PlayerController>();
                tempObject.AddComponent<AnimationController>();

                var animator = tempObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;

                var prefab = PrefabUtility.SaveAsPrefabAsset(tempObject, PrefabPath);
                EditorGUIUtility.PingObject(prefab);
                Selection.activeObject = prefab;
                Debug.Log($"Knight player prefab created at {PrefabPath}");
            }
            finally
            {
                Object.DestroyImmediate(tempObject);
            }
        }

        private static void EnsurePrefabFolder()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                var parts = PrefabFolder.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    current = next;
                }
            }
        }

        private static Sprite LoadIdleSprite()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath);
            var sprites = assets.OfType<Sprite>();
            var idleSprite = sprites.FirstOrDefault(s => s.name.Contains("Idle"));
            return idleSprite ?? sprites.FirstOrDefault();
        }
    }
}
