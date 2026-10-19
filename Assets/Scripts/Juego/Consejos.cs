// Consejos que aparecen en una nube cada vez que la rescatista cura, alimenta, baña o entrega en adopción
// a una mascota: por qué es importante ese cuidado y por qué debemos respetar y querer a los animales.
public static class Consejos
{
    static readonly string[] Curar =
    {
        "Una herida sin tratar puede infectarse. Lavarla con suero fisiológico y llevarla al veterinario evita que empeore.",
        "Los animales sienten dolor igual que nosotros. Curarlos a tiempo les ahorra sufrimiento y puede salvar su vida.",
        "Nunca uses alcohol ni medicamentos para personas: pueden ser tóxicos para perros y gatos.",
        "Un animal herido puede asustarse y morder. Acércate despacio, háblale con calma y cúbrelo con una toalla.",
    };

    static readonly string[] AlimentarPerro =
    {
        "Una buena alimentación les da energía y defensas contra las enfermedades. Dale concentrado según su edad y tamaño.",
        "El chocolate, las uvas, la cebolla y los huesos cocinados son peligrosos para los perros. ¡Nunca se los des!",
        "El agua fresca y limpia es tan importante como la comida: cámbiala todos los días.",
    };

    static readonly string[] AlimentarGato =
    {
        "Los gatos necesitan alimento para gatos: tiene taurina, un nutriente que su corazón y sus ojos necesitan.",
        "La leche de vaca le cae mal a muchos gatos. Lo mejor es agua fresca y limpia todos los días.",
        "Un gato que deja de comer puede enfermarse rápido. Si no come en un día, llévalo al veterinario.",
    };

    static readonly string[] BanarPerro =
    {
        "La higiene evita pulgas, garrapatas y enfermedades de la piel. Báñalo con champú para mascotas, nunca con el tuyo.",
        "Sécalo muy bien, sobre todo las orejas: la humedad puede causarle infecciones.",
        "Cepillarlo cada semana mantiene su pelo sano y te ayuda a revisar que no tenga heridas ni parásitos.",
    };

    static readonly string[] BanarGato =
    {
        "Los gatos se asean solos, pero cuando están muy sucios necesitan ayuda. Usa poca agua tibia y champú para gatos.",
        "Cepillar a tu gato evita las bolas de pelo y te ayuda a revisar que no tenga pulgas ni heridas.",
        "La suciedad y las moscas atraen infecciones. Mantener limpia su cama y su arenero también lo protege.",
    };

    static readonly string[] Adoptar =
    {
        "Adoptar salva dos vidas: la del animal que llega a tu casa y la del que ahora puede ocupar su lugar en el refugio.",
        "Una mascota vive de 10 a 20 años. Adoptar es un compromiso para toda su vida: es parte de la familia, no un juguete.",
        "Esterilizarla y ponerle una placa con tu teléfono evita camadas abandonadas y ayuda a que vuelva a casa si se pierde.",
    };

    static readonly string[] Respeto =
    {
        "Respétalos: sienten miedo, dolor y alegría igual que nosotros.",
        "Un animal tratado con amor confía, juega y vive más sano.",
        "En Colombia la Ley 1774 de 2016 reconoce a los animales como seres que sienten. Maltratarlos es un delito.",
        "Quererlos es darles tiempo, juego, paciencia y un hogar para toda la vida.",
        "Nunca los golpees ni les grites. Si ves maltrato, avísale a un adulto o a las autoridades.",
    };

    static int contador;

    public static string Titulo(Necesidad n, bool adopcion)
    {
        if (adopcion) return "¿Por qué es importante adoptar?";
        switch (n)
        {
            case Necesidad.Herida: return "¿Por qué es importante curarlos?";
            case Necesidad.Hambre: return "¿Por qué es importante alimentarlos bien?";
            case Necesidad.Sucia: return "¿Por qué es importante bañarlos?";
        }
        return "¿Por qué es importante cuidarlos?";
    }

    // Devuelve el consejo principal y una frase sobre respeto y amor (van rotando).
    public static string Texto(Necesidad n, Especie e, bool adopcion)
    {
        string[] lista = Curar;
        if (adopcion) lista = Adoptar;
        else if (n == Necesidad.Hambre) lista = e == Especie.Gato ? AlimentarGato : AlimentarPerro;
        else if (n == Necesidad.Sucia) lista = e == Especie.Gato ? BanarGato : BanarPerro;
        string principal = lista[contador % lista.Length];
        string respeto = Respeto[contador % Respeto.Length];
        contador++;
        return principal + "\n\n" + respeto;
    }
}
