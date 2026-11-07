using Pathless.Menu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Builds the menu: a test MenuDefinition (title + 10 options), the MenuRow prefab and the "MenuSystem" prefab
    /// (canvas, gold framed panel with corner diamonds, title with divider, option list, plus the input reader,
    /// controller, player lock and option handler, all wired together).
    ///
    /// Layout (canvas reference 640x360): a 150 pixel wide panel in the top-right corner whose height grows with
    /// the number of options; it slides in from the right edge (Animator). Same frame sprite and fonts as the
    /// dialogue box, so both UIs match.
    /// Safe to run again: the prefabs and the test definition are replaced.
    /// </summary>
    public static class MenuUiBuilder
    {
        /// <summary>Where the menu prefab is saved.</summary>
        public const string MenuPrefabPath = "Assets/Prefabs/UI/MenuSystem.prefab";

        /// <summary>Where the row prefab is saved.</summary>
        private const string RowPrefabPath = "Assets/Prefabs/UI/MenuRow.prefab";

        /// <summary>Where the test menu definition is saved.</summary>
        private const string DefinitionPath = "Assets/Data/Menu/Menu_Main_Claude.asset";

        /// <summary>Menu sprites folder.</summary>
        private const string MenuArt = "Assets/Art/UI/Menu";

        /// <summary>The gold frame sprite shared with the dialogue box (9-slice, border 8).</summary>
        private const string FrameSpritePath = "Assets/Art/UI/Dialogue/DialogueBox_Claude.png";

        /// <summary>Input Actions asset with the Menu map.</summary>
        private const string InputActionsPath = "Assets/Settings/MenuActions.inputactions";

        /// <summary>Row height in canvas pixels.</summary>
        private const float RowHeight = 18f;

        /// <summary>Title color (gold, #e8c872).</summary>
        private static readonly Color TitleColor = new Color(0.910f, 0.784f, 0.447f, 1f);

        /// <summary>Divider line color (#8a6a2e).</summary>
        private static readonly Color DividerColor = new Color(0.541f, 0.416f, 0.180f, 1f);

        /// <summary>Test options as { id, label } pairs.</summary>
        private static readonly string[,] TestOptions =
        {
            { "items", "Items" }, { "skills", "Skills" }, { "equipment", "Equipment" }, { "status", "Status" },
            { "formation", "Formation" }, { "quests", "Quests" }, { "map", "Map" }, { "settings", "Settings" },
            { "save", "Save" }, { "quit", "Quit" }
        };

        /// <summary>Menu entry that builds the menu animations and UI.</summary>
        [MenuItem("Tools/Pathless/Menu/Build Menu System (all steps)")]
        public static void BuildAllFromMenu()
        {
            AssetDatabase.ImportAsset("Assets/Art/UI", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            if (MenuAnimationBuilder.Build())
            {
                Build();
            }
        }

        /// <summary>Creates the definition, the row prefab and the menu prefab.</summary>
        /// <returns>True when the menu prefab was saved.</returns>
        public static bool Build()
        {
            if (TMP_Settings.instance == null)
            {
                Debug.LogError("MenuUiBuilder: TextMeshPro Essential Resources are missing. Use Window > TextMeshPro > Import TMP Essential Resources.");
                return false;
            }

            Sprite frame = AssetDatabase.LoadAssetAtPath<Sprite>(FrameSpritePath);
            Sprite highlight = AssetDatabase.LoadAssetAtPath<Sprite>(MenuArt + "/MenuHighlight_Claude.png");
            Sprite cursor = AssetDatabase.LoadAssetAtPath<Sprite>(MenuArt + "/MenuCursor_Claude.png");
            Sprite diamond = AssetDatabase.LoadAssetAtPath<Sprite>(MenuArt + "/OrnamentDiamond_Claude.png");
            RuntimeAnimatorController panelController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MenuAnimationBuilder.PanelControllerPath);
            RuntimeAnimatorController cursorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MenuAnimationBuilder.CursorControllerPath);
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);

            if (frame == null || highlight == null || cursor == null || diamond == null
                || panelController == null || cursorController == null || actions == null)
            {
                Debug.LogError("MenuUiBuilder: a sprite, controller or input asset is missing. Use 'Build Menu System (all steps)'.");
                return false;
            }

            TMP_FontAsset textFont = DialogueUiBuilder.EnsureFontAsset("VT323-Regular");
            TMP_FontAsset titleFont = DialogueUiBuilder.EnsureFontAsset("PressStart2P-Regular");

            MenuDefinition definition = BuildDefinition();
            MenuRowView rowPrefab = BuildRowPrefab(highlight, cursor, cursorController, textFont);

            // ---- Root with the logic components ----
            EditorAssetUtility.EnsureFolder("Assets/Prefabs/UI");
            GameObject root = new GameObject("MenuSystem");
            MenuInputReader inputReader = root.AddComponent<MenuInputReader>();
            MenuView view = root.AddComponent<MenuView>();
            MenuController controller = root.AddComponent<MenuController>();
            MenuPlayerLock playerLock = root.AddComponent<MenuPlayerLock>();
            MenuOptionHandler optionHandler = root.AddComponent<MenuOptionHandler>();

            // ---- Canvas (below the dialogue canvas, which uses sorting order 100) ----
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform));
            canvasObject.transform.SetParent(root.transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640f, 360f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            scaler.referencePixelsPerUnit = 32f;

            // ---- Panel: top-right corner, starts hidden beyond the right edge, height follows its content ----
            RectTransform panel = DialogueUiBuilder.CreateRect("Panel", canvasObject.transform);
            DialogueUiBuilder.SetAnchors(panel, Vector2.one, Vector2.one, Vector2.one);
            panel.sizeDelta = new Vector2(150f, 100f);
            panel.anchoredPosition = new Vector2(MenuAnimationBuilder.ClosedX, -14f);

            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = frame;
            panelImage.type = Image.Type.Sliced;
            panelImage.raycastTarget = false;

            VerticalLayoutGroup panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(12, 12, 12, 12);
            panelLayout.spacing = 6f;
            panelLayout.childAlignment = TextAnchor.UpperCenter;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            ContentSizeFitter panelFitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Animator panelAnimator = panel.gameObject.AddComponent<Animator>();
            panelAnimator.runtimeAnimatorController = panelController;

            // ---- Title ----
            TextMeshProUGUI title = DialogueUiBuilder.CreateText("Title", panel, 8f, TitleColor, TextAlignmentOptions.Center, titleFont);
            title.characterSpacing = 3f;
            title.text = "MENU";
            AddLayout(title.gameObject, -1f, 12f, 0f);

            // ---- Divider: line, diamond, line ----
            RectTransform divider = DialogueUiBuilder.CreateRect("Divider", panel);
            HorizontalLayoutGroup dividerLayout = divider.gameObject.AddComponent<HorizontalLayoutGroup>();
            dividerLayout.spacing = 4f;
            dividerLayout.childAlignment = TextAnchor.MiddleCenter;
            dividerLayout.childControlWidth = true;
            dividerLayout.childControlHeight = true;
            dividerLayout.childForceExpandWidth = false;
            dividerLayout.childForceExpandHeight = false;
            AddLayout(divider.gameObject, -1f, 7f, 0f);

            Image lineLeft = DialogueUiBuilder.CreateImage("LineLeft", divider, null, Image.Type.Simple);
            lineLeft.color = DividerColor;
            AddLayout(lineLeft.gameObject, 0f, 1f, 1f);

            Image divDiamond = DialogueUiBuilder.CreateImage("Diamond", divider, diamond, Image.Type.Simple);
            AddLayout(divDiamond.gameObject, 7f, 7f, 0f);

            Image lineRight = DialogueUiBuilder.CreateImage("LineRight", divider, null, Image.Type.Simple);
            lineRight.color = DividerColor;
            AddLayout(lineRight.gameObject, 0f, 1f, 1f);

            // ---- Option list (rows are created at runtime from the row prefab) ----
            RectTransform list = DialogueUiBuilder.CreateRect("List", panel);
            VerticalLayoutGroup listLayout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 1f;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            // ---- Gold diamonds on the panel corners (ignored by the layout) ----
            AddCornerDiamond("DiamondTL", panel, diamond, new Vector2(0f, 1f), new Vector2(2f, -2f));
            AddCornerDiamond("DiamondTR", panel, diamond, new Vector2(1f, 1f), new Vector2(-2f, -2f));
            AddCornerDiamond("DiamondBL", panel, diamond, new Vector2(0f, 0f), new Vector2(2f, 2f));
            AddCornerDiamond("DiamondBR", panel, diamond, new Vector2(1f, 0f), new Vector2(-2f, 2f));

            // ---- Wire the components together ----
            EditorAssetUtility.SetReference(inputReader, "actions", actions);

            EditorAssetUtility.SetReference(view, "titleLabel", title);
            EditorAssetUtility.SetReference(view, "rowPrefab", rowPrefab);
            EditorAssetUtility.SetReference(view, "listRoot", list);
            EditorAssetUtility.SetReference(view, "panelRect", panel);
            EditorAssetUtility.SetReference(view, "panelAnimator", panelAnimator);

            EditorAssetUtility.SetReference(controller, "definition", definition);
            EditorAssetUtility.SetReference(controller, "view", view);
            EditorAssetUtility.SetReference(controller, "input", inputReader);
            // The dialogue runner and the player are in the scene, so TestSceneBuilder assigns them.

            EditorAssetUtility.SetReference(playerLock, "controller", controller);
            EditorAssetUtility.SetReference(optionHandler, "controller", controller);

            PrefabUtility.SaveAsPrefabAsset(root, MenuPrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            Debug.Log("MenuUiBuilder: prefab saved at " + MenuPrefabPath);
            return true;
        }

        /// <summary>Creates the test menu definition asset.</summary>
        /// <returns>The saved definition.</returns>
        private static MenuDefinition BuildDefinition()
        {
            EditorAssetUtility.EnsureFolder("Assets/Data/Menu");
            AssetDatabase.DeleteAsset(DefinitionPath);

            // Fields are set while the object is still in memory, then it is saved (see DialogueTestDataBuilder).
            MenuDefinition definition = ScriptableObject.CreateInstance<MenuDefinition>();
            SerializedObject serialized = new SerializedObject(definition);
            serialized.FindProperty("title").stringValue = "MENU";

            SerializedProperty entries = serialized.FindProperty("entries");
            entries.arraySize = TestOptions.GetLength(0);
            for (int i = 0; i < TestOptions.GetLength(0); i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("id").stringValue = TestOptions[i, 0];
                entry.FindPropertyRelative("label").stringValue = TestOptions[i, 1];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(definition, DefinitionPath);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        /// <summary>Creates the prefab of one option row (highlight, bouncing cursor, label).</summary>
        /// <param name="highlight">Highlight bar sprite.</param>
        /// <param name="cursor">Cursor arrow sprite.</param>
        /// <param name="cursorController">Animator controller of the cursor bounce.</param>
        /// <param name="font">Text font.</param>
        /// <returns>The row component of the saved prefab.</returns>
        private static MenuRowView BuildRowPrefab(Sprite highlight, Sprite cursor, RuntimeAnimatorController cursorController, TMP_FontAsset font)
        {
            EditorAssetUtility.EnsureFolder("Assets/Prefabs/UI");

            RectTransform row = DialogueUiBuilder.CreateRect("MenuRow", null);
            MenuRowView rowView = row.gameObject.AddComponent<MenuRowView>();
            AddLayout(row.gameObject, -1f, RowHeight, 1f);

            Image highlightImage = DialogueUiBuilder.CreateImage("Highlight", row, highlight, Image.Type.Simple);
            DialogueUiBuilder.Stretch(highlightImage.rectTransform, 0f);

            Image cursorImage = DialogueUiBuilder.CreateImage("Cursor", row, cursor, Image.Type.Simple);
            DialogueUiBuilder.SetAnchors(cursorImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            cursorImage.rectTransform.sizeDelta = new Vector2(cursor.rect.width, cursor.rect.height);
            cursorImage.rectTransform.anchoredPosition = new Vector2(MenuAnimationBuilder.CursorRestX, 0f);
            Animator cursorAnimator = cursorImage.gameObject.AddComponent<Animator>();
            cursorAnimator.runtimeAnimatorController = cursorController;

            TextMeshProUGUI label = DialogueUiBuilder.CreateText("Label", row, 16f, new Color(0.937f, 0.890f, 0.769f, 1f), TextAlignmentOptions.MidlineLeft, font);
            DialogueUiBuilder.Stretch(label.rectTransform, 0f);
            label.rectTransform.offsetMin = new Vector2(18f, 0f);
            label.rectTransform.offsetMax = new Vector2(-4f, 0f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.text = "Option";

            EditorAssetUtility.SetReference(rowView, "label", label);
            EditorAssetUtility.SetReference(rowView, "highlight", highlightImage.gameObject);
            EditorAssetUtility.SetReference(rowView, "cursor", cursorImage.gameObject);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(row.gameObject, RowPrefabPath);
            Object.DestroyImmediate(row.gameObject);
            return prefab.GetComponent<MenuRowView>();
        }

        /// <summary>Adds a LayoutElement so a layout group knows how big an object should be.</summary>
        /// <param name="target">The object.</param>
        /// <param name="width">Preferred width (negative = not set).</param>
        /// <param name="height">Preferred height (negative = not set).</param>
        /// <param name="flexibleWidth">Share of extra width it may take (0 = none).</param>
        private static void AddLayout(GameObject target, float width, float height, float flexibleWidth)
        {
            LayoutElement element = target.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
            element.flexibleWidth = flexibleWidth;
            if (flexibleWidth > 0f)
            {
                element.minWidth = 0f;
            }
        }

        /// <summary>Adds a diamond ornament centered near a corner of the panel; the layout group ignores it.</summary>
        /// <param name="objectName">Object name.</param>
        /// <param name="panel">The panel.</param>
        /// <param name="diamond">Diamond sprite.</param>
        /// <param name="corner">Corner: (0,0) bottom-left ... (1,1) top-right.</param>
        /// <param name="offset">Distance from the corner (positive values move inward on both axes as given).</param>
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
