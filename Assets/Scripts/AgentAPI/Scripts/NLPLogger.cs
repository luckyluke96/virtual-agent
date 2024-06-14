using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NLPLogger : MonoBehaviour
{

    public static NLPLogger Instance;

    private SerializableList<NLPLog> logs = new SerializableList<NLPLog>();
    private string date;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        date = System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        // Create Directory ./NLPLogs if it doesn't exist
        if (!System.IO.Directory.Exists("./NLPLogs"))
        {
            System.IO.Directory.CreateDirectory("./NLPLogs");
        }
    }

    public void Log(string prompt, NLPAPI.GPT_Models model, int numberOfTokens, float temperature, NLPAPI.NLPResponse response)
    {
        NLPLog log = new NLPLog();
        log.prompt = prompt;
        log.model = model;
        log.numberOfTokens = numberOfTokens;
        log.temperature = temperature;
        log.response = response;

        logs.Add(log);
        Save();
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(logs);
        System.IO.File.WriteAllText("./NLPLogs/Log-" + date + ".json", json);
    }

    [System.Serializable]
    public class SerializableList<T> {
        public List<T> list;

        public SerializableList() {
            list = new List<T>();
        }

        public void Add(T item) {
            list.Add(item);
        }
    }

    [System.Serializable]
    class NLPLog
    {
        public string prompt;
        public NLPAPI.GPT_Models model;
        public int numberOfTokens;
        public float temperature;
        public NLPAPI.NLPResponse response;
    }
}
