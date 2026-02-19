using UnityEngine;

namespace Common.UI
{
    public class DevOnlyVisibility : MonoBehaviour
    {
        private void Awake()
        {
            // Editor'da veya DEVELOPMENT_BUILD'de görünür, release'de gizli
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            gameObject.SetActive(false);
#endif
        }
    }
}
