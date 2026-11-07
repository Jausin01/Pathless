using Pathless.Dialogue;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Builds the "DialogueSystem" prefab: the Screen Space Overlay canvas with the dialogue box
    /// (gold frame, portrait area, text box, name plate, continue arrow) plus the runner, input reader,
    /// player lock and audio source, all wired together.
    ///
    /// Layout (canvas reference 640x360, 1 canvas unit = 1 pixel):
    ///   Panel 624x144 at the bottom, 8 px above the screen edge
    ///   |- PortraitRoot 144x144 (left): stripes background, 128x128 portrait, gold frame
    ///   |- TextBox 476x144 (right of the portrait): gold frame + dark background + text + arrow
    ///   |- NamePlate: sits on the top-left border of the text box and grows with the name
    /// The Panel is moved by an Animator (slide in/out); the arrow is animated by its own Animator.
    /// Safe to run again: the prefab is replaced.
    /// </summary>
    public static class DialogueUiBuilder
    {
        /// <summary>Where the prefab is saved.</summary>
        public const string DialoguePrefabPath = "Assets/Prefabs/UI/DialogueSystem.prefab";

        /// <summary>Folder with the generated dialogue UI sprites.</summary>
        private const string UiArtFolder = "Assets/Art/UI/Dialogue";

        /// <summary>Silhouette shown for speakers without portrait art.</summary>
        private const string DefaultPortraitPath = "Assets/Art/Portraits/Default_Claude/Portrait_Default_Silhouette_Claude.png";

        /// <summary>Voice blip used for speakers without a character asset.</summary>
        private const string DefaultBlipPath = "Assets/Audio/Dialogue/TextBlip_Claude.wav";

        /// <summary>Input Actions asset with the Dialogue map.</summary>
        private const string InputActionsPath = "Assets/Settings/DialogueActions.inputactions";

        /// <summary>Folder with the dialogue fonts (VT323 for the text, Press Start 2P for the name).</summary>
        private const string FontFolder = "Assets/Art/UI/Fonts";

        /// <summary>Text color (warm white, #ece2c8).</summary>
        private static readonly Color TextColor = new Color(0.925f, 0.886f, 0.784f, 1f);

        /// <summary>Name plate text color (light gold, #f6e6b0).</summary>
        private static readonly Color NameColor = new Color(0.965f, 0.902f, 0.690f, 1f);

        /// <summary>Menu entry that builds the dialogue animations, data and UI prefab.</summary>
        [MenuItem("Tools/Pathless/Dialogue/Build Dialogue System (all steps)")]
        public static void BuildAllFromMenu()
        {
            AssetDatabase.ImportAsset("Assets/Art/UI", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/Art/Portraits", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            if (DialogueAnimationBuilder.Build() && DialogueTestDataBuilder.Build())
            {
                Build();
            }
        }

        /// <summary>Creates the prefab.</summary>
        /// <returns>True when the prefab was saved.</returns>
        public static bool Build()
        {
            // The dialogue text needs the TextMeshPro default font, which comes with the Essential Resources.
            if (TMP_Settings.instance == null)
            {
                Debug.LogError("DialogueUiBuilder: TextMeshPro Essential Resources are missing. Use Window > TextMeshPro > Import TMP Essential Resources, then run this again.");
                return false;
            }

            Sprite boxSprite = LoadSprite("DialogueBox_Claude.png");
            Sprite plateSprite = LoadSprite("NamePlate_Claude.png");
            Sprite stripesSprite = LoadSprite("PortraitStripes_Claude.png");
            Sprite arrowSprite = LoadSprite("ContinueArrow_Claude.png");
            Sprite studSprite = LoadSprite("OrnamentStud_Claude.png");
            Sprite flameSprite = LoadSprite("NameFlame_Claude.png");
            Sprite swirlBL = LoadSprite("OrnamentSwirl_BL_Claude.png");
            Sprite swirlBR = LoadSprite("OrnamentSwirl_BR_Claude.png");
            Sprite swirlTL = LoadSprite("OrnamentSwirl_TL_Claude.png");
            Sprite swirlTR = LoadSprite("OrnamentSwirl_TR_Claude.png");
            RuntimeAnimatorController flameController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DialogueAnimationBuilder.FlameControllerPath);
            TMP_FontAsset textFont = EnsureFontAsset("VT323-Regular");
            TMP_FontAsset nameFont = EnsureFontAsset("PressStart2P-Regular");
            RuntimeAnimatorController boxController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DialogueAnimationBuilder.BoxControllerPath);
            RuntimeAnimatorController arrowController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DialogueAnimationBuilder.IndicatorControllerPath);
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);

            if (boxSprite == null || plateSprite == null || stripesSprite == null || arrowSprite == null
                || studSprite == null || flameSprite == null || swirlBL == null || swirlBR == null || swirlTL == null || swirlTR == null
                || boxController == null || arrowController == null || flameController == null || actions == null)
            {
                Debug.LogError("DialogueUiBuilder: a sprite, controller or input asset is missing. Use 'Build Dialogue System (all steps)'.");
                return false;
            }

            // ---- Root with the logic components ----
            GameObject root = new GameObject("DialogueSystem");
            AudioSource voice = root.AddComponent<AudioSource>();
            voice.playOnAwake = false;

            DialogueInputReader inputReader = root.AddComponent<DialogueInputReader>();
            DialogueView view = root.AddComponent<DialogueView>();
            DialogueRunner runner = root.AddComponent<DialogueRunner>();
            DialoguePlayerLock playerLock = root.AddComponent<DialoguePlayerLock>();

            // ---- Canvas ----
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform));
            canvasObject.transform.SetParent(root.transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640f, 360f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            scaler.referencePixelsPerUnit = 32f;

            // ---- Panel (moved by the box Animator) ----
            RectTransform panel = CreateRect("Panel", canvasObject.transform);
            SetAnchors(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            panel.sizeDelta = new Vector2(624f, 144f);
            panel.anchoredPosition = new Vector2(0f, DialogueAnimationBuilder.ClosedY);
            Animator boxAnimator = panel.gameObject.AddComponent<Animator>();
            boxAnimator.runtimeAnimatorController = boxController;

            // ---- Portrait area ----
            RectTransform portraitRoot = CreateRect("PortraitRoot", panel);
            SetAnchors(portraitRoot, Vector2.zero, Vector2.zero, Vector2.zero);
            portraitRoot.sizeDelta = new Vector2(144f, 144f);
            portraitRoot.anchoredPosition = Vector2.zero;

            Image stripes = CreateImage("Stripes", portraitRoot, stripesSprite, Image.Type.Tiled);
            Stretch(stripes.rectTransform, 8f);

            Image portrait = CreateImage("Portrait", portraitRoot, null, Image.Type.Simple);
            portrait.preserveAspect = true;
            SetAnchors(portrait.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            portrait.rectTransform.sizeDelta = new Vector2(128f, 128f);
            portrait.rectTransform.anchoredPosition = Vector2.zero;

            // The same gold frame sprite is reused for the portrait; only its border is drawn (center is empty).
            Image portraitFrame = CreateImage("Frame", portraitRoot, boxSprite, Image.Type.Sliced);
            portraitFrame.fillCenter = false;
            Stretch(portraitFrame.rectTransform, 0f);

            // Gold swirl ornaments on the four corners of the portrait frame.
            CreateCornerImage("SwirlBL", portraitRoot, swirlBL, new Vector2(0f, 0f), new Vector2(1f, 1f));
            CreateCornerImage("SwirlBR", portraitRoot, swirlBR, new Vector2(1f, 0f), new Vector2(-1f, 1f));
            CreateCornerImage("SwirlTL", portraitRoot, swirlTL, new Vector2(0f, 1f), new Vector2(1f, -1f));
            CreateCornerImage("SwirlTR", portraitRoot, swirlTR, new Vector2(1f, 1f), new Vector2(-1f, -1f));

            // ---- Text box ----
            RectTransform textBox = CreateRect("TextBox", panel);
            SetAnchors(textBox, Vector2.zero, Vector2.zero, Vector2.zero);
            textBox.sizeDelta = new Vector2(476f, 144f);
            textBox.anchoredPosition = new Vector2(148f, 0f);
            Image textBoxImage = textBox.gameObject.AddComponent<Image>();
            textBoxImage.sprite = boxSprite;
            textBoxImage.type = Image.Type.Sliced;
            textBoxImage.raycastTarget = false;

            TextMeshProUGUI dialogueText = CreateText("Text", textBox, 16f, TextColor, TextAlignmentOptions.TopLeft, textFont);
            dialogueText.lineSpacing = 8f;
            Stretch(dialogueText.rectTransform, 0f);
            dialogueText.rectTransform.offsetMin = new Vector2(18f, 16f);
            dialogueText.rectTransform.offsetMax = new Vector2(-18f, -18f);
            dialogueText.textWrappingMode = TextWrappingModes.Normal;
            dialogueText.overflowMode = TextOverflowModes.Truncate;
            TypewriterEffect typewriter = dialogueText.gameObject.AddComponent<TypewriterEffect>();

            // Gold studs on the four corners of the text box.
            CreateCornerImage("StudBL", textBox, studSprite, new Vector2(0f, 0f), new Vector2(2f, 2f));
            CreateCornerImage("StudBR", textBox, studSprite, new Vector2(1f, 0f), new Vector2(-2f, 2f));
            CreateCornerImage("StudTL", textBox, studSprite, new Vector2(0f, 1f), new Vector2(2f, -2f));
            CreateCornerImage("StudTR", textBox, studSprite, new Vector2(1f, 1f), new Vector2(-2f, -2f));

            // ---- Continue arrow (own Animator) ----
            Image arrow = CreateImage("ContinueArrow", textBox, arrowSprite, Image.Type.Simple);
            SetAnchors(arrow.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f));
            arrow.rectTransform.sizeDelta = new Vector2(10f, 6f);
            arrow.rectTransform.anchoredPosition = new Vector2(-20f, 12f);
            arrow.enabled = false;
            Animator arrowAnimator = arrow.gameObject.AddComponent<Animator>();
            arrowAnimator.runtimeAnimatorController = arrowController;

            // ---- Name plate (last child so it draws on top of the text box border) ----
            RectTransform namePlate = CreateRect("NamePlate", panel);
            SetAnchors(namePlate, Vector2.zero, Vector2.zero, Vector2.zero);
            namePlate.anchoredPosition = new Vector2(138f, 130f);

            Image plateImage = namePlate.gameObject.AddComponent<Image>();
            plateImage.sprite = plateSprite;
            plateImage.type = Image.Type.Sliced;
            plateImage.raycastTarget = false;

            HorizontalLayoutGroup plateLayout = namePlate.gameObject.AddComponent<HorizontalLayoutGroup>();
            plateLayout.padding = new RectOffset(9, 9, 5, 5);
            plateLayout.spacing = 6f;
            plateLayout.childAlignment = TextAnchor.MiddleCenter;
            plateLayout.childControlWidth = true;
            plateLayout.childControlHeight = true;
            plateLayout.childForceExpandWidth = false;
            plateLayout.childForceExpandHeight = false;

            ContentSizeFitter plateFitter = namePlate.gameObject.AddComponent<ContentSizeFitter>();
            plateFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            plateFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI nameText = CreateText("Name", namePlate, 8f, NameColor, TextAlignmentOptions.Left, nameFont);
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.text = "Name";

            // Small flame next to the name; it flickers through its own Animator.
            Image flame = CreateImage("Flame", namePlate, flameSprite, Image.Type.Simple);
            flame.rectTransform.pivot = new Vector2(0.5f, 0f);
            LayoutElement flameLayout = flame.gameObject.AddComponent<LayoutElement>();
            flameLayout.preferredWidth = 7f;
            flameLayout.preferredHeight = 10f;
            Animator flameAnimator = flame.gameObject.AddComponent<Animator>();
            flameAnimator.runtimeAnimatorController = flameController;

            // ---- Wire the components together ----
            EditorAssetUtility.SetReference(inputReader, "actions", actions);

            EditorAssetUtility.SetReference(view, "nameLabel", nameText);
            EditorAssetUtility.SetReference(view, "namePlate", namePlate.gameObject);
            EditorAssetUtility.SetReference(view, "typewriter", typewriter);
            EditorAssetUtility.SetReference(view, "portraitRoot", portraitRoot.gameObject);
            EditorAssetUtility.SetReference(view, "portraitImage", portrait);
            EditorAssetUtility.SetReference(view, "boxAnimator", boxAnimator);
            EditorAssetUtility.SetReference(view, "indicatorAnimator", arrowAnimator);
            EditorAssetUtility.SetReference(view, "voiceSource", voice);
            EditorAssetUtility.SetReference(view, "textBoxRect", textBox);

            Sprite silhouette = AssetDatabase.LoadAssetAtPath<Sprite>(DefaultPortraitPath);
            if (silhouette == null)
            {
                Debug.LogWarning("DialogueUiBuilder: default silhouette portrait not found: " + DefaultPortraitPath);
            }

            EditorAssetUtility.SetReference(view, "defaultPortrait", silhouette);
            EditorAssetUtility.SetReference(view, "defaultVoiceBlip", AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultBlipPath));

            EditorAssetUtility.SetReference(runner, "view", view);
            EditorAssetUtility.SetReference(runner, "input", inputReader);

            // Lets the runner turn names written in .dialogue files into character assets.
            DialogueCharacterLibrary library = AssetDatabase.LoadAssetAtPath<DialogueCharacterLibrary>(DialogueCharacterLibraryBuilder.LibraryPath);
            if (library == null)
            {
                Debug.LogWarning("DialogueUiBuilder: character library not found; names from .dialogue files will not resolve.");
            }

            EditorAssetUtility.SetReference(runner, "characterLibrary", library);

            // The lock's player reference is filled by the scene builder, because the player lives in the scene.
            EditorAssetUtility.SetReference(playerLock, "runner", runner);

            // ---- Save ----
            EditorAssetUtility.EnsureFolder("Assets/Prefabs/UI");
            PrefabUtility.SaveAsPrefabAsset(root, DialoguePrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            Debug.Log("DialogueUiBuilder: prefab saved at " + DialoguePrefabPath);
            return true;
        }

        /// <summary>Loads one UI sprite by file name.</summary>
        /// <param name="fileName">File name inside the dialogue UI art folder.</param>
        /// <returns>The sprite, or null if missing.</returns>
        private static Sprite LoadSprite(string fileName)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiArtFolder}/{fileName}");
            if (sprite == null)
            {
                Debug.LogError("DialogueUiBuilder: sprite not found: " + fileName);
            }

            return sprite;
        }

        /// <summary>Creates an empty UI object with a RectTransform.</summary>
        /// <param name="objectName">Object name.</param>
        /// <param name="parent">Parent transform.</param>
        /// <returns>The new RectTransform.</returns>
        public static RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject created = new GameObject(objectName, typeof(RectTransform));
            created.transform.SetParent(parent, false);
            return (RectTransform)created.transform;
        }

        /// <summary>Creates a UI Image object that does not block clicks.</summary>
        /// <param name="objectName">Object name.</param>
        /// <param name="parent">Parent transform.</param>
        /// <param name="sprite">Sprite to show (can be null).</param>
        /// <param name="type">Image type (Simple, Sliced, Tiled).</param>
        /// <returns>The new Image.</returns>
        public static Image CreateImage(string objectName, Transform parent, Sprite sprite, Image.Type type)
        {
            RectTransform rect = CreateRect(objectName, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = type;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Creates a TextMeshPro text with the default font.</summary>
        /// <param name="objectName">Object name.</param>
        /// <param name="parent">Parent transform.</param>
        /// <param name="size">Font size.</param>
        /// <param name="color">Text color.</param>
        /// <param name="alignment">Text alignment.</param>
        /// <returns>The new text component.</returns>
        /// <param name="font">Font asset to use (null uses the TextMeshPro default).</param>
        public static TextMeshProUGUI CreateText(string objectName, Transform parent, float size, Color color, TextAlignmentOptions alignment, TMP_FontAsset font = null)
        {
            RectTransform rect = CreateRect(objectName, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>Creates a small decoration image fixed to one corner of its parent, at its native size.</summary>
        /// <param name="objectName">Object name.</param>
        /// <param name="parent">Parent transform.</param>
        /// <param name="sprite">Decoration sprite.</param>
        /// <param name="corner">Corner: (0,0) bottom-left, (1,0) bottom-right, (0,1) top-left, (1,1) top-right.</param>
        /// <param name="offset">Distance in pixels from that corner (positive moves inward on both axes as given).</param>
        private static void CreateCornerImage(string objectName, Transform parent, Sprite sprite, Vector2 corner, Vector2 offset)
        {
            Image image = CreateImage(objectName, parent, sprite, Image.Type.Simple);
            SetAnchors(image.rectTransform, corner, corner, corner);
            image.rectTransform.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height);
            image.rectTransform.anchoredPosition = offset;
        }

        /// <summary>
        /// Creates (or loads, if it already exists) a TextMeshPro font asset from a TTF in the dialogue font folder.
        /// Falls back to the TMP default font (returns null) if the font cannot be built.
        /// </summary>
        /// <param name="fileName">TTF file name without extension.</param>
        /// <returns>The TMP font asset, or null.</returns>
        public static TMP_FontAsset EnsureFontAsset(string fileName)
        {
            string assetPath = $"{FontFolder}/{fileName} SDF.asset";
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            Font font = AssetDatabase.LoadAssetAtPath<Font>($"{FontFolder}/{fileName}.ttf");
            if (font == null)
            {
                Debug.LogWarning("DialogueUiBuilder: font not found, using the default font: " + fileName);
                return null;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font, 48, 4, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
            if (fontAsset == null)
            {
                Debug.LogWarning("DialogueUiBuilder: could not create the font asset for " + fileName);
                return null;
            }

            // The material and atlas texture must be saved inside the font asset, or they are lost on reload.
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            fontAsset.material.name = fileName + " Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            foreach (Texture2D atlas in fontAsset.atlasTextures)
            {
                atlas.name = fileName + " Atlas";
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        /// <summary>Sets anchors and pivot of a RectTransform.</summary>
        /// <param name="rect">Target.</param>
        /// <param name="min">Anchor min.</param>
        /// <param name="max">Anchor max.</param>
        /// <param name="pivot">Pivot.</param>
        public static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
        }

        /// <summary>Makes a RectTransform fill its parent with an equal inset on every side.</summary>
        /// <param name="rect">Target.</param>
        /// <param name="inset">Distance in pixels from each parent edge.</param>
        public static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
