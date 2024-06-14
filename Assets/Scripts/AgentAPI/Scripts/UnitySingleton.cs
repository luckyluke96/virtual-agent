using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UnitySingleton<T> : MonoBehaviour where T : Component
{ 
    public static T Instance { get; private set; }
    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.

        if (Instance != null && !Instance.Equals(this))
        {
            Destroy(this);
        }
        else
        {
            Instance = this as T;
        }
    }
}
