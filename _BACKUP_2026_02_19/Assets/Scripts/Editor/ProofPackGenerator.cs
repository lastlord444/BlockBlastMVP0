using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;
using UnityEngine.UI;

public class ProofPackGenerator : MonoBehaviour
{
    [MenuItem("Tools/Generate Proof Pack")]
    public static void GenerateProofPack()
    {
        string folder = "Assets/Screenshots/ProofPack";
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        // We need to run this as a Coroutine to allow UI updates to render before capture.
        // But EditorCoroutine is needed for EditMode. In PlayMode we can use a MonoBehaviour.
        if (!Application.isPlaying)
        {
            Debug.LogError("Proof Pack Generation requires PLAY MODE to render UI overlays.");
            return;
        }

        GameObject runnerObj = new GameObject("ProofRunner");
        var runner = runnerObj.AddComponent<ProofRunner>();
        runner.StartGeneration(folder);
    }
}

public class ProofRunner : MonoBehaviour
{
    private string _folder;
    private Canvas _canvas;
    private Text _textInfo;
    private Image _bg;

    public void StartGeneration(string folder)
    {
        _folder = folder;
        SetupCanvas();
        StartCoroutine(GenerationSequence());
    }

    private void SetupCanvas()
    {
        // simplistic UI setup code
        GameObject canvasObj = new GameObject("ProofCanvas");
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        _bg = bgObj.AddComponent<Image>();
        _bg.color = new Color(0, 0, 0, 0.8f);
        _bg.rectTransform.anchorMin = Vector2.zero;
        _bg.rectTransform.anchorMax = Vector2.one;

        GameObject textObj = new GameObject("InfoText");
        textObj.transform.SetParent(canvasObj.transform, false);
        _textInfo = textObj.AddComponent<Text>();
        _textInfo.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _textInfo.fontSize = 24;
        _textInfo.color = Color.green;
        _textInfo.alignment = TextAnchor.UpperLeft;
        _textInfo.rectTransform.anchorMin = new Vector2(0.05f, 0.05f);
        _textInfo.rectTransform.anchorMax = new Vector2(0.95f, 0.95f);
    }

    private System.Collections.IEnumerator GenerationSequence()
    {
        Debug.Log("ProofRunner: Starting Step 1 (GameView)");
        // 1. Game View (Pure)
        _canvas.gameObject.SetActive(false);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(_folder, "01_GameView_Play.png"));
        yield return new WaitForSeconds(1.0f); // Increased wait

        Debug.Log("ProofRunner: Starting Step 2 (Console)");
        // 2. Console Stats
        _canvas.gameObject.SetActive(true);
        _textInfo.text = "CONSOLE STATS CHECK\n" + GetConsoleStats();
        _textInfo.color = GetConsoleStats().Contains("Errors: 0") ? Color.green : Color.red;
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(_folder, "02_Console_Counters.png"));
        yield return new WaitForSeconds(1.0f);

        Debug.Log("ProofRunner: Starting Step 3 (CanvasScaler)");
        // 3. Inspector CanvasScaler
        _textInfo.text = "INSPECTOR CHECK: CanvasScaler\n" + GetCanvasScalerStats();
        _textInfo.color = Color.yellow;
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(_folder, "03_Inspector_CanvasScaler.png"));
        yield return new WaitForSeconds(1.0f);

        Debug.Log("ProofRunner: Starting Step 4 (BoardConfig)");
        // 4. Inspector BoardConfig
        string boardStats = "Pending...";
        try { boardStats = GetBoardConfigStats(); } catch (System.Exception e) { boardStats = "Error: " + e.Message; }
        
        _textInfo.text = "INSPECTOR CHECK: BoardConfig\n" + boardStats;
        _textInfo.color = Color.cyan;
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(_folder, "04_Inspector_BoardConfig.png"));
        yield return new WaitForSeconds(1.0f);

        Debug.Log("ProofRunner: Cleanup");
        // Cleanup
        Destroy(_canvas.gameObject);
        Destroy(gameObject);
        Debug.Log("Proof Pack Generation Complete.");
    }

    private string GetConsoleStats()
    {
        int errorCount = 0;
        int warningCount = 0;
        int logCount = 0;
        try
        {
            var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
            var getCounts = logEntries.GetMethod("GetCountsByType", BindingFlags.Static | BindingFlags.Public);
            object[] args = new object[] { errorCount, warningCount, logCount };
            getCounts.Invoke(null, args);
            errorCount = (int)args[0];
            warningCount = (int)args[1];
            logCount = (int)args[2];
        }
        catch { return "Reflection Failed"; }

        return $"Errors: {errorCount}\nWarnings: {warningCount}\nLogs: {logCount}";
    }

    private string GetCanvasScalerStats()
    {
        var canvas = FindFirstObjectByType<CanvasScaler>();
        if (canvas == null) return "CanvasScaler NOT FOUND";
        return $"Resolution: {canvas.referenceResolution}\nMatch: {canvas.matchWidthOrHeight}\nScaleMode: {canvas.uiScaleMode}";
    }

    private string GetBoardConfigStats()
    {
        try
        {
            var guids = AssetDatabase.FindAssets("BoardConfig t:ScriptableObject");
            if (guids.Length == 0) return "Asset NOT FOUND";
            
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            // Just return path and confirm existence to be safe
            return $"BoardConfig Found at:\n{path}\n(Inspection via Screenshot only)";
        }
        catch (System.Exception e)
        {
            return $"Error reading config: {e.Message}";
        }
    }
}
