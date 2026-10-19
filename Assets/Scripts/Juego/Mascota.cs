using UnityEngine;

public enum Especie { Perro, Gato }
public enum Necesidad { Ninguna, Herida, Hambre, Sucia }

// Mascota (perro o gato) en una situación delicada: herida, con hambre o sucia.
// La rescatista la atiende con E usando un objeto (botiquín, alimento o jabón) y después la mascota la sigue.
[RequireComponent(typeof(SpriteRenderer))]
public class Mascota : Interactuable
{
    [Header("Identidad")]
    public string nombreMascota = "Toby";
    public Especie especie = Especie.Perro;
    public Necesidad necesidad = Necesidad.Ninguna;
    public bool spriteMiraIzquierda = true;   // los perros del pack miran a la izquierda, los gatos a la derecha
    [Tooltip("Miedosa: huye si la rescatista corre o salta cerca. Hay que acercarse caminando.")]
    public bool asustadiza = false;

    [Header("Animaciones")]
    public Sprite[] framesEspera;   // con hambre o sucia: pide ayuda (ladra / maúlla / se lame)
    public Sprite[] framesHerida;   // acostada, sin fuerzas
    public Sprite[] framesCamina;
    public Sprite[] framesFeliz;
    public float fps = 8f;
    public float velocidad = 3.5f;

    [Header("Burbujas")]
    public GameObject alerta;       // burbuja sobre la mascota
    public float alturaAlerta = 1.45f;
    public Sprite iconoHerida, iconoHambre, iconoSucia;
    public Sprite spriteCorazon;
    public GameObject moscas;       // moscas que vuelan alrededor cuando está sucia
    public Sprite[] framesMoscas;
    public MarcadorMapa marcador;

    public bool Rescatada { get; private set; }
    public bool Cuidada { get; private set; }

    SpriteRenderer sr;
    Transform objetivo;
    SpriteRenderer srObjetivo;
    int lugarEnFila;
    float tiempo;
    float tiempoCorazon;
    float huyendo, destinoHuida, enfriamiento;

    public override string Accion
    {
        get
        {
            switch (necesidad)
            {
                case Necesidad.Herida: return "curar las heridas de " + nombreMascota;
                case Necesidad.Hambre: return "darle de comer a " + nombreMascota;
                case Necesidad.Sucia: return "bañar a " + nombreMascota;
            }
            return "acariciar a " + nombreMascota;
        }
    }

    public override bool PuedeInteractuar { get { return !Cuidada && necesidad != Necesidad.Ninguna; } }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        tiempo = Random.value * 2f;
    }

    void Start()
    {
        AplicarEstado();
    }

    // Muestra la situación de la mascota: herida, hambrienta o sucia.
    public void AplicarEstado()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        bool sucia = necesidad == Necesidad.Sucia && !Cuidada;
        if (sucia) sr.color = new Color(0.68f, 0.56f, 0.44f);
        else if (necesidad == Necesidad.Herida && !Cuidada) sr.color = new Color(1f, 0.85f, 0.85f);
        else sr.color = Color.white;
        if (moscas != null) moscas.SetActive(sucia);
        // si necesita ayuda se dibuja delante de la rescatista para que siempre se vea
        sr.sortingOrder = Cuidada || Rescatada ? 1 : 4;
        if (alerta == null) return;
        Sprite icono = null;
        if (!Cuidada)
        {
            if (necesidad == Necesidad.Herida) icono = iconoHerida;
            else if (necesidad == Necesidad.Hambre) icono = iconoHambre;
            else if (necesidad == Necesidad.Sucia) icono = iconoSucia;
        }
        if (icono != null)
        {
            alerta.GetComponent<SpriteRenderer>().sprite = icono;
            alerta.SetActive(true);
        }
        else if (tiempoCorazon <= 0f) alerta.SetActive(false);
    }

    public override void Interactuar(PlayerController jugadora)
    {
        jugadora.IntentarCuidar(this);
    }

    // Se usa en la feria de adopción: la mascota ya está sana y sigue a la rescatista.
    public void Rescatar(Transform quien, int lugar)
    {
        Rescatada = true;
        Seguir(quien, lugar);
        AplicarEstado();
    }

    public void Seguir(Transform quien, int lugar)
    {
        objetivo = quien;
        srObjetivo = quien.GetComponent<SpriteRenderer>();
        lugarEnFila = lugar;
    }

    // Curada, alimentada o bañada: queda feliz y sigue a quien la cuidó.
    public void Cuidar(Transform quien, int lugar)
    {
        Cuidada = true;
        Rescatada = true;
        if (quien != null) Seguir(quien, lugar);
        AplicarEstado();
        MostrarCorazon(3f);
    }

    public void MostrarCorazon(float segundos)
    {
        if (alerta == null) return;
        if (spriteCorazon != null) alerta.GetComponent<SpriteRenderer>().sprite = spriteCorazon;
        alerta.SetActive(true);
        tiempoCorazon = segundos;
    }

    void Update()
    {
        tiempo += Time.deltaTime;
        Sprite[] frames = framesEspera;
        float vel = fps;

        if (!Cuidada && necesidad == Necesidad.Herida && framesHerida != null && framesHerida.Length > 0)
        {
            frames = framesHerida;
            vel = 2f; // respira despacio, sin fuerzas
        }

        // Mascota miedosa: si la rescatista corre o salta cerca, se asusta y se aleja
        if (asustadiza && !Cuidada && !Rescatada && necesidad != Necesidad.Herida)
        {
            enfriamiento -= Time.deltaTime;
            var j = PlayerController.Instancia;
            if (huyendo <= 0f && enfriamiento <= 0f && j != null && j.Corriendo && !MiniJuego.Activo
                && Mathf.Abs(j.transform.position.x - transform.position.x) < 3.2f)
            {
                float lado = Mathf.Sign(transform.position.x - j.transform.position.x);
                if (lado == 0f) lado = 1f;
                destinoHuida = Mathf.Clamp(transform.position.x + lado * 3.5f, -13.6f, 13.6f);
                if (Mathf.Abs(destinoHuida - transform.position.x) < 1.5f)  // contra la reja: escapa por el otro lado
                    destinoHuida = Mathf.Clamp(j.transform.position.x - lado * 3f, -13.6f, 13.6f);
                huyendo = 1.6f;
                Sonidos.Tocar(especie == Especie.Gato ? "maullido" : "ladrido");
                enfriamiento = 2.5f;
                if (LevelManager.Instancia != null)
                    LevelManager.Instancia.Mensaje("¡" + nombreMascota + " se asustó y huyó! Acércate caminando, sin correr ni saltar.", 2.6f);
            }
            if (huyendo > 0f)
            {
                huyendo -= Time.deltaTime;
                float dxh = destinoHuida - transform.position.x;
                if (Mathf.Abs(dxh) > 0.1f)
                {
                    transform.position += new Vector3(Mathf.Clamp(dxh, -6f * Time.deltaTime, 6f * Time.deltaTime), 0f, 0f);
                    sr.flipX = spriteMiraIzquierda ? dxh > 0f : dxh < 0f;
                    frames = framesCamina;
                    vel = fps * 1.5f;
                }
                else huyendo = 0f;
            }
        }

        if (Rescatada && objetivo != null)
        {
            // las personas miran a la derecha cuando flipX = false; la mascota se ubica a su espalda
            bool miraDerecha = srObjetivo == null || !srObjetivo.flipX;
            float detras = miraDerecha ? -1f : 1f;
            float destino = Mathf.Clamp(objetivo.position.x + detras * (1.1f + 0.9f * lugarEnFila), -14.3f, 14.3f);
            float dx = destino - transform.position.x;
            if (Mathf.Abs(dx) > 0.15f)
            {
                float paso = Mathf.Max(velocidad, Mathf.Abs(dx) * 2.5f) * Time.deltaTime;
                transform.position += new Vector3(Mathf.Clamp(dx, -paso, paso), 0f, 0f);
                sr.flipX = spriteMiraIzquierda ? dx > 0f : dx < 0f;
                frames = framesCamina;
                vel = fps;
            }
            else frames = framesFeliz;
        }

        if (alerta != null && alerta.activeSelf)
        {
            alerta.transform.localPosition = new Vector3(0f, alturaAlerta + Mathf.Sin(Time.time * 4f) * 0.08f, 0f);
            if (tiempoCorazon > 0f)
            {
                tiempoCorazon -= Time.deltaTime;
                if (tiempoCorazon <= 0f) { tiempoCorazon = 0f; AplicarEstado(); }
            }
        }

        if (moscas != null && moscas.activeSelf && framesMoscas != null && framesMoscas.Length > 0)
            moscas.GetComponent<SpriteRenderer>().sprite = framesMoscas[(int)(Time.time * 6f) % framesMoscas.Length];

        if (marcador != null)
        {
            if (Cuidada || Rescatada) marcador.color = new Color(0.35f, 0.9f, 0.45f);
            else marcador.color = new Color(1f, 0.25f, 0.3f, 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 4f)));
        }

        if (frames != null && frames.Length > 0)
            sr.sprite = frames[(int)(tiempo * vel) % frames.Length];
    }
}
