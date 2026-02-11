using UnityEngine;

namespace LittleHeroJourney.UI
{
    public class WorldSpaceHealthCanvas : MonoBehaviour
    {
        private Camera cam;

        private void Awake()
        {
            cam = GameObject.FindWithTag("MainCamera")?.GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            transform.LookAt(transform.position + cam.transform.forward);
        }
    }
}
