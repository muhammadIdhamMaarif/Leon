using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class PlayerSave : MonoBehaviour
{
    public List<GameObject> SaveTransform;
    private void Start()
    {
        foreach (GameObject obj in SaveTransform)
        {
            if (ES3.KeyExists(obj.name)) 
                ES3.LoadInto(obj.name, obj.transform);
        }
    }
    private void OnApplicationQuit()
    {
        foreach (GameObject obj in SaveTransform)
        {            
            ES3.Save(obj.name, obj.transform);
        }
    }
}
