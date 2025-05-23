using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LoginBootManager : MonoBehaviour
{
    // Resources
    public Animator bootAnimator;    
    [SerializeField] private Canvas targetCanvas;

    // Settings
    public bool bootOnEnable = true;    
    [Range(0, 30)] public float bootTime = 4;
    [Range(0, 5)] public float initTime = 0.5f;
    [Range(0.5f, 12)] public float fadeSpeed = 1.5f;

    // Helpers    
    float cachedOutLength = 0.5f;

    void OnEnable()
    {
        if (!gameObject.activeInHierarchy)
            return;

        bootAnimator.gameObject.SetActive(true);
        bootAnimator.enabled = true;
        bootAnimator.Play("Start");

        StopCoroutine("StartBootProcess");
        StartCoroutine("StartBootProcess");
    }

    private IEnumerator StartBootProcess()
    {
        yield return new WaitForSeconds(bootTime);

        if (bootTime != 0) { bootAnimator.Play("Out"); }
        else { bootAnimator.Play("Disabled"); }

        //if (userManager.userCreated)
        //{
        //    userManager.setupScreen.gameObject.SetActive(false);
        //    userManager.OpenLockScreen();
        //}

        //else
        //{
        //    userManager.setupScreen.gameObject.SetActive(true);
        //}

        StartCoroutine("DisableBootScreen");
    }    

    IEnumerator DisableBootScreen()
    {
        yield return new WaitForSeconds(cachedOutLength);
        bootAnimator.gameObject.SetActive(false);
    }
}
