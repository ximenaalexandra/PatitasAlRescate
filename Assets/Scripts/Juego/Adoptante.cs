using UnityEngine;

// Familia que quiere adoptar. Al terminar la conversación recibe una mascota con su collar.
public class Adoptante : Interactuable
{
    public string nombreFamilia = "Familia";
    public DialogueLine[] conversacion;   // se usa cuando hay collar y mascota disponible
    public DialogueLine[] sinCollar;      // si todavía no hay un collar para entregar
    public DialogueLine[] despues;        // cuando ya adoptó

    [Header("Pregunta de adopción responsable (minijuego)")]
    public string pregunta = "¿Qué necesita una mascota además de comida?";
    public string respuestaCorrecta = "Tiempo, juego y visitas al veterinario";
    public string[] respuestasIncorrectas = { "Nada más, se cuida sola", "Solo un patio grande" };
    public string explicacion = "Una mascota necesita compañía, juego y controles veterinarios.";

    public bool Adopto { get; private set; }

    public override string Accion { get { return Adopto ? "saludar a " + nombreFamilia : "hablar con " + nombreFamilia; } }

    public override void Interactuar(PlayerController jugadora)
    {
        var ui = UIJuego.Instancia;
        var nivel = LevelManager.Instancia;
        if (ui == null || nivel == null) return;

        if (Adopto) { ui.MostrarDialogo(despues, null); return; }
        if (!nivel.PuedeAdoptar) { ui.MostrarDialogo(sinCollar, null); return; }

        ui.MostrarDialogo(conversacion, () =>
        {
            if (Adopto) return;
            if (MiniJuego.Instancia == null) { if (nivel.EntregarEnAdopcion(this)) Adopto = true; return; }
            MiniJuego.Instancia.Iniciar("Adopción con " + nombreFamilia,
                MiniJuego.PasosAdopcion(pregunta, respuestaCorrecta, respuestasIncorrectas, explicacion), (ok, motivo) =>
                {
                    if (ok) { if (!Adopto && nivel.EntregarEnAdopcion(this)) Adopto = true; }
                    else nivel.Mensaje("Vuelve a hablar con " + nombreFamilia + " e inténtalo otra vez.", 2.5f);
                });
        });
    }
}
