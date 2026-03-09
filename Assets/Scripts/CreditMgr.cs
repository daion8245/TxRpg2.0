using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class CreditMgr : MonoBehaviour
{
    [SerializeField]
    RectTransform creditText;

    [SerializeField]
    float scrollSpeed;

    [SerializeField]
    float creditLength; // 크레딧 길이 (Inspector에서 조절)

    float startY;

    void Start()
    {
        startY = creditText.anchoredPosition.y;
    }

    void Update()
    {
        // 위로 이동
        creditText.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;

        // 시작 위치 + 설정한 길이만큼 올라가면 종료
        if (creditText.anchoredPosition.y >= startY + creditLength)
        {
            SceneManager.LoadScene("Title");
        }
    }
}
