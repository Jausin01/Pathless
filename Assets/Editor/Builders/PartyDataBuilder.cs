using Pathless.Party;
using UnityEditor;
using UnityEngine;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Creates the data of the nine friends: one Curse and one PartyMember asset per character, plus the roster that
    /// lists them. Everything is provisional (names, numbers, texts) and marked with the "_Claude" suffix.
    /// Existing assets are UPDATED in place (same GUID), so prefabs and scenes that reference them stay valid.
    /// Menu: Tools > Pathless > Party > Build Party Data.
    /// </summary>
    public static class PartyDataBuilder
    {
        /// <summary>Folder of the party assets.</summary>
        private const string PartyFolder = "Assets/Data/Party";

        /// <summary>Folder of the curse assets.</summary>
        private const string CurseFolder = "Assets/Data/Party/Curses";

        /// <summary>Folder of the portraits (all members use the same placeholder face for now).</summary>
        private const string PortraitFolder = "Assets/Art/Portraits/Test_Claude";

        /// <summary>Path of the roster asset.</summary>
        /// <remarks>It is inside a Resources folder so PartyRegistry can load it from any scene (see PartyRegistry.ResourcePath).</remarks>
        public const string RosterPath = "Assets/Resources/" + Pathless.Party.PartyRegistry.ResourcePath + ".asset";

        /// <summary>Where the roster used to be saved; it is moved from there (keeping its GUID) the first time.</summary>
        private const string OldRosterPath = PartyFolder + "/PartyRoster_Main_Claude.asset";

        /// <summary>Description of one character, used only to fill the assets.</summary>
        private struct Character
        {
            public string Id;
            public string Name;
            public string CurseName;
            public string CurseText;
            public bool CurseHidden;
        }

        /// <summary>The nine friends, in roster order. Texts are short placeholders based on the story notes.</summary>
        private static readonly Character[] Characters =
        {
            new Character { Id = "protagonist", Name = "Protagonist", CurseName = "Cursed Exchange",
                CurseText = "Asks for something tied to the story and pays the price it demands." },
            new Character { Id = "violet", Name = "Violet", CurseName = "Unknown",
                CurseText = "Nobody knows. Not even her.", CurseHidden = true },
            new Character { Id = "hina", Name = "Hina", CurseName = "Portal",
                CurseText = "Opens a portal she cannot aim. What comes through it is dangerous." },
            new Character { Id = "michael", Name = "Michael", CurseName = "Foresight",
                CurseText = "Sees what enemies and allies will do next." },
            new Character { Id = "alice", Name = "Alice", CurseName = "Infinite Speed",
                CurseText = "Moves anywhere at once, or acts several times in one turn." },
            new Character { Id = "kate", Name = "Kate", CurseName = "Borrowed Face",
                CurseText = "Copies the appearance and memories of the dead." },
            new Character { Id = "alexander", Name = "Alexander", CurseName = "Metal Summon",
                CurseText = "Creates metal projectiles, weapons and special attacks." },
            new Character { Id = "gabe", Name = "Gabe", CurseName = "Body Ward",
                CurseText = "Creates barriers from nothing, or uses his own body as one." },
            new Character { Id = "liz", Name = "Liz", CurseName = "Silence",
                CurseText = "Removes the sound from an area or a target." },
        };

        /// <summary>Menu entry: builds the data and selects the roster.</summary>
        [MenuItem("Tools/Pathless/Party/Build Party Data")]
        public static void BuildFromMenu()
        {
            PartyRoster roster = Build();
            if (roster != null)
            {
                Selection.activeObject = roster;
            }
        }

        /// <summary>Creates or updates every curse, member and the roster.</summary>
        /// <returns>The roster, or null if the placeholder portrait is missing.</returns>
        public static PartyRoster Build()
        {
            EditorAssetUtility.EnsureFolder(PartyFolder);
            EditorAssetUtility.EnsureFolder(CurseFolder);

            Sprite portrait = AssetDatabase.LoadAssetAtPath<Sprite>(PortraitFolder + "/Portrait_Npc_neutral_Claude.png");
            if (portrait == null)
            {
                Debug.LogError("PartyDataBuilder: placeholder portrait not found (or not a Sprite) in " + PortraitFolder);
                return null;
            }

            PartyMember[] members = new PartyMember[Characters.Length];
            for (int i = 0; i < Characters.Length; i++)
            {
                Curse curse = BuildCurse(Characters[i]);
                members[i] = BuildMember(Characters[i], curse, LoadPortrait(Characters[i], portrait));
            }

            EditorAssetUtility.EnsureFolder("Assets/Resources/Party");
            if (AssetDatabase.LoadAssetAtPath<PartyRoster>(RosterPath) == null
                && AssetDatabase.LoadAssetAtPath<PartyRoster>(OldRosterPath) != null)
            {
                AssetDatabase.MoveAsset(OldRosterPath, RosterPath);
            }

            PartyRoster roster = BuildRoster(members);
            AssetDatabase.SaveAssets();
            Debug.Log("PartyDataBuilder: " + members.Length + " members and curses ready at " + PartyFolder);
            return roster;
        }

        /// <summary>Creates (or updates) the curse of one character.</summary>
        private static Curse BuildCurse(Character character)
        {
            string path = $"{CurseFolder}/Curse_{character.Id}_Claude.asset";
            Curse curse = LoadOrCreate<Curse>(path, out bool isNew);

            SerializedObject serialized = new SerializedObject(curse);
            serialized.FindProperty("curseId").stringValue = character.Id;
            serialized.FindProperty("displayName").stringValue = character.CurseName;
            serialized.FindProperty("description").stringValue = character.CurseText;
            serialized.FindProperty("hpCost").intValue = character.CurseHidden ? 0 : 10;
            serialized.FindProperty("hidden").boolValue = character.CurseHidden;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return Save(curse, path, isNew);
        }

        /// <summary>
        /// Looks for the character's own neutral portrait (Assets/Art/Portraits/(Name)/Portrait_(Name)_neutral.png).
        /// Characters without art yet use the placeholder.
        /// </summary>
        private static Sprite LoadPortrait(Character character, Sprite placeholder)
        {
            string path = $"Assets/Art/Portraits/{character.Name}/Portrait_{character.Name}_neutral.png";
            Sprite own = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            return own != null ? own : placeholder;
        }

        /// <summary>Creates (or updates) one party member. It has a curse and no path yet.</summary>
        private static PartyMember BuildMember(Character character, Curse curse, Sprite portrait)
        {
            string path = $"{PartyFolder}/PartyMember_{character.Name}_Claude.asset";
            PartyMember member = LoadOrCreate<PartyMember>(path, out bool isNew);

            SerializedObject serialized = new SerializedObject(member);
            serialized.FindProperty("memberId").stringValue = character.Id;
            serialized.FindProperty("displayName").stringValue = character.Name;
            serialized.FindProperty("portrait").objectReferenceValue = portrait;
            serialized.FindProperty("level").intValue = 1;
            serialized.FindProperty("maxHp").intValue = 100;
            serialized.FindProperty("startingHp").intValue = 0;
            serialized.FindProperty("specialName").stringValue = "Special";
            serialized.FindProperty("maxSpecial").intValue = 50;
            serialized.FindProperty("startingSpecial").intValue = 0;
            serialized.FindProperty("curse").objectReferenceValue = curse;
            serialized.FindProperty("pathId").stringValue = string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return Save(member, path, isNew);
        }

        /// <summary>Creates (or updates) the roster that lists all members.</summary>
        private static PartyRoster BuildRoster(PartyMember[] members)
        {
            PartyRoster roster = LoadOrCreate<PartyRoster>(RosterPath, out bool isNew);

            SerializedObject serialized = new SerializedObject(roster);
            SerializedProperty list = serialized.FindProperty("members");
            list.arraySize = members.Length;
            for (int i = 0; i < members.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = members[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return Save(roster, RosterPath, isNew);
        }

        /// <summary>Loads the asset at the path, or makes a new in-memory one (not saved yet).</summary>
        private static T LoadOrCreate<T>(string path, out bool isNew) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            isNew = existing == null;
            return isNew ? ScriptableObject.CreateInstance<T>() : existing;
        }

        /// <summary>Saves a new asset to disk, or marks an existing one as changed. Fields are always set BEFORE this.</summary>
        private static T Save<T>(T asset, string path, bool isNew) where T : ScriptableObject
        {
            if (isNew)
            {
                AssetDatabase.CreateAsset(asset, path);
            }

            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
