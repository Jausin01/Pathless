using Pathless.Cutscene;
using Pathless.SceneFlow;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Builds the CutsceneSystem prefab: the CutsceneDirector (runs the @commands), the CutsceneCamera, and a canvas
    /// with a full-screen black Image animated by the fade Animator (ScreenFader).
    /// The scene-specific links (dialogue runner, camera follow) are assigned by TestSceneBuilder.
    /// Safe to run again: the prefab is replaced.
    /// </summary>
    public static class CutsceneSystemBuilder
    {
        /// <summary>Where the prefab is saved.</summary>
        public const string PrefabPath = "Assets/Prefabs/UI/CutsceneSystem.prefab";

        /// <summary>Canvas sorting order: above the dialogue (100) so the fade also covers the dialogue box.</summary>
        private const int CanvasSortingOrder = 200;

        /// <summary>Menu entry to rebuild only the cutscene prefab.</summary>
        [MenuItem("Tools/Pathless/Cutscene/Build Cutscene System")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>Creates the prefab (the fade controller must exist already).</summary>
        /// <returns>True when the prefab was created.</returns>
        public static bool Build()
        {
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CutsceneAnimationBuilder.FadeControllerPath);
            if (controller == null)
            {
                Debug.LogError("CutsceneSystemBuilder: fade controller not found, build the cutscene animations first.");
                return false;
            }

            EditorAssetUtility.EnsureFolder("Assets/Prefabs/UI");

            GameObject root = new GameObject("CutsceneSystem");
            CutsceneDirector director = root.AddComponent<CutsceneDirector>();
            CutsceneCamera cutsceneCamera = root.AddComponent<CutsceneCamera>();
            SceneLoader sceneLoader = root.AddComponent<SceneLoader>();

            // ---- Canvas with the black fade image ----
            GameObject canvasObject = new GameObject("FadeCanvas", typeof(RectTransform));
            canvasObject.transform.SetParent(root.transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640f, 360f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            scaler.referencePixelsPerUnit = 32f;

            GameObject fadeObject = new GameObject("FadeImage", typeof(RectTransform));
            fadeObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = (RectTransform)fadeObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = fadeObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = false;

            Animator animator = fadeObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            ScreenFader fader = fadeObject.AddComponent<ScreenFader>();
            EditorAssetUtility.SetReference(fader, "animator", animator);
            EditorAssetUtility.SetReference(director, "fader", fader);
            EditorAssetUtility.SetReference(director, "cutsceneCamera", cutsceneCamera);
            EditorAssetUtility.SetReference(director, "sceneLoader", sceneLoader);
            EditorAssetUtility.SetReference(sceneLoader, "fader", fader);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("CutsceneSystemBuilder: prefab saved at " + PrefabPath);
            return true;
        }
    }
}
