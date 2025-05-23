using UnityEngine;

public class LockToSetup : MonoBehaviour
{
    [SerializeField] private GameObject lockScreen;
    [SerializeField] private GameObject setupScreen;

    public void LockToCreate()
    {
        lockScreen.SetActive(false);
        setupScreen.SetActive(true);
    }

    public void CreateToLock()
    {
        lockScreen.SetActive(true);
        setupScreen.SetActive(false);
    }
}
