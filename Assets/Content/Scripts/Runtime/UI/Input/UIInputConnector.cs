using UnityEngine;
using LittleHeroJourney;

namespace LittleHeroJourney.UI
{
    public class UIInputConnector : MonoBehaviour
    {
        public void InvokeAction(string actionId)
        {
            if (string.IsNullOrEmpty(actionId)) return;
            GameEventSystem.Publish(new UIActionEvent(actionId));
        }
    }
}