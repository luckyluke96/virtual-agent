using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public class TTSAPI : MonoBehaviour
{

    public AudioSource sourceLipSync;


    public enum GenderVoice
    {
        female,
        male
    }

    [SerializeField]
    public GenderVoice genderVoice;


    [Header("DelayForDeepFaceLive")]
    public bool AddDelayToAudio;
    [Range(0f, 5f)]
    public float delayForSyncing;
    private AudioSource sourceAudioOut;

    private float audioPlayingUntil = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        if (sourceAudioOut == null)
            sourceAudioOut = GetComponent<AudioSource>();
        //TextToSpeechAndPlay("Hello");
    }

    /// <summary>
    /// Startet eine Coroutine die so lange läuft, bis die Sprachausgabe abgeschlossen ist
    /// </summary>
    /// <param name="text"></param>
    /// <param name="FinishedPlayingAudio_CB"></param>
    /// <returns></returns>
    public Coroutine TextToSpeechAndPlay(string text, Action FinishedPlayingAudio_CB = null, float yield_delta = 0.0f)
    {
        return StartCoroutine(StartAudioStream(text, FinishedPlayingAudio_CB, yield_delta));
    }

    /// <summary>
    /// Make a request to the TTS API and play the audio
    /// </summary>
    /// <param name="text">The text to be spoken</param>
    /// <param name="FinishedPlayingAudio_CB">The callback to be invoked when the audio has finished playing</param>
    /// <param name="yield_delta">An optional delay to be added to the yield, e.g. to return 1 seconds before the playback has finished</param>
    /// <returns></returns>
    private IEnumerator StartAudioStream(string text, Action FinishedPlayingAudio_CB, float yield_delta = 0.0f)
    {
        // End all lines of text with a period
        var t = text.Split('\n');
        for (int i = 0; i < t.Length; i++)
        {
            t[i] = t[i].Trim();
            if (t[i].Length > 0 && !t[i].EndsWith("."))
            {
                t[i] += ".";
            }
        }
        text = string.Join("\n", t);

        Debug.LogFormat("StartAudioStream: {0}", text);

        // Prepare Gender and Localization Audio String

        string voiceGenderLocalization = "";

        if (genderVoice == GenderVoice.female)
        {
            voiceGenderLocalization = "nova";
        }
        else
        {
            voiceGenderLocalization = "onyx";
        }


        // Prepare stream
        string url = $"{AgentSettings.nlp_server}tts-openai?text={UnityWebRequest.EscapeURL(text)}&key=Azxx8Lw7gkFrNeNr7Wy8pxU4&" +
            $"voice={(voiceGenderLocalization)}";
        DownloadHandlerAudioClip downloadHandler = new DownloadHandlerAudioClip(string.Empty, AudioType.MPEG);
        downloadHandler.streamAudio = false;
        UnityWebRequest request = new UnityWebRequest(url, "GET", downloadHandler, null);

        // Start stream
        UnityWebRequestAsyncOperation token = request.SendWebRequest();
        AudioClip audioClip = null;
        while (audioClip == null) // Ensure audio header completed
        {
            try
            {
                audioClip = DownloadHandlerAudioClip.GetContent(request);
            }
            catch (Exception) { }
            //Debug.LogFormat("Waiting for AudioClip: bytes={0}", request.downloadedBytes); 
            yield return 1f;
        }

        // Make sure, no audio is playing, wait otherwise
        if (audioPlayingUntil > Time.time)
        {
            yield return new WaitForSeconds(audioPlayingUntil - Time.time);
        }

        // Now we have the audio header, stream the rest
        // Für Chat App zwischenmeldung ausblenden
        //MobileSpecificSettings.Instance.InfoText.SetText("");

        // Play AudioClip
        // sourceLipSync.clip = audioClip;
        // var clonedCliP = CloneAudioClip(audioClip, "unmuted");
        var clip2 = CloneAudioClip(audioClip, "clip2");
        sourceLipSync.PlayOneShot(audioClip);


        // sourceAudioOut.clip = audioClip;
        // sourceAudioOut.PlayDelayed(0.5f);

        if (AddDelayToAudio)
        {
            StartCoroutine(PlayOneShotDelayed(clip2, delayForSyncing));
        }


        Debug.Log($"Waiting for {audioClip.length} seconds.");
        audioPlayingUntil = Time.time + audioClip.length;

        yield return new WaitForSeconds(Mathf.Max(audioClip.length + yield_delta, 0.0f));

        if (FinishedPlayingAudio_CB != null)
        {
            FinishedPlayingAudio_CB.Invoke();
        }

    }

    public AudioClip CloneAudioClip(AudioClip audioClip, string newName)
    {
        AudioClip newAudioClip = AudioClip.Create(newName, audioClip.samples, audioClip.channels, audioClip.frequency, false);
        float[] copyData = new float[audioClip.samples * audioClip.channels];
        audioClip.GetData(copyData, 0);
        newAudioClip.SetData(copyData, 0);
        return newAudioClip;
    }

    private IEnumerator PlayOneShotDelayed(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        sourceAudioOut.PlayOneShot(clip);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
