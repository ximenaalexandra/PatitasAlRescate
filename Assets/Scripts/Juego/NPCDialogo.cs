using UnityEngine;

// NPC con diálogo interactivo (tecla E). Las líneas se editan desde el Inspector.
public class NPCDialogo : Interactuable
{
    public string nombreNPC = "Dra. Lucía";
    public bool esGuia = true; // la guía explica la misión del nivel
    public DialogueLine[] conversacion;

    public override string Accion { get { return "hablar con " + nombreNPC; } }

    public override void Interactuar(PlayerController jugadora)
    {
        if (UIJuego.Instancia == null) return;
        UIJuego.Instancia.MostrarDialogo(conversacion, () =>
        {
            if (esGuia && LevelManager.Instancia != null) LevelManager.Instancia.HabloConGuia();
        });
    }
}
