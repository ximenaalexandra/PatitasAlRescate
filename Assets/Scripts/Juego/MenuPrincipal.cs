using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú principal: Jugar (elegir rescatista), Niveles (ver los 4 niveles y el progreso), Créditos y Salir.
public class MenuPrincipal : MonoBehaviour
{
    public static bool mostrarCreditosAlIniciar;
    static int nivelElegido = 1;

    public GameObject panelMenu;
    public GameObject panelSeleccion;
    public GameObject panelNiveles;
    public GameObject panelCreditos;
    public GameObject personajesMenu;       // rescatista y mascotas de adorno
    public GameObject personajesSeleccion;  // las tres rescatistas para elegir
    public Text textoCreditos;

    [Header("Tarjetas de niveles")]
    public Image[] tarjetasNivel;
    public Text[] estadosNivel;
    public Button[] botonesNivel;
    public Text textoProgreso;

    void Start()
    {
        if (textoCreditos != null) textoCreditos.text = Creditos.Completo;
        Mostrar(mostrarCreditosAlIniciar ? panelCreditos : panelMenu);
        mostrarCreditosAlIniciar = false;
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k == null) return;
        if (panelMenu.activeSelf)
        {
            if (k.enterKey.wasPressedThisFrame) Jugar();
            if (k.nKey.wasPressedThisFrame) VerNiveles();
            if (k.cKey.wasPressedThisFrame) VerCreditos();
        }
        else if (panelSeleccion.activeSelf)
        {
            if (k.digit1Key.wasPressedThisFrame) Elegir(0);
            if (k.digit2Key.wasPressedThisFrame) Elegir(1);
            if (k.digit3Key.wasPressedThisFrame) Elegir(2);
            if (k.escapeKey.wasPressedThisFrame) Volver();
        }
        else if (panelNiveles.activeSelf)
        {
            if (k.digit1Key.wasPressedThisFrame) JugarNivel(1);
            if (k.digit2Key.wasPressedThisFrame) JugarNivel(2);
            if (k.digit3Key.wasPressedThisFrame) JugarNivel(3);
            if (k.digit4Key.wasPressedThisFrame) JugarNivel(4);
            if (k.escapeKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame) Volver();
        }
        else if (k.escapeKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame) Volver();
    }

    public void Jugar() { nivelElegido = 1; Mostrar(panelSeleccion); }
    public void VerNiveles() { ActualizarNiveles(); Mostrar(panelNiveles); }
    public void VerCreditos() { Mostrar(panelCreditos); }
    public void Volver() { Mostrar(panelMenu); }

    // Un nivel está disponible si es el primero o si ya se completó el anterior.
    public static bool Disponible(int nivel) { return nivel <= LevelManager.NivelesCompletados + 1; }

    public void JugarNivel(int nivel)
    {
        if (!Disponible(nivel)) return;
        nivelElegido = nivel;
        Mostrar(panelSeleccion);
    }

    void ActualizarNiveles()
    {
        int hechos = LevelManager.NivelesCompletados;
        if (textoProgreso != null)
            textoProgreso.text = "Progreso: " + Mathf.Min(hechos, LevelManager.TotalNiveles) + " de " + LevelManager.TotalNiveles + " niveles completados";
        for (int i = 0; i < tarjetasNivel.Length; i++)
        {
            int n = i + 1;
            bool hecho = n <= hechos, abierto = Disponible(n);
            tarjetasNivel[i].color = hecho ? new Color(0.18f, 0.45f, 0.28f, 0.92f)
                                   : abierto ? new Color(0.55f, 0.35f, 0.08f, 0.92f)
                                   : new Color(0.15f, 0.15f, 0.2f, 0.85f);
            estadosNivel[i].text = hecho ? "COMPLETADO" : abierto ? "SIGUIENTE · pulsa " + n : "BLOQUEADO";
            botonesNivel[i].interactable = abierto;
        }
    }

    public void Elegir(int indice)
    {
        SeleccionPersonaje.Indice = indice;
        Sonidos.Tocar("click");
        SceneManager.LoadScene("Nivel" + nivelElegido);
    }

    public void Salir()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void Mostrar(GameObject panel)
    {
        if (Time.timeSinceLevelLoad > 0.2f) Sonidos.Tocar("click");
        panelMenu.SetActive(panel == panelMenu);
        panelSeleccion.SetActive(panel == panelSeleccion);
        panelNiveles.SetActive(panel == panelNiveles);
        panelCreditos.SetActive(panel == panelCreditos);
        if (personajesMenu != null) personajesMenu.SetActive(panel == panelMenu);
        if (personajesSeleccion != null) personajesSeleccion.SetActive(panel == panelSeleccion);
    }
}
