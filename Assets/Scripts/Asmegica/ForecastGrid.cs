using UnityEngine;
namespace Asmegica
{
    public static class ForecastGrid
    {
        const float LabelW = 110f, ColW = 146f;
        public static int Draw(GrayboxBattle b, int selected) // returns clicked slot, else selected
        {
            int clicked = selected;
            using (new GUILayout.VerticalScope(GUILayout.Width(920f)))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("칸 선택", GUILayout.Width(LabelW));
                    Color old = GUI.backgroundColor;
                    for (int s = 0; s < 5; s++)
                    {
                        GUI.backgroundColor = s == selected ? Color.yellow : old;
                        if (GUILayout.Button((s == selected ? "> " : "") + (s + 1) + "칸", GUILayout.Width(ColW))) clicked = s;
                    }
                    GUI.backgroundColor = old;
                }
                for (int e = 0; e < b.EnemyCount; e++)
                    Row($"적 {e + 1}  HP {b.GetEnemyHp(e)}", s => b.IntentLabel(e, s) + (b.GetEnemyHp(e) <= 0 && s >= b.CurrentSlot ? " 취소" : ""));
                Row("주문", b.PlanLabel);
            }
            return clicked;
        }

        static void Row(string head, System.Func<int, string> cell)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(head, GUILayout.Width(LabelW));
                for (int s = 0; s < 5; s++) GUILayout.Label(cell(s), GUILayout.Width(ColW));
            }
        }
    }
}
