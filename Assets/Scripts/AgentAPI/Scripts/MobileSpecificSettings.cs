using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobileSpecificSettings : UnitySingleton<MobileSpecificSettings>
{

    public AnimatedText InfoText;
    public AnimatedText ModalText;
    public Agent agent;

    public SeniorenChat seniorenChat;
    public GameObject WeiterfragenBtn;
    public GameObject NeueFrageBtn;
    public GameObject StopSTTBtn;

    public ContinuationState continuationState = ContinuationState.UNDEFINED;

    // Start is called before the first frame update
    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        ShowContinueButtons(false);
        continuationState = ContinuationState.CONTINUE_THREAD; // First Input
    }

    void Update(){
        // Check if audio is on
        if(AudioSettings.Mobile.muteState){
            ModalText.SetText("Der Ton vom Handy ist nicht eingeschaltet, bitte schalten Sie den Ton ein.");
        }else{
            ModalText.SetText("");
        }
    }


    public void QuiApp()
    {
        Application.Quit(); 
    }

    public void RestartApp()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    public void StopSTT(){
        API_Agent.Instance.STTAPI.StopSTT();
    }

    public void StartAskingQuestion()
    {
        AgentSettings.LanguageString = "de-DE";
        
        agent.State = Agent.AgentState.Initializing;

        seniorenChat.StartSeniorChat(agent.Username, true);
    }

    public void ShowContinueButtons(bool show)
    {
        WeiterfragenBtn.SetActive(show);
        NeueFrageBtn.SetActive(show);
    }

    public void SetContinuationState(ContinuationState new_state)
    {
        continuationState = new_state;
    }

    public void SetContinueState()
    {
        continuationState = ContinuationState.CONTINUE_THREAD;
    }

    public void SetNewQuestionState()
    {
        continuationState = ContinuationState.NEW_QUESTION;
    }

    public enum ContinuationState
    {
        UNDEFINED,
        NEW_QUESTION,
        CONTINUE_THREAD
    }
}
