using System;
using UnityEngine;
using UnityEngine.UI;

// Interfaz del nivel: diálogo, información, nube de consejos, corazones, poder, inventario y recorrido de niveles.
public class UIJuego : MonoBehaviour
{
    public static UIJuego Instancia;
    public static bool PanelAbierto { get { return Instancia != null && (Instancia.dialogoActivo || Instancia.infoActiva); } }
    public static bool NubeVisible { get { return Instancia != null && Instancia.nube != null && Instancia.nube.gameObject.activeSelf; } }

    [Header("Panel de diálogo")]
    public GameObject panelDialogo;
    public Text textoNombre;
    public Text textoDialogo;

    [Header("Panel de información (carteles)")]
    public GameObject panelInfo;
    public Text textoTituloInfo;
    public Text textoInfo;

    [Header("Vidas, poder e inventario")]
    public Image[] corazones;
    public Sprite corazonLleno;
    public Sprite corazonVacio;
    public RectTransform barraPoder;
    public Image[] ranuras;
    public Text textoItems;
    public Text textoMision;

    [Header("Nube de consejos")]
    public RectTransform nube;
    public Text textoTituloNube;
    public Text textoNube;
    public float duracionNube = 9f;

    [Header("Recorrido de niveles")]
    public Image[] fichasNivel;
    public GameObject[] marcasCompletado;
    public Color colorActual = new Color(1f, 0.78f, 0.3f);
    public Color colorCompletado = new Color(0.35f, 0.75f, 0.45f);

    DialogueLine[] lineas;
    int indice;
    Action alTerminar;
    bool dialogoActivo;
    bool infoActiva;
    Transform objetivoNube;
    float tiempoNube;
    RectTransform canvasRt;

    void Awake()
    {
        Instancia = this;
        if (panelDialogo != null) panelDialogo.SetActive(false);
        if (panelInfo != null) panelInfo.SetActive(false);
        if (nube != null) nube.gameObject.SetActive(false);
        canvasRt = (RectTransform)transform;
    }

    // Nube con el consejo: aparece sobre la mascota que se acaba de cuidar y se cierra sola.
    public void MostrarNube(Transform sobre, string titulo, string texto)
    {
        if (nube == null) return;
        objetivoNube = sobre;
        textoTituloNube.text = titulo;
        textoNube.text = texto;
        tiempoNube = duracionNube;
        nube.gameObject.SetActive(true);
        nube.SetAsLastSibling();
        nube.localScale = Vector3.one * 0.2f;
        Sonidos.Tocar("nube");
        PosicionarNube();
    }

    public void OcultarNube()
    {
        if (nube != null) nube.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (nube == null || !nube.gameObject.activeSelf) return;
        tiempoNube -= Time.deltaTime;
        if (tiempoNube <= 0f || objetivoNube == null) { OcultarNube(); return; }
        float s = Mathf.Min(1f, nube.localScale.x + Time.deltaTime * 5f);
        nube.localScale = new Vector3(s, s, 1f);
        PosicionarNube();
    }

    void PosicionarNube()
    {
        var cam = Camera.main;
        if (cam == null || objetivoNube == null) return;
        Vector3 pantalla = cam.WorldToScreenPoint(objetivoNube.position + Vector3.up * 0.9f);
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, pantalla, null, out local);
        Vector2 tam = canvasRt.rect.size;
        float medio = nube.rect.width * 0.5f + 10f;
        local.x = Mathf.Clamp(local.x, -tam.x * 0.5f + medio, tam.x * 0.5f - medio);
        local.y = Mathf.Clamp(local.y, -tam.y * 0.5f + 60f, tam.y * 0.5f - nube.rect.height - 152f);
        nube.anchoredPosition = local;
    }

    public void MarcarNivelCompletado(int numero)
    {
        int i = numero - 1;
        if (fichasNivel == null || i < 0 || i >= fichasNivel.Length) return;
        fichasNivel[i].color = colorCompletado;
        if (marcasCompletado != null && i < marcasCompletado.Length && marcasCompletado[i] != null) marcasCompletado[i].SetActive(true);
    }

    // Devuelve false si ya hay un panel abierto (evita diálogos duplicados).
    public bool MostrarDialogo(DialogueLine[] nuevas, Action fin)
    {
        if (PanelAbierto || nuevas == null || nuevas.Length == 0) return false;
        lineas = nuevas;
        indice = 0;
        alTerminar = fin;
        dialogoActivo = true;
        panelDialogo.SetActive(true);
        MostrarLinea();
        return true;
    }

    void MostrarLinea()
    {
        var l = lineas[indice];
        textoNombre.text = Reemplazar(l.speakerName);
        textoNombre.color = l.nameColor.a > 0f ? l.nameColor : Color.white;
        textoDialogo.text = Reemplazar(l.text);
    }

    static string Reemplazar(string s)
    {
        return string.IsNullOrEmpty(s) ? "" : s.Replace("{jugadora}", SeleccionPersonaje.Nombre);
    }

    public void MostrarInfo(string titulo, string texto)
    {
        if (PanelAbierto) return;
        infoActiva = true;
        panelInfo.SetActive(true);
        textoTituloInfo.text = titulo;
        textoInfo.text = texto;
    }

    // Se llama al pulsar E, Espacio o Enter con un panel abierto.
    public void Avanzar()
    {
        if (infoActiva)
        {
            infoActiva = false;
            panelInfo.SetActive(false);
            return;
        }
        if (!dialogoActivo) return;
        indice++;
        if (indice < lineas.Length)
        {
            MostrarLinea();
            return;
        }
        dialogoActivo = false;
        panelDialogo.SetActive(false);
        var fin = alTerminar;
        alTerminar = null;
        if (fin != null) fin();
    }

    public void ActualizarVidas(int vidas)
    {
        for (int i = 0; i < corazones.Length; i++)
            corazones[i].sprite = i < vidas ? corazonLleno : corazonVacio;
    }

    public void ActualizarPoder(float fraccion)
    {
        if (barraPoder != null) barraPoder.anchorMax = new Vector2(Mathf.Clamp01(fraccion), 1f);
    }

    public void ActualizarInventario(string nombre, int cantidad, int total, Sprite icono)
    {
        if (textoItems != null) textoItems.text = nombre + ": " + cantidad;
        for (int i = 0; i < ranuras.Length; i++)
        {
            ranuras[i].gameObject.SetActive(i < total);
            ranuras[i].sprite = icono;
            ranuras[i].color = i < cantidad ? Color.white : new Color(0f, 0f, 0f, 0.45f);
        }
    }
}
