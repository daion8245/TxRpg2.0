using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CreditMgr : MonoBehaviour
{
    [SerializeField]
    RectTransform creditText; // 크레딧 text

    [SerializeField]
    Button skipButton; // 스킵 버튼

    [SerializeField]
    float scrollSpeed;  // 크레딧 속도

    [SerializeField]
    float creditLength; // 크레딧 길이

    [SerializeField]
    float fastSpeed;  // 크레딧 빠른 속도

    float startY;

    void Start()
    {
        startY = creditText.anchoredPosition.y;

        // 스킵 버튼
        skipButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("Title");
        });
    }

    void Update()
    {
        float speed = scrollSpeed;

        // 스페이스 꾹 누르면 빠르게
        if (Keyboard.current.spaceKey.isPressed)
        {
            speed = fastSpeed;
        }

        creditText.anchoredPosition += Vector2.up * speed * Time.deltaTime;

        // 끝나면 타이틀 이동
        if (creditText.anchoredPosition.y >= startY + creditLength)
        {
            SceneManager.LoadScene("Title");
        }
    }

}
