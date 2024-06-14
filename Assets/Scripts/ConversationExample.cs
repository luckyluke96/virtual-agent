using UnityEngine;
using TMPro;

public class ConversationExample : MonoBehaviour
{
    public TTSAPI tts;

    public void SaySomething(string text)
    {
        tts.TextToSpeechAndPlay(text);
    }

    public void SayHi(TMP_InputField input)
    {
        tts.TextToSpeechAndPlay("Hallo " + input.text + ", schön, dass du hier bist.");
    }
}
