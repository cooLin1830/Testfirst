using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DimensionShift
{
    public sealed class DimensionPressurePlate : DimensionListenerBehaviour
    {
        [SerializeField] private bool onlyWorksIn3D = true;
        [SerializeField] private Renderer plateRenderer;
        [SerializeField] private Color inactiveColor = new Color(0.2f, 0.55f, 1f);
        [SerializeField] private Color activeColor = new Color(0.35f, 1f, 0.65f);

        public UnityEvent onPressed = new UnityEvent();
        public UnityEvent onReleased = new UnityEvent();

        private readonly HashSet<Rigidbody> bodiesOnPlate = new HashSet<Rigidbody>();
        private bool pressed;
        private DimensionMode currentMode;

        private void Reset()
        {
            plateRenderer = GetComponentInChildren<Renderer>();
        }

        private void Awake()
        {
            if (plateRenderer == null)
            {
                plateRenderer = GetComponentInChildren<Renderer>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Rigidbody otherBody = other.attachedRigidbody;
            if (otherBody != null)
            {
                bodiesOnPlate.Add(otherBody);
                RefreshState();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            Rigidbody otherBody = other.attachedRigidbody;
            if (otherBody != null)
            {
                bodiesOnPlate.Remove(otherBody);
                RefreshState();
            }
        }

        public override void SetDimensionMode(DimensionMode mode)
        {
            currentMode = mode;
            RefreshState();
        }

        private void RefreshState()
        {
            bodiesOnPlate.RemoveWhere(body => body == null);
            bool canWork = !onlyWorksIn3D || currentMode == DimensionMode.ThreeD;
            bool nextPressed = canWork && bodiesOnPlate.Count > 0;

            if (pressed != nextPressed)
            {
                pressed = nextPressed;
                if (pressed)
                {
                    onPressed.Invoke();
                }
                else
                {
                    onReleased.Invoke();
                }
            }

            if (plateRenderer != null)
            {
                plateRenderer.material.color = pressed ? activeColor : inactiveColor;
            }
        }
    }
}
