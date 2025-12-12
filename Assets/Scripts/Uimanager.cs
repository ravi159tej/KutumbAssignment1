using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Uimanager : MonoBehaviour
{
    [SerializeField]private Controller controller;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DialogueButton()
    {
        controller.PlayDialogue();
    }

    public void PlayButton()
    {
        controller.Smile();
    }

     public void Sad()
    {
        controller.Sad();
    }


}
