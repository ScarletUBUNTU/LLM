using UnityEngine;

namespace LLM.NPCConversation
{
    public class NPCConversationExample : MonoBehaviour
    {
        public NPCDialogueManager dialogueManager;
        public string samplePlayerInput = "Hello there! Can you tell me about this town?";

        private void Start()
        {
            if (dialogueManager == null)
            {
                dialogueManager = FindObjectOfType<NPCDialogueManager>();
            }

            if (dialogueManager != null)
            {
                dialogueManager.StartConversation(samplePlayerInput);
            }
            else
            {
                Debug.LogWarning("No NPCDialogueManager assigned or found in the scene.");
            }
        }
    }
}
