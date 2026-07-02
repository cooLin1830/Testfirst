using DimensionShift.PetsLike;
using UnityEngine;

namespace DimensionShift
{
    public sealed class DimensionPrototypeBootstrap : MonoBehaviour
    {
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private bool rebuildIfManagerExists;
        [SerializeField] private PetsPrototypeLevelKind levelKind = PetsPrototypeLevelKind.MechanicTestRoom;
        [SerializeField] private PetsEditableLevelAsset editableLevel;

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
    }
}
