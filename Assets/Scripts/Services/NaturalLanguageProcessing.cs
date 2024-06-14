using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Globalization;

public class NaturalLanguageProcessing : MonoBehaviour
{
    public Action<string> NLPResponseReceivedAction;

    public enum NLPModel
    {
        Ada, // worst
        Babbage,
        Curie,
        Davinci // best
    }

    /// <summary>
    /// Accesses a natural language processing model (GPT-3).
    /// </summary>
    /// <param name="prompt">The prompt to generate a completion for.</param>
    /// <param name="model">ID of the model to use. Davinci is the best (and most expensive), Ada is the worst.</param>
    /// <param name="maxTokens">The maximum number of tokens to generate in the completion.</param>
    /// <param name="temperature">What sampling temperature to use.  Higher values like 0.9 for more creative applications, lower values like 0 for ones with a well-defined answer.</param>
    /// <param name="stopSequence">A text sequence where the API will stop generating further tokens.</param>
    public void GetNLPResponse(string prompt, NLPModel model = NLPModel.Davinci, int maxTokens = 130, float temperature = 0.0f, string stopSequence = "")
    {
        StartCoroutine(SendTextToServer(prompt, model, maxTokens, temperature, stopSequence));
    }

    private IEnumerator SendTextToServer(string prompt, NLPModel model = NLPModel.Davinci, int maxTokens = 130, float temperature = 0.0f, string stopSequence = "")
    {
        WWWForm form = new WWWForm();
        form.AddField("prompt", prompt);
        form.AddField("model", model.ToString());
        form.AddField("max_tokens", maxTokens);
        form.AddField("temperature", Convert.ToString(temperature, CultureInfo.InvariantCulture));

        form.AddField("stop", stopSequence);

        using (UnityWebRequest req = UnityWebRequest.Post("TODO insert server name here" + "nlp", form))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(req.error);
            }
            else
            {
                string response = req.downloadHandler.text;
                NLPResponseReceivedAction?.Invoke(response);
            }
        }
    }
}
