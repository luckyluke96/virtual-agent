using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PHQ9_SimpleChat : MonoBehaviour
{
    public MicrophoneRecorder MicrophoneRecorder;
    public TTSAPI TTSAPI;
    public NLPAPI _NLPAPI;

    private List<Tuple<string, PHQ9LikertScale>> PHQ9Answers = new List<Tuple<string, PHQ9LikertScale>>();

    private List<NLPAPI.GPTMessage> prompt = new List<NLPAPI.GPTMessage>();

    private bool started = false;
    private string username = "Peter";
    private bool repeatQuestionnaireStep = true;

    enum PHQ9LikertScale {
       notAtAll = 0,
       someDays = 1,
       moreThanHalfTheDays = 2,
       almostEveryDay = 3 
    }

    private static List<string> PHQ9Questions = new List<string>{
        "Wie oft hatten sie wenig Interesse oder Freude an Ihren Tätigkeiten?",
        "Haben Sie sich niedergeschlagen, schwermütig oder hoffnungslos gefühlt?",
        "Hatten Sie schwierigkeiten beim ein- beziehungsweise durchzuschlafen oder haben Sie mehr als üblich geschlafen?",
        "Wie oft haben Sie sich müde gefühlt oder hatten das Gefühl keine Energie zu haben?",
        "Hatten Sie weniger appetit oder das erhöhte Bedürfnis zu essen?",
        "Wie häufig hatten Sie eine schlechte Meinung von sich selbst, das Gefühl ein Versager zu sein oder Ihre Familie enttäuscht zu haben?",
        "Hatten Sie schwierigkeiten sich zu konzentrieren, beispielsweise beim Zeitunglesen oder Fernsehen?",
        "War Ihre Sprache oder Bewegungen so verlangsamt, dass auch andere dies bemerken würden? Oder waren Sie im gegenteil zappelig, ruhelos und hatten einen stärkeren Bewegungsdrang als sonst?",
        "Hatten Sie gedanken, dass Sie lieber Tot wären oder sich Leid zufügen möchten?"
    };

    private NLPAPI.GPTMessage systemPrimer = 
        new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.SYSTEM,
        "Du kannst Antworten für den PHQ9 Fragebogen auswerten. \n" +
        "Die Aussagen der User beziehen sich auf den Zeitraum von 2 wochen. \n\n" +
        "Überhaupt nicht: 1 \n" +
        "An einzelnen Tagen: 2 \n" +
        "die hälfte / mehr als die hälfte der tage: 3\n" +
        "fast jeden tag: 4");

    private NLPAPI.GPTMessage assistentExampleQuestion = 
        new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.ASSISTANT, PHQ9Questions[0]);

    private NLPAPI.GPTMessage userResponseExample = 
        new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.USER, "Ich fühlte mich an einigen Tagen so.");

    private NLPAPI.GPTMessage assistantAnswerFormat = 
        new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.ASSISTANT, "Die Antwort ist 2");

/// <summary>
/// Initialisation Method to Load the needed Priming Statements for ChatGPT
/// </summary>
    private void initPrimingPrompt()
    {
        prompt.Add(systemPrimer);
        prompt.Add(assistentExampleQuestion);
        prompt.Add(userResponseExample);
        prompt.Add(assistantAnswerFormat);
    }

    /// <summary>
    /// Called to start the Chat
    /// </summary>
    /// <param name="username">The name of the patient</param>
    /// <param name="german">If the dialogue should be in German</param>
    public void StartPHQ9_SimpleChat(string username, bool german)
    {
        started = true;
        this.username = username;
     
        initPrimingPrompt();

        StartCoroutine(Start_PHQ9());
    }

/// <summary>
/// Called to Initiate full Round of PHQ9 with Welcome Message, Explanation and all Questions in PHQ9Questions
/// It will Repeat the Explanation and every Question as long as there is no valid Result understood.
/// </summary>
/// <returns></returns>
    private IEnumerator Start_PHQ9()
    {

        yield return StartCoroutine(Welcome_Procedure());
        
        while (repeatQuestionnaireStep)
        {
            yield return StartCoroutine(Explain_PHQ9());        
        }
        repeatQuestionnaireStep = true;

        foreach (string q in PHQ9Questions)
        {
            while (repeatQuestionnaireStep)
            {
            yield return StartCoroutine(AskQuestionFromList(q));
            }
            repeatQuestionnaireStep = true;
        }

        yield return StartCoroutine(End_PHQ9());
    }

/// <summary>
/// Function to Prompt with the Welcome Message of the PHQ9 Test
/// </summary>
/// <returns></returns>
    private IEnumerator Welcome_Procedure()
    {
        yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay($"Hallo {username}, schön das du da bist! Ich würde dir gerne 9 Fragen stellen, um besser Einschätzen zu können, wie es dir momentan geht. Sollen wir anfangen?");
        
        // String zum speichern der Speech To Text Eingabe
        string sst_result = "";

        // Warte auf Spracheingabe. Mikrofon wird aktiviert. Coroutine yielded, bis eingabe vermutlich abgeschlossen wurde.
        yield return API_Agent.Instance.STTAPI.GetSpeechToText((intermediate_result) =>
        {
            // Zwischenergebnisse, wahrscheinlich unwichtig.
            Debug.Log($"SST: {intermediate_result}");
            if (intermediate_result.ToLower().Contains("ja") || intermediate_result.ToLower().Contains("nein"))
            {
                API_Agent.Instance.STTAPI.StopSTT();
            }
        },
        (final_result) =>
        {
            // Finales Ergebnis wird gespeichert
            sst_result = final_result;
        });

        Debug.Log($"SST Final: {sst_result}");
        // Irgendwas mit String machen
        if (sst_result.ToLower().Contains("ja"))
        {
            yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay($"Sehr gut!");
        }
        else
        {
            yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay($"Kein problem, wenn du dich bereit fühlst komme gerne wieder!");
        }
    }

/// <summary>
/// Prompts the User with the Likert Scale Explanation of the PHQ9 Test and asks, wether or not this has
/// been understood. 
/// It will sett the repeatQuestionnaireStep to false, once the User acknowledged that he understood tghe explanation.
/// </summary>
/// <returns></returns>
    private IEnumerator Explain_PHQ9()
    {
        yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay($"Ich erkläre dir nun kurz den Ablauf und die Antwortmöglichkeiten.");

        yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay($"Versuche dich an die letzten beiden Wochen zu erinnern. Alle Fragen die ich dir stellen werde, beziehen sich auf diesen Zeitraum. Die Frage bezieht sich jeweils auf die Anzahl der Tage an denen du dich davon beeinträchtigt gefühlt hast. Du hast jeweils die Möglichkeit auf einer Skala von 1 - gar nicht, bis 4 - immer, zu antworten. Die 1 steht hierbei für Überhaupt nicht, 2 für An einzelnen Tagen, 3 - An mehr als die hälfte der Tage, und 4 für Beinahe jeden Tag ");

        yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay($"Hast du das soweit verstanden?");

                // String zum speichern der Speech To Text Eingabe
        string sst_result = "";

        // Warte auf Spracheingabe. Mikrofon wird aktiviert. Coroutine yielded, bis eingabe vermutlich abgeschlossen wurde.
        yield return API_Agent.Instance.STTAPI.GetSpeechToText((intermediate_result) =>
        {
            // Zwischenergebnisse, wahrscheinlich unwichtig.
            Debug.Log($"SST: {intermediate_result}");
        },
        (final_result) =>
        {
            // Finales Ergebnis wird gespeichert
            sst_result = final_result;
        });

        Debug.Log($"SST Final: {sst_result}");

        // Irgendwas mit String machen
        if (sst_result.ToLower().Contains("ja"))
        {
            repeatQuestionnaireStep = false;
            yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay($"Ok! dann lass uns mit den Fragen starten");
        }
        else
        {
            yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay($"Kein problem, Ich erkläre es dir gern nochmal.");
        }
    }

/// <summary>
/// This will Prompt the User with one Single Question given as the Parameter.
/// After reading the question the answer given by the User is added to the prompt List.
/// This List is used to let ChatGPT validate the statement of the User into the Likert Scale value.
/// </summary>
/// <param name="question">The Question the User should be Prompted with.</param>
/// <returns></returns>
    private IEnumerator AskQuestionFromList(string question)
    {   
        string gptValidatedresponse = "";
        prompt.Add(new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.ASSISTANT, question));

        yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay(question);

        string sst_result = "";

        yield return API_Agent.Instance.STTAPI.GetSpeechToText((intermediate_result) =>{},
        (final_result) =>
        {
            // Finales Ergebnis wird gespeichert
            sst_result = final_result;
        });

        Debug.Log($"SST Final: {sst_result}");

        NLPAPI.GPTMessage new_user_answer = new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.USER, sst_result);
        prompt.Add(new_user_answer);

        bool isGPTready = false;

        _NLPAPI.GetChat_NLPResponse(prompt.ToArray(), NLPAPI.GPT_Models.Chat_GPT_35, (response) =>
        {
            gptValidatedresponse = response.content;
            isGPTready = true;
        });

        yield return new WaitUntil(() => isGPTready);
        yield return  GPTResponseValidation(question, gptValidatedresponse);

        prompt.RemoveRange(prompt.Count-2, 2);
    }

/// <summary>
/// This Method takes a question of the PHQ9 and a response that was validated by ChatGPT and chooses the corresponding 
/// Answer and adds the Likert Value to the PHQ9Answers List.
/// It further detects the Answer format from 1-4. This answer will be stored as a Tuple 
/// within the PHQ9Answers List<Tuple<sting, PHQ9LikertScale>>
/// Also handles the repeatQuestionnaireStep bool, which is set to false after successful Answer has ben detect
/// </summary>
/// <param name="question">the question this answer belongs to</param>
/// <param name="response">the response from ChatGPT which assessed the statement of the User into the 1-4 Likert 
/// Scale</param>
/// <returns>IEnumerator for Coroutine Execution</returns>
    private IEnumerator GPTResponseValidation(string question, string response)
    {
        Debug.Log($"GPT Answer: {response}");
        switch (response)
        {
            case string a when a.Contains("1") || a.Contains("eins"):
                PHQ9Answers.Add(new Tuple<string, PHQ9LikertScale>(question, PHQ9LikertScale.notAtAll));        
                repeatQuestionnaireStep = false;
                yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay($"Okay Überhaupt nicht in den letzten beiden Wochen");
                yield break;
            case string b when b.Contains("2") || b.Contains("zwei"):
                PHQ9Answers.Add(new Tuple<string, PHQ9LikertScale>(question, PHQ9LikertScale.someDays));        
                repeatQuestionnaireStep = false;
                yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay($"An einzelnen Tagen ging es dir so, habe ich notiert");
                yield break;
            case string c when c.Contains("3") || c.Contains("drei"):
                PHQ9Answers.Add(new Tuple<string, PHQ9LikertScale>(question, PHQ9LikertScale.moreThanHalfTheDays));        
                repeatQuestionnaireStep = false;
                yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay($"Ich notiere an mehr als der Hälfte der Tage");
                yield break;
            case string d when d.Contains("4") || d.Contains("vier"):
                PHQ9Answers.Add(new Tuple<string, PHQ9LikertScale>(question, PHQ9LikertScale.almostEveryDay));        
                repeatQuestionnaireStep = false;
                yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay($"Fast jeden Tag, danke für deine Antwort");
                yield break;
            default:
                yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay($"Das habe ich leider nicht verstanden. Ich Frage noch einmal.");
                yield break;
        }
    }

/// <summary>
/// Goodbye Message for the End of the PHQ9
/// Also Prints the Results of the Questionnaire as a Summary to the Console.
/// </summary>
/// <returns></returns>
    private IEnumerator End_PHQ9()
    {
         yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay("Danke, dass du dir die Zeit genommen hast. Ich habe deine Antworten notiert und werde Sie nun auswerten und weiter geben. Bis bald und melde dich gerne, wenn ich noch etwas für dich tun kann.");

        PrintPHQ9Results();
    }

/// <summary>
/// Calculates the Sum of all detected Answers and Prints it. Also includes Information on the Diagnosis ranges.
/// </summary>
/// <returns>the Sum of all Answer Items the User gave</returns>
    public int PrintPHQ9Results()
    {
        int sum =0;
        Debug.Log($"############ Results ##############");
        
        foreach (Tuple<string, PHQ9LikertScale> answer in PHQ9Answers)
        {
            Debug.Log($"Q{PHQ9Answers.IndexOf(answer)} --> {answer.Item2}");
            sum += (int)answer.Item2;
        }

        Debug.Log($"Sum = {sum} /// < 5 = healthy, < 10 = ordniary, 10-14 = light Depression, 15-19 = moderate Depression, 20-27 = serious Depression");

        return sum;
    }
}
