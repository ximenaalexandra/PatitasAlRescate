using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Construye el juego completo del Previo 2: menú con selección de rescatista, 4 niveles,
// NPC con diálogos, carteles con consejos, coleccionables, estaciones de cuidado y adopción.
public static class ConstruirJuego
{
    const string GirlTex = "Assets/Sprites/Player_Rescatista.png";
    const string AnimDir = "Assets/Animations/Player";
    const string Base = "Assets/Scenes/SampleScene.unity";
    static readonly int[] GrillaChica = { 8, 8, 8, 8, 8, 5, 7, 5, 5, 5, 5 };
    static Font fuente;

    static readonly Color CJugadora = new Color(1f, 0.85f, 0.4f);
    static readonly Color CGuia = new Color(0.55f, 0.85f, 1f);
    static readonly Color CFamilia = new Color(1f, 0.6f, 0.75f);

    class Skin
    {
        public Especie especie; public bool miraIzq; public float alturaAlerta; public string[] nombres;
        public Sprite[] hambre, sucia, herida, camina, feliz, quieta;
    }
    class Pet { public float x; public Especie especie; public int skin; public string nombre; public bool miedo; }
    static Pet Miedosa(Pet p) { p.miedo = true; return p; }
    static Pet P(float x, int skin, string nombre) { return new Pet { x = x, especie = Especie.Perro, skin = skin, nombre = nombre }; }
    static Pet G(float x, int skin, string nombre) { return new Pet { x = x, especie = Especie.Gato, skin = skin, nombre = nombre }; }

    class Cartel { public float x; public string titulo, texto; }
    class Familia { public float x; public string nombre, sprite; public DialogueLine[] conv; public string pregunta, correcta, porQue; public string[] incorrectas; }

    class Nivel
    {
        public int numero; public string nombre, fondo, siguiente; public bool ultimo;
        public TipoNivel tipo; public float tiempo, regen, luz;
        public Pet[] mascotas;
        public (string tipo, float x)[] obstaculos;
        public (string tipo, float x, float vel)[] moviles;
        public string item, nombreItems, nombreItem; public (float x, float y)[] items;
        public Cartel[] carteles;
        public Familia[] familias = new Familia[0];
        public DialogueLine[] guia;
        public string consejo;
    }

    static DialogueLine L(string quien, Color c, string texto) { return new DialogueLine(quien, c, texto); }
    static DialogueLine Dra(string t) { return L("Dra. Lucía", CGuia, t); }
    static DialogueLine Yo(string t) { return L("{jugadora} (tú)", CJugadora, t); }

    static bool modoPrueba;

    [MenuItem("Taller/Construir juego completo (menú y 4 niveles)")]
    public static void Construir()
    {
        modoPrueba = false;
        ConstruirTodo();
    }

    // Igual que el anterior, pero sin límite de tiempo: solo para probar con calma.
    [MenuItem("Taller/Construir en modo prueba (sin tiempo)")]
    public static void ConstruirPrueba()
    {
        modoPrueba = true;
        ConstruirTodo();
    }

    static void ConstruirTodo()
    {
        EditorSceneManager.SaveOpenScenes();
        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        ImportarArte();
        var ctrls = CrearControladores();
        var skins = CargarSkins();
        var gatos = CargarGatos();
        var prefabs = CrearPrefabs(skins[0]);

        foreach (var n in Niveles()) CrearNivel(n, ctrls, prefabs, skins, gatos);
        CrearMenu(ctrls, prefabs, skins, gatos);

        EditorBuildSettings.scenes = new[] { "Menu", "Nivel1", "Nivel2", "Nivel3", "Nivel4" }
            .Select(s => new EditorBuildSettingsScene("Assets/Scenes/" + s + ".unity", true)).ToArray();
        PlayerSettings.productName = "Patitas al Rescate";
        PlayerSettings.companyName = "UTS B193";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.resizableWindow = true;
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity", OpenSceneMode.Single);
        Debug.Log("JUEGO_OK");
    }

    [MenuItem("Taller/Generar build de Windows")]
    public static void GenerarBuild()
    {
        string carpeta = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Build", "PatitasAlRescate"));
        var opciones = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = System.IO.Path.Combine(carpeta, "PatitasAlRescate.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        var reporte = BuildPipeline.BuildPlayer(opciones);
        Debug.Log("BUILD_" + reporte.summary.result + " " + reporte.summary.totalSize + " bytes, errores: " + reporte.summary.totalErrors);
    }

    // ---------------- Contenido de los niveles ----------------

    static Nivel[] Niveles()
    {
        var n1 = new Nivel
        {
            numero = 1, nombre = "Curar heridas", tipo = TipoNivel.Curar, fondo = "Fondo_Barrio", siguiente = "Nivel2",
            tiempo = 180, regen = 12, luz = 1f,
            mascotas = new[] { P(-3f, 0, "Toby"), G(2.6f, 0, "Michi"), P(7f, 1, "Canela"), G(11.2f, 1, "Sombra") },
            obstaculos = new[] { ("Charco", -8.2f), ("Cono", 0.5f), ("Basura", 5f) },
            moviles = new (string, float, float)[0],
            item = "Botiquin", nombreItems = "Botiquines", nombreItem = "botiquín",
            items = new[] { (-4.6f, 0.35f), (-1.3f, 2.2f), (2f, 2.2f), (5f, 2.2f), (8.3f, 0.35f) },
            carteles = new[]
            {
                new Cartel { x = -6.2f, titulo = "Si encuentras una mascota herida",
                    texto = "1. Acércate despacio y háblale con calma.\n2. No la obligues: un animal con dolor puede morder o arañar.\n3. Cúbrela con una toalla para moverla.\n4. Llévala al veterinario o llama a un refugio." },
                new Cartel { x = 9.3f, titulo = "Botiquín básico para mascotas",
                    texto = "• Suero fisiológico para lavar heridas\n• Gasas y vendas limpias\n• Guantes desechables\n• El teléfono de tu veterinario\n\nNunca le des medicamentos para personas: pueden ser tóxicos." },
            },
            guia = new[]
            {
                Dra("¡Hola, {jugadora}! Soy la Dra. Lucía, veterinaria del refugio Patitas."),
                Yo("¡Hola, doctora! Vine a ayudar. ¿Qué está pasando en el barrio?"),
                Dra("Hay 2 perritos y 2 gatitos heridos en la calle. Están acostados, sin fuerzas y tienen una cruz roja encima."),
                Dra("Recoge botiquines (algunos están en lo alto: salta con Espacio). Con un botiquín, acércate a cada mascota y pulsa E."),
                Yo("¿Y cómo las curo?"),
                Dra("En tres pasos: elige con qué limpiar la herida (1, 2 o 3), límpiala repitiendo las flechas y pon la venda con Espacio en el momento justo."),
                Dra("Si te equivocas pierdes tiempo, así que hazlo con calma. Cada vez que cures a una te contaré por qué es tan importante."),
            },
            consejo = "Consejo: lleva a tu mascota al veterinario al menos una vez al año y mantén sus vacunas al día.",
        };
        var n2 = new Nivel
        {
            numero = 2, nombre = "Alimentar", tipo = TipoNivel.Alimentar, fondo = "Fondo_Atardecer", siguiente = "Nivel3",
            tiempo = 165, regen = 10, luz = 0.95f,
            mascotas = new[] { Miedosa(G(-4f, 2, "Pelusa")), P(-0.6f, 2, "Rocky"), G(3f, 3, "Nina"), Miedosa(P(8.7f, 3, "Lola")), G(11.6f, 4, "Kiwi") },
            obstaculos = new[] { ("Charco", -8f), ("Cono", -2.2f), ("Basura", 1.3f), ("Charco", 5f), ("Cono", 10f) },
            moviles = new[] { ("Pelota", 14f, 3f) },
            item = "Croquetas", nombreItems = "Alimento", nombreItem = "paquete de alimento",
            items = new[] { (-5.6f, 0.35f), (-3f, 2.2f), (2.1f, 2.2f), (4.2f, 0.35f), (7.6f, 0.35f), (12.6f, 2.2f) },
            carteles = new[]
            {
                new Cartel { x = -6.4f, titulo = "Alimentación saludable",
                    texto = "• Perros: concentrado para perro según su edad y tamaño.\n• Gatos: alimento para gato (necesitan taurina).\n• Respeta las porciones y los horarios.\n• Siempre deben tener agua fresca y limpia." },
                new Cartel { x = 6.4f, titulo = "Alimentos prohibidos",
                    texto = "Estos alimentos son tóxicos para perros y gatos:\n• Chocolate y café\n• Uvas y uvas pasas\n• Cebolla y ajo\n• Huesos cocinados (se astillan)\n• Dulces con xilitol" },
            },
            guia = new[]
            {
                Dra("¡Buen trabajo con los heridos, {jugadora}! Ahora hay 2 perritos y 3 gatitos con mucha hambre."),
                Yo("¡Pobrecitos! Los gatos están maullando y los perritos están pidiendo comida."),
                Dra("Recoge los paquetes de alimento. Con E eliges su comida, sirves la porción justa con Espacio y le das agua fresca."),
                Dra("Recuerda: los gatos necesitan alimento para gatos y los perros alimento para perros. ¡Nunca chocolate ni uvas!"),
                Dra("Pelusa y Lola son miedosas: si corres o saltas cerca de ellas, huyen. Acércate caminando."),
                Dra("¡Ojo con la pelota que rueda por la calle! Sáltala con Espacio."),
            },
            consejo = "Consejo: un perro adulto come 2 veces al día y un gato adulto varias porciones pequeñas. Pregúntale a tu veterinario la porción ideal.",
        };
        var n3 = new Nivel
        {
            numero = 3, nombre = "Bañar y limpiar", tipo = TipoNivel.Banar, fondo = "Fondo_Noche", siguiente = "Nivel4",
            tiempo = 150, regen = 8, luz = 0.85f,
            mascotas = new[] { P(-4.4f, 4, "Max"), Miedosa(G(-1.2f, 5, "Coco")), Miedosa(P(2.4f, 5, "Kira")), G(4.7f, 2, "Felipe"), P(8.8f, 6, "Bruno"), Miedosa(G(11.6f, 3, "Mota")) },
            obstaculos = new[] { ("Cono", -8f), ("Charco", -2.9f), ("Basura", 0.8f), ("Cono", 3.6f), ("Charco", 7.2f), ("Basura", 10.2f) },
            moviles = new[] { ("Pelota", 14f, 4f), ("Patineta", 6f, 5.5f) },
            item = "Jabon", nombreItems = "Jabones", nombreItem = "jabón para mascotas",
            items = new[] { (-7f, 0.35f), (-5.4f, 2.2f), (-2.9f, 2.2f), (0.8f, 2.2f), (7.2f, 2.2f), (9.6f, 0.35f), (12.8f, 0.35f) },
            carteles = new[]
            {
                new Cartel { x = -6.2f, titulo = "Higiene de tu mascota",
                    texto = "• Perros: báñalos cada 1 o 2 meses o cuando estén sucios.\n• Gatos: se asean solos; báñalos solo si están muy sucios.\n• Usa champú para mascotas y agua tibia.\n• Sécalos muy bien y cepíllalos cada semana." },
                new Cartel { x = 6f, titulo = "Limpieza y salud",
                    texto = "• Corta sus uñas cuando estén largas.\n• Lava su cama, sus platos y el arenero con frecuencia.\n• Desparasítalos cada 3 meses (pregúntale a tu veterinario).\n• Revisa que no tengan pulgas ni garrapatas." },
            },
            guia = new[]
            {
                Dra("Ya es de noche, {jugadora}. Hay 6 mascotas que llevan días en la calle: están sucias y llenas de moscas."),
                Yo("Puedo bañarlas. ¿Con cualquier jabón?"),
                Dra("No. Usa jabón para mascotas; el de personas les irrita la piel. Con cada jabón baña a una mascota pulsando E."),
                Dra("Para bañarlas frota rápido alternando ← y →, y luego sécalas bien. Coco, Kira y Mota se asustan si corres: acércate despacio."),
                Dra("Cuidado con las pelotas y patinetas que ruedan por la calle. ¡Salta para esquivarlas!"),
                Yo("¡Voy por ellas!"),
            },
            consejo = "Consejo: después de cada paseo revisa sus patas y su pelo para detectar pulgas y garrapatas.",
        };
        var n4 = new Nivel
        {
            numero = 4, nombre = "Feria de adopción", tipo = TipoNivel.Adopcion, fondo = "Fondo_Feria", siguiente = "Menu", ultimo = true,
            tiempo = 0, regen = 12, luz = 1f,
            mascotas = new[] { P(-14f, 0, "Toby"), G(-13.6f, 0, "Michi"), P(-13.2f, 1, "Canela") },
            obstaculos = new[] { ("Charco", 3.4f), ("Cono", 9.4f) },
            moviles = new (string, float, float)[0],
            item = "Collar", nombreItems = "Collares", nombreItem = "collar",
            items = new[] { (-8f, 0.35f), (0.4f, 2.2f), (7.8f, 0.35f) },
            carteles = new[]
            {
                new Cartel { x = -6.2f, titulo = "Adopción responsable",
                    texto = "Antes de adoptar, pregúntate:\n• ¿Tengo tiempo para jugar con él y cuidarlo todos los días?\n• ¿Tengo espacio y dinero para su comida y el veterinario?\n• ¿Toda mi familia está de acuerdo?\n\nUn perro o un gato vive de 10 a 20 años." },
                new Cartel { x = 1.8f, titulo = "Esterilización e identificación",
                    texto = "• Esterilizar evita camadas no deseadas y el abandono.\n• Ponle siempre un collar con placa y tu teléfono.\n• Nunca abandones a tu mascota: si no puedes cuidarla, busca un refugio." },
            },
            familias = new[]
            {
                new Familia { x = -3f, nombre = "Doña Marta", sprite = "NPC_Familia1", conv = new[]
                {
                    L("Doña Marta", CFamilia, "¡Buenas! Vivo sola desde que se fueron mis hijos y quiero compañía."),
                    Yo("Esta mascota es muy tranquila. ¿Podrá dedicarle tiempo todos los días?"),
                    L("Doña Marta", CFamilia, "¡Claro! Estoy en casa casi todo el día. La cuidaré mucho."),
                    Yo("Aquí tiene su collar con placa. ¡Gracias por adoptar!"),
                }, pregunta = "Doña Marta pregunta: ¿qué necesita la mascota además de comida?", correcta = "Tiempo, juego y visitas al veterinario",
                   incorrectas = new[] { "Nada más, se cuida sola", "Solo un patio grande", "Estar amarrada todo el día" },
                   porQue = "Una mascota necesita compañía, juego, paseos y controles veterinarios." },
                new Familia { x = 5.5f, nombre = "Andrea", sprite = "NPC_Familia2", conv = new[]
                {
                    L("Andrea", CFamilia, "Hola, en mi casa somos cuatro y tenemos un patio grande."),
                    Yo("¿Todos en la familia están de acuerdo con adoptar?"),
                    L("Andrea", CFamilia, "Sí, ya repartimos las tareas: mi hermano le da la comida y yo lo baño."),
                    Yo("¡Perfecto! Este es su collar. Recuerden llevarlo al veterinario."),
                }, pregunta = "Andrea pregunta: ¿qué debe llevar siempre la mascota?", correcta = "Collar con placa y nuestro teléfono",
                   incorrectas = new[] { "Un moño bonito", "Nada, no hace falta", "Un cascabel solamente" },
                   porQue = "La placa con el teléfono ayuda a que vuelva a casa si se pierde." },
                new Familia { x = 12f, nombre = "Paula", sprite = "NPC_Familia3", conv = new[]
                {
                    L("Paula", CFamilia, "¡Hola! Quiero adoptar, pero vivo en un apartamento."),
                    Yo("Muchas mascotas viven felices en apartamento si reciben tiempo, juego y paseos."),
                    L("Paula", CFamilia, "Le dedicaré tiempo todos los días y ya le compré su cama."),
                    Yo("Entonces será muy feliz contigo. ¡Aquí está su collar!"),
                }, pregunta = "Paula pregunta: si algún día no puedo cuidarla, ¿qué hago?", correcta = "Buscarle un refugio o un hogar responsable",
                   incorrectas = new[] { "Dejarla en la calle", "Encerrarla en el patio", "Regalarla a cualquiera" },
                   porQue = "Abandonar a una mascota es maltrato. Si no puedes cuidarla, busca un refugio o un hogar responsable." },
            },
            guia = new[]
            {
                Dra("¡Lo lograste, {jugadora}! Las mascotas están sanas, alimentadas y limpias."),
                Dra("Hoy es la feria de adopción. Tres familias quieren darles un hogar."),
                Yo("¿Cómo sé que las van a cuidar bien?"),
                Dra("Habla con cada familia. Antes, recoge un collar con placa de identificación para cada mascota."),
                Dra("Adoptar es un compromiso para toda su vida: un perro o un gato puede vivir de 10 a 20 años."),
            },
            consejo = "Consejo: adopta, no compres. En los refugios hay muchos perros y gatos esperando un hogar.",
        };
        return new[] { n1, n2, n3, n4 };
    }

    // ---------------- Arte ----------------

    static void ImportarArte()
    {
        foreach (var f in new[] { "Fondo_Barrio", "Fondo_Atardecer", "Fondo_Noche", "Fondo_Feria" })
            ImportarSprite("Assets/Sprites/" + f + ".png", new Vector2(0.5f, 0.5f));
        foreach (var f in new[] { "Charco", "Cono", "Basura", "Patineta" })
            ImportarSprite("Assets/Sprites/Obstaculos/Obstaculo_" + f + ".png", new Vector2(0.5f, 0f));
        ImportarSprite("Assets/Sprites/Obstaculos/Obstaculo_Pelota.png", new Vector2(0.5f, 0.5f));
        foreach (var f in new[] { "Icono_Ayuda", "Icono_Huella", "Icono_Jugadora", "Corazon_Lleno", "Corazon_Vacio", "Tecla_E",
                                  "Icono_Corazon", "Icono_Casa", "Icono_Cruz", "Icono_Estrella", "Icono_Info",
                                  "Icono_Herida", "Icono_Hambre", "Icono_Sucio", "Moscas_0", "Moscas_1", "Nube" })
            ImportarSprite("Assets/Sprites/UI/" + f + ".png", new Vector2(0.5f, 0.5f));
        foreach (var f in new[] { "Botiquin", "Croquetas", "Jabon", "Collar" })
            ImportarSprite("Assets/Sprites/Items/Item_" + f + ".png", new Vector2(0.5f, 0.5f));
        foreach (var f in new[] { "Estacion_Curacion", "Estacion_Comedero", "Estacion_Banera", "Cartel", "Reja" })
            ImportarSprite("Assets/Sprites/Estaciones/" + f + ".png", new Vector2(0.5f, 0f));
        for (int i = 1; i <= 7; i++)
            Cortar("Assets/Sprites/Mascotas/Perro_0" + i + ".png", "Perro_0" + i + "_", 64, 48, new[] { 8, 8, 8, 8, 8, 8, 8, 8, 4 });
        // Gatos (Pet Cats Pack): filas Idle 10, Walk 8, Laying 8, Sleeping 1, Sitting 1, Meow 4, Licking 5.
        for (int i = 1; i <= 6; i++)
            Cortar("Assets/Sprites/Mascotas/Gato_0" + i + ".png", "Gato_0" + i + "_", 50, 34, FilasGato, 16);
        foreach (var f in new[] { "Rescatista_Luna", "Rescatista_Mia", "NPC_DraLucia", "NPC_Familia1", "NPC_Familia2", "NPC_Familia3" })
            Cortar("Assets/Sprites/Personajes/" + f + ".png", f + "_", 150, 85, GrillaChica);
    }

    static void ImportarSprite(string path, Vector2 pivot)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 32;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        var set = new TextureImporterSettings();
        imp.ReadTextureSettings(set);
        set.spriteAlignment = (int)SpriteAlignment.Custom;
        set.spritePivot = pivot;
        imp.SetTextureSettings(set);
        imp.SaveAndReimport();
    }

    static readonly int[] FilasGato = { 10, 8, 8, 1, 1, 4, 5 };

    static void Cortar(string tex, string prefijo, int w, int h, int[] columnas, int ppu = 32)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(tex);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(imp);
        dp.InitSpriteEditorDataProvider();
        var rects = new List<SpriteRect>();
        int alto = h * columnas.Length, n = 0;
        for (int r = 0; r < columnas.Length; r++)
            for (int c = 0; c < columnas[r]; c++)
                rects.Add(new SpriteRect
                {
                    name = prefijo + n++,
                    rect = new Rect(c * w, alto - (r + 1) * h, w, h),
                    alignment = SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0f),
                    spriteID = GUID.Generate()
                });
        dp.SetSpriteRects(rects.ToArray());
        dp.GetDataProvider<ISpriteNameFileIdDataProvider>()
          .SetNameFileIdPairs(rects.Select(x => new SpriteNameFileIdPair(x.name, x.spriteID)).ToList());
        dp.Apply();
        imp.SaveAndReimport();
    }

    static Dictionary<string, Sprite> Sprites(string tex)
    {
        return AssetDatabase.LoadAllAssetsAtPath(tex).OfType<Sprite>().ToDictionary(s => s.name);
    }

    static Sprite Spr(string path) { return AssetDatabase.LoadAssetAtPath<Sprite>(path); }
    static Sprite UI(string n) { return Spr("Assets/Sprites/UI/" + n + ".png"); }

    static Sprite[] Rango(Dictionary<string, Sprite> d, string prefijo, int desde, int cantidad)
    {
        return Enumerable.Range(desde, cantidad).Select(i => d[prefijo + i]).ToArray();
    }

    static Sprite[] IdlePersona(string nombre)
    {
        return Rango(Sprites("Assets/Sprites/Personajes/" + nombre + ".png"), nombre + "_", 0, 8);
    }

    static List<Skin> CargarSkins()
    {
        var lista = new List<Skin>();
        for (int i = 1; i <= 7; i++)
        {
            string p = "Perro_0" + i + "_";
            var d = Sprites("Assets/Sprites/Mascotas/Perro_0" + i + ".png");
            // Los perros 2, 4 y 6 del pack tienen filas más cortas (6 cuadros) y una fila menos.
            bool corto = i % 2 == 0;
            int n = corto ? 6 : 8;
            lista.Add(new Skin
            {
                especie = Especie.Perro, miraIzq = true, alturaAlerta = 1.45f,
                hambre = Rango(d, p, corto ? 40 : 56, n),  // de pie pidiendo comida
                sucia = Rango(d, p, 8, n),                  // sentado, ladrando
                herida = Rango(d, p, corto ? 48 : 64, 4),   // acurrucado, sin fuerzas
                camina = Rango(d, p, 32, 8), feliz = Rango(d, p, 0, 6), quieta = Rango(d, p, 8, n)
            });
        }
        return lista;
    }

    static List<Skin> CargarGatos()
    {
        var lista = new List<Skin>();
        for (int i = 1; i <= 6; i++)
        {
            string p = "Gato_0" + i + "_";
            var d = Sprites("Assets/Sprites/Mascotas/Gato_0" + i + ".png");
            lista.Add(new Skin
            {
                especie = Especie.Gato, miraIzq = false, alturaAlerta = 1.4f,
                hambre = Rango(d, p, 28, 4),   // maullando
                sucia = Rango(d, p, 32, 5),    // lamiéndose
                herida = new[] { d[p + 24], d[p + 25], d[p + 26], d[p + 25] }, // acostado
                camina = Rango(d, p, 10, 8), feliz = Rango(d, p, 0, 10), quieta = new[] { d[p + 27] }
            });
        }
        return lista;
    }

    // ---------------- Animaciones ----------------

    // Devuelve el controlador de Sofi y los override controllers de Luna y Mía.
    static RuntimeAnimatorController[] CrearControladores()
    {
        var g = Sprites(GirlTex);
        string P = "Player_Rescatista_";
        var clips = new List<AnimationClip>
        {
            AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimDir + "/Player_Idle.anim"),
            AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimDir + "/Player_Walk.anim"),
            AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimDir + "/Player_Run.anim"),
        };
        clips.AddRange(ClipsExtra(g, P, ""));
        string path = AnimDir + "/Rescatista.controller";
        AssetDatabase.DeleteAsset(path);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm = ctrl.layers[0].stateMachine;
        foreach (var c in clips)
        {
            var st = sm.AddState(c.name);
            st.motion = c;
            if (c.name == "Player_Idle") sm.defaultState = st;
        }

        var lista = new List<RuntimeAnimatorController> { ctrl };
        foreach (var nombre in new[] { "Luna", "Mia" })
        {
            string tex = "Rescatista_" + nombre;
            var d = Sprites("Assets/Sprites/Personajes/" + tex + ".png");
            string pre = tex + "_";
            var propios = new List<AnimationClip>
            {
                Clip(nombre + "_Idle", Rango(d, pre, 0, 8), 8, true),
                Clip(nombre + "_Walk", Rango(d, pre, 8, 8), 10, true),
                Clip(nombre + "_Run", Rango(d, pre, 32, 8), 12, true),
            };
            propios.AddRange(ClipsExtra(d, pre, nombre + "_"));
            string ruta = AnimDir + "/Rescatista_" + nombre + ".overrideController";
            AssetDatabase.DeleteAsset(ruta);
            var aoc = new AnimatorOverrideController(ctrl);
            for (int i = 0; i < clips.Count; i++) aoc[clips[i]] = propios[i];
            AssetDatabase.CreateAsset(aoc, ruta);
            lista.Add(aoc);
        }
        return lista.ToArray();
    }

    static List<AnimationClip> ClipsExtra(Dictionary<string, Sprite> g, string P, string prefijoNombre)
    {
        string n = prefijoNombre == "" ? "Player_" : prefijoNombre;
        return new List<AnimationClip>
        {
            Clip(n + (prefijoNombre == "" ? "Saltar" : "Saltar"), new[] { g[P + 33], g[P + 34] }, 6, true),
            Clip(n + "Rescatar", new[] { g[P + 40], g[P + 41] }, 5, false),
            Clip(n + "Celular", Rango(g, P, 24, 8), 10, true),
            Clip(n + "CelularQuieta", new[] { g[P + 27], g[P + 28] }, 2, true),
            Clip(n + "Derrota", Rango(g, P, 40, 5), 6, false),
        };
    }

    static AnimationClip Clip(string nombre, Sprite[] frames, float fps, bool bucle)
    {
        var clip = new AnimationClip { frameRate = fps };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int k = 0; k <= frames.Length; k++)
        {
            var s = k < frames.Length ? frames[k] : (bucle ? frames[0] : frames[frames.Length - 1]);
            keys[k] = new ObjectReferenceKeyframe { time = k / fps, value = s };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var set = AnimationUtility.GetAnimationClipSettings(clip);
        set.loopTime = bucle;
        AnimationUtility.SetAnimationClipSettings(clip, set);
        string path = AnimDir + "/" + nombre + ".anim";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    // ---------------- Prefabs ----------------

    static SpriteRenderer Render(GameObject go, Sprite s, int orden)
    {
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingLayerName = "Characters";
        sr.sortingOrder = orden;
        return sr;
    }

    static GameObject Hijo(GameObject padre, string nombre, Sprite s, float y, int orden, bool activo)
    {
        var h = new GameObject(nombre);
        h.transform.SetParent(padre.transform, false);
        h.transform.localPosition = new Vector3(0f, y, 0f);
        Render(h, s, orden);
        h.SetActive(activo);
        return h;
    }

    static BoxCollider2D Area(GameObject go, float w, float h)
    {
        var c = go.AddComponent<BoxCollider2D>();
        c.isTrigger = true;
        c.size = new Vector2(w, h);
        c.offset = new Vector2(0f, h / 2f);
        return c;
    }

    static MarcadorMapa Marca(GameObject go, Sprite icono, Color color, Vector2 tam, float altura)
    {
        var m = go.AddComponent<MarcadorMapa>();
        m.icono = icono; m.color = color; m.tamano = tam; m.alturaEnMapa = altura;
        return m;
    }

    static Dictionary<string, GameObject> CrearPrefabs(Skin skin)
    {
        var r = new Dictionary<string, GameObject>();
        var teclaE = UI("Tecla_E");

        // Mascota
        var m = new GameObject("Mascota");
        Render(m, skin.quieta[0], 1);
        Area(m, 1.8f, 1.2f);
        var masc = m.AddComponent<Mascota>();
        AplicarSkin(masc, skin, Necesidad.Ninguna);
        masc.alerta = Hijo(m, "Alerta", UI("Icono_Ayuda"), 1.45f, 5, true);
        masc.iconoHerida = UI("Icono_Herida");
        masc.iconoHambre = UI("Icono_Hambre");
        masc.iconoSucia = UI("Icono_Sucio");
        masc.framesMoscas = new[] { UI("Moscas_0"), UI("Moscas_1") };
        masc.moscas = Hijo(m, "Moscas", masc.framesMoscas[0], 0.75f, 6, false);
        masc.indicador = Hijo(m, "IndicadorE", teclaE, 0.6f, 7, false);
        masc.indicador.transform.localPosition = new Vector3(0.8f, 1.5f, 0f);
        masc.spriteCorazon = UI("Icono_Corazon");
        masc.marcador = Marca(m, UI("Icono_Huella"), Color.red, new Vector2(18, 16), -16f);
        r["Mascota"] = Guardar(m);

        // Obstáculos
        foreach (var tipo in new[] { "Charco", "Cono", "Basura", "Pelota", "Patineta" })
        {
            var o = new GameObject(tipo);
            Render(o, Spr("Assets/Sprites/Obstaculos/Obstaculo_" + tipo + ".png"), 0);
            Collider2D col = tipo == "Pelota" ? (Collider2D)o.AddComponent<CircleCollider2D>() : o.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            var ob = o.AddComponent<Obstaculo>();
            if (tipo == "Pelota" || tipo == "Patineta")
            {
                o.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                ob.gira = tipo == "Pelota";
                ob.velocidad = 3f;
            }
            r[tipo] = Guardar(o);
        }

        // Coleccionables
        foreach (var (tipo, nombre) in new[] { ("Botiquin", "Botiquín"), ("Croquetas", "Croquetas"), ("Jabon", "Jabón"), ("Collar", "Collar") })
        {
            var c = new GameObject("Item_" + tipo);
            var spr = Spr("Assets/Sprites/Items/Item_" + tipo + ".png");
            Render(c, spr, 2);
            var cc = c.AddComponent<CircleCollider2D>();
            cc.isTrigger = true;
            cc.radius = 0.32f;
            c.AddComponent<Coleccionable>().nombreItem = nombre;
            Marca(c, UI("Icono_Estrella"), new Color(1f, 0.85f, 0.25f), new Vector2(11, 11), 14f);
            r["Item_" + tipo] = Guardar(c);
        }

        // Cartel informativo
        var cartel = new GameObject("Cartel");
        Render(cartel, Spr("Assets/Sprites/Estaciones/Cartel.png"), 0);
        Area(cartel, 1.8f, 2f);
        var ci = cartel.AddComponent<CartelInfo>();
        ci.indicador = Hijo(cartel, "IndicadorE", teclaE, 1.45f, 7, false);
        Marca(cartel, UI("Icono_Info"), new Color(0.55f, 0.8f, 1f), new Vector2(11, 11), 14f);
        r["Cartel"] = Guardar(cartel);

        // NPC guía
        var npc = new GameObject("NPC_Guia");
        Render(npc, IdlePersona("NPC_DraLucia")[0], 2);
        npc.AddComponent<AnimadorSprites>().frames = IdlePersona("NPC_DraLucia");
        Area(npc, 2.6f, 2f);
        var nd = npc.AddComponent<NPCDialogo>();
        nd.indicador = Hijo(npc, "IndicadorE", teclaE, 2.0f, 7, false);
        Marca(npc, UI("Icono_Jugadora"), new Color(0.4f, 0.8f, 1f), new Vector2(13, 17), 2f);
        r["NPC_Guia"] = Guardar(npc);

        // Familia adoptante
        var fam = new GameObject("Adoptante");
        Render(fam, IdlePersona("NPC_Familia1")[0], 2);
        fam.AddComponent<AnimadorSprites>().frames = IdlePersona("NPC_Familia1");
        Area(fam, 2.6f, 2f);
        var ad = fam.AddComponent<Adoptante>();
        ad.indicador = Hijo(fam, "IndicadorE", teclaE, 2.0f, 7, false);
        Marca(fam, UI("Icono_Casa"), new Color(1f, 0.6f, 0.3f), new Vector2(13, 12), 0f);
        r["Adoptante"] = Guardar(fam);

        // Estaciones de cuidado
        foreach (var tipo in new[] { "Curacion", "Comedero", "Banera" })
        {
            var e = new GameObject("Estacion_" + tipo);
            Render(e, Spr("Assets/Sprites/Estaciones/Estacion_" + tipo + ".png"), 0);
            Area(e, 2.6f, 1.6f);
            var ec = e.AddComponent<EstacionCuidado>();
            ec.indicador = Hijo(e, "IndicadorE", teclaE, 1.45f, 7, false);
            Marca(e, UI("Icono_Cruz"), new Color(0.3f, 0.6f, 1f), new Vector2(12, 12), 0f);
            r["Estacion_" + tipo] = Guardar(e);
        }
        return r;
    }

    static void AplicarSkin(Mascota m, Skin skin, Necesidad necesidad)
    {
        m.especie = skin.especie;
        m.spriteMiraIzquierda = skin.miraIzq;
        m.alturaAlerta = skin.alturaAlerta;
        m.necesidad = necesidad;
        m.framesEspera = necesidad == Necesidad.Hambre ? skin.hambre : necesidad == Necesidad.Sucia ? skin.sucia : skin.quieta;
        m.framesHerida = skin.herida;
        m.framesCamina = skin.camina;
        m.framesFeliz = skin.feliz;
    }

    // ---------------- Sonido ----------------

    static readonly string[] Efectos = { "salto", "recoger", "acierto", "tecla", "error", "dano", "curado", "mision", "nube",
                                         "dialogo", "click", "adopcion", "victoria", "derrota", "ladrido", "maullido" };

    static void CrearSonidos()
    {
        var go = new GameObject("Sonidos");
        var so = go.AddComponent<Sonidos>();
        so.efectos = Efectos.Select(n => new Sonidos.Efecto
        {
            nombre = n,
            clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + n + ".wav"),
            volumen = n == "dialogo" || n == "tecla" ? 0.5f : 0.8f
        }).ToArray();
        so.musica = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/musica.wav");
        so.volumenMusica = 0.22f;
    }

    static GameObject Guardar(GameObject go)
    {
        string path = "Assets/Prefabs/" + go.name + ".prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject Instanciar(GameObject prefab, Transform padre, Vector3 pos, string nombre)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
        go.transform.position = pos;
        go.name = nombre;
        return go;
    }

    static void Registrar(params Object[] objetos)
    {
        foreach (var o in objetos) PrefabUtility.RecordPrefabInstancePropertyModifications(o);
    }

    // ---------------- Escenas ----------------

    static UnityEngine.SceneManagement.Scene CopiarBase(string nombre)
    {
        string path = "Assets/Scenes/" + nombre + ".unity";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CopyAsset(Base, path);
        var escena = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        foreach (var n in new[] { "Player", "Mascota", "HUD", "Fondo" })
        {
            var g = GameObject.Find(n);
            if (g != null) Object.DestroyImmediate(g);
        }
        var cam = Camera.main;
        var viejo = cam.GetComponent<CameraFollow>();
        if (viejo != null) Object.DestroyImmediate(viejo);
        cam.orthographic = true;
        cam.orthographicSize = 3f;
        cam.transform.position = new Vector3(0f, 1.3125f, -10f);
        return escena;
    }

    static void CrearFondo(string sprite, Transform padre)
    {
        var f = new GameObject("Fondo");
        f.transform.SetParent(padre, false);
        f.transform.position = new Vector3(0f, 1.3125f, 0f);
        var sr = f.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("Assets/Sprites/" + sprite + ".png");
        sr.sortingLayerName = "Default";
        sr.sortingOrder = -10;
    }

    static GameObject CrearRescatista(Transform padre, Vector3 pos, RuntimeAnimatorController ctrl, Sprite inicial)
    {
        var p = new GameObject("Player");
        p.tag = "Player";
        p.transform.SetParent(padre, false);
        p.transform.position = pos;
        var sr = p.AddComponent<SpriteRenderer>();
        sr.sprite = inicial;
        sr.sortingLayerName = "Characters";
        sr.sortingOrder = 3;
        p.AddComponent<Animator>().runtimeAnimatorController = ctrl;
        return p;
    }

    static void CrearMuro(Transform padre, float x, string nombre)
    {
        var m = new GameObject(nombre);
        m.transform.SetParent(padre, false);
        m.transform.position = new Vector3(x, -0.05f, 0f);
        Render(m, Spr("Assets/Sprites/Estaciones/Reja.png"), 4);
        var c = m.AddComponent<BoxCollider2D>(); // sólido: la rescatista no lo atraviesa
        c.size = new Vector2(0.5f, 6f);
        c.offset = new Vector2(0f, 3f);
    }

    static void CrearNivel(Nivel n, RuntimeAnimatorController[] ctrls, Dictionary<string, GameObject> prefabs, List<Skin> skins, List<Skin> gatos)
    {
        var escena = CopiarBase("Nivel" + n.numero);
        var luz = GameObject.Find("Global Light 2D");
        if (luz != null) luz.GetComponent<Light2D>().intensity = n.luz;

        var entorno = new GameObject("Entorno").transform;
        var personajes = new GameObject("Personajes").transform;
        var npcs = new GameObject("NPCs").transform;
        var grupoMascotas = new GameObject("Mascotas").transform;
        var interactivos = new GameObject("ObjetosInteractivos").transform;
        var coleccionables = new GameObject("Coleccionables").transform;
        var grupoObstaculos = new GameObject("Obstaculos").transform;
        var gm = new GameObject("GameManager");

        CrearFondo(n.fondo, entorno);
        var suelo = new GameObject("Suelo");
        suelo.transform.SetParent(entorno, false);
        suelo.transform.position = new Vector3(0f, -0.5f, 0f);
        suelo.AddComponent<BoxCollider2D>().size = new Vector2(40f, 1f);
        CrearMuro(entorno, -14.9f, "Muro_Izquierdo");
        CrearMuro(entorno, 14.9f, "Muro_Derecho");

        var g = Sprites(GirlTex);
        var p = CrearRescatista(personajes, new Vector3(-12.5f, 0f, 0f), ctrls[0], g["Player_Rescatista_0"]);
        var rb = p.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var col = p.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.5f, 1.6f);
        col.offset = new Vector2(0f, 0.8f);
        var pc = p.AddComponent<PlayerController>();
        pc.regeneracionPoder = n.regen;
        pc.personajes = ctrls;

        // NPC guía
        var guia = Instanciar(prefabs["NPC_Guia"], npcs, new Vector3(-10.5f, 0f, 0f), "NPC_Guia");
        var nd = guia.GetComponent<NPCDialogo>();
        nd.nombreNPC = "Dra. Lucía";
        nd.esGuia = true;
        nd.conversacion = n.guia;
        Registrar(nd);

        // Familias adoptantes
        foreach (var f in n.familias)
        {
            var go = Instanciar(prefabs["Adoptante"], npcs, new Vector3(f.x, 0f, 0f), "Familia_" + f.nombre.Replace(" ", ""));
            var a = go.GetComponent<Adoptante>();
            a.nombreFamilia = f.nombre;
            a.conversacion = f.conv;
            a.sinCollar = new[] { L(f.nombre, CFamilia, "¡Qué linda mascota! Pero necesita su collar con placa antes de venir a casa. ¿Lo traes?") };
            a.despues = new[] { L(f.nombre, CFamilia, "¡Gracias, {jugadora}! Le daremos mucho amor.") };
            a.pregunta = f.pregunta; a.respuestaCorrecta = f.correcta; a.respuestasIncorrectas = f.incorrectas; a.explicacion = f.porQue;
            var frames = IdlePersona(f.sprite);
            var an = go.GetComponent<AnimadorSprites>();
            an.frames = frames;
            go.GetComponent<SpriteRenderer>().sprite = frames[0];
            Registrar(a, an, go.GetComponent<SpriteRenderer>());
        }

        // Mascotas
        var necesidad = n.tipo == TipoNivel.Curar ? Necesidad.Herida : n.tipo == TipoNivel.Alimentar ? Necesidad.Hambre
                      : n.tipo == TipoNivel.Banar ? Necesidad.Sucia : Necesidad.Ninguna;
        for (int i = 0; i < n.mascotas.Length; i++)
        {
            var pet = n.mascotas[i];
            var go = Instanciar(prefabs["Mascota"], grupoMascotas, new Vector3(pet.x, 0f, 0f),
                                (pet.especie == Especie.Gato ? "Gato_" : "Perro_") + pet.nombre);
            var skin = pet.especie == Especie.Gato ? gatos[pet.skin % gatos.Count] : skins[pet.skin % skins.Count];
            var m = go.GetComponent<Mascota>();
            AplicarSkin(m, skin, necesidad);
            m.nombreMascota = pet.nombre;
            m.asustadiza = pet.miedo;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = necesidad == Necesidad.Herida ? skin.herida[0] : m.framesEspera[0];
            Registrar(m, sr);
        }

        // Carteles con consejos (objetos interactivos)
        int k = 1;
        foreach (var c in n.carteles)
        {
            var go = Instanciar(prefabs["Cartel"], interactivos, new Vector3(c.x, 0f, 0f), "Cartel_" + k++);
            var ci = go.GetComponent<CartelInfo>();
            ci.titulo = c.titulo;
            ci.contenido = c.texto;
            Registrar(ci);
        }

        // Coleccionables
        k = 1;
        foreach (var (x, y) in n.items)
            Instanciar(prefabs["Item_" + n.item], coleccionables, new Vector3(x, y, 0f), n.item + "_" + k++);

        // Obstáculos
        k = 1;
        foreach (var (tipo, x) in n.obstaculos)
            Instanciar(prefabs[tipo], grupoObstaculos, new Vector3(x, 0f, 0f), tipo + "_" + k++);
        foreach (var (tipo, x, vel) in n.moviles)
        {
            var go = Instanciar(prefabs[tipo], grupoObstaculos, new Vector3(x, tipo == "Pelota" ? 0.19f : 0f, 0f), tipo + "_" + k++);
            var ob = go.GetComponent<Obstaculo>();
            ob.velocidad = vel;
            Registrar(ob);
        }

        var cam = Camera.main;
        cam.transform.position = new Vector3(-9.6f, 1.3125f, -10f);
        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.objetivo = p.transform;
        follow.limiteX = 15f;

        var lm = gm.AddComponent<LevelManager>();
        lm.numeroNivel = n.numero;
        lm.nombreNivel = n.nombre;
        lm.tipo = n.tipo;
        lm.tiempoLimite = modoPrueba ? 0f : n.tiempo;
        lm.sinDanio = modoPrueba;
        lm.dificultad = Mathf.Clamp(n.numero, 1, 3);
        lm.siguienteEscena = n.siguiente;
        lm.esUltimoNivel = n.ultimo;
        lm.nombreItems = n.nombreItems;
        lm.nombreItem = n.nombreItem;
        lm.totalItems = n.items.Length;
        lm.iconoItem = Spr("Assets/Sprites/Items/Item_" + n.item + ".png");
        lm.consejoFinal = n.consejo;
        CrearHUD(lm, n.fondo, n.numero);
        CrearSonidos();

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
    }

    // ---------------- Interfaz ----------------

    static GameObject CrearCanvas(string nombre)
    {
        var go = new GameObject(nombre, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = go.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1280, 720);
        sc.matchWidthOrHeight = 0.5f;
        return go;
    }

    static RectTransform Caja(string nombre, Transform padre, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 tam)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = tam;
        return rt;
    }

    static RectTransform Esquina(string nombre, Transform padre, Vector2 ancla, Vector2 pos, Vector2 tam)
    {
        return Caja(nombre, padre, ancla, ancla, ancla, pos, tam);
    }

    static RectTransform Estirar(string nombre, Transform padre, float margen = 0f)
    {
        var rt = Caja(nombre, padre, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        rt.offsetMin = new Vector2(margen, margen);
        rt.offsetMax = new Vector2(-margen, -margen);
        return rt;
    }

    static Image Img(RectTransform rt, Color c, Sprite s = null)
    {
        var i = rt.gameObject.AddComponent<Image>();
        i.color = c;
        i.sprite = s;
        return i;
    }

    static Text Txt(RectTransform rt, string texto, int tam, TextAnchor alin, Color color, bool negrita = false)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.font = fuente;
        t.text = texto;
        t.fontSize = tam;
        t.alignment = alin;
        t.color = color;
        t.fontStyle = negrita ? FontStyle.Bold : FontStyle.Normal;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        rt.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.75f);
        return t;
    }

    static readonly Color Panel = new Color(0.10f, 0.12f, 0.20f, 0.88f);
    static readonly Color Dorado = new Color(1f, 0.85f, 0.45f);

    static void FranjaCreditos(Transform canvas)
    {
        var f = Caja("FranjaCreditos", canvas, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 28));
        Img(f, new Color(0.08f, 0.08f, 0.14f, 0.88f));
        Txt(Estirar("Texto", f), Creditos.Linea, 15, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.95f));
    }

    static void CrearHUD(LevelManager lm, string fondo, int numero)
    {
        var canvasGo = CrearCanvas("HUD");
        var canvas = canvasGo.transform;
        var ui = canvasGo.AddComponent<UIJuego>();
        FranjaCreditos(canvas);

        // Estado: nivel, corazones, poder, mascotas, tiempo e inventario
        var estado = Esquina("PanelEstado", canvas, new Vector2(0, 1), new Vector2(12, -36), new Vector2(450, 176));
        Img(estado, Panel);
        lm.textoNivel = Txt(Esquina("TextoNivel", estado, new Vector2(0, 1), new Vector2(14, -8), new Vector2(420, 26)), "NIVEL", 20, TextAnchor.MiddleLeft, Dorado, true);

        Txt(Esquina("EtiquetaVidas", estado, new Vector2(0, 1), new Vector2(14, -40), new Vector2(90, 30)), "VIDAS", 19, TextAnchor.MiddleLeft, Color.white, true);
        ui.corazones = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var c = Esquina("Corazon_" + (i + 1), estado, new Vector2(0, 1), new Vector2(106 + i * 40, -40), new Vector2(34, 31));
            ui.corazones[i] = Img(c, Color.white, UI("Corazon_Lleno"));
        }
        ui.corazonLleno = UI("Corazon_Lleno");
        ui.corazonVacio = UI("Corazon_Vacio");

        var bp = Esquina("BarraPODER", estado, new Vector2(0, 1), new Vector2(14, -78), new Vector2(420, 24));
        Txt(Caja("Etiqueta", bp, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(90, 0)), "PODER", 19, TextAnchor.MiddleLeft, Color.white, true);
        var marco = Estirar("Marco", bp);
        marco.offsetMin = new Vector2(92, 0);
        Img(marco, new Color(0.05f, 0.05f, 0.08f, 0.9f));
        var valor = Estirar("Valor", Estirar("Area", marco, 3f));
        Img(valor, new Color(0.25f, 0.6f, 0.98f));
        ui.barraPoder = valor;

        lm.textoMascotas = Txt(Esquina("TextoMascotas", estado, new Vector2(0, 1), new Vector2(14, -108), new Vector2(250, 26)), "Mascotas", 18, TextAnchor.MiddleLeft, Color.white);
        lm.textoTiempo = Txt(Esquina("TextoTiempo", estado, new Vector2(0, 1), new Vector2(290, -108), new Vector2(146, 26)), "Tiempo", 18, TextAnchor.MiddleRight, Color.white, true);
        ui.textoItems = Txt(Esquina("TextoItems", estado, new Vector2(0, 1), new Vector2(14, -140), new Vector2(190, 28)), "Items", 18, TextAnchor.MiddleLeft, Dorado, true);
        var inv = Esquina("Inventario", estado, new Vector2(0, 1), new Vector2(206, -138), new Vector2(230, 30));
        Img(inv, new Color(0f, 0f, 0f, 0.25f));
        ui.ranuras = new Image[7];
        for (int i = 0; i < 7; i++)
        {
            var s = Esquina("Ranura_" + (i + 1), inv, new Vector2(0, 0.5f), new Vector2(6 + i * 32, 0), new Vector2(28, 26));
            s.pivot = new Vector2(0, 0.5f);
            ui.ranuras[i] = Img(s, Color.white);
            ui.ranuras[i].preserveAspect = true;
        }

        // Misión actual y recorrido de niveles (muestra los 4 niveles y en cuál está la jugadora)
        var mision = Esquina("PanelMision", canvas, new Vector2(0.5f, 1), new Vector2(34, -36), new Vector2(400, 112));
        Img(mision, Panel);
        var recorrido = Esquina("RecorridoNiveles", mision, new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(388, 36));
        recorrido.pivot = new Vector2(0.5f, 1f);
        ui.fichasNivel = new Image[LevelManager.TotalNiveles];
        ui.marcasCompletado = new GameObject[LevelManager.TotalNiveles];
        string[] cortos = { "Curar", "Alimentar", "Bañar", "Adopción" };
        for (int i = 0; i < LevelManager.TotalNiveles; i++)
        {
            int n = i + 1;
            if (i > 0)
            {
                var linea = Esquina("Union_" + i, recorrido, new Vector2(0, 0.5f), new Vector2(i * 97 - 11, 0), new Vector2(13, 4));
                Img(linea, new Color(1f, 1f, 1f, 0.5f));
            }
            var ficha = Esquina("Nivel_" + n, recorrido, new Vector2(0, 0.5f), new Vector2(3 + i * 97, 0), new Vector2(82, 30));
            var color = n < numero ? new Color(0.35f, 0.75f, 0.45f) : n == numero ? new Color(1f, 0.78f, 0.3f) : new Color(0.3f, 0.32f, 0.4f);
            ui.fichasNivel[i] = Img(ficha, color);
            var txt = Txt(Estirar("Texto", ficha), n + ". " + cortos[i], n == numero ? 14 : 13, TextAnchor.MiddleCenter,
                          n == numero ? new Color(0.2f, 0.12f, 0.05f) : Color.white, n == numero);
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (n == numero) txt.GetComponent<Outline>().enabled = false;
            var marca = Esquina("Completado", ficha, new Vector2(1, 1), new Vector2(4, 6), new Vector2(16, 16));
            Img(marca, Color.white, UI("Icono_Estrella"));
            marca.gameObject.SetActive(n < numero);
            ui.marcasCompletado[i] = marca.gameObject;
        }
        var cuerpoMision = Caja("TextoMision", mision, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        cuerpoMision.offsetMin = new Vector2(8, 6); cuerpoMision.offsetMax = new Vector2(-8, -44);
        ui.textoMision = Txt(cuerpoMision, "Misión", 16, TextAnchor.MiddleCenter, Color.white, true);

        // Mapa
        var mapaRt = Esquina("Mapa", canvas, new Vector2(1, 1), new Vector2(-12, -36), new Vector2(384, 120));
        Img(mapaRt, Panel);
        Txt(Esquina("Titulo", mapaRt, new Vector2(0, 1), new Vector2(12, -3), new Vector2(360, 22)), "MAPA DEL BARRIO  (M o Tab: verlo en el celular)", 14, TextAnchor.MiddleLeft, Dorado, true);
        var area = Esquina("Area", mapaRt, new Vector2(0, 1), new Vector2(12, -26), new Vector2(360, 72));
        Img(area, Color.white, Spr("Assets/Sprites/" + fondo + ".png"));
        var plantilla = Caja("PlantillaMarcador", area, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14, 14));
        Img(plantilla, Color.white);
        plantilla.gameObject.AddComponent<Outline>().effectColor = Color.black;
        var pin = Caja("MarcadorJugadora", area, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(15, 20));
        Img(pin, new Color(1f, 0.45f, 0.78f), UI("Icono_Jugadora"));
        pin.gameObject.AddComponent<Outline>().effectColor = Color.black;
        var leyenda = Esquina("Leyenda", mapaRt, new Vector2(0, 0), new Vector2(12, 2), new Vector2(360, 20));
        Txt(leyenda, "Rojo: mascota que necesita ayuda · Verde: ya cuidada · Estrella: objeto · Rosa: tú", 10, TextAnchor.MiddleCenter, Color.white);
        var mapa = mapaRt.gameObject.AddComponent<MapaUI>();
        mapa.area = area;
        mapa.marcadorJugadora = pin;
        mapa.plantillaMarcador = plantilla;
        mapa.leyenda = leyenda.gameObject;
        lm.mapa = mapa;

        // Mensajes
        lm.textoMensaje = Txt(Esquina("TextoMensaje", canvas, new Vector2(0.5f, 0), new Vector2(0, 92), new Vector2(1000, 44)), "", 26, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.7f), true);
        lm.textoAyuda = Txt(Esquina("TextoAyuda", canvas, new Vector2(0.5f, 0), new Vector2(0, 46), new Vector2(800, 40)), "", 24, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.4f), true);

        // Controles
        var ctr = Caja("Controles", canvas, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 30));
        Img(ctr, new Color(0.08f, 0.08f, 0.14f, 0.85f));
        Txt(Estirar("Texto", ctr), "Mover: ← → o A D   ·   Correr: Shift   ·   Saltar: Espacio   ·   Interactuar: E   ·   Mapa: M / Tab   ·   Menú: Esc", 15, TextAnchor.MiddleCenter, Color.white);

        // Panel de diálogo (DialoguePanel)
        var dlg = Esquina("DialoguePanel", canvas, new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(980, 170));
        Img(dlg, new Color(0.06f, 0.08f, 0.15f, 0.95f));
        ui.textoNombre = Txt(Esquina("NPCNameText", dlg, new Vector2(0, 1), new Vector2(24, -12), new Vector2(600, 30)), "Nombre", 24, TextAnchor.MiddleLeft, CGuia, true);
        var cont = Caja("DialogueContentText", dlg, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        cont.offsetMin = new Vector2(24, 34); cont.offsetMax = new Vector2(-24, -46);
        ui.textoDialogo = Txt(cont, "", 22, TextAnchor.UpperLeft, Color.white);
        Txt(Esquina("Continuar", dlg, new Vector2(1, 0), new Vector2(-18, 6), new Vector2(420, 24)), "Presiona E o Espacio para continuar...", 15, TextAnchor.MiddleRight, Dorado);
        ui.panelDialogo = dlg.gameObject;

        // Panel de información (carteles)
        var info = Esquina("InfoPanel", canvas, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(720, 380));
        Img(info, new Color(0.98f, 0.94f, 0.82f, 0.97f));
        var borde = Estirar("Borde", info, 6f);
        Img(borde, new Color(0, 0, 0, 0));
        borde.gameObject.AddComponent<Outline>().effectColor = new Color(0.47f, 0.31f, 0.2f);
        ui.textoTituloInfo = Txt(Esquina("Titulo", info, new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(660, 40)), "Título", 28, TextAnchor.MiddleCenter, new Color(0.85f, 0.3f, 0.43f), true);
        ui.textoTituloInfo.GetComponent<Outline>().enabled = false;
        var cuerpo = Caja("Texto", info, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        cuerpo.offsetMin = new Vector2(40, 50); cuerpo.offsetMax = new Vector2(-40, -76);
        ui.textoInfo = Txt(cuerpo, "", 22, TextAnchor.UpperLeft, new Color(0.2f, 0.16f, 0.12f));
        ui.textoInfo.GetComponent<Outline>().enabled = false;
        var cerrar = Txt(Esquina("Cerrar", info, new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(600, 26)), "Presiona E para cerrar", 17, TextAnchor.MiddleCenter, new Color(0.45f, 0.35f, 0.25f), true);
        cerrar.GetComponent<Outline>().enabled = false;
        ui.panelInfo = info.gameObject;

        // Nube de consejos: aparece sobre cada mascota cuidada y explica por qué es importante
        var nube = Caja("NubeConsejo", canvas, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(540, 260));
        var imgNube = Img(nube, Color.white, UI("Nube"));
        imgNube.raycastTarget = false;
        var etiqueta = Esquina("Etiqueta", nube, new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(380, 20));
        Txt(etiqueta, "CONSEJO DE LA DRA. LUCÍA", 14, TextAnchor.MiddleCenter, new Color(0.3f, 0.5f, 0.75f), true).GetComponent<Outline>().enabled = false;
        ui.textoTituloNube = Txt(Esquina("Titulo", nube, new Vector2(0.5f, 1), new Vector2(0, -46), new Vector2(420, 28)), "", 19, TextAnchor.MiddleCenter, new Color(0.85f, 0.3f, 0.43f), true);
        ui.textoTituloNube.GetComponent<Outline>().enabled = false;
        var cuerpoNube = Caja("Texto", nube, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        cuerpoNube.offsetMin = new Vector2(50, 30); cuerpoNube.offsetMax = new Vector2(-50, -76);
        ui.textoNube = Txt(cuerpoNube, "", 17, TextAnchor.UpperCenter, new Color(0.18f, 0.16f, 0.22f));
        ui.textoNube.GetComponent<Outline>().enabled = false;
        ui.nube = nube;

        // Minijuego de cuidado (curar, alimentar, bañar, adoptar)
        var mjRt = Esquina("PanelMiniJuego", canvas, new Vector2(0.5f, 0.5f), new Vector2(0, -46), new Vector2(820, 380));
        Img(mjRt, new Color(1f, 0.85f, 0.45f));                       // marco dorado
        Img(Estirar("Fondo", mjRt, 4f), new Color(0.07f, 0.09f, 0.17f, 1f));
        var mj = canvasGo.AddComponent<MiniJuego>();
        mj.panel = mjRt.gameObject;
        mj.textoTitulo = Txt(Esquina("Titulo", mjRt, new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(780, 38)), "", 30, TextAnchor.MiddleCenter, Dorado, true);
        mj.textoPaso = Txt(Esquina("Paso", mjRt, new Vector2(0.5f, 1), new Vector2(0, -56), new Vector2(780, 30)), "", 22, TextAnchor.MiddleCenter, new Color(0.6f, 0.88f, 1f), true);
        mj.textoInstruccion = Txt(Esquina("Instruccion", mjRt, new Vector2(0.5f, 1), new Vector2(0, -90), new Vector2(760, 54)), "", 19, TextAnchor.UpperCenter, Color.white);
        mj.textoCentro = Txt(Esquina("Centro", mjRt, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(760, 60)), "", 44, TextAnchor.MiddleCenter, Color.white, true);
        mj.textoOpciones = Txt(Esquina("Opciones", mjRt, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(620, 130)), "", 24, TextAnchor.UpperLeft, Color.white, true);
        var reloj = Esquina("Reloj", mjRt, new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(700, 12));
        Img(reloj, new Color(0f, 0f, 0f, 0.5f));
        mj.rellenoReloj = Estirar("Relleno", reloj);
        Img(mj.rellenoReloj, new Color(1f, 0.7f, 0.3f));
        mj.grupoReloj = reloj.gameObject;
        var barraMj = Esquina("Barra", mjRt, new Vector2(0.5f, 0), new Vector2(0, 92), new Vector2(640, 40));
        Img(barraMj, new Color(0.2f, 0.22f, 0.3f));
        mj.zona = Estirar("Zona", barraMj);
        Img(mj.zona, new Color(0.3f, 0.85f, 0.45f));
        mj.marcador = Caja("Marca", barraMj, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 58));
        Img(mj.marcador, new Color(1f, 0.85f, 0.2f));
        mj.marcador.gameObject.AddComponent<Outline>().effectColor = Color.black;
        mj.grupoBarra = barraMj.gameObject;
        var prog = Esquina("Progreso", mjRt, new Vector2(0.5f, 0), new Vector2(0, 92), new Vector2(640, 40));
        Img(prog, new Color(0.42f, 0.3f, 0.18f));
        mj.rellenoProgreso = Estirar("Limpio", prog);
        Img(mj.rellenoProgreso, new Color(0.45f, 0.75f, 1f));
        Txt(Estirar("Etiqueta", prog), "Barro  →  Limpio", 18, TextAnchor.MiddleCenter, Color.white, true);
        mj.grupoProgreso = prog.gameObject;
        mj.textoResultado = Txt(Esquina("Resultado", mjRt, new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(780, 44)), "", 20, TextAnchor.MiddleCenter, Color.white, true);
        mjRt.gameObject.SetActive(false);

        // Panel de fin de nivel
        var fin = Esquina("PanelFin", canvas, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780, 360));
        Img(fin, new Color(0.08f, 0.1f, 0.18f, 0.95f));
        lm.textoFin = Txt(Estirar("TextoFin", fin, 26f), "", 26, TextAnchor.MiddleCenter, Color.white, true);
        lm.panelFin = fin.gameObject;
    }

    static Button Boton(Transform padre, string texto, float y, float ancho = 300f)
    {
        var rt = Esquina("Boton" + texto, padre, new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(ancho, 58));
        rt.pivot = new Vector2(0.5f, 1f);
        var img = Img(rt, new Color(0.93f, 0.42f, 0.55f));
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var colores = b.colors;
        colores.highlightedColor = new Color(1f, 0.8f, 0.85f);
        colores.pressedColor = new Color(0.8f, 0.3f, 0.45f);
        b.colors = colores;
        Txt(Estirar("Texto", rt), texto, 26, TextAnchor.MiddleCenter, Color.white, true);
        return b;
    }

    static RectTransform PanelTexto(Transform canvas, string nombre, out Text texto, int tamTexto)
    {
        var p = Esquina(nombre, canvas, new Vector2(0.5f, 0.5f), new Vector2(0, -12), new Vector2(900, 620));
        Img(p, new Color(0.08f, 0.1f, 0.18f, 0.95f));
        var t = Caja("Texto", p, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        t.offsetMin = new Vector2(34, 84);
        t.offsetMax = new Vector2(-34, -24);
        texto = Txt(t, "", tamTexto, TextAnchor.UpperCenter, Color.white);
        return p;
    }

    static void CrearMenu(RuntimeAnimatorController[] ctrls, Dictionary<string, GameObject> prefabs, List<Skin> skins, List<Skin> gatos)
    {
        var escena = CopiarBase("Menu");
        var entorno = new GameObject("Entorno").transform;
        CrearFondo("Fondo_Barrio", entorno);
        var g = Sprites(GirlTex);

        // Personajes de adorno del menú principal
        var adorno = new GameObject("PersonajesMenu").transform;
        var chica = CrearRescatista(adorno, new Vector3(-3.9f, 0f, 0f), ctrls[0], g["Player_Rescatista_0"]);
        chica.GetComponent<SpriteRenderer>().flipX = false; // mira a la derecha, hacia los perritos
        var adornos = new[] { (-2.6f, skins[0], true), (2.4f, gatos[0], true), (3.9f, skins[3], false), (5.2f, gatos[2], false) };
        for (int i = 0; i < adornos.Length; i++)
        {
            var (x, skin, haciaDerecha) = adornos[i];
            var go = Instanciar(prefabs["Mascota"], adorno, new Vector3(x, 0f, 0f), "Mascota_" + (i + 1));
            var m = go.GetComponent<Mascota>();
            AplicarSkin(m, skin, Necesidad.Ninguna);
            m.framesEspera = skin.especie == Especie.Gato && i == 3 ? skin.quieta : skin.feliz;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = m.framesEspera[0];
            // los perros del pack miran a la izquierda y los gatos a la derecha
            sr.flipX = skin.miraIzq ? haciaDerecha : !haciaDerecha;
            m.alerta.SetActive(false);
            Registrar(m, sr, m.alerta);
        }

        // Las tres rescatistas para elegir
        var seleccion = new GameObject("PersonajesSeleccion").transform;
        string[] iniciales = { "Player_Rescatista_0", "Rescatista_Luna_0", "Rescatista_Mia_0" };
        string[] texturas = { GirlTex, "Assets/Sprites/Personajes/Rescatista_Luna.png", "Assets/Sprites/Personajes/Rescatista_Mia.png" };
        for (int i = 0; i < 3; i++)
        {
            var spr = Sprites(texturas[i])[iniciales[i]];
            var r = CrearRescatista(seleccion, new Vector3(-3.6f + i * 3.6f, 0f, 0f), ctrls[i], spr);
            r.name = "Rescatista_" + SeleccionPersonaje.Nombres[i];
            r.tag = "Untagged";
            r.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
        }

        var canvas = CrearCanvas("UI").transform;
        Img(Estirar("Oscurecer", canvas), new Color(0.05f, 0.05f, 0.12f, 0.35f));
        FranjaCreditos(canvas);

        // Menú principal
        var panelMenu = Estirar("PanelMenu", canvas);
        Txt(Esquina("Titulo", panelMenu, new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(1100, 100)), "PATITAS AL RESCATE", 70, TextAnchor.MiddleCenter, Dorado, true);
        Txt(Esquina("Subtitulo", panelMenu, new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(1000, 40)), "Rescata, cura, alimenta, baña y busca un hogar para las mascotas del barrio", 24, TextAnchor.MiddleCenter, Color.white);
        var bJugar = Boton(panelMenu, "Jugar", -200);
        var bNiveles = Boton(panelMenu, "Niveles", -266);
        var bCreditos = Boton(panelMenu, "Créditos", -332);
        var bSalir = Boton(panelMenu, "Salir", -398);
        Txt(Esquina("Atajos", panelMenu, new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(900, 30)), "Enter: jugar   ·   N: niveles   ·   C: créditos", 18, TextAnchor.MiddleCenter, Color.white);

        // Selección de rescatista
        var panelSel = Estirar("PanelSeleccion", canvas);
        Txt(Esquina("Titulo", panelSel, new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(1100, 80)), "ELIGE TU RESCATISTA", 54, TextAnchor.MiddleCenter, Dorado, true);
        Txt(Esquina("Ayuda", panelSel, new Vector2(0.5f, 1), new Vector2(0, -118), new Vector2(1000, 34)), "Haz clic en una tarjeta o pulsa 1, 2 o 3  ·  Esc: volver", 20, TextAnchor.MiddleCenter, Color.white);
        var botonesPersonaje = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            var card = Esquina("Tarjeta_" + SeleccionPersonaje.Nombres[i], panelSel, new Vector2(0.5f, 0), new Vector2(-430 + i * 430, 22), new Vector2(330, 150));
            card.pivot = new Vector2(0.5f, 0f);
            var img = Img(card, new Color(0.10f, 0.12f, 0.20f, 0.88f));
            var b = card.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var col = b.colors; col.highlightedColor = new Color(1f, 0.75f, 0.85f); b.colors = col;
            Txt(Esquina("Nombre", card, new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(300, 40)), (i + 1) + ". " + SeleccionPersonaje.Nombres[i], 30, TextAnchor.MiddleCenter, Dorado, true);
            Txt(Esquina("Descripcion", card, new Vector2(0.5f, 1), new Vector2(0, -56), new Vector2(300, 80)), SeleccionPersonaje.Descripciones[i], 20, TextAnchor.UpperCenter, Color.white);
            botonesPersonaje[i] = b;
        }

        // Niveles: muestra los 4 niveles, cuáles están completados y cuál sigue
        var panelNiv = Esquina("PanelNiveles", canvas, new Vector2(0.5f, 0.5f), new Vector2(0, -12), new Vector2(1060, 600));
        Img(panelNiv, new Color(0.08f, 0.1f, 0.18f, 0.95f));
        Txt(Esquina("Titulo", panelNiv, new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(900, 56)), "NIVELES", 46, TextAnchor.MiddleCenter, Dorado, true);
        var progreso = Txt(Esquina("Progreso", panelNiv, new Vector2(0.5f, 1), new Vector2(0, -74), new Vector2(900, 30)), "Progreso", 20, TextAnchor.MiddleCenter, Color.white);
        string[] descNiveles =
        {
            "Barrio de día.\nPerros y gatos heridos: recoge botiquines y cura a cada uno.",
            "Atardecer.\nMascotas con hambre: dales alimento adecuado para perro o para gato.",
            "Noche.\nMascotas sucias y con moscas: báñalas con jabón para mascotas.",
            "Feria.\nEntrega a cada mascota con su collar a una familia responsable.",
        };
        string[] iconosNiveles = { "Icono_Herida", "Icono_Hambre", "Icono_Sucio", "Icono_Casa" };
        var tarjetas = new Image[4]; var estados = new Text[4]; var botonesNivel = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            var card = Esquina("Nivel_" + (i + 1), panelNiv, new Vector2(0.5f, 1), new Vector2(-390 + i * 260, -124), new Vector2(240, 340));
            card.pivot = new Vector2(0.5f, 1f);
            tarjetas[i] = Img(card, new Color(0.15f, 0.15f, 0.2f, 0.85f));
            var b = card.gameObject.AddComponent<Button>();
            b.targetGraphic = tarjetas[i];
            var col = b.colors; col.highlightedColor = new Color(1f, 0.85f, 0.9f); col.disabledColor = new Color(0.6f, 0.6f, 0.6f); b.colors = col;
            botonesNivel[i] = b;
            Txt(Esquina("Numero", card, new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(220, 40)), "NIVEL " + (i + 1), 30, TextAnchor.MiddleCenter, Dorado, true);
            var ic = Esquina("Icono", card, new Vector2(0.5f, 1), new Vector2(0, -54), new Vector2(54, 60));
            ic.pivot = new Vector2(0.5f, 1f);
            Img(ic, Color.white, UI(iconosNiveles[i])).preserveAspect = true;
            Txt(Esquina("Nombre", card, new Vector2(0.5f, 1), new Vector2(0, -122), new Vector2(226, 32)), LevelManager.NombresNiveles[i], 22, TextAnchor.MiddleCenter, Color.white, true);
            Txt(Esquina("Descripcion", card, new Vector2(0.5f, 1), new Vector2(0, -158), new Vector2(214, 120)), descNiveles[i], 16, TextAnchor.UpperCenter, new Color(0.92f, 0.92f, 0.95f));
            estados[i] = Txt(Esquina("Estado", card, new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(226, 30)), "", 17, TextAnchor.MiddleCenter, Dorado, true);
        }
        Txt(Esquina("Ayuda", panelNiv, new Vector2(0.5f, 0), new Vector2(0, 92), new Vector2(940, 30)),
            "Haz clic en un nivel disponible o pulsa su número · Esc: volver", 18, TextAnchor.MiddleCenter, Color.white);
        var bVolverC = Boton(panelNiv, "Volver", -528);

        // Créditos
        Text textoCreditos;
        var panelCred = PanelTexto(canvas, "PanelCreditos", out textoCreditos, 21);
        textoCreditos.text = Creditos.Completo;
        var bVolver = Boton(panelCred, "Volver", -548);

        CrearSonidos();
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        es.transform.SetAsLastSibling();

        var mm = new GameObject("MenuManager").AddComponent<MenuPrincipal>();
        mm.panelMenu = panelMenu.gameObject;
        mm.panelSeleccion = panelSel.gameObject;
        mm.panelNiveles = panelNiv.gameObject;
        mm.tarjetasNivel = tarjetas;
        mm.estadosNivel = estados;
        mm.botonesNivel = botonesNivel;
        mm.textoProgreso = progreso;
        mm.panelCreditos = panelCred.gameObject;
        mm.personajesMenu = adorno.gameObject;
        mm.personajesSeleccion = seleccion.gameObject;
        mm.textoCreditos = textoCreditos;
        UnityEventTools.AddPersistentListener(bJugar.onClick, new UnityAction(mm.Jugar));
        UnityEventTools.AddPersistentListener(bNiveles.onClick, new UnityAction(mm.VerNiveles));
        for (int i = 0; i < 4; i++)
            UnityEventTools.AddIntPersistentListener(botonesNivel[i].onClick, new UnityAction<int>(mm.JugarNivel), i + 1);
        UnityEventTools.AddPersistentListener(bCreditos.onClick, new UnityAction(mm.VerCreditos));
        UnityEventTools.AddPersistentListener(bSalir.onClick, new UnityAction(mm.Salir));
        UnityEventTools.AddPersistentListener(bVolver.onClick, new UnityAction(mm.Volver));
        UnityEventTools.AddPersistentListener(bVolverC.onClick, new UnityAction(mm.Volver));
        for (int i = 0; i < 3; i++)
            UnityEventTools.AddIntPersistentListener(botonesPersonaje[i].onClick, new UnityAction<int>(mm.Elegir), i);

        panelSel.gameObject.SetActive(false);
        panelNiv.gameObject.SetActive(false);
        panelCred.gameObject.SetActive(false);
        seleccion.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
    }
}
