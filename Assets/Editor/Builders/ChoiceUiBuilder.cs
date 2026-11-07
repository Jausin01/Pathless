using Pathless.Dialogue;
using Pathless.Menu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Builds the "ChoiceSystem" prefab: the list of options shown by a @choice. It is a framed panel in the
    /// bottom-right corner, above the dialogue box, that slides in from the right edge (Animator, reusing the menu
    /// panel controller) and uses the menu row prefab, so it looks like the menu. Width is 170 pixels, enough for
    /// options of about 20 characters; the panel grows in height with the number of options.
    /// Needs the menu to be built first (row prefab and panel controller). Safe to run again: the prefab is replaced.
    /// </summary>
    public static class ChoiceUiBuilder
    {
        /// <summary>Where the prefab is saved.</summary>
        public const string PrefabPath = "Assets/Prefabs/UI/ChoiceSystem.prefab";

        /// <summary>The menu row prefab reused for the options.</summary>
        private const string RowPrefabPath = "Assets/Prefabs/UI/MenuRow.prefab";

        /// <summary>The gold frame sprite shared with the dialogue box.</summary>
        private const string FrameSpritePath = "Assets/Art/UI/Dialogue/DialogueBox_Claude.png";

        /// <summary>Corner diamond sprite shared with the menu.</summary>
        private const string DiamondSpritePath = "Assets/Art/UI/Menu/OrnamentDiamond_Claude.png";

        /// <summary>Canvas sorting order: above the dialogue (100), below the screen fade (200).</summary>
        private const int CanvasSortingOrder = 110;

        /// <summary>Distance in canvas pixels from the bottom edge to the panel (the dialogue box is 144 pixels high).</summary>
        private const float PanelBottom = 152f;

        /// <summary>Menu entry to rebuild only this prefab.</summary>
        [MenuItem("Tools/Pathless/Dialogue/Build Choice System")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>Creates the prefab.</summary>
        /// <returns>True when the prefab was saved.</returns>
        public static bool Build()
        {
            Sprite frame = AssetDatabase.LoadAssetAtPath<Sprite>(FrameSpritePath);
            Sprite diamond = AssetDatabase.LoadAssetAtPath<Sprite>(DiamondSpritePath);
            MenuRowView rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath)?.GetComponent<MenuRowView>();
            RuntimeAnimatorController panelController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MenuAnimationBuilder.PanelControllerPath);

            if (frame == null || diamond == null || rowPrefab == null || panelController == null)
            {
                Debug.LogError("ChoiceUiBuilder: a sprite, the menu row prefab or the menu panel controller is missing. Build the menu first.");
                return false;
            }

            EditorAssetUtility.EnsureFolder("Assets/Prefabs/UI");

            GameObject root = new GameObject("ChoiceSystem");
            ChoiceView view = root.AddComponent<ChoiceView>();

            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform));
            canvasObject.transform.SetParent(root.transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640f, 360f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            scaler.referencePixelsPerUnit = 32f;

            // Panel anchored to the bottom-right corner; the menu panel animation only changes its X position.
            RectTransform panel = DialogueUiBuilder.CreateRect("Panel", canvasObject.transform);
            DialogueUiBuilder.SetAnchors(panel, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));
            panel.sizeDelta = new Vector2(170f, 40f);
            panel.anchoredPosition = new Vector2(MenuAnimationBuilder.ClosedX, PanelBottom);

            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = frame;
            panelImage.type = Image.Type.Sliced;
            panelImage.raycastTarget = false;

            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 1f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Animator animator = panel.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = panelController;

            AddCornerDiamond("DiamondTL", panel, diamond, new Vector2(0f, 1f), new Vector2(2f, -2f));
            AddCornerDiamond("DiamondTR", panel, diamond, new Vector2(1f, 1f), new Vector2(-2f, -2f));
            AddCornerDiamond("DiamondBL", panel, diamond, new Vector2(0f, 0f), new Vector2(2f, 2f));
            AddCornerDiamond("DiamondBR", panel, diamond, new Vector2(1f, 0f), new Vector2(-2f, 2f));

            EditorAssetUtility.SetReference(view, "rowPrefab", rowPrefab);
            EditorAssetUtility.SetReference(view, "listRoot", panel);
            EditorAssetUtility.SetReference(view, "panelRect", panel);
            EditorAssetUtility.SetReference(view, "panelAnimator", animator);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("ChoiceUiBuilder: prefab saved at " + PrefabPath);
            return true;
        }

        /// <summary>Adds a diamond ornament near a corner of the panel; the layout group ignores it.</summary>
        /// <param name="objectName">Object name.</param>
        /// <param name="panel">The panel.</param>
        /// <param name="diamond">Diamond sprite.</param>
        /// <param name="corner">Corner: (0,0) bottom-left ... (1,1) top-right.</param>
        /// <param name="offset">Distance from the corner.</param>
        private static void AddCornerDiamond(string objectName, RectTransform panel, Sprite diamond, Vector2 corner, Vector2 offset)
        {
            Image image = DialogueUiBuilder.CreateImage(objectName, panel, diamond, Image.Type.Simple);
            DialogueUiBuilder.SetAnchors(image.rectTransform, corner, corner, new Vector2(0.5f, 0.5f));
            image.rectTransform.sizeDelta = new Vector2(diamond.rect.width, diamond.rect.height);
            image.rectTransform.anchoredPosition = offset;
            image.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        }
    }
}
