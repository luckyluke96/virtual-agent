using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentSettings : MonoBehaviour
{
    public static readonly string ip = "134.100.14.194";
    public static readonly string sst_server = $"ws://{ip}:8140/";
    public static readonly string nlp_server = $"http://{ip}:8150/";
    
    public static string LanguageString = "en-US";

    public static readonly string englishPrompt = "" +
            "The folowing is a conversation with an AI assistant for a psychotherapist. " +
            "The assistant is helpful, creative, clever, and very friendly. " +
            "They can give known hints about psychiatric issues.\n\n" +
            "Human: Hi.\n\n" +
            "The doctor's artificial intelligence assistant introduces itself and asks for the patient's name:\n\n" +
            "AI:";

    public static readonly string germanPrompt = "" +
            "Das Folgende ist eine Unterhaltung mit einem KI Assistent in der Psychotherapie." +
            "Der Assistent ist freundlich, klever, kreativ und hilft gerne. " +
            "Er kann Tips in Bezug zu psychotherapeutischen Themen geben.\n\n" +
            "Mensch: Hi.\n\n" +
            "Die ünstliche Intelligenz stellt sich vor und fragt nach dem Namen der Person:\n\n" +
            "KI:";

    public static readonly string germanAI = "KI";
    public static readonly string germanHuman = "Mensch";
    public static readonly string englishAI = "Assistant";
    public static readonly string englishHuman = "Patient";

    public static string AI { get {
            switch (LanguageString)
            {
                case "en-US": 
                    return englishAI;
                case "de-DE":
                    return germanAI;
            }
            return englishAI;
        }
    }

    public static string Human
    {
        get
        {
            switch (LanguageString)
            {
                case "en-US":
                    return englishHuman;
                case "de-DE":
                    return germanHuman;
            }
            return englishHuman;
        }
    }
}

