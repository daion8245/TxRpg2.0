using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SettingMgr : MonoBehaviour
{
    [SerializeField] GameObject SettingPanel; // 설정창 패널
    [SerializeField] GameObject ClosedPanel;  // 바깥 클릭 감지 패널

    void Update()
    {
        // ESC 눌렀을 때 설정창 열기/닫기
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool isOpen = SettingPanel.activeSelf;

            SettingPanel.SetActive(!isOpen); // 열기 / 닫기
            Time.timeScale = isOpen ? 1f : 0f; // 열리면 게임 멈춤
        }

        // 마우스 왼쪽 클릭 감지
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = Mouse.current.position.ReadValue(); // 현재 마우스 위치

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results); // 클릭된 UI 검사

            foreach (var result in results)
            {
                // SettingPanel 또는 그 자식을 클릭했으면 닫지 않음
                if (result.gameObject.transform.IsChildOf(SettingPanel.transform))
                {
                    return;
                }

                // ClosedPanel을 클릭했으면 닫기
                if (result.gameObject == ClosedPanel)
                {
                    CloseSetting();
                    return;
                }
            }
        }
    }

    void CloseSetting()
    {
        if (SettingPanel.activeSelf)
        {
            SettingPanel.SetActive(false); // 설정창 닫기
            Time.timeScale = 1f; // 게임 다시 진행
        }
    }
}
