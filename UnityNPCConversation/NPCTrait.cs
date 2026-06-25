using System;
using UnityEngine;

namespace LLM.NPCConversation
{
    [Serializable]
    public class NPCTrait
    {
        public string traitName = "Trait";
        [TextArea(2, 4)]
        public string description = "Describe how this trait affects dialogue.";
        [Range(0f, 1f)]
        public float intensity = 0.5f;

        public override string ToString()
        {
            return string.IsNullOrEmpty(traitName)
                ? $"{description} ({intensity:F2})"
                : $"{traitName} ({intensity:F2}): {description}";
        }
    }
}
