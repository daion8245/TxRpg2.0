using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameStart : MonoBehaviour
{
    [SerializeField]
    Button Startbtn;

    void Start()
    {
        Startbtn.onClick.AddListener(() => SceneManager.LoadScene(1));
    }
}
