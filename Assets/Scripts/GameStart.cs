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

    [SerializeField]
    Button Exitbtn;

    void Start()
    {

        // 시작버튼을 눌렀을떄 스토리 씬으로 이동
        Startbtn.onClick.AddListener(() => SceneManager.LoadScene("Story_01"));

        // 튜토리얼 버튼을 눌럿을때 튜토리얼 씬으로 이동
        Tutorialsbtn.onClick.AddListener(() => SceneManager.LoadScene("Tutorials"));

        // 나가기 버튼을 눌럿을때 강제로 나가기
        Exitbtn.onClick.AddListener(() =>
        {
            // 에디터 강제 종료
            UnityEditor.EditorApplication.isPlaying = false;
            // 빌드된 게임 강제 종료
            Application.Quit();
        });
    }
}
