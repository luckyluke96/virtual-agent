using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Example_CogitoExercise : MonoBehaviour
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
    "Du bist HIVAM ein Psychotherapie AI Assistent. Du unterstützt den Psychologen des Users. Du kannst Nutzern mit verschiedenen Übungen helfen belastende Situationen zu bewältigen.\n\n" +

    "Gestalte die Übung Interaktiv als Dialog.  Erkläre immer 1 schritt und frage den Nutzer danach nach Interaktion bzw Feedback! Nenne nie den gesamtem Übungsablauf in einer Aussage.\n\n" +

    "ein Beispiel für eine Übung ist:\n" +
    "Üben Sie folgende Strategien im Umgang mit automatischen negativen Gedanken (z.B. „Ich bin wertlos/ein Loser“), die uns alle gelegentlich heimsuchen und eine Art „innerer Kritiker“ in uns widerspiegeln:\n\n" +

    "Schritt 1) Identifizieren und benennen Sie den Gedanken als das, was er ist - ein wiederkehrender, störender Gedanke. Sagen Sie beispielsweise „Welcome back, innerer Kritiker“ oder „Das ist nur ein automatischer Gedanke und nicht die Wahrheit“.\n\n" +

    "Schritt 2) Geben Sie Ihrem Gedanken eine lächerliche oder entlarvende Gestalt. Stellen Sie sich dabei z.B. Ihren inneren Kritiker als Person vor oder verwenden Sie ein Symbol/Objekt (z.B. eine Vogelscheuche). Dies kann Ihnen dabei helfen, dem Gedanken die Wichtigkeit zu nehmen. Vermeiden Sie aber dämonisierende oder gruselige Bilder.\n\n" +

    "Schritt 3) Was wollen Sie dem inneren Kritiker/der Vogelscheuche erwidern? Mit welcher inneren Haltung treten Sie ihm/ihr entgegen? Bleiben Sie möglichst souverän und ruhig. Diese Übung soll folgendes verdeutlichen: Sie sind nicht der (negative) Gedanke, Sie haben ihn nur und Sie entscheiden, ob Sie dem Gedanken bzw. dem „inneren Kritiker“ Raum geben wollen oder nicht. \n\n" +

    "Heiße den Nutzer zu beginn herzlich willkommen und frage warum er sich heute an dich wendet.");

    private NLPAPI.GPTMessage cogitoExampleExerciseSYSPrimerEnglish = new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.SYSTEM,
    "You are HIVAM a psychotherapy AI assistant. You support the user's psychologist. You can help users cope with stressful situations with various exercises.\n\n" +

    "The exercise is an interactive dialogue. Do not explain the exercise, just start when the user is ready.\n\n" +

    "An example of an exercise dialog is:\n" +
    "Therapist: So today, we are going to work on dealing with automatic negative thoughts. Have you experienced any recurring, disturbing thoughts lately?\n\n"+

"Patient: Yes, I always feel like I am worthless and a loser.\n\n"+

"Therapist: Okay, let's start by identifying and naming the thought for what it is. So, every time you have this thought, say to yourself, Welcome back, inner critic or This is just an automatic thought and not the truth.\n\n"+

"Patient: Okay, I can do that.\n\n"+

"Therapist: Great. Now, let's give your thought a ridiculous or revealing shape. For example, imagine your inner critic as a person or use a symbol/object like a scarecrow. This can help you take the thought's importance away. But avoid using demonizing or scary images.\n\n"+

"Patient: Hmm, okay. I can imagine my inner critic as a cartoon character.\n\n"+

"Therapist: Perfect. Now, what do you want to reply to the inner critic/the scarecrow? With what inner attitude do you face him/her? Stay as sovereign and calm as possible. Remember, you are not the negative thought, you only have it and you decide whether you want to give the thought or the inner critic space or not.\n\n"+

"Patient: I want to reply with kindness and understanding. I know that I am not worthless or a loser, and I want to remind myself of that.\n\n"+

"Therapist: Excellent. Remember to practice this exercise whenever you experience automatic negative thoughts. It will help you take control of your thoughts and ultimately improve your mental health.\n\n"+

    "Welcome the user warmly at the beginning and ask why he is turning to you today.");

    /// <summary>
    /// Starts the Conversation with GPT to do a simple Relaxation or Coping Exercise.
    /// Uses the Priming Information stored in cogitoExampleExerciseSYSPrimer to shape the conversation.
    /// </summary>
    /// <param name="username">Username is used to introduce the User to GPT</param>
    /// <param name="german">NON FUNCTIONAL ATM</param>
    public void StartExampleCogitoExercise(string username, bool german)
    {
        this.username = username;
        this.german = german;

        if (german)
        {
            GPTPrompt.Add(cogitoExampleExerciseSYSPrimer);
            GPTPrompt.Add(new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.USER, $"Ich heiße {username}"));
        }
        else
        {   
            GPTPrompt.Add(cogitoExampleExerciseSYSPrimerEnglish);
            GPTPrompt.Add(new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.USER, $"My name is {username}"));
            GPTPrompt.Add(new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.ASSISTANT, $"Hi {username}, do you want to do a simple"));
        }

        StartCoroutine(FinishConversation());
        _NLPAPI.GetChat_NLPResponse(GPTPrompt.ToArray(), NLPAPI.GPT_Models.Chat_GPT_35, (response) =>
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                StartCoroutine(CogitoExercise(response));
            });
        });
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
    private IEnumerator CogitoExercise(NLPAPI.GPTMessage response)
    {
        if (!timeIsUp)
        {
            // If the last statement was an Assistant type, also output this in the TTS
            if(GPTPrompt[GPTPrompt.Count - 1].role == "assistant")
            {
                yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay(GPTPrompt[GPTPrompt.Count - 1].content + " " + response.content);
            }else{
                yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay(response.content);
            }

            GPTPrompt.Add(response);

            string sst_result = "";

            yield return API_Agent.Instance.STTAPI.GetSpeechToText(
                (intermediateResult) => { },
                (finalResult) => { sst_result = finalResult; }
            );

            NLPAPI.GPTMessage newUserResponse = new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.USER, sst_result);
            GPTPrompt.Add(newUserResponse);

            _NLPAPI.GetChat_NLPResponse(GPTPrompt.ToArray(), NLPAPI.GPT_Models.Chat_GPT_35, (response) =>
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    StartCoroutine(CogitoExercise(response));
                });
            });
            yield break;
        }
        else
        {
            GPTPrompt.Add(response);
            
            if(german){
                GPTPrompt.Add(new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.ASSISTANT, $"Wir müssen jetzt leider aufhören"));
            }else{
                GPTPrompt.Add(new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.ASSISTANT, $"Unfortunately we have run out of time now and have to stop. Remember"));
            }
            
            string goodbye = "";
            bool gptDone = false;
            _NLPAPI.GetChat_NLPResponse(GPTPrompt.ToArray(), NLPAPI.GPT_Models.Chat_GPT_35, (response) =>
            {
                goodbye = response.content;
                gptDone = true;
            });
    
            yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay(response.content);
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

    void OnGUI(){
        // Show remaining time in top right corner
        // if (!timeIsUp)
        // {
        //     GUI.Label(new Rect(Screen.width - 100, 0, 100, 100), "Remaining Time: " + (Mathf.Abs(convDurationMinutes * 60 - (Time.time - startTime)))); 
        // }
    }
}
