using UnityEngine;

// (Versión anterior) Estación de cuidado. Desde la versión 3 cada mascota se atiende de forma individual,
// así que este componente solo muestra un recordatorio si se usa en una escena.
public class EstacionCuidado : Interactuable
{
    public string nombreEstacion = "refugio";
    public string verbo = "leer el recordatorio";

    public override string Accion { get { return verbo; } }

    public override void Interactuar(PlayerController jugadora)
    {
        if (LevelManager.Instancia != null)
            LevelManager.Instancia.Mensaje("Atiende a cada mascota: acércate a ella y pulsa E.", 2f);
    }
}
