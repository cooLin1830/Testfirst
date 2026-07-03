using DimensionShift.PetsLike;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DimensionShift
{
    public sealed class DimensionPrototypeBootstrap : MonoBehaviour
    {
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private bool rebuildIfManagerExists;
        [SerializeField] private PetsPrototypeLevelKind levelKind = PetsPrototypeLevelKind.MechanicTestRoom;
        [SerializeField] private PetsEditableLevelAsset editableLevel;
        [SerializeField] private string nextSceneName;
        [SerializeField] private float nextSceneDelay = 0.25f;

        private bool isLoadingNextScene;

        public PetsPrototypeLevelKind LevelKind => levelKind;
        public PetsEditableLevelAsset EditableLevel => editableLevel;
        public string NextSceneName => nextSceneName;
        public bool HasNextScene => !string.IsNullOrWhiteSpace(nextSceneName);

        public static bool TryCompleteActiveLevel()
        {
            DimensionPrototypeBootstrap bootstrap = Object.FindObjectOfType<DimensionPrototypeBootstrap>();
            if (bootstrap == null)
            {
                return false;
            }

            return bootstrap.TryLoadNextScene();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateInEmptyScene()
        {
            if (PetsModeManager.Instance != null || Object.FindObjectOfType<DimensionPrototypeBootstrap>() != null)
            {
                return;
            }

            GameObject bootstrap = new GameObject("Dimension Prototype Bootstrap");
            bootstrap.AddComponent<DimensionPrototypeBootstrap>();
        }

        private void Start()
        {
            if (buildOnStart)
            {
                BuildIfNeeded();
            }
        }

        [ContextMenu("Build Prototype Level")]
        public void BuildIfNeeded()
        {
            if (!rebuildIfManagerExists && PetsModeManager.Instance != null)
            {
                return;
            }

            PetsPrototypeFactory.Build(levelKind, editableLevel);
        }

        public bool TryLoadNextScene()
        {
            if (!Application.isPlaying)
            {
                return false;
            }

            if (!HasNextScene)
            {
                PetsHud.ShowCompletionScreen();
                return true;
            }

            if (isLoadingNextScene)
            {
                return true;
            }

            isLoadingNextScene = true;
            StartCoroutine(LoadNextSceneAfterDelay());
            return true;
        }

        private IEnumerator LoadNextSceneAfterDelay()
        {
            if (nextSceneDelay > 0f)
            {
                yield return new WaitForSeconds(nextSceneDelay);
            }

            string sceneName = nextSceneName.Trim();
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Cannot load next scene '{sceneName}'. Add it to Build Settings or fix the next scene name on {nameof(DimensionPrototypeBootstrap)}.", this);
                isLoadingNextScene = false;
                yield break;
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}
