using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PHQ9 : MonoBehaviour
{

    public NLPAPI LanguageProcessing;

    private string main_prompt = "The folowing is a conversation with an AI assistant for a psychotherapist. " +
            "The assistant is helpful, creative, clever, and very friendly. " +
            "Their main objective is to run a PHQ9 questionnaire with the patient. It will try to move the conversation back to the next question if it deviates.\n\n" +
            "Human: Hi.\n\n" +
            "The doctor's AI assistant introduces itself and asks for the patient's name. It does not ask for numbers specifically. Afterwards it will start with the first question. Once it has finished all the questions, it bids the patient farewell.\n\n" +
            "AI:";

    private string noAI = "The folowing is a conversation with Marc, an assistant Doctor Gallinat. " +
            "The assistant is helpful, creative, clever, and very friendly. " +
            "The main objective is to run a PHQ9 questionnaire with the patient. They will try keep the conversation on track if it deviates too far.\n\n" +
            "Patient: Hi.\n\n" +
            "The assistant introduces himself and asks for the patient's name. Afterwards he will start with the first question. The answers don't have to be numbers. Once they have finished all the questions, he bids the patient farewell.\n\n" +
            "Assistant:";

    private NLPAPI.GPTMessage BeginningPrompt = 
        new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.SYSTEM, 
            "Du bist PHQ-9 GPT, ein Assistent von Psychotherapeuten, der mit Patienten PHQ interviews durchführt. Stelle immer nur eine Frage zur Zeit und warte auf die Antworten der Nutzer! Versuche außerdem nicht zu repetitiv in deinen Antworten zu sein. Die Fragen und Reihenfolge ist wie folgt:\n\n" +
            "Wie oft hatten sie wenig Interesse oder Freude an Ihren Tätigkeiten?\n",
            "Haben Sie sich niedergeschlagen, schwermütig oder hoffnungslos gefühlt?\n",
            "Hatten Sie schwierigkeiten beim ein- beziehungsweise durchzuschlafen oder haben Sie mehr als üblich geschlafen?\n",
            "Wie oft haben Sie sich müde gefühlt oder hatten das Gefühl keine Energie zu haben?\n",
            "Hatten Sie weniger appetit oder das erhöhte Bedürfnis zu essen?\n",
            "Wie häufig hatten Sie eine schlechte Meinung von sich selbst, das Gefühl ein Versager zu sein oder Ihre Familie enttäuscht zu haben?\n",
            "Hatten Sie schwierigkeiten sich zu konzentrieren, beispielsweise beim Zeitunglesen oder Fernsehen?\n",
            "War Ihre Sprache oder Bewegungen so verlangsamt, dass auch andere dies bemerken würden? Oder waren Sie im gegenteil zappelig, ruhelos und hatten einen stärkeren Bewegungsdrang als sonst?\n",
            "Hatten Sie gedanken, dass Sie lieber Tot wären oder sich Leid zufügen möchten?\n");

    public string Username = "Peter";
    public bool Translate = false;
    private List<NLPAPI.GPTMessage> namedStart(string username, bool translate = false)
    {
        if (username == "")
        {
            Debug.LogError($"Name is empty");
            return null;
        }

        var messages = new List<NLPAPI.GPTMessage>();
        messages.Add(BeginningPrompt);
        messages.Add(new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.USER, $"Hi, ich bin {username}, wie kannst du mir helfen?"));

        return messages;
    }

    public string[] Questions = new string[] {
        "Little interest or pleasure in doing things",
        "Feeling down, depressed, or hopeless",
        "Trouble falling or staying asleep, or sleeping too much",
        "Feeling tired or having little energy",
        "Poor appetite or overeating",
        "Feeling bad about yourself - or that you are a failure or have let yourself or your family down",
        "Trouble concentrating on things, such as reading the newspaper or watching television",
        "Moving or speaking so slowly that other people could have noticed. Or the opposite - being so fidgety or restless that you have been moving around a lot more than usual",
        "Thoughts that you would be better off dead, or of hurting yourself in some way"
    };

    public List<int> Answers_to_Questions;

    public LinkedList<string> AI_Responses;
    public LinkedList<string> Human_Responses;
    public LinkedList<string> All_Responses;
    private LinkedList<Response> Responses;
    public string Dialogue;

    private LinkedList<NLPAPI.GPTMessage[]> History;

    private int current_question = 0;

    private int MAX_QUESTIONS = 8;

    // Start is called before the first frame update
    void Start()
    {
        Answers_to_Questions = new List<int>();
        AI_Responses = new LinkedList<string>();
        Human_Responses = new LinkedList<string>();
        All_Responses = new LinkedList<string>();
        Responses = new LinkedList<Response>();
        History = new LinkedList<NLPAPI.GPTMessage[]>();
        Dialogue = "";
    }

    // Update is called once per frame
    void Update()
    {

    }

    public List<NLPAPI.GPTMessage> GetStartPrompt()
    {
        return namedStart(Username);
    }

    public void StartPHQ9(string name, bool translate = false)
    {
        StartCoroutine(Init_PHQ9(name, translate));
    }

    public IEnumerator Init_PHQ9(string name, bool translate = false)
    {
        Username = name;
        Translate = translate;

        var prompt = GetStartPrompt();
        //Dialogue = prompt;
        History.AddLast(prompt.ToArray());
        MAX_QUESTIONS = 8;

        // TEST
        LanguageProcessing.GetChat_NLPResponse(prompt.ToArray(), NLPAPI.GPT_Models.Chat_GPT_35, (response) =>
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                StartCoroutine(CheckAIResponse(prompt, response));
            });
        });

        yield break;
    }

    public void SaveDialogue(string suffix = "")
    {

        var json = JsonConvert.SerializeObject(Responses);

        var path = Application.persistentDataPath + "/Dialogue" + DateTime.Now.ToString("yyyyMMddHHmmss") + suffix + ".txt";
        var file = File.CreateText(path);
        Debug.Log("Saving Dialogue: " + Application.persistentDataPath + "/Dialogue" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");
        //file.WriteLine(Dialogue);
        file.WriteLine(json);
        file.Close();
    }

    public string Prompt_IsAppropriate(string response)
    {
        return $"In a PHQ9 interview, the student gives the patient the following reponse: \"{response}\"\n\n" +
            $"Is this appropriate in the interview? Yes or no:";
    }

    public string Prompt_IsOnCourse(string response)
    {
        return $"In a PHQ9 interview, the interviewer says the following: \"{response}\"?\n" +
            $"Is this a questions and does it help move on the evaluation?" +
            $"Yes or no:";
    }

    class Response
    {
        public string prompt;
        public string response;
    }

    class AI_Response : Response
    {
        public string appropriate;
        public string on_course;
        public bool repetitive;

        public override string ToString()
        {
            return $"AI_Response\n" +
                $"Response: {response}\n" +
                $"Prompt: {prompt}\n" +
                $"Appropriate: {appropriate}\n" +
                $"On course: {on_course}\n" +
                $"Repetitive: {repetitive}\n";
        }

    }


    public IEnumerator CheckAIResponse(List<NLPAPI.GPTMessage> prompt, NLPAPI.GPTMessage response)
    {
        AI_Responses.AddLast(response.content);
        All_Responses.AddLast(response.content);
        Dialogue += "\n" + response;

        var current_response = new AI_Response();
        current_response.prompt = prompt[prompt.Count-1].content;
        current_response.response = response.content;

        //yield return new WaitForSeconds(2f);
        //// Appropriate
        //var appropriate = LanguageProcessing.GetNLPResponse(Prompt_IsAppropriate(response), NLPAPI.GPT_Models.Chat_GPT_35, (response) =>
        //{
        //    current_response.appropriate = response;
        //}, 10, 0f);

        //yield return new WaitForSeconds(2f);
        //// On course
        //var on_course = LanguageProcessing.GetNLPResponse(Prompt_IsOnCourse(response), NLPAPI.GPT_Models.Chat_GPT_35, (response) =>
        //{
        //    current_response.on_course = response;
        //}, 10, 0f);

        //Coroutine repetitive = null;
        //// Repetitive
        //if (AI_Responses.Count > 1)
        //{
        //    current_response.repetitive = GetIsRepetitive(AI_Responses.Last.Previous.Value, response);
        //}
        //else
        //{
        //    current_response.repetitive = false;
        //}
        
        //yield return appropriate;
        //yield return on_course;
        //if (repetitive != null)
        //    yield return repetitive;

        yield return API_Agent.Instance.TTSAPI.TextToSpeechAndPlay(response.content);

        //Debug.Log("Summary:");
        Debug.Log(current_response.ToString());

        Responses.AddLast(current_response);

        //yield return new WaitForSeconds(2f);
        //StartCoroutine(FakeDialogue(current_response));

        string sst_result = "";

        // Warte auf Spracheingabe. Mikrofon wird aktiviert. Coroutine yielded, bis eingabe vermutlich abgeschlossen wurde.
        yield return API_Agent.Instance.STTAPI.GetSpeechToText((intermediate_result) =>{},
        (final_result) =>
        {
            // Finales Ergebnis wird gespeichert
            sst_result = final_result;
        });

        var new_user_message = new NLPAPI.GPTMessage(NLPAPI.GPTMessageRoles.USER, sst_result);
        prompt.Add(response);
        prompt.Add(new_user_message);

        LanguageProcessing.GetChat_NLPResponse(prompt.ToArray(), NLPAPI.GPT_Models.Chat_GPT_35, (response) =>
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                StartCoroutine(CheckAIResponse(prompt, response));
            });
        });

        yield break;
    }

    private bool GetIsRepetitive(string response1, string response2)
    {
        // Are these two repsonses repetitive?
        // Determine using Levenshtein distance
        var distance = LevenshteinDistance.Compute(response1, response2);
        Debug.Log($"Levenshtein distance: {distance}");
        return distance < 5;
    }

    public class LevenshteinDistance
    {
        public static int Compute(string s, string t)
        {
            if (string.IsNullOrEmpty(s))
            {
                if (string.IsNullOrEmpty(t))
                    return 0;
                return t.Length;
            }

            if (string.IsNullOrEmpty(t))
            {
                return s.Length;
            }

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
                return m;

            if (m == 0)
                return n;

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            // Step 7
            return d[n, m];
        }
    }

    public string Prompt_IsEmergency_Human(string response)
    {
        return $"In a PHQ9 interview, the patient gives student the following reponse: \"{response}\"\n\n" +
            $"Is there an emergency where a doctor should immediately interfere? Yes or no:";
    }

    public string Prompt_IsAnswer_Human(string response)
    {
        return $"In a PHQ9 interview, the patient gives student the following reponse: \"{response}\"\n\n" +
            $"Is this an answer to the question? Yes or no:";
    }

    public string Prompt_GetAnswer_Human(string response)
    {
        return $"In a PHQ9 interview, the patient gives student the following reponse: \"{response}\"\n\n" +
            $"What is the answer to the question on a scale of 0 to 3, where 0 is 'not at all' and 3 is 'every day'?\n\nAnswer:";
    }

    public string Prompt_IsAppropriate_Human(string response)
    {
        return $"In a PHQ9 interview, the patient gives the student the following reponse: \"{response}\"\n\n" +
            $"Is this appropriate in the constext of the interview? Yes or no:";
    }

    class Human_Response : Response
    {

        public string emergency;
        public string appropriate;
        public string is_answer;
        public string answer;

        public override string ToString()
        {
            return $"Human_Response\n" +
                $"Response: {response}\n" +
                $"Prompt: {prompt}\n" +
                $"Emergency: {emergency}\n" +
                $"Appropriate: {appropriate}\n" +
                $"Is answer: {is_answer}\n" +
                $"Answer: {answer}\n";
        }
    }

    public IEnumerator CheckHumanResponse(string prompt, string response)
    {
        Human_Responses.AddLast(response);
        All_Responses.AddLast(response);
        Dialogue += "\n" + response;

        var current_response = new Human_Response();
        current_response.prompt = prompt;
        current_response.response = response;

        yield return new WaitForSeconds(2f);
        // Emergency
        //var emergency = LanguageProcessing.GetNLPResponse(Prompt_IsEmergency_Human(response), NLPAPI.GPT_Models.Chat_GPT_35, (response) =>
        //{
        //    current_response.emergency = response;
        //}, 10, 0f);

        //yield return new WaitForSeconds(2f);
        //// Appropriate
        //var appropriate = LanguageProcessing.GetNLPResponse(Prompt_IsAppropriate_Human(response), NLPAPI.GPT_Models.Chat_GPT_35, (response) =>
        //{
        //    current_response.appropriate = response;
        //}, 10, 0f);

        //yield return new WaitForSeconds(2f);
        //// Is Answer
        //var is_answer = LanguageProcessing.GetNLPResponse(Prompt_IsAnswer_Human(response), NLPAPI.GPT_Models.Chat_GPT_35, (response) =>
        //{
        //    current_response.is_answer = response;
        //}, 10, 0f);

        //yield return emergency;
        //yield return appropriate;
        //yield return is_answer;

        Coroutine answer = null;

        yield return new WaitForSeconds(2f);
        // Get Answer
        if (current_response.is_answer.ToLower().Contains("yes"))
        {
            answer = LanguageProcessing.GetNLPResponse(Prompt_GetAnswer_Human(response), NLPAPI.GPT_Models.Chat_GPT_35, (response) =>
            {
                current_response.answer = response;
            }, 10, 0f);
        }
        else
        {
            current_response.answer = "-1";
        }

        if (answer != null)
            yield return answer;

        //Debug.Log("Summary:");
        Debug.Log(current_response.ToString());

        Responses.AddLast(current_response);

        yield return new WaitForSeconds(2f);
        // StartCoroutine(AI_Dialogue(current_response));


        yield return null;
    }

    private string Get_FindAnswerPrompt(int question_id, string last_answer)
    {
        string prompt = $"This is the response of the patient in an interview with a psychotherapist. The psychotherapist is conducting a PHQ9 questionnaire with the patient. The answers should be on a scale of 0 to 3, where 0 is 'not at all' and 3 is 'every day'. Determine if the question was answered. If yes, print the Answer. If the question was not answered, print -1 as the answer.\n\nPatient response:\"{last_answer}\"\n\nAnswer:";

        return prompt;
    }

    public void CheckLastAnswer(string last_answer)
    {
        int questionID = current_question;
        LanguageProcessing.GetNLPResponse(Get_FindAnswerPrompt(current_question, last_answer), NLPAPI.GPT_Models.Chat_GPT_35, (s) =>
        {
            int answer;
            if (int.TryParse(s, out answer))
            {
                if (answer >= 0)
                {
                    Answers_to_Questions.Add(answer);
                    current_question++;
                    Debug.Log($"The answer was {answer}");
                }
            }

        }, 2, 0.0f);
    }
}
