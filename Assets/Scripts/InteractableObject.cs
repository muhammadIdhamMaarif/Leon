using StarterAssets;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InteractableObject : MonoBehaviour
{
    [SerializeField] private GameObject playerHead;
    [SerializeField] private GameObject player;
    [SerializeField] private bool monitor;
    private ThirdPersonController _controller;
    private static bool _isInteractable = true;    
    public string test;

    private Renderer objRenderer;
    private Material[] originalMaterials;
    private bool isHighlighted = false;
    private GameObject _uiCanvas;
    private GameObject _uiCanvasHighlighted;

    [SerializeField] private Material customMaterial;

    [Header("On Interaction Events")]
    public UnityEvent onInteraction = new UnityEvent();
    public UnityEvent doneInteraction = new UnityEvent();


    private void Start()
    {
        _controller = player.GetComponent<ThirdPersonController>();
    }

    private void Awake()
    {
        objRenderer = GetComponent<Renderer>();
        if (objRenderer != null)
            originalMaterials = objRenderer.materials;
        _uiCanvas = transform.GetChild(0).GetChild(0).gameObject;
        _uiCanvasHighlighted = transform.GetChild(0).GetChild(1).gameObject;
        _uiCanvas.SetActive(false);
        _uiCanvasHighlighted.SetActive(false);
    }

    public void SetHighlight(bool state, Material highlightMaterial)
    {
        if (_isInteractable)
        {
            if (!monitor)
            {
                if (objRenderer == null || highlightMaterial == null) return;

                if (state && !isHighlighted)
                {

                    var mats = new Material[originalMaterials.Length + 1];
                    originalMaterials.CopyTo(mats, 0);
                    mats[mats.Length - 1] = highlightMaterial;
                    objRenderer.materials = mats;
                    isHighlighted = true;
                    HighlightInteractionInterface(true);

                }
                else if (!state && isHighlighted)
                {
                    objRenderer.materials = originalMaterials;
                    isHighlighted = false;
                    HighlightInteractionInterface(false);
                }
            }

            else
            {
                GameObject monitor = transform.GetChild(2).gameObject;
                if (state && !isHighlighted)
                {                    
                    HighlightInteractionInterface(true);

                }
                else if (!state && isHighlighted)
                {                    
                    HighlightInteractionInterface(false);
                }
                foreach (Transform child in monitor.transform)
                {
                    objRenderer = child.GetComponent<Renderer>();

                    if (objRenderer == null || highlightMaterial == null) return;

                    if (objRenderer != null)
                        originalMaterials = objRenderer.materials;

                    if (state && !isHighlighted)
                    {
                        var mats = new Material[originalMaterials.Length + 1];
                        originalMaterials.CopyTo(mats, 0);
                        mats[mats.Length - 1] = highlightMaterial;
                        objRenderer.materials = mats;
                        isHighlighted = true;                        

                    }
                    else if (!state && isHighlighted)
                    {
                        objRenderer.materials = originalMaterials;
                        isHighlighted = false;                        
                    }
                }
            }

        }
    }    

    private void HighlightInteractionInterface(bool state)
    {
        _uiCanvas.SetActive(!state);
        _uiCanvasHighlighted.SetActive(state);
    }

    public void OnInteraction()
    {
        if (_isInteractable)
        {
            _isInteractable = false;
            Debug.Log(test);
            onInteraction.Invoke();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0)) { DoneInteraction(); }
        if (!_isInteractable) { Cursor.lockState = CursorLockMode.None; }
    }

    public void DoneInteraction()
    {        
        _isInteractable = true;
        _controller.LockPlayer(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        doneInteraction.Invoke();
    } 

    private void OnTriggerEnter(Collider other)
    {       
        if (other.CompareTag("Player") && _isInteractable)
        {    
            _uiCanvas.SetActive(true);
        }
    }

    public void OnTriggerStay(Collider other)
    {        
        if (other.CompareTag("Player"))
        {
            _uiCanvas.transform.LookAt(playerHead.transform);
            _uiCanvasHighlighted.transform.LookAt(playerHead.transform);
        }
    }

    public void OnTriggerExit(Collider other)
    {     
        if (other.CompareTag("Player"))
        {
            _uiCanvas.SetActive(false); 
            
        }
    }
}
