using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Turns the 12 test tile sprites (Assets/Art/Tilesets/TestTiles_Claude) into Tile assets
    /// that can be painted on a Tilemap. Wall tiles get a collider, floor and door tiles do not.
    /// Run it from Tools > Pathless > Build Test Tiles (or through "Build Everything").
    /// Running it again REPLACES the generated Tile assets.
    /// </summary>
    public static class TestTilesBuilder
    {
        /// <summary>Folder with the tile sprites (PNG files named "[name]_Claude.png").</summary>
        private const string SpriteFolder = "Assets/Art/Tilesets/TestTiles_Claude";

        /// <summary>Folder where the generated Tile assets are saved.</summary>
        private const string TilesFolder = "Assets/Art/Tilesets/TestTiles_Claude/Tiles";

        /// <summary>Base names of the tiles (the sprite file is "[name]_Claude.png").</summary>
        private static readonly string[] TileNames =
        {
            "grass_a", "grass_b", "grass_tuft", "grass_flowers",
            "dirt_a", "dirt_b", "stone_floor",
            "wall_cap", "wall_face_upper", "wall_face_lower",
            "door_top", "door_bottom"
        };

        /// <summary>Tiles that block the player (they get a grid collider). Everything else is walkable.</summary>
        private static readonly HashSet<string> SolidTiles = new HashSet<string>
        {
            "wall_cap", "wall_face_upper", "wall_face_lower"
        };

        /// <summary>Menu entry that builds only the test tiles.</summary>
        [MenuItem("Tools/Pathless/Build Test Tiles")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>
        /// Loads the already generated Tile assets from disk. Use it instead of keeping references from Build():
        /// opening a scene unloads unused assets, which would turn old references into empty ones.
        /// </summary>
        /// <returns>The tiles by base name (for example "grass_a"), or an empty dictionary if any is missing.</returns>
        public static Dictionary<string, Tile> LoadAll()
        {
            Dictionary<string, Tile> tiles = new Dictionary<string, Tile>();

            foreach (string tileName in TileNames)
            {
                string tilePath = $"{TilesFolder}/{tileName}_Claude.asset";
                Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);

                if (tile == null)
                {
                    Debug.LogError($"TestTilesBuilder: Tile asset not found at {tilePath}. Run 'Build Test Tiles' first.");
                    return new Dictionary<string, Tile>();
                }

                tiles[tileName] = tile;
            }

            return tiles;
        }

        /// <summary>Creates one Tile asset per sprite.</summary>
        /// <returns>The tiles by base name (for example "grass_a"), or an empty dictionary if sprites are missing.</returns>
        public static Dictionary<string, Tile> Build()
        {
            Dictionary<string, Tile> tiles = new Dictionary<string, Tile>();
            EditorAssetUtility.EnsureFolder(TilesFolder);

            foreach (string tileName in TileNames)
            {
                string spritePath = $"{SpriteFolder}/{tileName}_Claude.png";
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

                if (sprite == null)
                {
                    Debug.LogError($"TestTilesBuilder: sprite not found at {spritePath}");
                    return new Dictionary<string, Tile>();
                }

                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.colliderType = SolidTiles.Contains(tileName) ? Tile.ColliderType.Grid : Tile.ColliderType.None;

                string tilePath = $"{TilesFolder}/{tileName}_Claude.asset";
                AssetDatabase.DeleteAsset(tilePath);
                AssetDatabase.CreateAsset(tile, tilePath);
                tiles[tileName] = tile;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("TestTilesBuilder: 12 Tile assets created in " + TilesFolder);
            return tiles;
        }
    }
}
