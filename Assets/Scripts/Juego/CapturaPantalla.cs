using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

// Pulsa F12 para guardar una captura de pantalla (evidencias gráficas).
public class CapturaPantalla : MonoBehaviour
{
    static int contador;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        var go = new GameObject("CapturaPantalla");
        DontDestroyOnLoad(go);
        go.AddComponent<CapturaPantalla>();
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k == null || !k.f12Key.wasPressedThisFrame) return;
#if UNITY_EDITOR
        string carpeta = Path.Combine(Application.dataPath, "..", "Evidencias");
#else
        string carpeta = Path.Combine(Application.persistentDataPath, "Evidencias");
#endif
        Directory.CreateDirectory(carpeta);
        contador++;
        string archivo = Path.Combine(carpeta, "captura_" + System.DateTime.Now.ToString("HHmmss") + "_" + contador + ".png");
        ScreenCapture.CaptureScreenshot(archivo, 1);
        Debug.Log("Captura guardada: " + archivo);
    }
}
