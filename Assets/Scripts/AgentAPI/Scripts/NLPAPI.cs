using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NLPAPI : MonoBehaviour
{

    // The AudioAPI has a websocket we are going to use for making the streamed chat requests
    // We will use the same websocket for all requests
    public AudioAPI audioAPI;

    private string NLP_URL = AgentSettings.nlp_server;

    const string temp_key = "Azxx8Lw7gkFrNeNr7Wy8pxU4";

    bool error = false;
    string error_string = "";

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(GetAPIInfo());
    }

    public Coroutine GetNLPResponse(string input, GPT_Models model, Action<string> callback, bool translate = false)
    {
        return GetNLPResponse(input, model, callback, 100, 0.35f);
    }

    public Coroutine GetNLPResponse(string input, GPT_Models model, Action<string> callback, int max_tokens, float temperature, bool translate = false)
    {
        return GetNLPResponse(input, model, callback, max_tokens, temperature, "");
    }

    public Coroutine GetNLPResponse(string input, GPT_Models model, Action<string> callback, string stopword, bool translate = false)
    {
        return StartCoroutine(GetNLPCompletion(input, model, callback, 100, 0.35f, stopword, translate));
    }

    public Coroutine GetNLPResponse(string input, GPT_Models model, Action<string> callback, int max_tokens, float temperature, string stopword, bool translate = false)
    {
        return StartCoroutine(GetNLPCompletion(input, model, callback, max_tokens, temperature, stopword, translate));
    }

    public Coroutine GetChat_NLPResponse(GPTMessage[] input, GPT_Models model, Action<GPTMessage> callback)
    {
        return StartCoroutine(GetChatGPTCompletion(input, callback, 400, 0.35f));
    }

    public Coroutine GetChat_NLPResponseStreamed(GPTMessage[] input, GPT_Models model, Action<GPTMessage> callback, Action<GPTStreamMessage> stream_callback)
    {
        return StartCoroutine(GetChatGPTCompletion(input, callback, 400, 0.35f, true, stream_callback));
    }

    private IEnumerator GetAPIInfo()
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(NLP_URL + "test"))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();
            Debug.Log(webRequest.uri);
            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError(": Error: " + webRequest.error);
                    error = true;
                    error_string = webRequest.error;
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError(": HTTP Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.Success:
                    var info = JsonConvert.DeserializeObject<NLP_API_INFO>(webRequest.downloadHandler.text, new JsonSerializerSettings
                    {
                        Error = (obj, err) => Debug.LogError(err + " " + webRequest.downloadHandler.text)
                    });
                    Debug.Log($"Online: {info.online}, Limit: {info.limit}");
                    break;
            }
        }
    }

    public enum GPTMessageRoles
    {
        SYSTEM, ASSISTANT, USER
    }

    [Serializable]
    public class GPTMessage
    {
        public string role;
        public string content;

        [JsonConstructor]
        public GPTMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }

        public GPTMessage(GPTMessageRoles messageRole, params string[] content)
        {
            switch (messageRole)
            {
                case GPTMessageRoles.SYSTEM:
                    role = "system";
                    break;
                case GPTMessageRoles.ASSISTANT:
                    role = "assistant";
                    break;
                case GPTMessageRoles.USER:
                    role = "user";
                    break;
            }
            this.content = string.Join("", content);
        }
    }

    public class GPTStreamMessage{
        public string delta { get; set; }
        public bool finished { get; set; }

        public GPTStreamMessage(string delta, bool finished)
        {
            this.delta = delta;
            this.finished = finished;
        }
    }

    /// <summary>
    /// Example from OpenAI: Messages should contain system, user, assistant roles
    /// 
    /// https://platform.openai.com/docs/guides/chat/instructing-chat-models
    /// 
    /// messages=[
    ///    {"role": "system", "content": "You are a helpful assistant."},
    ///    { "role": "user", "content": "Who won the world series in 2020?"},
    ///    { "role": "assistant", "content": "The Los Angeles Dodgers won the World Series in 2020."},
    ///    { "role": "user", "content": "Where was it played?"}
    ///]
    /// 
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="callback"></param>
    /// <param name="max_tokens"></param>
    /// <param name="temperature"></param>
    /// <returns></returns>
    private IEnumerator GetChatGPTCompletion(GPTMessage[] messages, Action<GPTMessage> callback, int max_tokens,
        float temperature, bool stream = false, Action<GPTStreamMessage> stream_callback = null)
    {
        var data = new WWWForm();
        var dict = new Dictionary<string, string>();
        dict.Add("model", GPT_Models_String.GetModelString(GPT_Models.Chat_GPT_35));
        dict.Add("input", "");
        dict.Add("chat", "true");
        dict.Add("messages", Newtonsoft.Json.JsonConvert.SerializeObject(messages));

        dict.Add("key", temp_key);
        dict.Add("maxtokens", max_tokens.ToString());
        dict.Add("temperature", temperature.ToString());

        if (stream)
        {
            string message_id = GenerateUniqueID();
            dict.Add("streamOn", "true");
            dict.Add("sendResultsOn", ".:!?");
            dict.Add("message_id", message_id);

            string accumulated = "";

            bool finished = false;
            var listener = new Action<AudioAPI.ChatResult>((AudioAPI.ChatResult chatResult) =>
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    if (chatResult.message_id == message_id)
                    {
                        accumulated += chatResult.delta;
                        stream_callback.Invoke(new GPTStreamMessage(chatResult.delta, chatResult.done));
                        if (chatResult.done)
                        {
                            finished = true;
                        }
                    }
                });
            });

            // Listener will be called every time a sendResultsOn character is found
            audioAPI.chatListeners.Add(listener);
            
            var task = audioAPI.SendChatGPTRequest(dict);

            yield return new WaitUntil(() => finished);
            callback.Invoke(new GPTMessage(GPTMessageRoles.ASSISTANT, accumulated));
            audioAPI.chatListeners.Remove(listener);

            Debug.Log("Chat GPT Request Finished");
        }
        else
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Post(NLP_URL + "nlp", dict))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError(": Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError(": HTTP Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        //Debug.Log(webRequest.downloadHandler.text);
                        var response = JsonConvert.DeserializeObject<ChatNLPResponse>(webRequest.downloadHandler.text, new JsonSerializerSettings
                        {
                            Error = (obj, err) => Debug.LogError(err + " " + webRequest.downloadHandler.text)
                        });
                        // TODO Later more
                        Debug.Log($"Tokens: {response.usage.total_tokens} \n Response: {response.choices[0].message.content}");
                        callback.Invoke(response.choices[0].message);
                        break;
                }
            }
        }




        yield break;
    }

    private IEnumerator GetNLPCompletion(string input, GPT_Models model, Action<string> callback, int max_tokens,
        float temperature, string stopword, bool translate)
    {
        var data = new WWWForm();

        var dict = new Dictionary<string, string>();

        dict.Add("model", GPT_Models_String.GetModelString(model));
        if (model == GPT_Models.Chat_GPT_35 || model == GPT_Models.Chat_GPT_35)
        {
            var m = new GPTMessage(GPTMessageRoles.USER, input);
            var ms = new GPTMessage[] { m };

            yield return GetChatGPTCompletion(ms, (c) => callback(c.content), max_tokens, temperature);
            yield break;

            //dict.Add("input", "");
            //dict.Add("chat", "true");
            //dict.Add("messages", Newtonsoft.Json.JsonConvert.SerializeObject(ms));
        }
        else
        {
            dict.Add("input", input);
            dict.Add("stop", stopword);
        }

        dict.Add("key", temp_key);
        dict.Add("maxtokens", max_tokens.ToString());
        dict.Add("temperature", temperature.ToString());

        if (translate)
        {
            dict.Add("translate", "true");
            dict.Add("languageCode", "de");
        }

        using (UnityWebRequest webRequest = UnityWebRequest.Post(NLP_URL + "nlp", dict))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError(": Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError(": HTTP Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.Success:
                    Debug.Log(webRequest.downloadHandler.text);
                    if (model == GPT_Models.Chat_GPT_35)
                    {
                        var response = JsonConvert.DeserializeObject<ChatNLPResponse>(webRequest.downloadHandler.text, new JsonSerializerSettings
                        {
                            Error = (obj, err) => Debug.LogError(err + " " + webRequest.downloadHandler.text)
                        });
                        // TODO Later more
                        Debug.Log($"Response: {response.choices[0].message.content}");
                        callback.Invoke(response.choices[0].message.content);
                        break;
                    }
                    else
                    {
                        var response = JsonConvert.DeserializeObject<NLPResponse>(webRequest.downloadHandler.text, new JsonSerializerSettings
                        {
                            Error = (obj, err) => Debug.LogError(err + " " + webRequest.downloadHandler.text)
                        });
                        Debug.Log($"Response: {response.choices[0].text}");
                        callback.Invoke(response.choices[0].text);
                    }

                    break;
            }
        }
    }

    public enum GPT_Models
    {
        Davinci_Best, Curie_Good, Babbage_Simple, Ada_Cheapest, Chat_GPT_35, Chat_GPT_4_NEW
    }

    private class GPT_Models_String
    {

        public static string GetModelString(GPT_Models model)
        {
            switch (model)
            {
                case GPT_Models.Chat_GPT_35: return "gpt-3.5-turbo";
                case GPT_Models.Davinci_Best: return "text-davinci-003";
                case GPT_Models.Curie_Good: return "text-curie-001";
                case GPT_Models.Babbage_Simple: return "text-babbage-001";
                case GPT_Models.Ada_Cheapest: return "text-ada-001";
                case GPT_Models.Chat_GPT_4_NEW: return "gpt-4-1106-preview";
            }
            return "text-ada-001";
        }
    }

    private void OnGUI()
    {
        if (error)
        {
            GUI.color = Color.red;
            GUILayout.Label("Die Verbindung konnte nicht hergestellt werden. Bitte Bescheid sagen.");
        }
    }

    private string GenerateUniqueID()
    {
        return Guid.NewGuid().ToString();
    }

    private class NLP_API_INFO
    {
        public string online;
        public int limit;
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Choice
    {
        public string text { get; set; }
        public int index { get; set; }
        public object logprobs { get; set; }
        public string finish_reason { get; set; }
    }

    public class MessageChoice
    {
        public GPTMessage message { get; set; }
        public int index { get; set; }
        public string finish_reason { get; set; }
    }

    public class NLPResponse
    {
        public string id { get; set; }
        public string @object { get; set; }
        public int created { get; set; }
        public string model { get; set; }
        public List<Choice> choices { get; set; }
        public Usage usage { get; set; }
    }

    public class ChatNLPResponse
    {
        public string id { get; set; }
        public string @object { get; set; }
        public int created { get; set; }
        public string model { get; set; }
        public List<MessageChoice> choices { get; set; }
        public Usage usage { get; set; }
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }


}
