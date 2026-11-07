using Pathless.Audio;
using Pathless.Cutscene;
using Pathless.Dialogue;
using Pathless.Party;
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

            // Extra command groups (@party, @bond ...). The director asks them for commands it does not know.
            PartyCommandModule partyModule = root.AddComponent<PartyCommandModule>();
            FormationCommandModule formationModule = root.AddComponent<FormationCommandModule>();
            AudioCommandModule audioModule = BuildAudio(root);

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
            SetModules(director, new CommandModule[] { partyModule, formationModule, audioModule });

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("CutsceneSystemBuilder: prefab saved at " + PrefabPath);
            return true;
        }

        /// <summary>
        /// Adds the audio system to the cutscene object: the music player (two sources for crossfading), the sound
        /// effect player and the command module (@music, @sfx), all using the AudioLibrary.
        /// </summary>
        /// <param name="root">The CutsceneSystem object.</param>
        /// <returns>The audio command module.</returns>
        private static AudioCommandModule BuildAudio(GameObject root)
        {
            AudioLibrary library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(AudioBuilder.LibraryPath);
            if (library == null)
            {
                Debug.LogWarning("CutsceneSystemBuilder: audio library not found, @music and @sfx will not find any sound. Run 'Build Audio Library' first.");
            }

            AudioSource musicA = CreateSource(root.transform, "MusicA");
            AudioSource musicB = CreateSource(root.transform, "MusicB");
            AudioSource sfxSource = CreateSource(root.transform, "Sfx");

            MusicPlayer music = root.AddComponent<MusicPlayer>();
            EditorAssetUtility.SetReference(music, "library", library);
            EditorAssetUtility.SetReference(music, "sourceA", musicA);
            EditorAssetUtility.SetReference(music, "sourceB", musicB);

            SfxPlayer sfx = root.AddComponent<SfxPlayer>();
            EditorAssetUtility.SetReference(sfx, "library", library);
            EditorAssetUtility.SetReference(sfx, "source", sfxSource);

            AudioCommandModule module = root.AddComponent<AudioCommandModule>();
            EditorAssetUtility.SetReference(module, "musicPlayer", music);
            EditorAssetUtility.SetReference(module, "sfxPlayer", sfx);
            return module;
        }

        /// <summary>Creates a child object with a silent 2D AudioSource.</summary>
        private static AudioSource CreateSource(Transform parent, string objectName)
        {
            GameObject child = new GameObject(objectName);
            child.transform.SetParent(parent, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        /// <summary>Fills the "modules" list of the director.</summary>
        /// <param name="director">The cutscene director.</param>
        /// <param name="modules">The command modules to give it.</param>
        private static void SetModules(CutsceneDirector director, CommandModule[] modules)
        {
            SerializedObject serialized = new SerializedObject(director);
            SerializedProperty list = serialized.FindProperty("modules");
            list.arraySize = modules.Length;
            for (int i = 0; i < modules.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = modules[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
