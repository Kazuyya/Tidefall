using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LittleHeroJourney
{
    public class StorySequenceDisplay : MonoBehaviour
    {
        [Header("Text (TMP)")]
        [SerializeField] private TextMeshProUGUI textNarrative;
        [SerializeField] private TextMeshProUGUI textDialogue;

        [Header("Background")]
        [SerializeField] private Image solidColorImage;
        [SerializeField] private Image customImage;

        public void ApplyStep(StorySequenceSO.StoryStep step)
        {
            if (step == null) return;

            if (step.IsNarrative)
            {
                if (textNarrative != null)
                {
                    textNarrative.text = step.GetDisplayNarrativeText();
                    textNarrative.gameObject.SetActive(true);
                }
                if (textDialogue != null)
                    textDialogue.gameObject.SetActive(false);
            }
            else
            {
                if (textNarrative != null)
                    textNarrative.gameObject.SetActive(false);
                if (textDialogue != null)
                {
                    textDialogue.text = step.GetDisplayDialogueText();
                    textDialogue.gameObject.SetActive(true);
                }
            }

            if (step.backgroundType == StoryBackgroundType.Solid)
            {
                if (solidColorImage != null)
                {
                    solidColorImage.color = step.GetDisplayColor();
                    solidColorImage.enabled = true;
                    solidColorImage.gameObject.SetActive(true);
                }
                if (customImage != null)
                {
                    customImage.enabled = false;
                    customImage.gameObject.SetActive(false);
                }
            }
            else
            {
                if (solidColorImage != null)
                {
                    solidColorImage.enabled = false;
                    solidColorImage.gameObject.SetActive(false);
                }
                if (customImage != null)
                {
                    customImage.sprite = step.GetDisplayImage();
                    customImage.color = Color.white;
                    customImage.enabled = true;
                    customImage.gameObject.SetActive(true);
                }
            }
        }

        public void Clear()
        {
            if (textNarrative != null)
            {
                textNarrative.text = "";
                textNarrative.gameObject.SetActive(false);
            }
            if (textDialogue != null)
            {
                textDialogue.text = "";
                textDialogue.gameObject.SetActive(false);
            }
            if (solidColorImage != null)
            {
                solidColorImage.enabled = false;
                solidColorImage.gameObject.SetActive(false);
            }
            if (customImage != null)
            {
                customImage.sprite = null;
                customImage.enabled = false;
                customImage.gameObject.SetActive(false);
            }
        }
    }
}
