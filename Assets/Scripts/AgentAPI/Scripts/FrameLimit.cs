using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameLimit : MonoBehaviour
{
    public int targetFrameRate = 30;
    private int CurrentTargetFrameRate = 30;

    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
    }

    private void Update()
    {
        if(CurrentTargetFrameRate != targetFrameRate)
        {
            CurrentTargetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = CurrentTargetFrameRate;
        }
    }
}

