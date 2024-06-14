using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(AudioAPI))]
public class MicrophoneRecorder : MonoBehaviour
{
    private AudioAPI api;

    public delegate void TranscriptionDelegate(AudioAPI.TranscriptionResult transcriptionResult);
    public TranscriptionDelegate transcriptionDelegate;
    public string SelectedMicrophoneDevice = "";
    public bool SelectedMicrophone = false;

    public GameObject MicOn;
    public GameObject MicOff;

    [Header("For Mobile")]
    public bool AutoselectMicrophone;

    // https://github.com/oshoham/UnityGoogleStreamingSpeechToText/blob/master/Runtime/StreamingRecognizer.cs


    public bool isRecording
    {
        get
        {
            foreach (var device in Microphone.devices)
            {
                if (Microphone.IsRecording(device))
                    return true;
            }
            return false;
        }
    }

    private int recordingHZ;

    // Note that if you want to use the Microphone
    // class in the web player, you need to get the user's permission to do so.
    // Call Application.RequestUserAuthorization before calling any Microphone methods.

    // Start is called before the first frame update
    void Start()
    {
        api = GetComponent<AudioAPI>();
        api.StartSpeechToTextAPI(() =>
        {
            Debug.Log("Connected AudioAPI");
        }, (t) =>
        {
            if (t.isFinal)
                StopRecording();
            transcriptionDelegate(t);
        });

        if (AutoselectMicrophone)
        {
            SelectedMicrophoneDevice = Microphone.devices[0];
            SelectedMicrophone = true;
        }
    }

#if UNITY_WEBGL && !UNITY_EDITOR
        void Awake()
        {
            Microphone.Init();
            Microphone.QueryAudioInput();
        }
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        void Update()
        {
            Microphone.Update();
        }
#endif

    /// <summary>
    /// Startet eine Coroutine, die l�uft, bis die Sprachausgabe finalisiert ist.
    /// Aufrufen in einer Coroutine via : yield return GetSpeechToText(...);
    /// </summary>
    /// <param name="intermediate_result">Zwischenergebnisse kommen hier an</param>
    /// <param name="final_result">Das resultat nachdem die Sprachausgabe abgeschlossen ist</param>
    /// <returns></returns>
    public Coroutine GetSpeechToText(Action<string> intermediate_result, Action<string> final_result)
    {
        return StartCoroutine(StartRecordingCoroutine(intermediate_result, final_result));
    }

    private IEnumerator StartRecordingCoroutine(Action<string> intermediate_result = null, Action<string> final_result = null)
    {
        if (SelectedMicrophoneDevice == null)
        {
            Debug.LogWarning("No microphone was selected");
            yield break;
        }

        Microphone.GetDeviceCaps(SelectedMicrophoneDevice, out var minFreq, out int maxFreq);
        var audioConfig = new AudioAPI.AudioConfiguration("LINEAR16", Mathf.Clamp(16000, minFreq, maxFreq), AgentSettings.LanguageString);
        recordingHZ = audioConfig.sampleRateHertz;

        AudioClip recording = Microphone.Start(SelectedMicrophoneDevice, true, 1, audioConfig.sampleRateHertz);

        var enable_audio_task = api.EnableAudioWebsocket(audioConfig, (started_successfully) =>
        {
            if (!started_successfully)
            {
                Debug.LogError("There was an error starting Speech to Text");
            }
            else
            {
                // Has to be called on the main thread because EnableAudioWebsocket running as a Task
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    StartCoroutine(RecordingHandler(recording, SelectedMicrophoneDevice));
                });
            }
        });

        // Wait for the websocket to be enabled
        if (enable_audio_task != null)
        {
            yield return new WaitUntil(() => enable_audio_task.IsCompleted);
        }

        bool final = false;
        if (intermediate_result != null || final_result != null)
        {
            void test(AudioAPI.TranscriptionResult result)
            {
                if (!result.isFinal)
                {
                    intermediate_result(result.alternatives[0].transcript);
                }
                else
                {
                    final_result(result.alternatives[0].transcript);
                    final = true;
                }
            }
            transcriptionDelegate += test;

            Debug.Log("Listening SST");
            yield return new WaitUntil(() => final);
            transcriptionDelegate -= test;
            Debug.Log("Stopped   SST");
        }
    }

    /// <summary>
    /// Am besten nur bei dem Intermediate results callback verwenden, um frühzeitig aus STT herauszubrechen.
    /// </summary>
    public void StopSTT()
    {
        StopRecording();
        var stop_task = api.StopSTT_Task();
    }

    public void StopRecording(string device = null)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (device != null)
                Microphone.End(device);
            else
                foreach (var dev in Microphone.devices)
                {
                    if (Microphone.IsRecording(dev))
                        Microphone.End(dev);
                }
        });
    }

    private IEnumerator RecordingHandler(AudioClip _recording, string _microphoneID)
    {

        if (_recording == null)
        {
            Debug.Log("Stopping");
            yield break;
        }

        bool bFirstBlock = true;
        int midPoint = _recording.samples / 2;
        float[] samples = null;

        while (_recording != null)
        {
            int writePos = Microphone.GetPosition(_microphoneID);
            if (writePos > _recording.samples || !Microphone.IsRecording(_microphoneID))
            {
                Debug.Log("RecordingHandler() Microphone disconnected.");

                yield break;
            }

            if ((bFirstBlock && writePos >= midPoint)
              || (!bFirstBlock && writePos < midPoint))
            {
                // front block is recorded, make a RecordClip and pass it onto our callback.
                samples = new float[midPoint];
                _recording.GetData(samples, bFirstBlock ? 0 : midPoint);

                //AudioData record = new AudioData();
                //record.MaxLevel = Mathf.Max(Mathf.Abs(Mathf.Min(samples)), Mathf.Max(samples));
                var clip = AudioClip.Create("Recording", midPoint, _recording.channels, recordingHZ, false);
                clip.SetData(samples, 0);

                //_service.OnListen(record);
                api.sendAudioData(AudioAPI.GetL16(clip));

                bFirstBlock = !bFirstBlock;
            }
            else
            {
                // calculate the number of samples remaining until we ready for a block of audio, 
                // and wait that amount of time it will take to record.
                int remaining = bFirstBlock ? (midPoint - writePos) : (_recording.samples - writePos);
                float timeRemaining = (float)remaining / (float)recordingHZ;

                yield return new WaitForSeconds(timeRemaining);
            }
        }
        yield break;
    }

    private IEnumerator SendAudioData(AudioClip recording, string device)
    {
        Debug.Log("Sending Audio");
        int offset = 0;
        yield return new WaitForSeconds(1f);

        //float[] data = new float[recording.samples * recording.channels];
        //recording.GetData(data, 0);
        //var data = SavWav.ConvertAudioClipToByteArray(recording, offset, Microphone.GetPosition(device));

        var clip = AudioClip.Create("Recording", recording.samples, recording.channels, recordingHZ, false);
        var samples = new float[recording.samples];

        recording.GetData(samples, 0);

        clip.SetData(samples, 0);

        offset = Microphone.GetPosition(device);

        //var arr = new ArraySegment<byte>(data);

        api.sendAudioData(AudioAPI.GetL16(clip));

        yield return new WaitForSeconds(1);

    }

    private void OnGUI()
    {
        // For WebGL
        // https://github.com/tgraupmann/UnityWebGLMicrophone
        // https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html
        if (!SelectedMicrophone)
        {
            var x = 10;
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            
            GUI.color = Color.green;
            GUILayout.Label("Mikrofon auswählen:");
            GUI.color = Color.white;

            foreach (var device in Microphone.devices)
            {
                
                if (GUILayout.Button(device, GUILayout.ExpandWidth(true)))
                {
                    //StartRecording(device);
                    SelectedMicrophoneDevice = device;
                    SelectedMicrophone = true;
                }
                x += 22;
            }
            GUILayout.EndVertical();
        }

        if (isRecording)
        {

            GUI.color = Color.red;

            GUI.Label(new Rect(10, 30, 500, 100), "Mikrofon ist an.");
            // Draw MicOn Texture bottom center of the screen
            MicOn.SetActive(true);
            MicOff.SetActive(false);

            // Button to stop recording at bottom center of the screen
            if (GUI.Button(new Rect(10, Screen.height - 110, 100, 100), "Mic off"))
            {
                StopSTT();
            }


            //MobileSpecificSettings.Instance.InfoText.SetText("Jetzt bitte sprechen.");
        }else{
            GUI.color = Color.green;
            MicOff.SetActive(true);
            MicOn.SetActive(false);
        }
    }
}
