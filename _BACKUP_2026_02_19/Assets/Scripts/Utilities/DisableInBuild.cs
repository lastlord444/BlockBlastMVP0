using UnityEngine;

namespace Common.Utilities
{
    public class DisableInBuild : MonoBehaviour
    {
        private void Awake()
        {
#if !UNITY_EDITOR
            gameObject.SetActive(false);
#endif
        }
    }
}
