using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BadEnding : MonoBehaviour
{
    [SerializeField]
    Button AgainBtn;
    [SerializeField]
    Button ExitBtn;

    private void Start()
    {
        AgainBtn.onClick.AddListener(Btn_Again);
        ExitBtn.onClick.AddListener(Btn_Exit);
    }

    void Btn_Again()
    {
        // 다시하기 버튼을 눌렀을떄 처음부터(타이틀)로 이동
        SceneManager.LoadScene("Title");
    }

    void Btn_Exit()
    {
        Application.Quit();
        UnityEditor.EditorApplication.isPlaying = false;
    }
}
