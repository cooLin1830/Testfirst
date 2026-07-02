using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DimensionShift
{
    [Serializable]
    public sealed class DimensionModeChangedEvent : UnityEvent<DimensionMode>
    {
    }

    public sealed class DimensionModeManager : MonoBehaviour
    {
        public static DimensionModeManager Instance { get; private set; }

        [SerializeField] private DimensionMode startingMode = DimensionMode.TwoD;
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
        [SerializeField] private bool allowKeyboardToggle = true;

        public DimensionModeChangedEvent onModeChanged = new DimensionModeChangedEvent();

        private readonly List<IDimensionModeListener> listeners = new List<IDimensionModeListener>();

        public event Action<DimensionMode> ModeChanged;

        public DimensionMode CurrentMode { get; private set; }
        public bool Is2D => CurrentMode == DimensionMode.TwoD;
        public bool Is3D => CurrentMode == DimensionMode.ThreeD;

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

        private void Update()
        {
            if (allowKeyboardToggle && Input.GetKeyDown(toggleKey))
            {
                ToggleMode();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Register(IDimensionModeListener listener)
        {
            if (listener == null || listeners.Contains(listener))
            {
                return;
            }

            listeners.Add(listener);
            listener.SetDimensionMode(CurrentMode);
        }

        public void Unregister(IDimensionModeListener listener)
        {
            if (listener == null)
            {
                return;
            }

            listeners.Remove(listener);
        }

        public void ToggleMode()
        {
            SetMode(CurrentMode == DimensionMode.TwoD ? DimensionMode.ThreeD : DimensionMode.TwoD);
        }

        public void SetMode(DimensionMode mode)
        {
            if (CurrentMode == mode)
            {
                return;
            }

            CurrentMode = mode;
            Broadcast(CurrentMode);
        }

        private void Broadcast(DimensionMode mode)
        {
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                if (listeners[i] == null)
                {
                    listeners.RemoveAt(i);
                    continue;
                }

                listeners[i].SetDimensionMode(mode);
            }

            ModeChanged?.Invoke(mode);
            onModeChanged.Invoke(mode);
        }
    }
}
