using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StoryManager : MonoBehaviour
{
    [SerializeField]
    TMP_Text storyTxt;

    [SerializeField]
    Button NextBtn;

    [SerializeField]
    Button SkipBtn;

    // 스토리 줄들을 배열로 저장
    string[] stroyLines =
    {
        "플레이어는 기억을 잃은 채 눈을 뜬다.",
        "플레이어 : 여기가어디지...?",
        "마을에 있는 거대한 던전.",
        "사람들은 그곳에서 \n 돈을 번다.",
        "플레이어는 던전에서 돈을 \n 벌기로 결심한다.",
        "플레이어는 무기를 사기 위해 근처를 \n 뒤졋지만 나오지 않았다",
        "플레이어 : 아... 추워.....",
        "주머니에 손을 넣어보니 \n 돈이 들어있었다",
        "플레이어 : 이게 왜 여기에 있지?",
        "플레이어 : 일단 이걸로 \n 무기를 사자",
        "무기를 샀다",
        "플레이어 : 아 너무 늦었네 \n 이제 20골드밖에 안남았는데",
        "플레이어 : 일단 늦었으니 \n 여관에서 자자",
        "플레이어는 여관으로 이동"
    };

    private int index = 0;

    void Start()
    {
        storyTxt.text = stroyLines[index];

        // 버튼 클릭 이벤트 연결
        NextBtn.onClick.AddListener(NextLine);
        SkipBtn.onClick.AddListener(SkipStory);
    }

    void Update()
    {
        // 스페이스바 눌렀을 때
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            // 다음 줄로 이동
            NextLine();
        }
    }

    void NextLine()
    {
        // 다음 줄로 이동
        index++;
        if (index < stroyLines.Length)
        {
            // 스토리 텍스트 업데이트
            storyTxt.text = stroyLines[index];
        }
        else
        {
            // 스토리가 끝났을 때 게임 씬으로 이동
            SceneManager.LoadScene("Village_01");
        }
    }

    void SkipStory()
    {
        // 스토리 건너뛰기
        StartGame();
    }

    void StartGame()
    {
        // 게임 씬으로 이동
        SceneManager.LoadScene("Village_01");
    }
}
