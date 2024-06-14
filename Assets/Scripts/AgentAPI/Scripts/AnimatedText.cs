using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnimatedText : MonoBehaviour
{

    public TMPro.TMP_Text Text;

    public bool HideParentIfEmpty = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SetText(string txt)
    {
        StopAllCoroutines();
        Text.text = txt;
    }

    public void SetTextAnimatedLoop(string txt, string looped)
    {
        StopAllCoroutines();
        StartCoroutine(LoopText(txt, looped));
    }

    public void SetTextAnimated(string txt)
    {
        Text.text = txt;
    }

    private IEnumerator LoopText(string txt, string loop)
    {
        Text.text = txt;
        int i = 0;

        while (true)
        {
            Text.text = txt + loop.Substring(0, i);
            i = (i + 1) % (loop.Length+1);
            yield return new WaitForSeconds(0.5f);
        }

    }

    // Update is called once per frame
    void Update()
    {
        if(HideParentIfEmpty)
            transform.parent.GetComponent<Image>().enabled = Text.text.Length > 0;
    }
}
