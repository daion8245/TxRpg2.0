using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TutorialsMgr : MonoBehaviour
{
    [SerializeField]
    TMP_Text TutorialTxt;

    [SerializeField]
    Button NextBtn;

    [SerializeField]
    Button SkipBtn;

    public string[] TutorialsLines =
    {};

    private int index = 0;

    void Start()
    {
        TutorialTxt.text = TutorialsLines[index];

        NextBtn.onClick.AddListener(Next);
        SkipBtn.onClick.AddListener(() => SceneManager.LoadScene("Title"));
    }

    void Update()
    {
        // 스페이스바 눌렀을 때
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            // 다음 줄로 이동
            Next();
        }
    }

    public void Next()
    {
        // 다음 줄로 이동
        index++;
        if (index < TutorialsLines.Length)
        {
            // 스토리 텍스트 업데이트
            TutorialTxt.text = TutorialsLines[index];
        }
        else
        {
            // 튜토리얼이 끝났을때 타이틀씬으로 이동
            SceneManager.LoadScene("Title");
        }
    }
}
