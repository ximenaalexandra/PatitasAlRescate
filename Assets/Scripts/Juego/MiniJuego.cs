using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Minijuegos de cuidado: cada acción (curar, alimentar, bañar, adoptar) se hace en varios pasos
// que hay que superar con habilidad. La dificultad sube con cada nivel.
public class MiniJuego : MonoBehaviour
{
    public enum Tipo { Opcion, Secuencia, Tiempo, Frotar, Llenar }

    public class Paso
    {
        public Tipo tipo;
        public string nombre;          // "Limpiar la herida"
        public string instruccion;     // qué teclas usar
        public string[] opciones;      // Opcion
        public int correcta;           // Opcion
        public string porQue;          // explicación si se equivoca
        public int largo = 4;          // Secuencia: cantidad de flechas / Frotar: frotes necesarios
        public float tiempo;           // límite de tiempo del paso (0 = sin límite)
        public float velocidad = 1f;   // Tiempo: velocidad de la marca / Llenar: velocidad de llenado
        public float ancho = 0.2f;     // ancho de la zona correcta (0 a 1)
        public int aciertos = 1;       // Tiempo: aciertos necesarios
        public int fallosPermitidos = 1;
        public float meta = 0.6f;      // Llenar: dónde está la línea
    }

    public static MiniJuego Instancia;
    public static bool Activo { get { return Instancia != null && Instancia.activo; } }
    public static float FactorTiempo = 1f;   // en el modo prueba los pasos dan más tiempo

    [Header("Interfaz")]
    public GameObject panel;
    public Text textoTitulo;
    public Text textoPaso;
    public Text textoInstruccion;
    public Text textoCentro;
    public Text textoOpciones;
    public Text textoResultado;
    public GameObject grupoBarra;
    public RectTransform zona;
    public RectTransform marcador;
    public GameObject grupoProgreso;
    public RectTransform rellenoProgreso;
    public RectTransform rellenoReloj;
    public GameObject grupoReloj;

    static readonly string[] Flechas = { "←", "↑", "→", "↓" };

    bool activo;
    List<Paso> pasos;
    int indice;
    Action<bool, string> alTerminar;
    float t;              // tiempo restante del paso
    float pos, dir = 1f;  // marca de la barra
    int aciertos, fallos;
    int[] secuencia;
    int posSecuencia;
    float progreso;
    int ultimoLado;       // Frotar: -1 izquierda, 1 derecha
    float llenado;
    bool llenando;
    float espera;         // pausa para mostrar el resultado
    bool termino, exito;
    string motivo;

    void Awake()
    {
        Instancia = this;
        if (panel != null) panel.SetActive(false);
    }

    public void Iniciar(string titulo, List<Paso> nuevos, Action<bool, string> fin)
    {
        pasos = nuevos;
        alTerminar = fin;
        indice = 0;
        termino = false;
        espera = 0f;
        activo = true;
        textoTitulo.text = titulo;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        EmpezarPaso();
    }

    public void Cancelar()
    {
        activo = false;
        if (panel != null) panel.SetActive(false);
    }

    void EmpezarPaso()
    {
        var p = pasos[indice];
        textoPaso.text = "Paso " + (indice + 1) + " de " + pasos.Count + ": " + p.nombre;
        textoInstruccion.text = p.instruccion;
        textoResultado.text = "";
        textoCentro.text = "";
        textoOpciones.text = "";
        t = p.tiempo * FactorTiempo;
        pos = 0f; dir = 1f;
        aciertos = 0; fallos = 0;
        progreso = 0f; ultimoLado = 0;
        llenado = 0f; llenando = false;
        grupoBarra.SetActive(p.tipo == Tipo.Tiempo || p.tipo == Tipo.Llenar);
        grupoProgreso.SetActive(p.tipo == Tipo.Frotar);
        grupoReloj.SetActive(p.tiempo > 0f);

        if (p.tipo == Tipo.Opcion)
        {
            string s = "";
            for (int i = 0; i < p.opciones.Length; i++) s += (i + 1) + ".  " + p.opciones[i] + "\n";
            textoOpciones.text = s;
        }
        else if (p.tipo == Tipo.Secuencia)
        {
            secuencia = new int[p.largo];
            for (int i = 0; i < p.largo; i++) secuencia[i] = UnityEngine.Random.Range(0, 4);
            posSecuencia = 0;
        }
        else if (p.tipo == Tipo.Tiempo)
        {
            if (FactorTiempo > 1f) { p.ancho = Mathf.Max(p.ancho, 0.7f); p.velocidad *= 0.5f; } // modo prueba
            MoverZona(p);
        }
        else if (p.tipo == Tipo.Llenar) ColocarZona(p.meta, p.ancho);
        Dibujar();
    }

    void MoverZona(Paso p)
    {
        ColocarZona(UnityEngine.Random.Range(0.15f + p.ancho / 2f, 0.9f - p.ancho / 2f), p.ancho);
    }

    void ColocarZona(float centro, float ancho)
    {
        zona.anchorMin = new Vector2(Mathf.Clamp01(centro - ancho / 2f), 0f);
        zona.anchorMax = new Vector2(Mathf.Clamp01(centro + ancho / 2f), 1f);
    }

    bool EnZona(float v)
    {
        return v >= zona.anchorMin.x && v <= zona.anchorMax.x;
    }

    void Update()
    {
        if (!activo) return;
        var k = Keyboard.current;
        float dt = Time.deltaTime;

        if (espera > 0f)
        {
            espera -= dt;
            if (espera <= 0f)
            {
                if (termino) Cerrar();
                else EmpezarPaso();
            }
            return;
        }
        if (k == null) return;

        var p = pasos[indice];
        if (p.tiempo > 0f)
        {
            t -= dt;
            if (t <= 0f) { Fallar("¡Se acabó el tiempo de este paso!"); return; }
        }

        switch (p.tipo)
        {
            case Tipo.Opcion:
                int elegida = -1;
                if (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame) elegida = 0;
                if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) elegida = 1;
                if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame) elegida = 2;
                if (elegida >= 0 && elegida < p.opciones.Length)
                {
                    if (elegida == p.correcta) Superar("¡Correcto!");
                    else Fallar("No es correcto. " + p.porQue);
                }
                break;

            case Tipo.Secuencia:
                int flecha = -1;
                if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) flecha = 0;
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) flecha = 1;
                if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) flecha = 2;
                if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) flecha = 3;
                if (flecha >= 0)
                {
                    if (flecha == secuencia[posSecuencia])
                    {
                        posSecuencia++;
                        if (posSecuencia < secuencia.Length) Sonidos.Tocar("tecla");
                        if (posSecuencia >= secuencia.Length) { Superar("¡Muy bien!"); return; }
                    }
                    else
                    {
                        fallos++;
                        posSecuencia = 0;
                        if (fallos <= p.fallosPermitidos) Sonidos.Tocar("error");
                        if (fallos > p.fallosPermitidos) { Fallar("Te equivocaste de flecha. Hay que hacerlo con calma y cuidado."); return; }
                        textoResultado.text = "<color=#ff8080>¡Flecha equivocada! Empieza de nuevo.</color>";
                    }
                }
                break;

            case Tipo.Tiempo:
                pos += dir * p.velocidad * dt;
                if (pos > 1f) { pos = 1f; dir = -1f; }
                if (pos < 0f) { pos = 0f; dir = 1f; }
                if (k.spaceKey.wasPressedThisFrame)
                {
                    if (EnZona(pos))
                    {
                        aciertos++;
                        if (aciertos >= p.aciertos) { Superar("¡Perfecto!"); return; }
                        textoResultado.text = "<color=#80ff9a>¡Bien! Otra vez.</color>";
                        Sonidos.Tocar("tecla");
                        MoverZona(p);
                    }
                    else
                    {
                        fallos++;
                        if (fallos > p.fallosPermitidos) { Fallar("Fallaste el momento justo."); return; }
                        Sonidos.Tocar("error");
                        textoResultado.text = "<color=#ff8080>¡Casi! Quedan " + (p.fallosPermitidos - fallos + 1) + " intento(s).</color>";
                    }
                }
                break;

            case Tipo.Frotar:
                int lado = 0;
                if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) lado = -1;
                if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) lado = 1;
                if (lado != 0 && lado != ultimoLado)
                {
                    progreso += 1f / p.largo;
                    if (Mathf.RoundToInt(progreso * p.largo) % 4 == 0) Sonidos.Tocar("tecla");
                    ultimoLado = lado;
                }
                progreso = Mathf.Max(0f, progreso - p.velocidad * 0.01f * dt); // el barro vuelve si se deja de frotar
                if (progreso >= 1f) { Superar("¡Quedó limpiecito!"); return; }
                break;

            case Tipo.Llenar:
                if (k.spaceKey.isPressed) { llenando = true; llenado += p.velocidad * dt; }
                if (llenado >= 1f) { Fallar("¡Se desbordó! Servir de más también le hace daño."); return; }
                if (llenando && k.spaceKey.wasReleasedThisFrame)
                {
                    if (EnZona(llenado)) Superar("¡Porción justa!");
                    else if (llenado < zona.anchorMin.x) Fallar("Quedó muy poquito. Necesita su porción completa.");
                    else Fallar("Le serviste demasiado. Comer de más le hace daño.");
                    return;
                }
                break;
        }
        if (espera <= 0f) Dibujar();
    }

    void Dibujar()
    {
        var p = pasos[indice];
        if (grupoReloj.activeSelf && p.tiempo > 0f)
            rellenoReloj.anchorMax = new Vector2(Mathf.Clamp01(t / (p.tiempo * FactorTiempo)), 1f);
        if (p.tipo == Tipo.Secuencia && secuencia != null)
        {
            string s = "";
            for (int i = 0; i < secuencia.Length; i++)
                s += (i < posSecuencia ? "<color=#5ee07a>" : i == posSecuencia ? "<color=#ffd84a>" : "<color=#ffffff>") + Flechas[secuencia[i]] + "</color>  ";
            textoCentro.text = s;
        }
        else if (p.tipo == Tipo.Tiempo) marcador.anchorMin = marcador.anchorMax = new Vector2(pos, 0.5f);
        else if (p.tipo == Tipo.Llenar)
        {
            marcador.anchorMin = marcador.anchorMax = new Vector2(Mathf.Clamp01(llenado), 0.5f);
            textoCentro.text = "";
        }
        else if (p.tipo == Tipo.Frotar)
        {
            rellenoProgreso.anchorMax = new Vector2(Mathf.Clamp01(progreso), 1f);
            textoCentro.text = (ultimoLado < 0 ? "<color=#ffd84a>←</color>      →" : ultimoLado > 0 ? "←      <color=#ffd84a>→</color>" : "←      →");
        }
    }

    void Superar(string texto)
    {
        Sonidos.Tocar("acierto");
        textoResultado.text = "<color=#80ff9a>" + texto + "</color>";
        if (pasos[indice].tipo == Tipo.Secuencia) posSecuencia = secuencia.Length;
        Dibujar();
        indice++;
        if (indice >= pasos.Count)
        {
            termino = true;
            exito = true;
            motivo = "";
            espera = 0.9f;
        }
        else espera = 0.7f;
    }

    void Fallar(string texto)
    {
        Sonidos.Tocar("error");
        textoResultado.text = "<color=#ff8080>" + texto + "</color>";
        termino = true;
        exito = false;
        motivo = texto;
        espera = 2.6f;
    }

    void Cerrar()
    {
        activo = false;
        panel.SetActive(false);
        var fin = alTerminar;
        alTerminar = null;
        if (fin != null) fin(exito, motivo);
    }

    // ---------------- Pasos de cada cuidado ----------------

    static Paso Opcion(string nombre, string correcta, string[] incorrectas, string porQue, float tiempo)
    {
        var lista = new List<string>(incorrectas);
        // dos distractores al azar
        while (lista.Count > 2) lista.RemoveAt(UnityEngine.Random.Range(0, lista.Count));
        int c = UnityEngine.Random.Range(0, 3);
        lista.Insert(c, correcta);
        return new Paso
        {
            tipo = Tipo.Opcion, nombre = nombre, opciones = lista.ToArray(), correcta = c, porQue = porQue, tiempo = tiempo,
            instruccion = "Elige la respuesta correcta con las teclas 1, 2 o 3."
        };
    }

    // d = dificultad del nivel (1 a 3)
    public static List<Paso> PasosCuidado(Necesidad n, Especie e, int d)
    {
        var l = new List<Paso>();
        bool gato = e == Especie.Gato;
        if (n == Necesidad.Herida)
        {
            l.Add(Opcion("Elegir con qué limpiar la herida", "Suero fisiológico", new[] { "Alcohol", "Jabón de loza", "Agua oxigenada pura" },
                "El alcohol y el jabón arden e irritan la herida. Se lava con suero fisiológico.", 13f - 2f * d));
            l.Add(new Paso
            {
                tipo = Tipo.Secuencia, nombre = "Limpiar la herida con suavidad", largo = 3 + d, tiempo = 8f - d,
                fallosPermitidos = d >= 3 ? 0 : 1, instruccion = "Repite las flechas en orden (← ↑ → ↓ o A W D S)."
            });
            l.Add(new Paso
            {
                tipo = Tipo.Tiempo, nombre = "Poner la venda", aciertos = d, velocidad = 0.6f + 0.3f * d, ancho = 0.26f - 0.05f * d,
                fallosPermitidos = d >= 3 ? 1 : 2, instruccion = "Pulsa ESPACIO cuando la marca esté sobre la zona verde."
            });
        }
        else if (n == Necesidad.Hambre)
        {
            if (gato)
                l.Add(Opcion("Elegir su comida", "Alimento para gato", new[] { "Leche de vaca", "Concentrado para perro", "Chocolate", "Atún con sal y cebolla" },
                    "Los gatos necesitan alimento para gato: tiene taurina. La leche, la comida de perro y el chocolate les hacen daño.", 12f - 2f * d));
            else
                l.Add(Opcion("Elegir su comida", "Concentrado para perro", new[] { "Chocolate", "Uvas", "Huesos cocinados", "Sobras con cebolla" },
                    "Chocolate, uvas, cebolla y huesos cocinados son peligrosos. Lo correcto es concentrado para perro.", 12f - 2f * d));
            l.Add(new Paso
            {
                tipo = Tipo.Llenar, nombre = "Servir la porción justa", velocidad = 0.3f + 0.12f * d, ancho = 0.22f - 0.04f * d,
                meta = UnityEngine.Random.Range(0.45f, 0.75f), instruccion = "Mantén ESPACIO para servir y suéltalo justo en la zona verde."
            });
            if (d >= 2)
                l.Add(new Paso
                {
                    tipo = Tipo.Secuencia, nombre = "Darle agua fresca", largo = 2 + d, tiempo = 7f - d, fallosPermitidos = 1,
                    instruccion = "Repite las flechas para llenar el plato de agua."
                });
        }
        else if (n == Necesidad.Sucia)
        {
            if (gato)
                l.Add(Opcion("Preparar el baño", "Poca agua tibia y champú para gatos", new[] { "Meterlo en agua fría", "Detergente de ropa", "Tu champú y agua caliente" },
                    "A los gatos se les baña con poca agua tibia y champú para gatos; el agua fría, el detergente o el champú de personas les hacen daño.", 13f - 2f * d));
            else
                l.Add(Opcion("Preparar el baño", "Champú para mascotas y agua tibia", new[] { "Tu champú", "Detergente de ropa", "Agua muy caliente" },
                    "El champú de personas y el detergente irritan su piel, y el agua muy caliente lo quema. Usa champú para mascotas y agua tibia.", 13f - 2f * d));
            l.Add(new Paso
            {
                tipo = Tipo.Frotar, nombre = "Frotar para quitar el barro", largo = 12 + 5 * d, tiempo = 9f, velocidad = 1f + d,
                instruccion = "Alterna ← y → (o A y D) lo más rápido que puedas antes de que se acabe el tiempo."
            });
            l.Add(new Paso
            {
                tipo = Tipo.Tiempo, nombre = "Secarlo muy bien", aciertos = Mathf.Max(1, d - 1), velocidad = 0.7f + 0.3f * d, ancho = 0.24f - 0.04f * d,
                fallosPermitidos = 1, instruccion = "Pulsa ESPACIO en la zona verde para secar orejas y patas."
            });
        }
        return l;
    }

    public static List<Paso> PasosAdopcion(string pregunta, string correcta, string[] incorrectas, string porQue)
    {
        var l = new List<Paso>();
        var op = Opcion("Pregunta de adopción responsable", correcta, incorrectas, porQue, 0f);
        op.instruccion = pregunta + "\n(Elige con 1, 2 o 3)";
        l.Add(op);
        l.Add(new Paso
        {
            tipo = Tipo.Tiempo, nombre = "Ponerle el collar con placa", aciertos = 1, velocidad = 1.1f, ancho = 0.18f,
            fallosPermitidos = 2, instruccion = "Pulsa ESPACIO cuando la marca esté en la zona verde."
        });
        return l;
    }
}
