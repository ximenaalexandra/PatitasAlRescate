using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum TipoNivel { Curar, Alimentar, Banar, Adopcion }

// GameManager del nivel: misión, tiempo, mascotas, objetos, cuidado individual, adopción, victoria y derrota.
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instancia;
    public const int TotalNiveles = 4;
    public static readonly string[] NombresNiveles = { "Curar heridas", "Alimentar", "Bañar y limpiar", "Feria de adopción" };

    [Header("Configuración del nivel")]
    public int numeroNivel = 1;
    public string nombreNivel = "Curar heridas";
    public TipoNivel tipo = TipoNivel.Curar;
    public float tiempoLimite = 150f;          // 0 = sin límite de tiempo
    public string siguienteEscena = "Nivel2";
    public bool esUltimoNivel = false;
    public string nombreGuia = "Dra. Lucía";
    public bool sinDanio = false;
    [Range(1, 3)] public int dificultad = 1;   // dificultad de los minijuegos de cuidado
    public float penalizacionFallo = 10f;     // segundos que se pierden al fallar un cuidado              // solo en el modo prueba: los obstáculos no quitan corazones

    [Header("Objetos para cuidar")]
    public string nombreItems = "Botiquines";
    public string nombreItem = "botiquín";
    public int totalItems = 5;
    public Sprite iconoItem;

    [Header("Mensajes")]
    [TextArea(2, 4)] public string consejoFinal;

    [Header("HUD")]
    public Text textoNivel;
    public Text textoMascotas;
    public Text textoTiempo;
    public Text textoMensaje;
    public Text textoAyuda;
    public GameObject panelFin;
    public Text textoFin;
    public MapaUI mapa;

    public bool Jugando { get; private set; }
    public bool MisionIniciada { get { return habloConGuia; } }
    public bool PuedeAdoptar { get { return items > 0 && seguidoras.Count > 0; } }

    Mascota[] mascotas;
    readonly List<Mascota> seguidoras = new List<Mascota>();
    PlayerController jugadora;
    int cuidadas, items, recogidos, adoptadas;
    bool habloConGuia, gano, terminando;
    float tiempo, tiempoMensaje;

    // Progreso guardado: cuántos niveles se han completado (para el menú de niveles).
    public static int NivelesCompletados
    {
        get { return PlayerPrefs.GetInt("nivelesCompletados", 0); }
        set { PlayerPrefs.SetInt("nivelesCompletados", value); PlayerPrefs.Save(); }
    }

    void Awake()
    {
        Instancia = this;
    }

    void Start()
    {
        mascotas = FindObjectsByType<Mascota>(FindObjectsSortMode.InstanceID);
        System.Array.Sort(mascotas, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        jugadora = FindFirstObjectByType<PlayerController>();
        tiempo = tiempoLimite;
        Jugando = true;
        MiniJuego.FactorTiempo = sinDanio ? 4f : 1f;
        if (panelFin != null) panelFin.SetActive(false);
        if (textoNivel != null) textoNivel.text = "NIVEL " + numeroNivel + " DE " + TotalNiveles + ": " + nombreNivel;

        if (tipo == TipoNivel.Adopcion)
        {
            // En la feria las mascotas ya vienen sanas y siguen a la rescatista desde el inicio
            foreach (var m in mascotas)
            {
                m.Rescatar(jugadora.transform, seguidoras.Count);
                seguidoras.Add(m);
            }
        }

        if (mapa != null && jugadora != null) mapa.Configurar(jugadora);
        Mensaje("Habla con la " + nombreGuia + " para conocer tu misión.", 4f);
        ActualizarHUD();
    }

    void Update()
    {
        var k = Keyboard.current;

        if (Jugando && !UIJuego.PanelAbierto && !UIJuego.NubeVisible && tiempoLimite > 0f && !terminando)
        {
            tiempo -= Time.deltaTime;
            if (tiempo <= 0f) { tiempo = 0f; PerderNivel("¡Se acabó el tiempo!"); }
        }
        else if (!Jugando && k != null)
        {
            if (gano && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame))
            {
                if (esUltimoNivel) MenuPrincipal.mostrarCreditosAlIniciar = true;
                SceneManager.LoadScene(siguienteEscena);
            }
            if (!gano && k.rKey.wasPressedThisFrame) Reiniciar();
        }

        if (k != null && k.escapeKey.wasPressedThisFrame) SceneManager.LoadScene("Menu");

        if (tiempoMensaje > 0f)
        {
            tiempoMensaje -= Time.deltaTime;
            if (tiempoMensaje <= 0f && textoMensaje != null) textoMensaje.text = "";
        }

        if (textoAyuda != null)
        {
            var obj = jugadora != null ? jugadora.Objetivo : null;
            textoAyuda.text = Jugando && obj != null && !UIJuego.PanelAbierto && !MiniJuego.Activo ? "Presiona E para " + obj.Accion : "";
        }

        ActualizarHUD();
    }

    public void Reiniciar()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void ActualizarHUD()
    {
        if (textoMascotas != null && mascotas != null)
            textoMascotas.text = tipo == TipoNivel.Adopcion
                ? "Adoptadas: " + adoptadas + "/" + mascotas.Length
                : "Atendidas: " + cuidadas + "/" + mascotas.Length;
        if (textoTiempo != null)
        {
            if (tiempoLimite <= 0f) textoTiempo.text = "Sin límite";
            else
            {
                int t = Mathf.CeilToInt(tiempo);
                textoTiempo.text = "Tiempo: " + (t / 60) + ":" + (t % 60).ToString("00");
                textoTiempo.color = tiempo < 20f ? new Color(1f, 0.45f, 0.45f) : Color.white;
            }
        }
        var ui = UIJuego.Instancia;
        if (ui != null)
        {
            ui.ActualizarInventario(nombreItems, items, totalItems, iconoItem);
            if (ui.textoMision != null) ui.textoMision.text = "Misión: " + TextoMision();
        }
    }

    string Verbo()
    {
        switch (tipo)
        {
            case TipoNivel.Curar: return "cura a las mascotas heridas";
            case TipoNivel.Alimentar: return "alimenta a las mascotas con hambre";
            case TipoNivel.Banar: return "baña a las mascotas sucias";
        }
        return "cuida a las mascotas";
    }

    string TextoMision()
    {
        if (gano) return "¡Completada!";
        if (!habloConGuia) return "habla con la " + nombreGuia + " (acércate y pulsa E).";
        if (tipo == TipoNivel.Adopcion)
            return "recoge los collares y entrega cada mascota a una familia (" + adoptadas + "/" + mascotas.Length + ").";
        if (cuidadas >= mascotas.Length) return "¡Todas las mascotas están bien!";
        if (items == 0)
            return "recoge " + nombreItems.ToLower() + " y " + Verbo() + " (" + cuidadas + "/" + mascotas.Length + ").";
        return Verbo() + ": acércate a cada una y pulsa E (" + cuidadas + "/" + mascotas.Length + ").";
    }

    public void Mensaje(string texto, float duracion)
    {
        if (textoMensaje == null) return;
        textoMensaje.text = texto;
        tiempoMensaje = duracion;
    }

    public void HabloConGuia()
    {
        if (habloConGuia) return;
        habloConGuia = true;
        Sonidos.Tocar("mision");
        Mensaje("¡Misión recibida! Revisa el objetivo arriba y el mapa (M).", 3f);
    }

    public void AgregarItem(string nombre, int valor)
    {
        items += valor;
        recogidos += valor;
        Mensaje("Recogiste: " + nombre + " (tienes " + items + ")", 1.6f);
        ActualizarHUD();
    }

    // Curar, alimentar o bañar a una mascota concreta. Devuelve true si se pudo.
    public bool CuidarMascota(Mascota m, Transform rescatista)
    {
        if (!Jugando || m.Cuidada) return false;
        if (!habloConGuia)
        {
            Mensaje("Primero habla con la " + nombreGuia + ": ella te dirá cómo ayudarla.", 2.2f);
            return false;
        }
        if (items <= 0)
        {
            Mensaje("Necesitas un " + nombreItem + " para ayudar a " + m.nombreMascota + ". ¡Búscalo en la calle!", 2.2f);
            return false;
        }
        if (MiniJuego.Instancia == null) { CompletarCuidado(m, rescatista); return true; }
        string verbo = m.necesidad == Necesidad.Herida ? "Curar a " : m.necesidad == Necesidad.Hambre ? "Alimentar a " : "Bañar a ";
        string especie = m.especie == Especie.Gato ? " (gato)" : " (perro)";
        MiniJuego.Instancia.Iniciar(verbo + m.nombreMascota + especie, MiniJuego.PasosCuidado(m.necesidad, m.especie, dificultad), (ok, motivo) =>
        {
            if (!Jugando) return;
            if (ok) CompletarCuidado(m, rescatista);
            else FalloCuidado(m);
        });
        return true;
    }

    void FalloCuidado(Mascota m)
    {
        if (tiempoLimite > 0f)
        {
            tiempo = Mathf.Max(1f, tiempo - penalizacionFallo);
            Mensaje(m.nombreMascota + " todavía necesita ayuda. ¡Inténtalo de nuevo! (-" + penalizacionFallo + " s)", 2.5f);
        }
        else Mensaje(m.nombreMascota + " todavía necesita ayuda. ¡Inténtalo de nuevo!", 2.5f);
    }

    void CompletarCuidado(Mascota m, Transform rescatista)
    {
        if (m.Cuidada) return;
        items--;
        Necesidad necesidad = m.necesidad;
        Sonidos.Tocar("curado");
        Sonidos.Tocar(m.especie == Especie.Gato ? "maullido" : "ladrido");
        m.Cuidar(rescatista, seguidoras.Count);
        seguidoras.Add(m);
        cuidadas++;

        string hecho = necesidad == Necesidad.Herida ? " ya tiene sus heridas curadas" :
                       necesidad == Necesidad.Hambre ? " ya comió y está feliz" : " ya se bañó y huele delicioso";
        Mensaje("¡" + m.nombreMascota + hecho + "!", 2.5f);
        if (UIJuego.Instancia != null)
            UIJuego.Instancia.MostrarNube(m.transform, Consejos.Titulo(necesidad, false), Consejos.Texto(necesidad, m.especie, false));

        if (cuidadas >= mascotas.Length) StartCoroutine(GanarCuandoTermineNube());
        ActualizarHUD();
    }

    public bool EntregarEnAdopcion(Adoptante familia)
    {
        if (!PuedeAdoptar) return false;
        var m = seguidoras[0];
        seguidoras.RemoveAt(0);
        for (int i = 0; i < seguidoras.Count; i++) seguidoras[i].Seguir(jugadora.transform, i);
        m.Seguir(familia.transform, 0);
        m.MostrarCorazon(3f);
        Sonidos.Tocar("adopcion");
        Sonidos.Tocar(m.especie == Especie.Gato ? "maullido" : "ladrido");
        items--;
        adoptadas++;
        jugadora.HacerAccion(0.8f);
        int faltan = mascotas.Length - adoptadas;
        Mensaje(faltan > 0 ? "¡" + familia.nombreFamilia + " adoptó a " + m.nombreMascota + "! Faltan " + faltan + "." : "¡Todas las mascotas tienen un hogar!", 2.5f);
        if (UIJuego.Instancia != null)
            UIJuego.Instancia.MostrarNube(m.transform, Consejos.Titulo(Necesidad.Ninguna, true), Consejos.Texto(Necesidad.Ninguna, m.especie, true));
        if (faltan == 0) StartCoroutine(GanarCuandoTermineNube());
        return true;
    }

    IEnumerator GanarCuandoTermineNube()
    {
        terminando = true;
        yield return new WaitForSeconds(1.5f);
        while (UIJuego.NubeVisible) yield return null;
        GanarNivel();
    }

    void GanarNivel()
    {
        if (!Jugando) return;
        Jugando = false;
        gano = true;
        Sonidos.PausarMusica(true);
        Sonidos.Tocar("victoria");
        if (NivelesCompletados < numeroNivel) NivelesCompletados = numeroNivel;
        if (UIJuego.Instancia != null) UIJuego.Instancia.MarcarNivelCompletado(numeroNivel);
        Mensaje("", 0f);
        if (panelFin == null) return;
        panelFin.SetActive(true);
        textoFin.text = (esUltimoNivel
            ? "¡FELICIDADES, " + SeleccionPersonaje.Nombre.ToUpper() + "!\nTodas las mascotas encontraron un hogar."
            : "¡NIVEL " + numeroNivel + " DE " + TotalNiveles + " COMPLETADO!")
            + "\n\n" + consejoFinal + "\n\n" +
            (esUltimoNivel ? "Pulsa Enter para ver los créditos" : "Pulsa Enter para ir al nivel " + (numeroNivel + 1) + ": " + NombresNiveles[numeroNivel]);
    }

    public void PerderNivel(string motivo)
    {
        if (!Jugando) return;
        Jugando = false;
        gano = false;
        Sonidos.PausarMusica(true);
        Sonidos.Tocar("derrota");
        Mensaje("", 0f);
        if (UIJuego.Instancia != null) UIJuego.Instancia.OcultarNube();
        if (MiniJuego.Instancia != null) MiniJuego.Instancia.Cancelar();
        if (panelFin == null) return;
        panelFin.SetActive(true);
        textoFin.text = motivo + "\n\nPulsa R para intentarlo de nuevo\nEsc para volver al menú";
    }
}
