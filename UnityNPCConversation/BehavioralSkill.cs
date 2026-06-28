using System;
using System.Collections.Generic;
using UnityEngine;

namespace LLM.NPCConversation
{
    [Serializable]
    public class BehavioralSkill
    {
        public string skillName = "New Skill";
        [TextArea(2, 4)]
        public string description = "A brief description of the behavior or talent.";
        public bool isGood = true;
        public List<StatBonus> statBonuses = new List<StatBonus>();

        public override string ToString()
        {
            return string.IsNullOrEmpty(skillName)
                ? $"{(isGood ? "Good" : "Bad")} Skill"
                : $"{skillName} ({(isGood ? "Good" : "Bad")})";
        }
    }

    [Serializable]
    public class StatBonus
    {
        public string statName = "Stat";
        public int amount = 0;

        public override string ToString()
        {
            var sign = amount >= 0 ? "+" : string.Empty;
            return $"{statName}: {sign}{amount}";
        }
    }
}
