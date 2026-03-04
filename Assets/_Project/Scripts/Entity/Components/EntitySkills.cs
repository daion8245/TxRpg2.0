using System.Collections.Generic;
using TxRpg.Data;
using UnityEngine;

namespace TxRpg.Entity
{
    /// <summary>
    /// 유닛의 스킬 목록과 쿨다운을 관리합니다.
    /// </summary>
    public class EntitySkills
    {
        private readonly List<SkillRuntime> _skills = new();

        public IReadOnlyList<SkillRuntime> Skills => _skills;

        public EntitySkills(SkillDataSO[] skillDatas)
        {
            if (skillDatas == null) return;

            foreach (var data in skillDatas)
            {
                if (data != null)
                {
                    _skills.Add(new SkillRuntime(data));
                }
            }
        }

        public bool CanUse(int index, int currentMp)
        {
            if (index < 0 || index >= _skills.Count) return false;
            var skill = _skills[index];
            return skill.CooldownRemaining <= 0f && currentMp >= skill.Data.mpCost;
        }

        public void UseSkill(int index)
        {
            if (index < 0 || index >= _skills.Count) return;
            _skills[index].CooldownRemaining = _skills[index].Data.cooldown;
        }

        public void TickCooldowns()
        {
            foreach (var skill in _skills)
            {
                if (skill.CooldownRemaining > 0f)
                {
                    skill.CooldownRemaining -= 1f;
                }
            }
        }

        public SkillDataSO GetSkillData(int index)
        {
            if (index < 0 || index >= _skills.Count) return null;
            return _skills[index].Data;
        }

        public SkillDataSO[] AllSkills
        {
            get
            {
                var result = new SkillDataSO[_skills.Count];
                for (int i = 0; i < _skills.Count; i++)
                    result[i] = _skills[i].Data;
                return result;
            }
        }

        public List<SkillDataSO> GetUsableSkills(int currentMp)
        {
            var usable = new List<SkillDataSO>();
            for (int i = 0; i < _skills.Count; i++)
            {
                if (CanUse(i, currentMp))
                    usable.Add(_skills[i].Data);
            }
            return usable;
        }
    }

    public class SkillRuntime
    {
        public SkillDataSO Data { get; }
        public float CooldownRemaining { get; set; }

        public SkillRuntime(SkillDataSO data)
        {
            Data = data;
            CooldownRemaining = 0f;
        }
    }
}
