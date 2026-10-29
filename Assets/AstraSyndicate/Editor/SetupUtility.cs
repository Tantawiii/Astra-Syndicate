using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AstraSyndicate.EditorTools
{
    /// <summary>Asset helpers for the content generators. Everything is create-if-missing.</summary>
    internal static class SetupUtility
    {
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Returns the asset at <paramref name="path"/>, creating and saving it first if it is missing.</summary>
        public static T LoadOrCreate<T>(string path, Func<T> create) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            T asset = create();
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[Setup] Created {path}");
            return asset;
        }

        /// <summary>Returns <typeparamref name="T"/> on the prefab at <paramref name="path"/>, building and saving the prefab first if it is missing.</summary>
        public static T LoadOrCreatePrefab<T>(string path, Func<GameObject> build) where T : Component
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<T>();

            GameObject instance = build();
            try
            {
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
                Debug.Log($"[Setup] Created {path}");
                return prefab.GetComponent<T>();
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        public static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException($"Missing asset {path}. Run Tools > Astra Syndicate > Setup Sprint 1 Content.");
            return asset;
        }

        /// <summary>Sets serialized fields by name. Fails loudly if a field was renamed, instead of silently skipping it.</summary>
        public static void Assign(Object target, params (string Field, object Value)[] values)
        {
            var serialized = new SerializedObject(target);
            foreach ((string field, object value) in values)
            {
                SerializedProperty property = serialized.FindProperty(field);
                if (property == null) throw new ArgumentException($"{target.GetType().Name} has no serialized field '{field}'.");

                switch (value)
                {
                    case float number:
                        property.floatValue = number;
                        break;
                    case int number when property.propertyType == SerializedPropertyType.Float:
                        property.floatValue = number;
                        break;
                    case int number:
                        property.intValue = number;
                        break;
                    case bool flag:
                        property.boolValue = flag;
                        break;
                    case string text:
                        property.stringValue = text;
                        break;
                    case Object reference:
                        property.objectReferenceValue = reference;
                        break;
                    default:
                        throw new ArgumentException($"Unsupported value for {target.GetType().Name}.{field}: {value}");
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Makes script changes to a prefab instance stick when the scene is saved.</summary>
        public static void RecordInstance(params Object[] targets)
        {
            foreach (Object target in targets) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
