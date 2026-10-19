using UnityEngine;

// Guarda qué rescatista eligió el jugador en el menú.
public static class SeleccionPersonaje
{
    public static readonly string[] Nombres = { "Sofi", "Luna", "Mía" };
    public static readonly string[] Descripciones =
    {
        "Equilibrada: buena en todo.",
        "Veloz: camina y corre más rápido.",
        "Enérgica: recupera el poder más rápido."
    };

    public static int Indice
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt("personaje", 0), 0, Nombres.Length - 1); }
        set { PlayerPrefs.SetInt("personaje", value); PlayerPrefs.Save(); }
    }

    public static string Nombre { get { return Nombres[Indice]; } }
}
