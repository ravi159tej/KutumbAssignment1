using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LoginHandler : MonoBehaviour
{
    [SerializeField]private TMP_InputField UserName;
    [SerializeField]private TMP_InputField Password;

    public void LoginAuthenticate()
    {
        SceneManager.LoadScene("SampleScene");
    }
}
