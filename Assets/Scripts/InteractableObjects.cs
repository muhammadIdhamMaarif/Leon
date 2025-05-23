using UnityEngine;

public class InteractableObjects : MonoBehaviour
{
    [SerializeField] private string test;
    public void Interaction()
    {
        Debug.Log(test);
    }
}
