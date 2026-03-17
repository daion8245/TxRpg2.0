using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HidenJinEnding : MonoBehaviour
{
    [SerializeField]
    TMP_Text HidenJinEndingTxt;

    [SerializeField]
    Button NextBtn;

    [SerializeField]
    Button SkipBtn;

    public string[] HidenJinEndingLines =
    {};

    private int index = 0;

    void Start()
    {
        HidenJinEndingTxt.text = HidenJinEndingLines[index];

        NextBtn.onClick.AddListener(Next);
        SkipBtn.onClick.AddListener(() => SceneManager.LoadScene("Credit"));
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

    void Next()
    {
        // 다음 줄로 이동
        index++;
        if (index < HidenJinEndingLines.Length)
        {
            // 엔딩 텍스트 업데이트
            HidenJinEndingTxt.text = HidenJinEndingLines[index];
        }
        else
        {
            // 히든진엔딩이 끝났을때 크레딧씬으로 이동
            SceneManager.LoadScene("Credit");
        }
    }
}
