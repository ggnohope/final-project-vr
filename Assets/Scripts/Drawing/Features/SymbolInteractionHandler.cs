using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRDrawing.Features
{
    /// <summary>
    /// Attached to each spawned symbol GameObject.
    /// Bridges XR Interaction Toolkit hover/select events to GeologicalSymbolObject callbacks.
    /// Requires XRSimpleInteractable to receive XRI events.
    /// </summary>
    [RequireComponent(typeof(GeologicalSymbolObject))]
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class SymbolInteractionHandler : MonoBehaviour
    {
        private GeologicalSymbolObject symbolObject;
        private XRSimpleInteractable interactable;

        private void Awake()
        {
            symbolObject = GetComponent<GeologicalSymbolObject>();
            interactable = GetComponent<XRSimpleInteractable>();

            interactable.hoverEntered.AddListener(OnHoverEntered);
            interactable.hoverExited.AddListener(OnHoverExited);
            interactable.selectEntered.AddListener(OnSelectEntered);
        }

        private void OnDestroy()
        {
            if (interactable == null) return;
            interactable.hoverEntered.RemoveListener(OnHoverEntered);
            interactable.hoverExited.RemoveListener(OnHoverExited);
            interactable.selectEntered.RemoveListener(OnSelectEntered);
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            symbolObject?.NotifyHoverEnter();
        }

        private void OnHoverExited(HoverExitEventArgs args)
        {
            symbolObject?.NotifyHoverExit();
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            symbolObject?.NotifySelected();
        }
    }
}
