using StarterAssets;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactionRange = 2.0f;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Material highlightMaterial;

    private StarterAssetsInputs _input;
    private InteractableObject lastHighlighted;
    private ThirdPersonController _controller;

    private void Start()
    {
        _input = GetComponent<StarterAssetsInputs>();
        _controller = GetComponent<ThirdPersonController>();
    }

    private void Update()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        InteractableObject currentInteractable = null;

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange))
        {
            if (hit.collider.isTrigger)
                return;

            if (hit.collider.TryGetComponent(out InteractableObject interactable))
            {                
                currentInteractable = interactable;                                
                if (_input.interact)
                {
                    interactable.OnInteraction();
                    _controller.LockPlayer(true);
                }
            }
        }
        
        if (currentInteractable != lastHighlighted)
        {
            if (lastHighlighted != null)
            {                
                lastHighlighted.SetHighlight(false, highlightMaterial);
            }

            if (currentInteractable != null)
            {                
                currentInteractable.SetHighlight(true, highlightMaterial);
            }

            lastHighlighted = currentInteractable;
        }        

        _input.interact = false;
    }    
}
