using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameStart : MonoBehaviour
{
    [SerializeField]
    Button Startbtn;

    [SerializeField]
    Button Tutorialsbtn;

    void Start()
    {
        // 시작버튼을 눌렀을떄 스토리 씬으로 이동
        Startbtn.onClick.AddListener(() => SceneManager.LoadScene("Story_01"));

        // 튜토리얼 버튼 → 튜토리얼 씬
        Tutorialsbtn.onClick.AddListener(() => SceneManager.LoadScene("Tutorials"));
    }
}
