using UnityEngine;

// Efectos de sonido y música de fondo. Desde cualquier script: Sonidos.Tocar("salto");
public class Sonidos : MonoBehaviour
{
    public static Sonidos Instancia;

    [System.Serializable]
    public struct Efecto { public string nombre; public AudioClip clip; [Range(0f, 1f)] public float volumen; }

    public Efecto[] efectos;
    public AudioClip musica;
    [Range(0f, 1f)] public float volumenMusica = 0.22f;

    AudioSource fuenteEfectos;
    AudioSource fuenteMusica;

    void Awake()
    {
        Instancia = this;
        fuenteEfectos = gameObject.AddComponent<AudioSource>();
        fuenteEfectos.playOnAwake = false;
        if (musica != null)
        {
            fuenteMusica = gameObject.AddComponent<AudioSource>();
            fuenteMusica.clip = musica;
            fuenteMusica.loop = true;
            fuenteMusica.volume = volumenMusica;
            fuenteMusica.Play();
        }
    }

    public static void Tocar(string nombre)
    {
        if (Instancia == null || Instancia.efectos == null) return;
        foreach (var e in Instancia.efectos)
            if (e.nombre == nombre && e.clip != null)
            {
                Instancia.fuenteEfectos.PlayOneShot(e.clip, e.volumen > 0f ? e.volumen : 1f);
                return;
            }
    }

    // Baja la música un momento (por ejemplo, para que se escuche la fanfarria de victoria)
    public static void PausarMusica(bool pausar)
    {
        if (Instancia == null || Instancia.fuenteMusica == null) return;
        if (pausar) Instancia.fuenteMusica.Pause(); else Instancia.fuenteMusica.UnPause();
    }
}
