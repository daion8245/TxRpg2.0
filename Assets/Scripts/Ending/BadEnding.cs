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
        AgainBtn.onClick.AddListener(() => SceneManager.LoadScene("Title"));
        ExitBtn.onClick.AddListener(Btn_Exit);
    }

    void Btn_Exit()
    {
        // 빌드, 에디터 강제 종료
        Application.Quit();
        UnityEditor.EditorApplication.isPlaying = false;
    }
}
