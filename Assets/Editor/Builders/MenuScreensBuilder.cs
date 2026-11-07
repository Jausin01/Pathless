using Pathless.Menu;
using Pathless.Party;
using Pathless.Quests;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Builds the full menu screens and their test data:
    ///  - Test data: two provisional party members, a party roster, and a quest catalog with two test quests.
    ///  - The "MenuScreens" prefab: a canvas with the Status screen (four party rows, Final Fantasy VII style: face, name,
    ///    level, health and a second "Special" value with bars; the unused slots show "EMPTY SLOT") and the Quests screen (Active / Completed
    ///    tabs, list and description), plus the MenuScreenRouter that opens them from the menu.
    /// Same gold frame and fonts as the dialogue box and the menu. Needs the menu to be built first (row prefab).
    /// Safe to run again: the data and prefab are replaced.
    /// </summary>
    public static class MenuScreensBuilder
    {
        /// <summary>Where the prefab is saved.</summary>
        public const string PrefabPath = "Assets/Prefabs/UI/MenuScreens.prefab";

        /// <summary>Folder of the quest test data.</summary>
        private const string QuestFolder = "Assets/Data/Quests";

        /// <summary>The menu row prefab reused for the quest list.</summary>
        private const string RowPrefabPath = "Assets/Prefabs/UI/MenuRow.prefab";

        /// <summary>The gold frame sprite shared with the dialogue box.</summary>
        private const string FrameSpritePath = "Assets/Art/UI/Dialogue/DialogueBox_Claude.png";

        /// <summary>Canvas sorting order: above the menu (90), below the dialogue (100).</summary>
        private const int CanvasSortingOrder = 95;

        /// <summary>Panel size in canvas pixels.</summary>
        private static readonly Vector2 PanelSize = new Vector2(600f, 328f);

        /// <summary>Title color (gold).</summary>
        private static readonly Color TitleColor = new Color(0.910f, 0.784f, 0.447f, 1f);

        /// <summary>Normal text color (cream).</summary>
        private static readonly Color LightColor = new Color(0.937f, 0.890f, 0.769f, 1f);

        /// <summary>Dim text color.</summary>
        private static readonly Color DimColor = new Color(0.55f, 0.50f, 0.42f, 1f);

        /// <summary>Divider color.</summary>
        private static readonly Color DividerColor = new Color(0.541f, 0.416f, 0.180f, 1f);

        /// <summary>Dark color behind the bars.</summary>
        private static readonly Color BarBackColor = new Color(0.10f, 0.08f, 0.12f, 1f);

        /// <summary>Health bar color.</summary>
        private static readonly Color HpColor = new Color(0.36f, 0.75f, 0.40f, 1f);

        /// <summary>Menu entry to rebuild only the screens.</summary>
        [MenuItem("Tools/Pathless/Menu/Build Menu Screens")]
        public static void BuildFromMenu()
        {
            if (MenuScreenAnimationBuilder.Build())
            {
                Build();
            }
        }

        /// <summary>Creates the test data and the prefab.</summary>
        /// <returns>True when the prefab was saved.</returns>
        public static bool Build()
        {
            Sprite frame = AssetDatabase.LoadAssetAtPath<Sprite>(FrameSpritePath);
            MenuRowView rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath)?.GetComponent<MenuRowView>();
            RuntimeAnimatorController screenController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MenuScreenAnimationBuilder.ControllerPath);

            if (frame == null || rowPrefab == null || screenController == null)
            {
                Debug.LogError("MenuScreensBuilder: the frame sprite, the menu row prefab or the screen controller is missing. Build the menu first.");
                return false;
            }

            TMP_FontAsset textFont = DialogueUiBuilder.EnsureFontAsset("VT323-Regular");
            TMP_FontAsset titleFont = DialogueUiBuilder.EnsureFontAsset("PressStart2P-Regular");

            PartyRoster roster = PartyDataBuilder.Build();
            QuestCatalog catalog = BuildQuestData();
            if (roster == null || catalog == null)
            {
                return false;
            }

            EditorAssetUtility.EnsureFolder("Assets/Prefabs/UI");
            GameObject root = new GameObject("MenuScreens");
            MenuScreenRouter router = root.AddComponent<MenuScreenRouter>();

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

            StatusScreen status = BuildStatusScreen(canvasObject.transform, frame, screenController, textFont, titleFont, roster);
            QuestsScreen quests = BuildQuestsScreen(canvasObject.transform, frame, screenController, textFont, titleFont, catalog, rowPrefab);

            // Tell the router which screens it can open. The menu and input reader are in the scene, so TestSceneBuilder assigns them.
            SerializedObject routerObject = new SerializedObject(router);
            SerializedProperty screens = routerObject.FindProperty("screens");
            screens.arraySize = 2;
            screens.GetArrayElementAtIndex(0).objectReferenceValue = status;
            screens.GetArrayElementAtIndex(1).objectReferenceValue = quests;
            routerObject.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("MenuScreensBuilder: prefab saved at " + PrefabPath);
            return true;
        }

        // ------------------------------------------------------------------------------------------------ data

        /// <summary>Creates the test quest catalog.</summary>
        /// <returns>The catalog.</returns>
        private static QuestCatalog BuildQuestData()
        {
            EditorAssetUtility.EnsureFolder(QuestFolder);
            string path = QuestFolder + "/QuestCatalog_Test_Claude.asset";
            AssetDatabase.DeleteAsset(path);

            string[,] quests =
            {
                { "find_bell", "The Lost Bell", "The Elder lost the bell of the old tower. He asked for your help finding it." },
                { "deliver_message", "A Message Delivered", "The Messenger had an important message for you. You listened to all of it." }
            };

            QuestCatalog catalog = ScriptableObject.CreateInstance<QuestCatalog>();
            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty list = serialized.FindProperty("quests");
            list.arraySize = quests.GetLength(0);
            for (int i = 0; i < quests.GetLength(0); i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("id").stringValue = quests[i, 0];
                entry.FindPropertyRelative("title").stringValue = quests[i, 1];
                entry.FindPropertyRelative("description").stringValue = quests[i, 2];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(catalog, path);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        // ------------------------------------------------------------------------------------------------ screens

        /// <summary>
        /// Creates the parts every screen has: a full-screen root (faded by the Animator through a CanvasGroup),
        /// a dark backdrop, the framed panel, the title and the hint line at the bottom.
        /// </summary>
        /// <returns>The panel that holds the screen's content.</returns>
        private static RectTransform CreateScreenRoot(string screenName, Transform canvas, Sprite frame,
            RuntimeAnimatorController controller, TMP_FontAsset textFont, TMP_FontAsset titleFont, string title, string hint,
            out Animator animator)
        {
            RectTransform root = DialogueUiBuilder.CreateRect(screenName, canvas);
            DialogueUiBuilder.Stretch(root, 0f);

            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            animator = root.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            Image backdrop = DialogueUiBuilder.CreateImage("Backdrop", root, null, Image.Type.Simple);
            DialogueUiBuilder.Stretch(backdrop.rectTransform, 0f);
            backdrop.color = new Color(0.04f, 0.03f, 0.07f, 0.94f);

            RectTransform panel = DialogueUiBuilder.CreateRect("Panel", root);
            DialogueUiBuilder.SetAnchors(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            panel.sizeDelta = PanelSize;
            panel.anchoredPosition = Vector2.zero;
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = frame;
            panelImage.type = Image.Type.Sliced;
            panelImage.raycastTarget = false;

            TextMeshProUGUI titleText = DialogueUiBuilder.CreateText("Title", panel, 8f, TitleColor, TextAlignmentOptions.Center, titleFont);
            titleText.characterSpacing = 3f;
            titleText.text = title;
            PlaceTopLeft(titleText.rectTransform, 0f, 12f, PanelSize.x, 16f);

            TextMeshProUGUI hintText = DialogueUiBuilder.CreateText("Hint", panel, 14f, DimColor, TextAlignmentOptions.Center, textFont);
            hintText.text = hint;
            DialogueUiBuilder.SetAnchors(hintText.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero);
            hintText.rectTransform.sizeDelta = new Vector2(PanelSize.x, 14f);
            hintText.rectTransform.anchoredPosition = new Vector2(0f, 8f);

            return panel;
        }

        /// <summary>Builds the Status screen: the party as rows stacked vertically (Final Fantasy VII style).</summary>
        private static StatusScreen BuildStatusScreen(Transform canvas, Sprite frame, RuntimeAnimatorController controller,
            TMP_FontAsset textFont, TMP_FontAsset titleFont, PartyRoster roster)
        {
            RectTransform panel = CreateScreenRoot("StatusScreen", canvas, frame, controller, textFont, titleFont, "STATUS",
                "Esc / B: back", out Animator animator);
            StatusScreen screen = panel.parent.gameObject.AddComponent<StatusScreen>();

            RectTransform rows = DialogueUiBuilder.CreateRect("Rows", panel);
            PlaceTopLeft(rows, 14f, 36f, PanelSize.x - 28f, 280f);
            VerticalLayoutGroup rowLayout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
            rowLayout.spacing = 2f;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;

            PartyMemberCard[] cards = new PartyMemberCard[PartyRoster.MaxSlots];
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i] = BuildRow(rows, i + 1, textFont);
            }

            EditorAssetUtility.SetString(screen, "screenId", "status");
            EditorAssetUtility.SetReference(screen, "animator", animator);
            EditorAssetUtility.SetReference(screen, "roster", roster);

            SerializedObject serialized = new SerializedObject(screen);
            SerializedProperty cardList = serialized.FindProperty("cards");
            cardList.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++)
            {
                cardList.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return screen;
        }

        /// <summary>
        /// Builds one party row (572 x 68 pixels): face on the left; next to it name, level, health and special lines
        /// stacked; to the right the health and special bars, aligned with their lines. The empty placeholder is a
        /// second child shown when the slot has no member. The upper right of the row is left free for future data
        /// (for example an experience bar).
        /// </summary>
        private static PartyMemberCard BuildRow(Transform parent, int number, TMP_FontAsset font)
        {
            RectTransform row = DialogueUiBuilder.CreateRect("Row" + number, parent);
            Image rowImage = row.gameObject.AddComponent<Image>();
            rowImage.color = new Color(0.10f, 0.08f, 0.15f, 0.80f);
            rowImage.raycastTarget = false;
            PartyMemberCard cardView = row.gameObject.AddComponent<PartyMemberCard>();

            RectTransform content = DialogueUiBuilder.CreateRect("Content", row);
            DialogueUiBuilder.Stretch(content, 0f);

            Image portrait = DialogueUiBuilder.CreateImage("Portrait", content, null, Image.Type.Simple);
            PlaceTopLeft(portrait.rectTransform, 6f, 2f, 64f, 64f);
            portrait.preserveAspect = true;

            TextMeshProUGUI nameLabel = CreateRowText("Name", content, font, 18f, TitleColor, TextAlignmentOptions.Left, 78f, 1f, 190f, 16f);
            TextMeshProUGUI levelLabel = CreateRowText("Level", content, font, 16f, LightColor, TextAlignmentOptions.Left, 78f, 17f, 190f, 15f);

            CreateRowText("HpLabel", content, font, 16f, DimColor, TextAlignmentOptions.Left, 78f, 34f, 62f, 15f).text = "HP";
            TextMeshProUGUI hpValue = CreateRowText("HpValue", content, font, 16f, LightColor, TextAlignmentOptions.Left, 142f, 34f, 120f, 15f);
            RectTransform hpFill = CreateBar("HpBar", content, 270f, 38f, 292f, 8f, HpColor, out _);

            TextMeshProUGUI specialName = CreateRowText("SpecialLabel", content, font, 16f, DimColor, TextAlignmentOptions.Left, 78f, 50f, 62f, 15f);
            TextMeshProUGUI specialValue = CreateRowText("SpecialValue", content, font, 16f, LightColor, TextAlignmentOptions.Left, 142f, 50f, 120f, 15f);
            RectTransform specialFill = CreateBar("SpecialBar", content, 270f, 54f, 292f, 8f, new Color(0.62f, 0.38f, 0.85f, 1f), out Image specialFillImage);

            RectTransform empty = DialogueUiBuilder.CreateRect("Empty", row);
            DialogueUiBuilder.Stretch(empty, 0f);
            TextMeshProUGUI emptyLabel = DialogueUiBuilder.CreateText("Label", empty, 16f, DimColor, TextAlignmentOptions.MidlineLeft, font);
            DialogueUiBuilder.Stretch(emptyLabel.rectTransform, 0f);
            emptyLabel.rectTransform.offsetMin = new Vector2(78f, 0f);
            emptyLabel.text = "- EMPTY SLOT -";

            EditorAssetUtility.SetReference(cardView, "contentRoot", content.gameObject);
            EditorAssetUtility.SetReference(cardView, "emptyRoot", empty.gameObject);
            EditorAssetUtility.SetReference(cardView, "portrait", portrait);
            EditorAssetUtility.SetReference(cardView, "nameLabel", nameLabel);
            EditorAssetUtility.SetReference(cardView, "levelLabel", levelLabel);
            EditorAssetUtility.SetReference(cardView, "hpFill", hpFill);
            EditorAssetUtility.SetReference(cardView, "hpValueLabel", hpValue);
            EditorAssetUtility.SetReference(cardView, "specialNameLabel", specialName);
            EditorAssetUtility.SetReference(cardView, "specialFill", specialFill);
            EditorAssetUtility.SetReference(cardView, "specialFillImage", specialFillImage);
            EditorAssetUtility.SetReference(cardView, "specialValueLabel", specialValue);
            return cardView;
        }

        /// <summary>Creates a text line inside a row, placed by its top-left corner.</summary>
        private static TextMeshProUGUI CreateRowText(string objectName, Transform parent, TMP_FontAsset font, float size,
            Color color, TextAlignmentOptions alignment, float x, float y, float width, float height)
        {
            TextMeshProUGUI text = DialogueUiBuilder.CreateText(objectName, parent, size, color, alignment, font);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            PlaceTopLeft(text.rectTransform, x, y, width, height);
            return text;
        }

        /// <summary>Creates a bar: a dark background with a colored fill that PartyMemberCard resizes.</summary>
        /// <returns>The fill rect (its right anchor is moved to show the amount).</returns>
        private static RectTransform CreateBar(string objectName, Transform parent, float x, float y, float width, float height,
            Color color, out Image fillImage)
        {
            Image back = DialogueUiBuilder.CreateImage(objectName, parent, null, Image.Type.Simple);
            back.color = BarBackColor;
            PlaceTopLeft(back.rectTransform, x, y, width, height);

            fillImage = DialogueUiBuilder.CreateImage("Fill", back.transform, null, Image.Type.Simple);
            fillImage.color = color;
            RectTransform fill = fillImage.rectTransform;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            return fill;
        }

        /// <summary>Builds the Quests screen: tabs, list on the left, description on the right.</summary>
        private static QuestsScreen BuildQuestsScreen(Transform canvas, Sprite frame, RuntimeAnimatorController controller,
            TMP_FontAsset textFont, TMP_FontAsset titleFont, QuestCatalog catalog, MenuRowView rowPrefab)
        {
            RectTransform panel = CreateScreenRoot("QuestsScreen", canvas, frame, controller, textFont, titleFont, "QUESTS",
                "Left / Right: tabs     Up / Down: select     Esc / B: back", out Animator animator);
            QuestsScreen screen = panel.parent.gameObject.AddComponent<QuestsScreen>();

            TextMeshProUGUI activeTab = DialogueUiBuilder.CreateText("TabActive", panel, 18f, TitleColor, TextAlignmentOptions.Left, textFont);
            activeTab.text = "ACTIVE";
            PlaceTopLeft(activeTab.rectTransform, 18f, 36f, 100f, 20f);

            TextMeshProUGUI completedTab = DialogueUiBuilder.CreateText("TabCompleted", panel, 18f, DimColor, TextAlignmentOptions.Left, textFont);
            completedTab.text = "COMPLETED";
            PlaceTopLeft(completedTab.rectTransform, 118f, 36f, 110f, 20f);

            RectTransform list = DialogueUiBuilder.CreateRect("List", panel);
            PlaceTopLeft(list, 14f, 62f, 206f, 240f);
            VerticalLayoutGroup listLayout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 1f;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            TextMeshProUGUI emptyLabel = DialogueUiBuilder.CreateText("Empty", panel, 16f, DimColor, TextAlignmentOptions.Left, textFont);
            emptyLabel.text = "No quests here.";
            PlaceTopLeft(emptyLabel.rectTransform, 22f, 68f, 200f, 20f);

            Image divider = DialogueUiBuilder.CreateImage("Divider", panel, null, Image.Type.Simple);
            divider.color = DividerColor;
            PlaceTopLeft(divider.rectTransform, 228f, 40f, 1f, 262f);

            TextMeshProUGUI title = DialogueUiBuilder.CreateText("QuestTitle", panel, 22f, TitleColor, TextAlignmentOptions.TopLeft, textFont);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            PlaceTopLeft(title.rectTransform, 242f, 40f, 344f, 24f);

            TextMeshProUGUI description = DialogueUiBuilder.CreateText("QuestDescription", panel, 18f, LightColor, TextAlignmentOptions.TopLeft, textFont);
            description.textWrappingMode = TextWrappingModes.Normal;
            PlaceTopLeft(description.rectTransform, 242f, 70f, 344f, 220f);

            EditorAssetUtility.SetString(screen, "screenId", "quests");
            EditorAssetUtility.SetReference(screen, "animator", animator);
            EditorAssetUtility.SetReference(screen, "catalog", catalog);
            EditorAssetUtility.SetReference(screen, "rowPrefab", rowPrefab);
            EditorAssetUtility.SetReference(screen, "listRoot", list);
            EditorAssetUtility.SetReference(screen, "titleLabel", title);
            EditorAssetUtility.SetReference(screen, "descriptionLabel", description);
            EditorAssetUtility.SetReference(screen, "emptyLabel", emptyLabel.gameObject);

            SerializedObject serialized = new SerializedObject(screen);
            SerializedProperty tabs = serialized.FindProperty("tabLabels");
            tabs.arraySize = 2;
            tabs.GetArrayElementAtIndex(0).objectReferenceValue = activeTab;
            tabs.GetArrayElementAtIndex(1).objectReferenceValue = completedTab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return screen;
        }

        /// <summary>Places a rect by its top-left corner inside its parent (x right, y down from the corner).</summary>
        private static void PlaceTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            DialogueUiBuilder.SetAnchors(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
        }
    }
}
