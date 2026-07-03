using DimensionShift.PetsLike;
using DimensionShift;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DimensionShiftEditor
{
    public static class DimensionPrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/DimensionShift/Scenes/DimensionShiftPrototype.unity";
        private const string TutorialScenePath = "Assets/DimensionShift/Scenes/DimensionShiftTutorial.unity";
        private const string PainterTestScenePath = "Assets/DimensionShift/Scenes/DimensionShiftPainterTest.unity";
        private const string EditableLevelFolder = "Assets/DimensionShift/EditableLevels";

        [MenuItem("Tools/Dimension Shift/Create Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PetsPrototypeFactory.BuildMechanicTestRoom();

            if (!AssetDatabase.IsValidFolder("Assets/DimensionShift/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets/DimensionShift", "Scenes");
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            SetBuildScenes(new[] { ScenePath });

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log($"Created PETS-like mechanic prototype scene at {ScenePath}");
        }

        [MenuItem("Tools/Dimension Shift/Create Tutorial Scene")]
        public static void CreateTutorialScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PetsPrototypeFactory.BuildTutorialLevel();

            if (!AssetDatabase.IsValidFolder("Assets/DimensionShift/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets/DimensionShift", "Scenes");
            }

            EditorSceneManager.SaveScene(scene, TutorialScenePath);
            SetBuildScenes(new[] { ScenePath, TutorialScenePath });

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(TutorialScenePath);
            Debug.Log($"Created PETS-like tutorial scene at {TutorialScenePath}");
        }

        [MenuItem("Tools/Dimension Shift/Create Painter Test Scene")]
        public static void CreateEditablePainterTestScene()
        {
            PetsEditableLevelAsset asset = Selection.activeObject as PetsEditableLevelAsset;
            CreateEditablePainterTestScene(asset);
        }

        [MenuItem("Tools/Dimension Shift/Create Map For Current Scene")]
        public static void CreateEditableLevelForCurrentSceneMenu()
        {
            PetsEditableLevelAsset asset = CreateEditableLevelForCurrentScene();
            BindEditableLevelToCurrentScene(asset);
        }

        [MenuItem("Tools/Dimension Shift/Bind Selected Map To Current Scene")]
        public static void BindSelectedEditableLevelToCurrentSceneMenu()
        {
            PetsEditableLevelAsset asset = Selection.activeObject as PetsEditableLevelAsset;
            if (asset == null)
            {
                EditorUtility.DisplayDialog("Bind PETS Map", "Select a PETS Editable Level asset first, then run this command again.", "OK");
                return;
            }

            BindEditableLevelToCurrentScene(asset);
        }

        [MenuItem("Tools/Dimension Shift/Ensure Scene Map Preview")]
        public static void EnsureSceneMapPreviewMenu()
        {
            if (!TryGetCurrentSceneEditableLevel(out PetsEditableLevelAsset editableLevel))
            {
                EditorUtility.DisplayDialog("PETS Scene Map Preview", "The active scene does not have a bound PETS editable map.", "OK");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            PetsSceneMapPreview preview = EnsureSceneMapPreview(scene);
            Undo.RecordObject(preview, "Ensure PETS Scene Map Preview");
            preview.EditableLevel = editableLevel;
            preview.RebuildGeneratedPreview();
            EditorUtility.SetDirty(preview);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeObject = preview.gameObject;
        }

        public static void CreateEditablePainterTestScene(PetsEditableLevelAsset editableLevel)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject bootstrap = new GameObject("Dimension Painter Test Bootstrap");
            DimensionPrototypeBootstrap component = bootstrap.AddComponent<DimensionPrototypeBootstrap>();
            SerializedObject serialized = new SerializedObject(component);
            serialized.FindProperty("levelKind").enumValueIndex = (int)PetsPrototypeLevelKind.EditableAsset;
            serialized.FindProperty("editableLevel").objectReferenceValue = editableLevel;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PetsSceneMapPreview preview = EnsureSceneMapPreview(scene);
            preview.EditableLevel = editableLevel;
            preview.RebuildGeneratedPreview();

            EnsureSceneFolder();
            EditorSceneManager.SaveScene(scene, PainterTestScenePath);
            SetBuildScenes(new[] { ScenePath, TutorialScenePath, PainterTestScenePath });
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(PainterTestScenePath);
            Debug.Log($"Created PETS-like editable-map test scene at {PainterTestScenePath}");
        }

        public static PetsEditableLevelAsset CreateEditableLevelForCurrentScene()
        {
            EnsureEditableLevelFolder();
            Scene scene = SceneManager.GetActiveScene();
            string sceneName = string.IsNullOrEmpty(scene.name) ? "CurrentScene" : scene.name;
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{EditableLevelFolder}/{SanitizeAssetName(sceneName)}_Map.asset");

            PetsEditableLevelAsset asset = ScriptableObject.CreateInstance<PetsEditableLevelAsset>();
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            Debug.Log($"Created PETS editable level asset at {assetPath}");
            return asset;
        }

        public static PetsEditableLevelAsset CreateAndBindEditableLevelToCurrentScene()
        {
            PetsEditableLevelAsset asset = CreateEditableLevelForCurrentScene();
            BindEditableLevelToCurrentScene(asset);
            return asset;
        }

        public static void BindEditableLevelToCurrentScene(PetsEditableLevelAsset editableLevel)
        {
            if (editableLevel == null)
            {
                EditorUtility.DisplayDialog("Bind PETS Map", "Create or assign a PETS Editable Level asset first.", "OK");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            DimensionPrototypeBootstrap bootstrap = FindBootstrapInScene(scene);
            if (bootstrap == null)
            {
                GameObject bootstrapObject = new GameObject("Dimension Editable Level Bootstrap");
                Undo.RegisterCreatedObjectUndo(bootstrapObject, "Create Dimension Editable Level Bootstrap");
                SceneManager.MoveGameObjectToScene(bootstrapObject, scene);
                bootstrap = bootstrapObject.AddComponent<DimensionPrototypeBootstrap>();
            }
            else
            {
                Undo.RecordObject(bootstrap, "Bind PETS Editable Level");
            }

            SerializedObject serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("buildOnStart").boolValue = true;
            serialized.FindProperty("levelKind").enumValueIndex = (int)PetsPrototypeLevelKind.EditableAsset;
            serialized.FindProperty("editableLevel").objectReferenceValue = editableLevel;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PetsSceneMapPreview preview = EnsureSceneMapPreview(scene);
            Undo.RecordObject(preview, "Bind PETS Scene Map Preview");
            preview.EditableLevel = editableLevel;
            preview.RebuildGeneratedPreview();
            EditorUtility.SetDirty(preview);

            EditorUtility.SetDirty(bootstrap);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeObject = preview.gameObject;
            Debug.Log($"Bound PETS editable level '{editableLevel.name}' to scene '{scene.name}'.");
        }

        public static bool TryGetCurrentSceneEditableLevel(out PetsEditableLevelAsset editableLevel)
        {
            editableLevel = null;
            DimensionPrototypeBootstrap bootstrap = FindBootstrapInScene(SceneManager.GetActiveScene());
            if (bootstrap == null)
            {
                return false;
            }

            SerializedObject serialized = new SerializedObject(bootstrap);
            editableLevel = serialized.FindProperty("editableLevel").objectReferenceValue as PetsEditableLevelAsset;
            return editableLevel != null;
        }

        public static void EnsureSceneMapPreviewForCurrentScene()
        {
            EnsureSceneMapPreviewMenu();
        }

        private static DimensionPrototypeBootstrap FindBootstrapInScene(Scene scene)
        {
            if (!scene.IsValid())
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                DimensionPrototypeBootstrap bootstrap = roots[i].GetComponentInChildren<DimensionPrototypeBootstrap>(true);
                if (bootstrap != null)
                {
                    return bootstrap;
                }
            }

            return null;
        }

        private static PetsSceneMapPreview EnsureSceneMapPreview(Scene scene)
        {
            PetsSceneMapPreview existing = FindSceneMapPreview(scene);
            if (existing != null)
            {
                return existing;
            }

            GameObject previewObject = new GameObject("PETS Scene Map Preview");
            Undo.RegisterCreatedObjectUndo(previewObject, "Create PETS Scene Map Preview");
            SceneManager.MoveGameObjectToScene(previewObject, scene);
            return previewObject.AddComponent<PetsSceneMapPreview>();
        }

        private static PetsSceneMapPreview FindSceneMapPreview(Scene scene)
        {
            if (!scene.IsValid())
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                PetsSceneMapPreview preview = roots[i].GetComponentInChildren<PetsSceneMapPreview>(true);
                if (preview != null)
                {
                    return preview;
                }
            }

            return null;
        }

        private static void EnsureSceneFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/DimensionShift/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets/DimensionShift", "Scenes");
            }
        }

        private static void SetBuildScenes(string[] paths)
        {
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                scenes[i] = new EditorBuildSettingsScene(paths[i], true);
            }

            EditorBuildSettings.scenes = scenes;
        }

        private static void EnsureEditableLevelFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/DimensionShift"))
            {
                AssetDatabase.CreateFolder("Assets", "DimensionShift");
            }

            if (!AssetDatabase.IsValidFolder(EditableLevelFolder))
            {
                AssetDatabase.CreateFolder("Assets/DimensionShift", "EditableLevels");
            }
        }

        private static string SanitizeAssetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "CurrentScene";
            }

            string sanitized = name;
            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalidCharacters.Length; i++)
            {
                sanitized = sanitized.Replace(invalidCharacters[i], '_');
            }

            return sanitized.Replace(' ', '_');
        }
    }
}
