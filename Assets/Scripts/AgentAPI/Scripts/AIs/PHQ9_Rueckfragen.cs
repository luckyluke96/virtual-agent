using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PHQ9_Rueckfragen : MonoBehaviour
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

    private NLPAPI.GPTMessage phq9PrimerSystem = new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.SYSTEM,
    "Du bist ein freundlicher psychologischer AI Assistent. Du sprichst mit dem User über psychologische Probleme. Du bist immer höflich, nett und versuchst Fragen zu beantworten und findest heraus, was für psychologische Probleme der User hat.\n\n" +

    "Kontextinformationen: Der Assistant führt ein psychologisches Diagnostikgespräch mit dem User. Er stellt einzelne Fragen des PHQ-9 Tests, zu denen er Rückfragen stellt, um genauer festzustellen, wie es der Person geht.\n" +

    "Fragen:\n" +
    "Bitte beschreiben Sie bestehende psychische Probleme und Anliegen in eigenen Worten.\n" +
    "Wie oft fühlten Sie sich im Verlauf der letzten zwei Wochen durch wenig Interesse oder Freude an Ihren Tätigkeiten beeinträchtigt? + Rückfragen\n\n" +

    "Datenschutz: Unsere Einstellungen erlauben es nicht, dass Ihre Aussagen (also Eingaben in das System) zum Weiterlernen des Sprachmodells verwendet werden können und werden laut Open.AI nach 30 Tagen gelöscht. Wir erheben die beidseitigen Chatverläufe der Gespräche mit dem Agenten inklusive Zeitstempeln und Audioaufnahmen von den Eingaben (Das grüne Mikrofonsymbol auf dem Laptop wird diese Audioaufnahmen kennzeichnen). Die Daten werden verschlüsselt auf Servern der Universität Hamburg gespeichert. Wir verwenden diese zur Verbesserung der Interaktion mit unserem intelligenten virtuellen Agenten, indem wir Fehler beim Verständnis der Ein- und Ausgabe analysieren, und Gesprächsverläaufe optimieren.\n");

    private NLPAPI.GPTMessage cogitoExampleExerciseSYSPrimerEnglish = new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.SYSTEM,
    "You are HIVAM a psychotherapy AI assistant. You support the user's psychologist. You can help users cope with stressful situations with various exercises.\n\n" +

    "Welcome the user warmly at the beginning and ask why he is turning to you today.");

    /// <summary>
    /// Starts the Conversation with GPT to do a simple Relaxation or Coping Exercise.
    /// Uses the Priming Information stored in cogitoExampleExerciseSYSPrimer to shape the conversation.
    /// </summary>
    /// <param name="username">Username is used to introduce the User to GPT</param>
    /// <param name="german">NON FUNCTIONAL ATM</param>
    public void StartPHQ9Rueckfragen(string username, bool german)
    {
        this.username = username;
        this.german = german;
        // TODO mit einfachem NLP überprüfen, ob Fragen vorgekommen sind, um die nächsten anzufangen
        GPTPrompt.Clear();

        if (german)
        {
            GPTPrompt.Add(phq9PrimerSystem);
            GPTPrompt.Add(new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.USER, $"Ich heiße {username}."));
            GPTPrompt.Add(new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.ASSISTANT, $"Guten Tag {username}, An dieser Stelle würde ich gerne genauer herausfinden, wie es Ihnen geht.\r\n\r\n" +
                $"Dazu stelle ich Ihnen Fragen, die sich auf ihr Befinden im Verlauf der letzten zwei Wochen beziehen.\r\n\r\n" +
                $"Ihre Antworten können dabei helfen festzustellen, ob gegebenenfalls Auffälligkeiten im Hinblick auf psychische Erkrankungen vorliegen.\r\n\r\n" +
                $"Ihre Daten werden absolut vertraulich und anonym behandelt.\r\n\r\nBitte versuchen Sie die gestellten Fragen offen und spontan zu beantworten.\r\n\r\n" +
                $"Bitte beachten Sie, dass dadurch keine vollständige psychiatrische Diagnostik gegeben ist. Auch gegebenenfalls vorliegende Hinweise könnten Fehlern unterliegen. " +
                $"Eine abschließende Einschätzung inwiefern psychiatrische Diagnosen vorliegen, kann ausschließlich durch qualifizierte Kliniker:innen erfolgen.\r\n\r\n" +
                $"Haben Sie weitere Fragen zum Schutz Ihrer Daten?"));
        }
        else
        {
            GPTPrompt.Add(cogitoExampleExerciseSYSPrimerEnglish);
            GPTPrompt.Add(new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.USER, $"My name is {username}"));
            GPTPrompt.Add(new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.ASSISTANT, $"Hi {username}, do you want to do a simple"));
        }

        StartCoroutine(StartPHQ9Chat());

        //StartCoroutine(FinishConversation());
        //if(german)
        // MobileSpecificSettings.Instance.InfoText.SetTextAnimatedLoop("Ich denke nach\n ", "...");

        // _NLPAPI.GetChat_NLPResponse(GPTPrompt.ToArray(), NLPAPI.GPT_Models.Chat_GPT_35, (response) =>
        // {
        //     UnityMainThreadDispatcher.Instance().Enqueue(() =>
        //     {
        //         MobileSpecificSettings.Instance.InfoText.SetText("Ich bin fast fertig.");
        //         StartCoroutine(CogitoExercise(response));
        //     });
        // });

        // StartCoroutine(StartConversation());

        // Start_NLPandPlayTTS(GPTPrompt, (response) =>
        // {
        //     UnityMainThreadDispatcher.Instance().Enqueue(() =>
        //     {
        //         // MobileSpecificSettings.Instance.InfoText.SetText("Ich bin fast fertig.");
        //         StartCoroutine(ContinueChat(response));
        //     });
        // });


    }

    private IEnumerator StartPHQ9Chat(){
        // TTS Last from GPTPrompt

        yield return _TTSAPI.TextToSpeechAndPlay(GPTPrompt[GPTPrompt.Count - 1].content);

        string sst_res = "";

        yield return API_Agent.Instance.STTAPI.GetSpeechToText(
            (intermRes) => { },
            (finalRes) => { sst_res = finalRes; }
        );

        NLPAPI.GPTMessage userPrompt = new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.USER, sst_res);
        GPTPrompt.Add(userPrompt);

        // NLP
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
        // yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay(response.content);

            GPTPrompt.Add(response);

            string sst_res = "";

            yield return API_Agent.Instance.STTAPI.GetSpeechToText(
                (intermRes) => { },
                (finalRes) => { sst_res = finalRes; }
            );

            NLPAPI.GPTMessage userPrompt = new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.USER, sst_res);
            GPTPrompt.Add(userPrompt);

            // NLPAPI.GetChat_NLPResponse(GPTPrompt.ToArray(), NLPAPI.GPT_Models.Chat_GPT_35, (response) =>
            // {
            //     UnityMainThreadDispatcher.Instance().Enqueue(() =>
            //     {
            //         StartCoroutine(CogitoExercise1_Strict(response));
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
