using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BattleSystem.UI
{
    public class BattleUiAnimation : MonoBehaviour
    {
        [SerializeField] private Button[] battleButton;
        private List<Vector3> _baseScale = new List<Vector3>();
        
        void Start()
        {
            for (int i = 0; i < battleButton.Length; i++)
            {
                _baseScale.Add(battleButton[i].transform.localScale);
                
                int index = i; // 클로저 캡처 문제 방지용 지역 복사
            
                EventTrigger trigger = battleButton[i].gameObject.AddComponent<EventTrigger>();
            
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerEnter;
                entry.callback.AddListener((_) => OnMouseEnterAnimation(index));
            
                trigger.triggers.Add(entry);
                
                EventTrigger.Entry exitEntry = new EventTrigger.Entry();
                exitEntry.eventID = EventTriggerType.PointerExit;
                exitEntry.callback.AddListener((_) => OnMouseExitAnimation());
            
                trigger.triggers.Add(exitEntry);
            }
        }

        void OnMouseEnterAnimation(int num)
        {
            for (int i = 0; i < battleButton.Length; i++)
            {
                if (battleButton[i].name == battleButton[num].name)
                {
                    battleButton[i].transform.localScale = _baseScale[i] * 1.5f;
                }
                else
                {
                    battleButton[i].transform.localScale = (_baseScale[i] / 1.5f);
                }
            }
        }
        
        void OnMouseExitAnimation()
        {
            Debug.Log("마우스 나감 확인");
            for (int i = 0; i < battleButton.Length; i++)
            {
                battleButton[i].transform.localScale = _baseScale[i];
            }
        }
    }
}
