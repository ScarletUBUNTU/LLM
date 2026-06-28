using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LLM.NPCConversation
{
    [CreateAssetMenu(fileName = "NPCProfile", menuName = "LLM/NPC Profile", order = 100)]
    public class NPCProfile : ScriptableObject
    {
        public string npcName = "Unnamed NPC";
        public string role = "Villager";

        [TextArea(3, 5)]
        public string background = "A short biography or background story for the NPC.";

        [TextArea(2, 4)]
        public string conversationTone = "Friendly and helpful.";

        [TextArea(2, 4)]
        public string currentGoal = "Assist the player or share rumors.";

        public List<NPCTrait> traits = new List<NPCTrait>();
        public List<NPCTrait> assignedTraits = new List<NPCTrait>();
        public List<BehavioralSkill> assignedSkills = new List<BehavioralSkill>();
        public List<NPCStat> stats = new List<NPCStat>();

        public string BuildTraitSummary()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"NPC Name: {npcName}");
            builder.AppendLine($"Role: {role}");
            builder.AppendLine($"Tone: {conversationTone}");
            builder.AppendLine($"Goal: {currentGoal}");
            builder.AppendLine($"Background: {background}");
            builder.AppendLine("Traits:");

            if (traits.Count == 0)
            {
                builder.AppendLine("- None");
            }
            else
            {
                foreach (var trait in traits)
                {
                    builder.AppendLine($"- {trait}");
                }
            }

            builder.AppendLine("Skills:");
            if (assignedSkills.Count == 0)
            {
                builder.AppendLine("- None");
            }
            else
            {
                foreach (var skill in assignedSkills)
                {
                    builder.AppendLine($"- {skill}");
                }
            }

            builder.AppendLine("Stats:");
            if (stats.Count == 0)
            {
                builder.AppendLine("- None");
            }
            else
            {
                foreach (var stat in stats)
                {
                    builder.AppendLine($"- {stat}");
                }
            }
            builder.AppendLine("Assigned Traits:");
            if (assignedTraits.Count == 0)
            {
                builder.AppendLine("- None");
            }
            else
            {
                foreach (var assignedTrait in assignedTraits)
                {
                    builder.AppendLine($"- {assignedTrait}");
                }
            }

            builder.AppendLine("Skills:");
            if (assignedSkills.Count == 0)
            {
                builder.AppendLine("- None");
            }
            else
            {
                foreach (var skill in assignedSkills)
                {
                    builder.AppendLine($"- {skill}");
                }
            }

            builder.AppendLine("Stats:");
            if (stats.Count == 0)
            {
                builder.AppendLine("- None");
            }
            else
            {
                foreach (var stat in stats)
                {
                    builder.AppendLine($"- {stat}");
                }
            }
            return builder.ToString();
        }

        public void ApplyStatChanges(List<StatBonus> statBonuses)
        {
            if (statBonuses == null)
            {
                return;
            }

            foreach (var bonus in statBonuses)
            {
                if (string.IsNullOrEmpty(bonus.statName))
                {
                    continue;
                }

                var existing = stats.Find(s => s.statName == bonus.statName);
                if (existing != null)
                {
                    existing.value += bonus.amount;
                }
                else
                {
                    stats.Add(new NPCStat { statName = bonus.statName, value = bonus.amount });
                }
            }
        }
    }
}
