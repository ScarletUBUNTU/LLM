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

            return builder.ToString();
        }
    }
}
