using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class SettingMgr : MonoBehaviour
{
    [SerializeField] GameObject SettingPanel;       // 설정창 패널
    [SerializeField] GameObject ClosedPanel;        // 바깥 클릭 감지 패널

    [SerializeField] Button CommonBtn;              // 일반 설정창 버튼
    [SerializeField] GameObject CommonPanel;        // 일반 설정창 패널

    [SerializeField] Button GraphicsBtn;            // 그래픽 설정창 버튼
    [SerializeField] GameObject GraphicsPanel;      // 그래픽 설정창 패널

    [SerializeField] Button SoundBtn;               // 사운드 설정창 버튼
    [SerializeField] GameObject SoundPanel;         // 사운드 설정창 패널

    [SerializeField] Button ManipulationBtn;        // 조작 설정창 버튼
    [SerializeField] GameObject ManipulationPanel;  // 조작 설정창 패널

    [SerializeField] Button MeBtn;                  // 계정 설정창 버튼
    [SerializeField] GameObject MePanel;            // 계정 설정창 패널

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

    private void Start()
    {
        // 일반 버튼을 눌럿을때 패널 키기
        CommonBtn.onClick.AddListener(() =>
        {
            CommonPanel.SetActive(true);

            // 일반 패널을 제외한 세팅 패널들 닫기
            GraphicsPanel.SetActive(false);
            SoundPanel.SetActive(false);
            ManipulationPanel.SetActive(false);
            MePanel.SetActive(false);
        });

        // 그래픽 버튼을 눌럿을때 패널 키기
        GraphicsBtn.onClick.AddListener(() =>
        {
            GraphicsPanel.SetActive(true);

            // 그래픽 패널을 제외한 세팅 패널들 닫기
            CommonPanel.SetActive(false);
            SoundPanel.SetActive(false);
            ManipulationPanel.SetActive(false);
            MePanel.SetActive(false);
        });

        // 사운드 버튼을 눌럿을떄 패널 키기
        SoundBtn.onClick.AddListener(() =>
        {
            SoundPanel.SetActive(true);

            // 사운드 패널을 제외한 세팅패널들 닫기
            CommonPanel.SetActive(false);
            GraphicsPanel.SetActive(false);
            ManipulationPanel.SetActive(false);
            MePanel.SetActive(false);
        });

        // 조작 버튼을 눌럿을떄 패널 키기
        ManipulationBtn.onClick.AddListener(() =>
        {
            ManipulationPanel.SetActive(true);

            // 조작 패널을 제외한 세팅 패널들 닫기
            CommonPanel.SetActive(false);
            GraphicsPanel.SetActive(false);
            SoundPanel.SetActive(false);
            MePanel.SetActive(false);
        });

        // 계정 버튼을 눌럿을떄 패널 키기
        MeBtn.onClick.AddListener(() =>
        {
            MePanel.SetActive(true);

            // 계정 패널을 제외한 세팅 패널들 닫기
            CommonPanel.SetActive(false);
            GraphicsPanel.SetActive(false);
            SoundPanel.SetActive(false);
            ManipulationPanel.SetActive(false);
        });

        // 일반패널을 제외한 설정패널들 닫기
        GraphicsPanel.SetActive(false);
        SoundPanel.SetActive(false);
        ManipulationPanel.SetActive(false);
        MePanel.SetActive(false);
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
