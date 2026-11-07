using System.IO;
using UnityEditor;
using UnityEngine;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Small helpers shared by the Pathless editor builders: creating folders and setting
    /// serialized fields on components. Editor-only (this file lives in an Editor folder).
    /// </summary>
    public static class EditorAssetUtility
    {
        /// <summary>
        /// Creates a folder (and any missing parent folders) inside the Assets folder.
        /// Does nothing if the folder already exists.
        /// </summary>
        /// <param name="folderPath">Project-relative path such as "Assets/Animations/Player".</param>
        public static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            string folderName = Path.GetFileName(folderPath);

            // Make sure the parent exists first, then create this folder inside it.
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        /// <summary>
        /// Assigns an object reference to a [SerializeField] field of a component, by field name.
        /// Used because the fields are private, so they cannot be set directly from the builders.
        /// </summary>
        /// <param name="component">The component that owns the field.</param>
        /// <param name="fieldName">Exact name of the field in the script (for example "target").</param>
        /// <param name="value">The object to assign (a scene object or an asset).</param>
        public static void SetReference(Component component, string fieldName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(component);
            SerializedProperty property = serializedObject.FindProperty(fieldName);

            if (property == null)
            {
                Debug.LogError($"EditorAssetUtility: field '{fieldName}' not found on {component.GetType().Name}.", component);
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Assigns an enum value (by its index) to a [SerializeField] enum field of a component.
        /// </summary>
        /// <param name="component">The component that owns the field.</param>
        /// <param name="fieldName">Exact name of the enum field in the script.</param>
        /// <param name="enumIndex">Index of the enum entry (0 for the first entry, 1 for the second...).</param>
        public static void SetEnum(Component component, string fieldName, int enumIndex)
        {
            SerializedObject serializedObject = new SerializedObject(component);
            SerializedProperty property = serializedObject.FindProperty(fieldName);

            if (property == null)
            {
                Debug.LogError($"EditorAssetUtility: field '{fieldName}' not found on {component.GetType().Name}.", component);
                return;
            }

            property.enumValueIndex = enumIndex;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Assigns a string to a [SerializeField] string field of a component.
        /// </summary>
        /// <param name="component">The component that owns the field.</param>
        /// <param name="fieldName">Exact name of the string field in the script.</param>
        /// <param name="value">The text to assign.</param>
        public static void SetString(Component component, string fieldName, string value)
        {
            SerializedObject serializedObject = new SerializedObject(component);
            SerializedProperty property = serializedObject.FindProperty(fieldName);

            if (property == null)
            {
                Debug.LogError($"EditorAssetUtility: field '{fieldName}' not found on {component.GetType().Name}.", component);
                return;
            }

            property.stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
