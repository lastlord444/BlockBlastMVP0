using UnityEngine;

namespace Utilities
{
    public class DebugHUD : MonoBehaviour
    {
        private void OnGUI()
        {
#if UNITY_EDITOR
            int w = Screen.width, h = Screen.height;
            float dpi = Screen.dpi;
            Rect safe = Screen.safeArea;
            float scale = 1f;

            var scaler = FindFirstObjectByType<UnityEngine.UI.CanvasScaler>();
            if (scaler) scale = scaler.scaleFactor;

            float guiScale = w > 1080 ? 2f : 1f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(guiScale, guiScale, 1));

            GUILayout.BeginArea(new Rect(10, 300, w/guiScale - 20, h/guiScale - 300));
            GUILayout.Label($"Screen: {w}x{h} @ {dpi}dpi");
            GUILayout.Label($"Safe: {safe.x},{safe.y} {safe.width}x{safe.height}");
            GUILayout.Label($"Canvas Scale: {scale:F2}");
            
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                GUILayout.Label($"Touch: {t.position}");
            }
            else
            {
                GUILayout.Label($"Mouse: {Input.mousePosition}");
            }
            GUILayout.EndArea();
#endif
        }
    }
}
