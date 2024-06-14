using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class API_Agent : UnitySingleton<API_Agent>
{
    /// <summary>
    /// Call Natural language Processing
    /// </summary>
    public NLPAPI NLPAPI;

    /// <summary>
    /// Call speech to text
    /// </summary>
    public MicrophoneRecorder STTAPI;

    /// <summary>
    /// Call zext to speech
    /// </summary>
    public TTSAPI TTSAPI; 
     
}
