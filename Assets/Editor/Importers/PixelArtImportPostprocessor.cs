using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Automatically applies the pixel art import settings to every texture inside Assets/Art,
    /// so new art never needs manual setup: Sprite, 32 pixels per unit, Point filter, no compression.
    /// Pivots: the player's frames get the pivot on the feet; props get a bottom-center pivot.
    /// Every image becomes a single sprite, except files whose name ends with "_Sheet" (for example
    /// "Player_Walk_Sheet.png"): those keep their Sprite Mode so they can be sliced by hand.
    /// To exclude an asset from these rules, keep it outside Assets/Art.
    /// </summary>
    public class PixelArtImportPostprocessor : AssetPostprocessor
    {
        /// <summary>Only textures under this folder are processed.</summary>
        private const string ArtRoot = "Assets/Art/";

        /// <summary>Folder with the 64x64 character frames (player, Violet...); these use a custom pivot placed on the feet.</summary>
        private const string PlayerArtRoot = "Assets/Art/Characters/";

        /// <summary>Folder with props (NPC blocks, objects); these use a bottom-center pivot.</summary>
        private const string PropsRoot = "Assets/Art/Props/";

        /// <summary>Folder with UI sprites (dialogue box, name plate...); these are 9-slice sprites with full rect meshes.</summary>
        private const string UiRoot = "Assets/Art/UI/";

        /// <summary>
        /// 9-slice borders in pixels (left, bottom, right, top) by UI file name. The corners inside the border
        /// are never stretched, so the gold frame stays crisp at any size. UI files not listed here get no border.
        /// </summary>
        private static readonly Dictionary<string, Vector4> UiBorders = new Dictionary<string, Vector4>
        {
            { "DialogueBox_Claude", new Vector4(8f, 8f, 8f, 8f) },
            { "NamePlate_Claude", new Vector4(6f, 6f, 6f, 6f) }
        };

        /// <summary>Files whose name ends with this suffix are sprite sheets and are NOT forced to a single sprite.</summary>
        private const string SheetSuffix = "_Sheet";

        /// <summary>Pixels per unit used by the whole project (tiles are 32x32 pixels = 1 unit).</summary>
        private const float PixelsPerUnit = 32f;

        /// <summary>
        /// Pivot of the 64x64 player frames. The feet stand on pixel row 58 of 64, so the pivot sits
        /// 6 pixels above the bottom edge: 6 / 64 = 0.09375. Y sorting and collisions use this point.
        /// </summary>
        private static readonly Vector2 PlayerPivot = new Vector2(0.5f, 6f / 64f);

        /// <summary>Called by Unity just before a texture is imported; changes its import settings.</summary>
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;

            importer.textureType = TextureImporterType.Sprite;

            // 2D projects import new images as Sprite Mode "Multiple" by default, which would ignore the pivot
            // rules below. So everything becomes a single sprite, except files named "*_Sheet".
            bool isSheet = Path.GetFileNameWithoutExtension(assetPath).EndsWith(SheetSuffix);
            if (!isSheet)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
            }

            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            // Pivots only make sense for single sprites.
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                return;
            }

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            if (assetPath.StartsWith(PlayerArtRoot))
            {
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = PlayerPivot;
                importer.SetTextureSettings(settings);
            }
            else if (assetPath.StartsWith(PropsRoot))
            {
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                importer.SetTextureSettings(settings);
            }
            else if (assetPath.StartsWith(UiRoot))
            {
                // FullRect keeps the whole rectangle as the mesh, which Sliced and Tiled UI images need.
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Center;

                string fileName = Path.GetFileNameWithoutExtension(assetPath);
                settings.spriteBorder = UiBorders.TryGetValue(fileName, out Vector4 border) ? border : Vector4.zero;
                importer.SetTextureSettings(settings);
            }
        }
    }
}
