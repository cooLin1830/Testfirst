using DimensionShift.PetsLike;
using DimensionShift;
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

        public static void CreateEditablePainterTestScene(PetsEditableLevelAsset editableLevel)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject bootstrap = new GameObject("Dimension Painter Test Bootstrap");
            DimensionPrototypeBootstrap component = bootstrap.AddComponent<DimensionPrototypeBootstrap>();
            SerializedObject serialized = new SerializedObject(component);
            serialized.FindProperty("levelKind").enumValueIndex = (int)PetsPrototypeLevelKind.EditableAsset;
            serialized.FindProperty("editableLevel").objectReferenceValue = editableLevel;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EnsureSceneFolder();
            EditorSceneManager.SaveScene(scene, PainterTestScenePath);
            SetBuildScenes(new[] { ScenePath, TutorialScenePath, PainterTestScenePath });
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(PainterTestScenePath);
            Debug.Log($"Created PETS-like editable-map test scene at {PainterTestScenePath}");
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
    }
}
