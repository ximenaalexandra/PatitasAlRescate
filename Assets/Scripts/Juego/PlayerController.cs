using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Control de la rescatista: caminar, correr, saltar, interactuar (E) y mirar el mapa en el celular (M).
// Tiene 3 corazones de vida y una barra de poder (energía).
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instancia;

    [Header("Personajes elegibles (Sofi, Luna, Mía)")]
    public RuntimeAnimatorController[] personajes;

    [Header("Movimiento")]
    public float velocidad = 3f;
    public float multiplicadorCorrer = 1.8f;
    public float fuerzaSalto = 9f;
    public float limiteX = 14.8f;

    [Header("Vidas y poder")]
    public int vidasMax = 3;
    public float poderMax = 100f;
    public float regeneracionPoder = 10f;
    public float gastoCorrer = 20f;
    public float costoSalto = 10f;
    public float costoRescate = 15f;

    public int Vidas { get; private set; }
    public float Poder { get; private set; }
    public bool UsandoCelular { get; private set; }
    public bool Derrotada { get { return Vidas <= 0; } }
    public Interactuable Objetivo { get; private set; }
    public bool Corriendo { get; private set; }   // corre o salta: asusta a las mascotas miedosas
    public bool MiraDerecha { get { return !sr.flipX; } }

    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;
    LevelManager nivel;
    readonly HashSet<Interactuable> cercanos = new HashSet<Interactuable>();
    float tiempoAccion;
    bool mapaFijo; // Tab deja el mapa abierto
    float invulnerable;
    string estadoActual = "";

    void Awake()
    {
        Instancia = this;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        AplicarPersonaje();
        Vidas = vidasMax;
        Poder = poderMax;
    }

    void AplicarPersonaje()
    {
        int i = SeleccionPersonaje.Indice;
        if (personajes != null && i < personajes.Length && personajes[i] != null)
            anim.runtimeAnimatorController = personajes[i];
        if (i == 1) velocidad *= 1.2f;            // Luna: veloz
        if (i == 2) regeneracionPoder *= 1.5f;    // Mía: enérgica
    }

    void Start()
    {
        nivel = LevelManager.Instancia;
        ActualizarHUD();
    }

    bool EnSuelo
    {
        get { return transform.position.y < 0.1f && Mathf.Abs(rb.linearVelocity.y) < 0.05f; }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        var k = Keyboard.current;

        if (invulnerable > 0f)
        {
            invulnerable -= dt;
            sr.enabled = invulnerable <= 0f || Mathf.FloorToInt(invulnerable * 12f) % 2 == 0;
        }
        else sr.enabled = true;

        UsandoCelular = false;
        Objetivo = null;

        if (Derrotada) { Frenar(); Reproducir("Player_Derrota"); return; }
        if (nivel != null && !nivel.Jugando) { Frenar(); Reproducir("Player_Idle"); return; }

        // Durante un minijuego de cuidado la rescatista se queda agachada atendiendo a la mascota
        if (MiniJuego.Activo)
        {
            Frenar();
            Corriendo = false;
            Reproducir("Player_Rescatar");
            return;
        }

        // Con un diálogo o cartel abierto la jugadora se queda quieta y E / Espacio avanzan el texto
        if (UIJuego.PanelAbierto)
        {
            Frenar();
            Reproducir("Player_Idle");
            if (k != null && (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame))
            {
                Sonidos.Tocar("dialogo");
                UIJuego.Instancia.Avanzar();
            }
            return;
        }

        if (tiempoAccion > 0f)
        {
            tiempoAccion -= dt;
            Frenar();
            Reproducir("Player_Rescatar");
            RecuperarPoder(dt);
            ActualizarHUD();
            return;
        }

        Objetivo = BuscarObjetivo();

        float x = 0f;
        bool correr = false, saltar = false, interactuar = false;
        if (k != null)
        {
            if (k.leftArrowKey.isPressed || k.aKey.isPressed) x -= 1f;
            if (k.rightArrowKey.isPressed || k.dKey.isPressed) x += 1f;
            correr = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
            saltar = k.spaceKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame;
            interactuar = k.eKey.wasPressedThisFrame;
            if (k.tabKey.wasPressedThisFrame) mapaFijo = !mapaFijo;
            UsandoCelular = k.mKey.isPressed || mapaFijo;
        }

        correr = correr && x != 0f && !UsandoCelular && Poder > 1f;
        Corriendo = correr || !EnSuelo;
        if (correr) Poder -= gastoCorrer * dt;
        else RecuperarPoder(dt);
        Poder = Mathf.Clamp(Poder, 0f, poderMax);

        // Tecla E: solo actúa si hay algo dentro del área de interacción (si no, no pasa nada)
        if (interactuar && EnSuelo && Objetivo != null)
        {
            Objetivo.Interactuar(this);
            if (UIJuego.PanelAbierto) { Frenar(); return; }
        }

        if (saltar && EnSuelo && !UsandoCelular)
        {
            if (Poder >= costoSalto)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
                Poder -= costoSalto;
                Sonidos.Tocar("salto");
            }
            else if (nivel != null) nivel.Mensaje("Sin poder para saltar. Descansa un momento.", 1.5f);
        }

        float v = velocidad * (correr ? multiplicadorCorrer : 1f) * (UsandoCelular ? 0.6f : 1f);
        rb.linearVelocity = new Vector2(x * v, rb.linearVelocity.y);

        if (Mathf.Abs(transform.position.x) > limiteX)
        {
            transform.position = new Vector3(Mathf.Sign(transform.position.x) * limiteX, transform.position.y, 0f);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        if (x != 0f) sr.flipX = x < 0f; // el sprite original mira a la derecha

        if (!EnSuelo) Reproducir("Player_Saltar");
        else if (UsandoCelular) Reproducir(x != 0f ? "Player_Celular" : "Player_CelularQuieta");
        else if (x != 0f) Reproducir(correr ? "Player_Run" : "Player_Walk");
        else Reproducir("Player_Idle");

        ActualizarHUD();
    }

    Interactuable BuscarObjetivo()
    {
        Interactuable mejor = null;
        float mejorDist = float.MaxValue;
        cercanos.RemoveWhere(c => c == null);
        foreach (var c in cercanos)
        {
            if (!c.isActiveAndEnabled || !c.PuedeInteractuar) continue;
            float d = Mathf.Abs(c.transform.position.x - transform.position.x);
            if (d < mejorDist) { mejorDist = d; mejor = c; }
        }
        return mejor;
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        var i = otro.GetComponent<Interactuable>();
        if (i != null) cercanos.Add(i);
    }

    void OnTriggerExit2D(Collider2D otro)
    {
        var i = otro.GetComponent<Interactuable>();
        if (i != null) cercanos.Remove(i);
    }

    void OnTriggerStay2D(Collider2D otro)
    {
        var obstaculo = otro.GetComponent<Obstaculo>();
        if (obstaculo != null) RecibirDanio(obstaculo.danio);
    }

    void RecuperarPoder(float dt)
    {
        Poder = Mathf.Min(poderMax, Poder + regeneracionPoder * dt);
    }

    void Frenar()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    // Animación de agacharse (rescatar, curar, alimentar, bañar, entregar)
    public void HacerAccion(float segundos)
    {
        tiempoAccion = segundos;
    }

    // Curar, alimentar o bañar a una mascota: gasta poder y un objeto del inventario.
    public void IntentarCuidar(Mascota mascota)
    {
        if (nivel == null) return;
        if (Poder < costoRescate)
        {
            nivel.Mensaje("Te falta poder para ayudarla. Descansa un momento.", 1.5f);
            return;
        }
        if (!nivel.CuidarMascota(mascota, transform)) return;
        Poder -= costoRescate;
    }

    public void RecibirDanio(int danio)
    {
        if (invulnerable > 0f || Derrotada) return;
        if (nivel != null && (!nivel.Jugando || UIJuego.PanelAbierto || MiniJuego.Activo || nivel.sinDanio)) return;
        Vidas = Mathf.Max(0, Vidas - danio);
        invulnerable = 1.5f;
        Sonidos.Tocar("dano");
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 4f);
        ActualizarHUD();
        if (Derrotada && nivel != null) nivel.PerderNivel("¡Perdiste tus tres corazones!");
        else if (nivel != null) nivel.Mensaje("¡Auch! Perdiste un corazón. Salta los obstáculos con Espacio.", 1.8f);
    }

    void ActualizarHUD()
    {
        var ui = UIJuego.Instancia;
        if (ui == null) return;
        ui.ActualizarVidas(Vidas);
        ui.ActualizarPoder(Poder / poderMax);
    }

    void Reproducir(string estado)
    {
        if (estado == estadoActual || anim == null) return;
        estadoActual = estado;
        anim.Play(estado, 0, 0f);
    }
}
