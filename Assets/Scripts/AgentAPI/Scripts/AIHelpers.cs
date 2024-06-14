using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIHelpers : MonoBehaviour
{

    public MicrophoneRecorder MicRecorder;
    public TTSAPI TTSAPI;
    public NLPAPI NLPAPI;

    // Start is called before the first frame update
    void Start()
    {
        MicRecorder = FindObjectOfType<MicrophoneRecorder>();
        TTSAPI = FindObjectOfType<TTSAPI>();
        NLPAPI = FindObjectOfType<NLPAPI>();
    }


    public IEnumerator NLPandPlayTTS(List<NLPAPI.GPTMessage> input, Action<NLPAPI.GPTMessage> callback, List<NLPAPI.GPTMessage> GPTPrompt)
    {
        string responseText = "";
        if (GPTPrompt[GPTPrompt.Count - 1].role == "assistant")
            responseText = GPTPrompt[GPTPrompt.Count - 1].content;

        List<string> toPlay = new List<string>();
        bool isDone = false;
        NLPAPI.GPTMessage result = null;

        NLPAPI.GetChat_NLPResponseStreamed(GPTPrompt.ToArray(), NLPAPI.GPT_Models.Chat_GPT_35, (response) =>
        {
            isDone = true;
            result = response;
        }, (stream_response) =>
        {
            if (!stream_response.finished)
            {
                // MobileSpecificSettings.Instance.InfoText.SetText("Ich bin fast fertig.");
                responseText += " " + stream_response.delta;
                toPlay.Add(responseText);
                responseText = "";
            }
        });

        int i = 0;
        // Wait until the response is finished or if there are strings to play
        while (!isDone || toPlay.Count > i)
        {
            Debug.Log($"IsDone: {isDone} | toPlay: {toPlay.Count - i}");
            if (toPlay.Count > i)
            {
                // Combine all strings to play and play them
                var toPlayString = "";
                for (; i < toPlay.Count; i++)
                {
                    toPlayString += toPlay[i];
                }
                Debug.Log($"Playing: {toPlayString}");
                yield return TTSAPI.TextToSpeechAndPlay(toPlayString, null, -1f);
            }
            else
            {
                yield return new WaitUntil(() => isDone || toPlay.Count > 0);
            }
        }
        Debug.Log($"IsDone: {isDone} | toPlay: {toPlay.Count}");

        // If the last statement was an Assistant type, remove the last statement and add the combined one
        if (GPTPrompt[GPTPrompt.Count - 1].role == "assistant")
        {
            var last = GPTPrompt[GPTPrompt.Count - 1];
            GPTPrompt.RemoveAt(GPTPrompt.Count - 1);
            result = new NLPAPI.GPTMessage(last.role, last.content + " " + result.content);
        }

        callback(result);
        Debug.Log($"IsDone: {isDone} | toPlay: {toPlay.Count}");
    }
}
