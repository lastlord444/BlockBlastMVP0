using UnityEngine;
using Common.GameModes;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class StartupReferenceValidator : MonoBehaviour
{
    [Header("Critical References")]
    [SerializeField] private BlockBlastGameManager _gameManager;
    [SerializeField] private ScriptableObject _boardConfig;

    private void Awake()
    {
        Validate();
    }

    private void Validate()
    {
        bool criticalError = false;

        if (_gameManager == null)
        {
            _gameManager = FindObjectOfType<BlockBlastGameManager>();
            if (_gameManager == null)
            {
                 Debug.LogError($"[Validator] CRITICAL: BlockBlastGameManager missing on {gameObject.name}");
                 criticalError = true;
            }
        }

        if (_boardConfig == null)
        {
             Debug.LogError($"[Validator] CRITICAL: BoardConfig missing on {gameObject.name}. Trying to load from Resources...");
             _boardConfig = Resources.Load<ScriptableObject>("BoardConfig");
             if(_boardConfig == null) Debug.LogError("[Validator] FATAL: BoardConfig not found in Resources either!");
        }

        if (_gameManager != null)
        {
#if UNITY_EDITOR
             // Check if GM has it too (Editor only patch)
             var gmSo = new SerializedObject(_gameManager);
             var boardProp = gmSo.FindProperty("_boardConfig");
             if(boardProp.objectReferenceValue == null)
             {
                 Debug.LogWarning("[Validator] GameManager has null _boardConfig! Attempting to patch runtime...");
                 if(_boardConfig != null) 
                 {
                     boardProp.objectReferenceValue = _boardConfig;
                     gmSo.ApplyModifiedProperties();
                     Debug.Log("[Validator] Patched GameManager with BoardConfig.");
                 }
             }
#endif
        }

        if (criticalError)
        {
            Debug.LogError("[Validator] Stopping execution due to missing critical references.");
            enabled = false;
            return;
        }
        
        Debug.Log("[Validator] All critical checks passed.");
    }
}
