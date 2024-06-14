using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeniorenChat : MonoBehaviour
{
    public MicrophoneRecorder MicrophoneRecorder;
    public TTSAPI _TTSAPI;
    public NLPAPI _NLPAPI;

    private List<NLPAPI.GPTMessage> GPTPrompt = new List<NLPAPI.GPTMessage>();

    private bool timeIsUp = false;
    private string username = "Peter";

    public int convDurationMinutes = 2;

    private float startTime = 0;

    private bool german = true;

    private NLPAPI.GPTMessage cogitoExampleExerciseSYSPrimer = new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.SYSTEM,
    "Du bist ein freundlicher AI Assistent. Du beantwortest für ältere Menschen fragen. Du bist immer höflich, nett und versuchst die Fragen sehr gut und SeniorengerechtW zu beantworten.\n\n" +

    "Heiße den Nutzer zu beginn herzlich willkommen und frage was für eine Frage er hat.");

    private NLPAPI.GPTMessage cogitoExampleExerciseSYSPrimerEnglish = new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.SYSTEM,
    "You are HIVAM a psychotherapy AI assistant. You support the user's psychologist. You can help users cope with stressful situations with various exercises.\n\n" +

    "Welcome the user warmly at the beginning and ask why he is turning to you today.");

    /// <summary>
    /// Starts the Conversation with GPT to do a simple Relaxation or Coping Exercise.
    /// Uses the Priming Information stored in cogitoExampleExerciseSYSPrimer to shape the conversation.
    /// </summary>
    /// <param name="username">Username is used to introduce the User to GPT</param>
    /// <param name="german">NON FUNCTIONAL ATM</param>
    public void StartSeniorChat(string username, bool german)
    {
        this.username = username;
        this.german = german;

        GPTPrompt.Clear();

        // TODO STT per button beenden

        // TODO Gucken ob streaming funktioniert

        // TODO Kiosk Mode  

        // TODO Anzeigen wenn Sprachausgabe läuft -> Tonaus prüfen

        // TODO Connection prüfen bei button klick

        // TODO Design Guidelines wg. CHI Accessibility

        if (german)
        {
            GPTPrompt.Add(cogitoExampleExerciseSYSPrimer);
            GPTPrompt.Add(new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.USER, $"Ich heiße {username}."));
            GPTPrompt.Add(new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.ASSISTANT, $"Hallo {username}, was für eine Frage"));
        }
        else
        {
            GPTPrompt.Add(cogitoExampleExerciseSYSPrimerEnglish);
            GPTPrompt.Add(new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.USER, $"My name is {username}"));
            GPTPrompt.Add(new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.ASSISTANT, $"Hi {username}, do you want to do a simple"));
        }

        //StartCoroutine(FinishConversation());
        //if(german)
        MobileSpecificSettings.Instance.InfoText.SetTextAnimatedLoop("Ich denke nach\n ", "...");

        // _NLPAPI.GetChat_NLPResponse(GPTPrompt.ToArray(), NLPAPI.GPT_Models.Chat_GPT_35, (response) =>
        // {
        //     UnityMainThreadDispatcher.Instance().Enqueue(() =>
        //     {
        //         MobileSpecificSettings.Instance.InfoText.SetText("Ich bin fast fertig.");
        //         StartCoroutine(CogitoExercise(response));
        //     });
        // });

        // StartCoroutine(StartConversation());

        Start_NLPandPlayTTS(GPTPrompt, (response) =>
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                // MobileSpecificSettings.Instance.InfoText.SetText("Ich bin fast fertig.");
                StartCoroutine(ContinueChat(response));
            });
        });
        

    }

    private Coroutine Start_NLPandPlayTTS(List<NLPAPI.GPTMessage> input, Action<NLPAPI.GPTMessage> callback)
    {
        return StartCoroutine(NLPandPlayTTS(input, callback));
    }

    private IEnumerator NLPandPlayTTS(List<NLPAPI.GPTMessage> input, Action<NLPAPI.GPTMessage> callback)
    {
        string responseText = "";
        if (GPTPrompt[GPTPrompt.Count - 1].role == "assistant")
            responseText = GPTPrompt[GPTPrompt.Count - 1].content;

        List<string> toPlay = new List<string>();
        bool isDone = false;
        NLPAPI.GPTMessage result = null;
        
        _NLPAPI.GetChat_NLPResponseStreamed(GPTPrompt.ToArray(), NLPAPI.GPT_Models.Chat_GPT_35, (response) =>
        {
            isDone = true;
            result = response;
        }, (stream_response) =>
        {
            if(!stream_response.finished){
                MobileSpecificSettings.Instance.InfoText.SetText("Ich bin fast fertig.");
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
                yield return _TTSAPI.TextToSpeechAndPlay(toPlayString, null, -1f);
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

    /// <summary>
    /// Recursive Function which takes a Statement as input, outputs it via the Text to Speech API and 
    /// Captures a Response by the client via the Speech to Text API.
    /// Once the Response is recorded, the new Statement is sent to GPT to produce an adequate answer.
    /// With the Answer of GPT this Function calls itself to start another Dialogue cycle.
    /// 
    /// This repeats itself until the timeIsUp bool is set to true. Once the time is up, GPT will evaluate one last Goodbye 
    /// statement and output it vie TTS API.
    /// </summary>
    /// <param name="response">The Statement from GPT or the System on which the User should act.</param>
    /// <returns></returns>
    private IEnumerator ContinueChat(NLPAPI.GPTMessage response)
    {
        // // If the last statement was an Assistant type, also output this in the TTS
        // if (GPTPrompt[GPTPrompt.Count - 1].role == "assistant")
        // {
        //     // yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay(GPTPrompt[GPTPrompt.Count - 1].content + " " + response.content);
        // }
        // else
        // {
        //     yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay(response.content);
        // }

        if (MobileSpecificSettings.Instance.continuationState == MobileSpecificSettings.ContinuationState.UNDEFINED)
        {
            MobileSpecificSettings.Instance.ShowContinueButtons(true);
            MobileSpecificSettings.Instance.InfoText.SetText("Möchten Sie dieses Gespräch fortführen, oder haben Sie eine andere Frage?");
        }

        yield return new WaitUntil(() => MobileSpecificSettings.Instance.continuationState != MobileSpecificSettings.ContinuationState.UNDEFINED);
        if (MobileSpecificSettings.Instance.continuationState == MobileSpecificSettings.ContinuationState.NEW_QUESTION)
        {
            MobileSpecificSettings.Instance.continuationState = MobileSpecificSettings.ContinuationState.CONTINUE_THREAD;
            StartSeniorChat(username, german);
            yield break;
        }
        else if (MobileSpecificSettings.Instance.continuationState == MobileSpecificSettings.ContinuationState.CONTINUE_THREAD)
        {
            // Just Continue for now
        }
        else
        {
            Debug.LogError("Unreachable");
            yield break;
        }

        MobileSpecificSettings.Instance.continuationState = MobileSpecificSettings.ContinuationState.UNDEFINED;

        if (!timeIsUp)
        {

            GPTPrompt.Add(response);

            string sst_result = "";

            bool firstResult = true;
            yield return API_Agent.Instance.STTAPI.GetSpeechToText(
                (intermediateResult) =>
                {
                    if (firstResult)
                    {
                        firstResult = false;
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            MobileSpecificSettings.Instance.StopSTTBtn.SetActive(true);
                        });
                    }
                },
                (finalResult) => { sst_result = finalResult; }
            );
            MobileSpecificSettings.Instance.StopSTTBtn.SetActive(false);

            NLPAPI.GPTMessage newUserResponse = new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.USER, sst_result);
            GPTPrompt.Add(newUserResponse);

            MobileSpecificSettings.Instance.InfoText.SetTextAnimatedLoop("Ich denke nach\n ", "...");
            // _NLPAPI.GetChat_NLPResponse(GPTPrompt.ToArray(), NLPAPI.GPT_Models.Chat_GPT_35, (response) =>
            // {
            //     UnityMainThreadDispatcher.Instance().Enqueue(() =>
            //     {
            //         MobileSpecificSettings.Instance.InfoText.SetText("Ich bin fast fertig.");
            //         StartCoroutine(ContinueChat(response));
            //     });
            // });
            Start_NLPandPlayTTS(GPTPrompt, (response) =>
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    // MobileSpecificSettings.Instance.InfoText.SetText("Ich bin fast fertig.");
                    StartCoroutine(ContinueChat(response));
                });
            });
            yield break;
        }
        else
        {
            GPTPrompt.Add(response);

            if (german)
            {
                GPTPrompt.Add(new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.ASSISTANT, $"Wir müssen jetzt leider aufhören"));
            }
            else
            {
                GPTPrompt.Add(new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.ASSISTANT, $"Unfortunately we have run out of time now and have to stop. Remember"));
            }

            string goodbye = "";
            bool gptDone = false;
            _NLPAPI.GetChat_NLPResponse(GPTPrompt.ToArray(), NLPAPI.GPT_Models.Chat_GPT_35, (response) =>
            {
                goodbye = response.content;
                gptDone = true;
            });

            yield return new WaitUntil(() => gptDone);
            // yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay(goodbye);
            Debug.Log(GPTPrompt[GPTPrompt.Count - 1].content + " " + goodbye);
            yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay(GPTPrompt[GPTPrompt.Count - 1].content + " " + goodbye);
            // stop unity editor
            //UnityEditor.EditorApplication.isPlaying = false;
            yield break;
        }
    }

    /// <summary>
    /// This Method will prompt GPT with the Statement to End the conversation after the time specified in 
    /// convDurationMinutes ran out. Sets the timeIsUp bool to true and stops all currently runing Coroutines resulting in 
    /// a interruption of the ongoing conversation.
    /// Calls the CogitoExercise Methos one last time in order to let GPT output its goodbye Prompt.
    /// </summary>
    /// <returns>IEnumerator to let this function be executed as a Coroutine</returns>
    private IEnumerator FinishConversation()
    {
        NLPAPI.GPTMessage goodbyeMessage = new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.USER, "Wir müssen nun leider aufhören, unsere zeit ist fast vorbei und meine nächsten Patienten warten schon");

        startTime = Time.time;
        int secTilEnd = convDurationMinutes * 60;
        yield return new WaitForSecondsRealtime(secTilEnd);
        timeIsUp = true;
        // StopCoroutine(coroutineHandle);
        // StopAllCoroutines();
        // StartCoroutine(CogitoExercise(goodbyeMessage));
    }

    void OnGUI()
    {
        return;
        // Show remaining time in top right corner
        if (!timeIsUp)
        {
            GUI.Label(new Rect(Screen.width - 100, 0, 100, 100), "Remaining Time: " + (Mathf.Abs(convDurationMinutes * 60 - (Time.time - startTime))));
        }
    }
}
