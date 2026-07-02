using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DimensionShift.PetsLike
{
    [Serializable]
    public sealed class PetsPerspectiveChangedEvent : UnityEvent<PetsPerspectiveMode>
    {
    }

    public sealed class PetsModeManager : MonoBehaviour
    {
        public static PetsModeManager Instance { get; private set; }

        [SerializeField] private PetsPerspectiveMode startingMode = PetsPerspectiveMode.TwoD;

        public PetsPerspectiveChangedEvent onModeChanged = new PetsPerspectiveChangedEvent();

        private readonly List<IPetsPerspectiveListener> listeners = new List<IPetsPerspectiveListener>();

        public event Action<PetsPerspectiveMode> ModeChanged;

        public PetsPerspectiveMode CurrentMode { get; private set; }
        public bool Is2D => CurrentMode == PetsPerspectiveMode.TwoD;
        public bool IsTwoPointFiveD => CurrentMode == PetsPerspectiveMode.TwoPointFiveD;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CurrentMode = startingMode;
        }

        private void Start()
        {
            Broadcast(CurrentMode);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Register(IPetsPerspectiveListener listener)
        {
            if (listener == null || listeners.Contains(listener))
            {
                return;
            }

            listeners.Add(listener);
            listener.SetPerspectiveMode(CurrentMode);
        }

        public void Unregister(IPetsPerspectiveListener listener)
        {
            if (listener == null)
            {
                return;
            }

            listeners.Remove(listener);
        }

        public void TrySetInitialMode(PetsPerspectiveMode mode)
        {
            CurrentMode = mode;
            Broadcast(CurrentMode);
        }

        public bool TrySetMode(PetsPerspectiveMode targetMode, PetsLikePlayerController player, PetsLevelRuntime level)
        {
            if (CurrentMode == targetMode)
            {
                return false;
            }

            if (player == null || level == null)
            {
                return false;
            }

            PetsGridCoord coord = player.CurrentGridCoord;
            if (!level.CanSwitchAt(coord, targetMode))
            {
                return false;
            }

            if (!level.IsValidPlayerCell(coord, targetMode))
            {
                return false;
            }

            CurrentMode = targetMode;
            Broadcast(CurrentMode);
            player.SnapToGridCoord(coord, CurrentMode);
            player.RecordSafePosition();
            return true;
        }

        private void Broadcast(PetsPerspectiveMode mode)
        {
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                if (listeners[i] == null)
                {
                    listeners.RemoveAt(i);
                    continue;
                }

                listeners[i].SetPerspectiveMode(mode);
            }

            ModeChanged?.Invoke(mode);
            onModeChanged.Invoke(mode);
        }
    }
}
