using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private float updateInterval = 0.5f;

    private float elapsedTime = 0f;
    private int frameCount = 0;
    private float currentFPS = 0f;

    private void Start()
    {
        if (fpsText == null)
        {
            fpsText = GetComponent<TextMeshProUGUI>();
        }
    }

    private void Update()
    {
        frameCount++;
        elapsedTime += Time.deltaTime;

        if (elapsedTime >= updateInterval)
        {
            currentFPS = frameCount / elapsedTime;
            
            if (fpsText != null)
            {
                fpsText.text = $"FPS: {currentFPS:F1}";
            }

            frameCount = 0;
            elapsedTime = 0f;
        }
    }
}
