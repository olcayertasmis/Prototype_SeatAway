using UnityEngine;

namespace PSA.Gameplay.UI
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform _panel;
        private Rect _lastSafeArea = new Rect(0, 0, 0, 0);

        #region Unity Lifecycle

        private void Start()
        {
            _panel = GetComponent<RectTransform>();
            ApplySafeArea(GetSafeArea());
        }

        private void Update()
        {
            Refresh();
        }

        #endregion

        #region Core Logic

        private void Refresh()
        {
            Rect safeArea = GetSafeArea();

            if (safeArea != _lastSafeArea)
            {
                ApplySafeArea(safeArea);
            }
        }

        private Rect GetSafeArea()
        {
            return Screen.safeArea;
        }

        private void ApplySafeArea(Rect r)
        {
            _lastSafeArea = r;

            Vector2 anchorMin = r.position;
            Vector2 anchorMax = r.position + r.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _panel.anchorMin = anchorMin;
            _panel.anchorMax = anchorMax;
        }

        #endregion
    }
}