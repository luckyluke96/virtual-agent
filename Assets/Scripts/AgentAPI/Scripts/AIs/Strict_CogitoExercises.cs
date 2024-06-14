using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Strict_CogitoExercises : MonoBehaviour
{
    public MicrophoneRecorder MicRecorder;
    public TTSAPI TTSAPI;
    public NLPAPI NLPAPI;

    private bool timeIsUp = false;
    private string username = "Peter";
    private int exerciseNo = 1;
    public int convDurationMinutes = 2;

    private bool german = true;
    private float startTime = 0;

    private List<NLPAPI.GPTMessage> GPTPrompt = new List<NLPAPI.GPTMessage>();

    private NLPAPI.GPTMessage sysPrimer = new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.SYSTEM,
    "Du bist ein AI Psychologen Assistent. Deine Aufgabe ist es Übungen mit Benutzern durchzuführen.");

    private NLPAPI.GPTMessage agentExplanationPrompt = new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.USER,
    "Ich werde dir einen Übungstext geben, welchen du in einzelne schritte aufteilst. Erkläre mir immer genau einen schritt und warte ab, bis ich dir geantwortet habe. Gehe auf meine Antworten ein. Wichtig, der Text wird in eine Sprachausgabe gegeben, also benutze keine komplexen Satzzeichen wie Sternchen, Semikolon oder ähnliches. Versuche außerdem die Übung fließend zu gestalten und lass die Nutzer nicht die einzelnen Schritte genau wissen.");

    private NLPAPI.GPTMessage exercise1Text = new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.USER,
    "Menschen, die zu psychischen Problemen neigen, haben oftmals „doppelte Standards“ bei moralischen Bewertungen – häufig ohne dies zu wissen. Aufgrund entsprechender Erziehung wird eine höhere moralische Messlatte an sich selbst als an andere angelegt. Ist dies auch bei Ihnen der Fall?\n" +

    "Stellen Sie sich zwei bis vier Missgeschicke der folgenden Art vor: Ihnen wird Geld gestohlen, weil Sie vielleicht die Autotür nicht abgeschlossen haben. Eine andere Situation könnte sein: Sie haben den Geburtstag eines guten Freundes vergessen. Überlegen Sie nun, wie hart und mitleidslos Sie vielleicht mit sich selbst ins Gericht gehen würden oder sogar schon gegangen sind in solchen Situationen.\n" +

    "Wären Sie bei einem Freund, dem dasselbe passiert, genauso streng? Bei zukünftigem, tatsächlichem oder angeblichem Fehlverhalten versuchen Sie, sich selbst das zu sagen, was Sie in einer vergleichbaren Situation einem guten Freund erwidern würden. Wahrscheinlich würden Sie ihn trösten und gute Gründe nennen, weshalb sein Missgeschick verzeihlich ist.");

    private NLPAPI.GPTMessage exercise2Text = new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.USER,
    "Neuer Blickwinkel\n" +

    "Nehmen Sie Ihrem Denken gegenüber eine neue Position ein\n" +

    "Stellen Sie sich folgende Fragen: Sehen Sie sich als Wächter Ihrer Gedanken oder als Gefangener ? Warum ? Welchen Ihrer Gedanken können Sie sich nie merken ? Unter der Dusche kommen einem oft die besten Ideen.Wo kommen Ihnen die schlechtesten Ideen ? Welchen Gedanken würden Sie niemals denken ? \n" +

    "Wahrscheinlich werden Sie auf die meisten Fragen keine klaren Antworten gefunden haben.Das ist auch gar nicht das Ziel der Übung.Vielmehr soll die Übung zeigen, was wir mit unserem Denken eigentlich Tolles anstellen können.Gedankenspiele sind hilfreiche Metakognitionen(d.h. „Denken über das Denken“), die uns verblüffen und Spaß bereiten können.Gleichzeitig helfen sie, einseitige oder festgefahrene Denkmuster aufzubrechen, von denen bekannt ist, dass sie psychische Probleme begünstigen.\n " +

    "Lassen Sie Ihren Gedanken also ihren Lauf bzw.gestehen Sie ihnen ein gewisses Eigenleben zu.Der Versuch, sie zu kontrollieren, verstärkt dagegen negative Empfindungen.");

    private NLPAPI.GPTMessage exercise3Text = new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.USER,
    "Bringen Sie Farbe in Ihre Welt\n" +

    "Manche Menschen neigen zu „Schwarz-Weiß-Denken“, gerade in negativen Situationen, was die Bewertung komplexer Situationen zwar vereinfacht, jedoch der Realität selten gerecht wird. Fast alles ist relativ (tritt nicht „immer“ oder „nie“, sondern „manchmal“ auf; betrifft nicht „alle“ oder „keine“, sondern „manche“ oder „viele“). Besonders wenn es um die eigene Person geht, kann eine einseitige Sichtweise schädlich sein, gerade bei negativen Gedanken, denn kein Mensch ist perfekt und makellos, aber auch nicht von Grund auf schlecht.\n" +

    "Kennen Sie solche 'Schwarz-Weiß-Gedanken' von sich selbst ? Beschreiben Sie sich gelegentlich mit Extremen(z.B.der Dümmste oder hässlich zu sein) ?\n" +

    "Nehmen Sie jeweils einen konkreten Gedanken und hinterfragen Sie dieses Urteil über sich selbst. Überlegen Sie anschließend eine Alternative, die mehr „Farben“ (Abstufungen) hat als der ursprüngliche Gedanke und notieren Sie sich diese. Wenn Sie z.B. den Gedanken „Ich bin der Dümmste“ hatten, könnte eine Relativierung lauten: „Ich habe vielleicht nicht das Pulver erfunden und kenne nicht jedes Fremdwort, aber ich weiß, wie man an Autos schraubt, verstehe viel von Handball und bin ein guter Zuhörer“. Versuchen Sie in Zukunft, vermehrt darauf zu achten, nicht „schwarz-weiß“ zu denken und alternative Gedanken zu finden, wenn Sie sich dabei erwischen, in negativen Extremen über sich selbst zu urteilen.");

    public void StartExercise(string username, bool german = true, int exerciseNo = 1)
    {
        this.username = username;
        this.german = german;
        this.exerciseNo = exerciseNo;

        if (german)
        {
            GPTPrompt.Add(sysPrimer);
            GPTPrompt.Add(agentExplanationPrompt);

            switch (exerciseNo)
            {
                case 1:
                    GPTPrompt.Add(exercise1Text);
                    break;
                case 2:
                    GPTPrompt.Add(exercise2Text);
                    break;
                case 3:
                    GPTPrompt.Add(exercise3Text);
                    break;
                default:
                    GPTPrompt.Add(exercise1Text);
                    break;
            }

            GPTPrompt.Add(new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.USER,
            $"Ab jetzt sprichst du direkt mit dem Benutzer namens {username}. Spreche ihn mit Du oder seinem Namen an. Begrüße ihn freundlich und frage ihn, ob er die übung machen möchte und welches ziel diese hat."));
        }
        else
        {
            //TODO: Handle English Version
        }

        // StartCoroutine(FinishConversation());

        Start_NLPandPlayTTS(GPTPrompt, (response) =>
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                StartCoroutine(CogitoExercise1_Strict(response));
            });
        });

        // NLPAPI.GetChat_NLPResponse(GPTPrompt.ToArray(), NLPAPI.GPT_Models.Chat_GPT_35, (response) =>
        // {
        //     UnityMainThreadDispatcher.Instance().Enqueue(() =>
        //     {
        //         StartCoroutine(CogitoExercise1_Strict(response));
        //     });
        // });
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
        
        NLPAPI.GetChat_NLPResponseStreamed(GPTPrompt.ToArray(), NLPAPI.GPT_Models.Chat_GPT_35, (response) =>
        {
            isDone = true;
            result = response;
        }, (stream_response) =>
        {
            if(!stream_response.finished){
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

    private IEnumerator CogitoExercise1_Strict(NLPAPI.GPTMessage response)
    {
        if (!timeIsUp)
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
                    StartCoroutine(CogitoExercise1_Strict(response));
                });
            });
            yield break;
        }
        else
        {
            GPTPrompt.Add(response);

            if (german)
            {
                GPTPrompt.Add(new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.ASSISTANT, $"Wir müssen jetzt leider aufhören "));
            }
            else
            {
                GPTPrompt.Add(new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.ASSISTANT, $"Unfortunately we have run out of time now and have to stop. Remember"));
            }

            string goodbye = "";
            bool gptDone = false;
            ; NLPAPI.GetChat_NLPResponse(GPTPrompt.ToArray(), NLPAPI.GPT_Models.Chat_GPT_35, (response) =>
            {
                goodbye = response.content;
                gptDone = true;
            });

            // yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay(response.content);
            yield return new WaitUntil(() => gptDone);
            // yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay(goodbye);
            Debug.Log(GPTPrompt[GPTPrompt.Count - 1].content + " " + goodbye);
            yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay(GPTPrompt[GPTPrompt.Count - 1].content + " " + goodbye);

#if UNITY_EDITOR
            // stop unity editor
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // stop application
            Application.Quit();
#endif
            
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


