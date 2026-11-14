using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using UnityEngine.Rendering;

// Funciones compartidas por los menus de particulas (3. Niebla, 4. Luciernagas, 5. Petalos).
// No tiene menu propio.
//
// Hace tres cosas:
//   1) dibuja por codigo las texturas que faltaban (copo de niebla, punto de luz, petalo)
//      y las guarda como PNG en Assets/GreenMist/Textures;
//   2) crea los materiales de particulas de URP en Assets/GreenMist/Materials;
//   3) arma los emisores (ParticleSystem) con los valores que comparten todos.
public static class GreenMistParticulas
{
    public const string CarpetaTexturas = GreenMistEditorUtil.CarpetaGenerados + "/Textures";
    public const string CarpetaMateriales = GreenMistEditorUtil.CarpetaGenerados + "/Materials";

    const string ShaderSinLuz = "Universal Render Pipeline/Particles/Unlit";
    const string ShaderConLuz = "Universal Render Pipeline/Particles/Simple Lit";

    // Orden de dibujado entre las tres capas (se suma a la cola "Transparent" = 3000).
    // Niebla primero, petalos encima, luciernagas al final. Todas despues del agua del estanque (3000).
    public const int ColaNiebla = 1;
    public const int ColaPetalos = 2;
    public const int ColaLuciernagas = 3;

    // Altura del suelo plano del pueblo (sando, calle, estanque), en coordenadas locales de "Assets".
    public const float SueloLocalY = -30f;

    // =====================================================================
    //  1) TEXTURAS
    // =====================================================================

    // Copo de niebla: una mancha blanca redonda, mas densa al centro y con "nubes" de ruido.
    public static Texture2D TexturaNiebla()
    {
        const string nombre = "GM_Niebla";
        Texture2D ya = CargarTextura(nombre);
        if (ya != null) return ya;

        const int n = 128;
        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                // u, v van de -1 a 1; r es la distancia al centro (1 = borde).
                float u = (x + 0.5f) / n * 2f - 1f;
                float v = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);

                // 1 en el centro, 0 en el borde, con caida suave.
                float caida = Mathf.Pow(1f - Mathf.SmoothStep(0f, 1f, r), 1.4f);

                // Dos capas de ruido Perlin para que no sea un circulo perfecto.
                float ruido = 0.65f * Mathf.PerlinNoise(x / (float)n * 3f + 11.3f, y / (float)n * 3f + 4.7f)
                            + 0.35f * Mathf.PerlinNoise(x / (float)n * 7f + 31.1f, y / (float)n * 7f + 17.9f);
                float nubes = Mathf.Lerp(0.45f, 1f, Mathf.InverseLerp(0.25f, 0.75f, ruido));

                px[y * n + x] = new Color(1f, 1f, 1f, caida * nubes);
            }
        }
        return GuardarTextura(nombre, n, px);
    }

    // Punto de luz: nucleo chico y brillante con un halo tenue alrededor.
    public static Texture2D TexturaPunto()
    {
        const string nombre = "GM_Punto";
        Texture2D ya = CargarTextura(nombre);
        if (ya != null) return ya;

        const int n = 64;
        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f;
                float v = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);

                float nucleo = Mathf.Exp(-(r * r) / (0.16f * 0.16f));
                float halo = 0.30f * Mathf.Exp(-(r * r) / (0.45f * 0.45f));
                float borde = 1f - Mathf.SmoothStep(0.75f, 1f, r);   // llega a 0 justo en el borde

                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Max(nucleo, halo) * borde));
            }
        }
        return GuardarTextura(nombre, n, px);
    }

    // Petalo de sakura: forma de gota, con la muesca tipica en la punta.
    public static Texture2D TexturaPetalo()
    {
        const string nombre = "GM_Petalo";
        Texture2D ya = CargarTextura(nombre);
        if (ya != null) return ya;

        const int n = 64;
        const int muestras = 4;   // 4x4 muestras por pixel para que el borde no quede dentado
        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                int adentro = 0;
                for (int sy = 0; sy < muestras; sy++)
                {
                    for (int sx = 0; sx < muestras; sx++)
                    {
                        float u = (x + (sx + 0.5f) / muestras) / n * 2f - 1f;
                        float v = (y + (sy + 0.5f) / muestras) / n * 2f - 1f;
                        if (DentroDelPetalo(u, v)) adentro++;
                    }
                }
                float cobertura = adentro / (float)(muestras * muestras);

                // Un poco mas rosado en la base, casi blanco en la punta.
                float t = Mathf.Clamp01((y + 0.5f) / n);
                Color tono = Color.Lerp(new Color(1f, 0.84f, 0.89f), Color.white, t);
                tono.a = cobertura;
                px[y * n + x] = tono;
            }
        }
        return GuardarTextura(nombre, n, px);
    }

    // u, v de -1 a 1. La base del petalo esta abajo (v = -1) y la punta arriba (v = 1).
    static bool DentroDelPetalo(float u, float v)
    {
        // El petalo ocupa de v = -0.9 a v = 0.9: queda un margen transparente contra el borde de la textura.
        float t = (v / 0.9f + 1f) * 0.5f;                  // 0 en la base, 1 en la punta
        if (t <= 0f || t >= 1f) return false;

        // Ancho del petalo a cada altura: angosto en la base, panzon cerca de la punta.
        float ancho = 0.62f * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(t, 1.3f)), 0.8f);
        if (Mathf.Abs(u) > ancho) return false;

        // La muesca: un cortecito en "V" en la punta.
        if (t > 0.84f && Mathf.Abs(u) < (t - 0.84f) * 1.1f) return false;

        return true;
    }

    static string RutaTextura(string nombre)
    {
        return CarpetaTexturas + "/" + nombre + ".png";
    }

    // Devuelve la textura si ya existe en el proyecto (asi no se vuelve a generar).
    static Texture2D CargarTextura(string nombre)
    {
        string ruta = RutaTextura(nombre);
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        if (tex != null) return tex;

        // El archivo esta en el disco pero Unity todavia no lo importo.
        if (File.Exists(RutaEnDisco(ruta)))
        {
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }
        return tex;
    }

    // "Assets/GreenMist/..." -> ruta completa en el disco.
    static string RutaEnDisco(string rutaDeAsset)
    {
        // Application.dataPath termina en ".../Assets"; le sacamos ese "Assets" repetido.
        return Application.dataPath + rutaDeAsset.Substring("Assets".Length);
    }

    // Escribe los pixeles como PNG, lo importa y le pone los ajustes de importacion.
    static Texture2D GuardarTextura(string nombre, int lado, Color[] pixeles)
    {
        GreenMistEditorUtil.AsegurarCarpeta(CarpetaTexturas);
        string ruta = RutaTextura(nombre);

        Texture2D temporal = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
        temporal.SetPixels(pixeles);
        temporal.Apply();
        byte[] png = temporal.EncodeToPNG();
        Object.DestroyImmediate(temporal);

        File.WriteAllBytes(RutaEnDisco(ruta), png);
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);

        TextureImporter importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importador != null)
        {
            importador.textureType = TextureImporterType.Default;
            importador.sRGBTexture = true;
            importador.alphaSource = TextureImporterAlphaSource.FromInput;
            importador.alphaIsTransparency = true;                 // evita bordes oscuros
            importador.wrapMode = TextureWrapMode.Clamp;           // que no se repita en los bordes
            importador.mipmapEnabled = true;                       // que no titile de lejos
            importador.filterMode = FilterMode.Bilinear;
            importador.textureCompression = TextureImporterCompression.Uncompressed; // son chicas: sin bandas
            importador.SaveAndReimport();
        }

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        if (tex == null) Debug.LogError("[Green Mist] No pude crear la textura " + ruta);
        return tex;
    }

    // =====================================================================
    //  2) MATERIALES
    // =====================================================================

    // Crea (o actualiza) un material TRANSPARENTE de particulas de URP en Assets/GreenMist/Materials.
    //
    //   conLuz       false = Particles/Unlit (no le afectan las luces). true = Particles/Simple Lit (los faroles lo iluminan).
    //   aditivo      true = suma luz (luciernagas). false = transparencia normal (niebla, petalos).
    //   dobleCara    true = se ve tambien de atras (petalos que giran).
    //   suaveLejos   "soft particles": metros en los que la particula se desvanece al tocar el suelo o una pared. 0 = apagado.
    //   camaraCerca / camaraLejos   la particula es invisible a menos de "cerca" metros de la camara
    //                               y se ve entera a partir de "lejos". 0 y 0 = apagado.
    //   cola         orden de dibujado (ver las constantes Cola... de arriba).
    //
    // IMPORTANTE: los shaders de particulas de URP no se arreglan solos al cambiar estos numeros.
    // Hay que llamar a BaseShaderGUI.SetMaterialKeywords (lo mismo que hace el Inspector del material):
    // es lo que pasa el material a "Transparent" y calcula los parametros ocultos del desvanecido.
    // Sin esa llamada las particulas salen invisibles u opacas.
    public static Material MaterialParticulas(string nombre, bool conLuz, Texture2D textura, Color color,
                                              bool aditivo, bool dobleCara, float suaveLejos,
                                              float camaraCerca, float camaraLejos, int cola)
    {
        string nombreShader = conLuz ? ShaderConLuz : ShaderSinLuz;
        Shader shader = Shader.Find(nombreShader);
        if (shader == null)
        {
            Debug.LogError("[Green Mist] No encontré el shader '" + nombreShader + "'. ¿Está instalado URP?");
            return null;
        }

        GreenMistEditorUtil.AsegurarCarpeta(CarpetaMateriales);
        string ruta = CarpetaMateriales + "/" + nombre + ".mat";

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        bool esNuevo = mat == null;
        if (esNuevo) mat = new Material(shader);
        else if (mat.shader != shader) mat.shader = shader;

        // ---- Superficie ----
        mat.SetFloat("_Surface", 1f);                    // 1 = Transparent
        mat.SetFloat("_Blend", aditivo ? 2f : 0f);       // 0 = Alpha, 2 = Additive
        mat.SetFloat("_AlphaClip", 0f);
        mat.SetFloat("_Cull", dobleCara ? 0f : 2f);      // 0 = las dos caras, 2 = solo el frente
        mat.SetFloat("_ColorMode", 0f);                  // 0 = Multiply: color del material x color de la particula
        mat.SetFloat("_FlipbookBlending", 0f);
        mat.SetFloat("_DistortionEnabled", 0f);
        mat.SetFloat("_QueueOffset", cola);

        // ---- Textura y color ----
        mat.SetTexture("_BaseMap", textura);
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_EmissionColor", Color.black);

        // ---- Desvanecido contra el suelo (soft particles) ----
        bool suave = suaveLejos > 0f;
        mat.SetFloat("_SoftParticlesEnabled", suave ? 1f : 0f);
        mat.SetFloat("_SoftParticlesNearFadeDistance", 0f);
        mat.SetFloat("_SoftParticlesFarFadeDistance", suave ? suaveLejos : 1f);

        // ---- Desvanecido cerca de la camara ----
        bool fundeCamara = camaraLejos > camaraCerca;
        mat.SetFloat("_CameraFadingEnabled", fundeCamara ? 1f : 0f);
        mat.SetFloat("_CameraNearFadeDistance", fundeCamara ? camaraCerca : 1f);
        mat.SetFloat("_CameraFarFadeDistance", fundeCamara ? camaraLejos : 2f);

        if (conLuz)
        {
            // Petalos mate: sin brillo especular y sin recibir sombras (son muy chicos para notarlo).
            mat.SetFloat("_SpecularHighlights", 1f);     // 1 = NoSpecular en SimpleLitGUI
            mat.SetColor("_SpecColor", new Color(0f, 0f, 0f, 1f));
            mat.SetFloat("_Smoothness", 0f);
            mat.SetFloat("_ReceiveShadows", 0f);

            BaseShaderGUI.SetMaterialKeywords(mat, SimpleLitGUI.SetMaterialKeywords, ParticleGUI.SetMaterialKeywords);
        }
        else
        {
            BaseShaderGUI.SetMaterialKeywords(mat, null, ParticleGUI.SetMaterialKeywords);
        }

        // Control: si esto falla, las particulas no se verian. Mejor avisar que quedarse mirando la nada.
        if (mat.GetTag("RenderType", false) != "Transparent")
            Debug.LogWarning("[Green Mist] El material " + nombre + " no quedó transparente. Abrilo en el Inspector y poné Surface Type = Transparent.");
        if ((suave || fundeCamara) && !mat.IsKeywordEnabled("_FADING_ON"))
            Debug.LogWarning("[Green Mist] El material " + nombre + " quedó sin desvanecido (_FADING_ON apagado).");

        if (esNuevo) AssetDatabase.CreateAsset(mat, ruta);
        else EditorUtility.SetDirty(mat);

        return mat;
    }

    // =====================================================================
    //  3) EMISORES
    // =====================================================================

    // Busca (o crea) un hijo de "grupo" con un ParticleSystem y lo deja frenado y vacio,
    // listo para ponerle valores. Si ya existia, se reutiliza: nunca se duplica.
    public static ParticleSystem PrepararEmisor(Transform grupo, string nombre, List<string> usados)
    {
        GameObject go = GreenMistEditorUtil.BuscarOCrearHijo(grupo, nombre);
        ParticleSystem ps = GreenMistEditorUtil.ObtenerOAgregar<ParticleSystem>(go);
        ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();   // viene junto con el ParticleSystem

        Undo.RecordObject(go.transform, "Green Mist: emisor " + nombre);
        Undo.RecordObject(ps, "Green Mist: emisor " + nombre);
        if (rend != null) Undo.RecordObject(rend, "Green Mist: emisor " + nombre);

        // Hay valores (como la duracion) que Unity no deja cambiar mientras el sistema esta andando.
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        if (usados != null) usados.Add(nombre);
        return ps;
    }

    // Valores que comparten todos los emisores:
    //   - en loop y con prewarm: el juego arranca en el menu con el tiempo frenado, y con prewarm
    //     las particulas ya estan repartidas desde el primer frame;
    //   - espacio de simulacion World: las particulas no se mueven si se mueve el emisor;
    //   - un tope bajo y explicito de particulas (maxParticles), por rendimiento.
    public static void ConfigurarBase(ParticleSystem ps, int cantidad, float vidaMin, float vidaMax)
    {
        ParticleSystem.MainModule main = ps.main;
        main.duration = vidaMax;                 // prewarm simula una vuelta entera: con esto ya queda lleno
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.startDelay = 0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(vidaMin, vidaMax);
        main.startSpeed = 0f;                    // el movimiento lo dan "Velocity over Lifetime" y "Noise"
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = cantidad;

        // Las que nacen por segundo = cuantas queremos vivas / cuanto vive cada una.
        ParticleSystem.EmissionModule emision = ps.emission;
        emision.enabled = true;
        emision.rateOverTime = cantidad / ((vidaMin + vidaMax) * 0.5f);
        emision.rateOverDistance = 0f;
        emision.burstCount = 0;

        // Modulos que no usamos. El de luces a proposito: la escena ya tiene 51 luces de verdad.
        ParticleSystem.LightsModule luces = ps.lights;
        luces.enabled = false;
        ParticleSystem.CollisionModule choque = ps.collision;
        choque.enabled = false;
        ParticleSystem.TrailModule estelas = ps.trails;
        estelas.enabled = false;
    }

    // Las particulas nacen en cualquier punto de una caja de este tamaño (en metros), centrada en el emisor.
    public static void FormaCaja(ParticleSystem ps, Vector3 tamano)
    {
        ParticleSystem.ShapeModule forma = ps.shape;
        forma.enabled = true;
        forma.shapeType = ParticleSystemShapeType.Box;
        forma.position = Vector3.zero;
        forma.rotation = Vector3.zero;
        forma.scale = tamano;
        forma.randomDirectionAmount = 0f;
        forma.randomPositionAmount = 0f;
    }

    // Velocidad constante en el mundo (m/s), al azar entre un minimo y un maximo por eje.
    // Unity exige que los tres ejes usen el mismo "modo": por eso los tres van como (min, max).
    public static void Velocidad(ParticleSystem ps, Vector3 minimo, Vector3 maximo)
    {
        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(minimo.x, maximo.x);
        vel.y = new ParticleSystem.MinMaxCurve(minimo.y, maximo.y);
        vel.z = new ParticleSystem.MinMaxCurve(minimo.z, maximo.z);
        vel.enabled = true;
    }

    // Vaiven suave (ruido): cada particula se desvia un poco de su camino, como llevada por el aire.
    public static void Ruido(ParticleSystem ps, Vector3 fuerza, float frecuencia, float velocidadDeCambio)
    {
        ParticleSystem.NoiseModule ruido = ps.noise;
        ruido.separateAxes = true;
        ruido.strengthX = fuerza.x;
        ruido.strengthY = fuerza.y;
        ruido.strengthZ = fuerza.z;
        ruido.frequency = frecuencia;
        ruido.scrollSpeed = velocidadDeCambio;
        ruido.damping = true;
        ruido.octaveCount = 1;
        ruido.quality = ParticleSystemNoiseQuality.Medium;
        ruido.enabled = true;
    }

    // "Color over Lifetime" solo de transparencia: la particula aparece de a poco, se mantiene
    // y se va de a poco. "entra" y "sale" son momentos de su vida entre 0 y 1.
    public static void Desvanecer(ParticleSystem ps, float entra, float sale)
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, entra),
                new GradientAlphaKey(1f, sale),
                new GradientAlphaKey(0f, 1f)
            });

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(g);
    }

    // El renderer: cartelitos (billboards) que miran a la camara, con nuestro material.
    //   tamanoMaxEnPantalla: tope del tamaño de una particula como fraccion de la pantalla.
    //   El valor por defecto de Unity (0.5) achica de golpe los copos grandes cuando te acercas.
    public static void ConfigurarRenderer(ParticleSystem ps, Material mat, float tamanoMaxEnPantalla)
    {
        ParticleSystemRenderer rend = ps.GetComponent<ParticleSystemRenderer>();
        if (rend == null) return;

        rend.enabled = true;
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.alignment = ParticleSystemRenderSpace.View;
        rend.sharedMaterial = mat;
        rend.trailMaterial = null;
        rend.sortMode = ParticleSystemSortMode.None;
        rend.flip = Vector3.zero;
        rend.allowRoll = false;                 // que no giren si la camara se inclina
        rend.minParticleSize = 0f;
        rend.maxParticleSize = tamanoMaxEnPantalla;
        rend.shadowCastingMode = ShadowCastingMode.Off;
        rend.receiveShadows = false;

        EditorUtility.SetDirty(rend);
        EditorUtility.SetDirty(ps);
    }

    // Deja una "foto" de las particulas a la vista en modo edicion, apenas termina el menu.
    // Sin esto, fuera de Play un ParticleSystem solo se ve mientras lo tenes seleccionado.
    // Es solo una vista previa quieta: no se guarda en la escena, y en Play se mueven de verdad.
    public static void VistaPrevia(Transform grupo)
    {
        foreach (ParticleSystem ps in grupo.GetComponentsInChildren<ParticleSystem>())
        {
            // Adelanta el sistema una vuelta entera (desde cero) y lo deja en pausa, ya lleno.
            ps.Simulate(ps.main.duration, false, true);
        }
    }

    // Borra los hijos de "grupo" cuyo nombre empieza con "prefijo" y que esta corrida no uso
    // (por ejemplo, emisores viejos de una version anterior del menu). Devuelve cuantos borro.
    public static int BorrarSobrantes(Transform grupo, string prefijo, List<string> usados)
    {
        int borrados = 0;
        for (int i = grupo.childCount - 1; i >= 0; i--)
        {
            Transform hijo = grupo.GetChild(i);
            if (!hijo.name.StartsWith(prefijo)) continue;
            if (usados.Contains(hijo.name)) continue;

            Undo.DestroyObjectImmediate(hijo.gameObject);
            borrados++;
        }
        return borrados;
    }

    // =====================================================================
    //  4) BUSCAR COSAS DE LA ESCENA
    // =====================================================================

    // Los arboles de sakura: hijos directos de "Assets" cuyo nombre empieza con "SAKURA-".
    public static List<Transform> BuscarSakuras(Transform aldea)
    {
        List<Transform> lista = new List<Transform>();
        for (int i = 0; i < aldea.childCount; i++)
        {
            Transform hijo = aldea.GetChild(i);
            if (hijo.name.StartsWith("SAKURA-") && hijo.gameObject.activeInHierarchy) lista.Add(hijo);
        }
        return lista;
    }

    // La caja (en coordenadas de MUNDO) que envuelve todo lo que se dibuja de un objeto.
    // Se usa en vez del pivote porque los sakuras tienen el pivote hundido y escalas muy distintas.
    public static bool LimitesDe(Transform objeto, out Bounds limites)
    {
        limites = new Bounds(objeto.position, Vector3.zero);
        bool hay = false;

        foreach (Renderer r in objeto.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;   // solo la malla del arbol
            if (!hay) { limites = r.bounds; hay = true; }
            else limites.Encapsulate(r.bounds);
        }
        return hay;
    }

    // Y de MUNDO del suelo plano del pueblo.
    public static float SueloPlanoMundoY(Transform aldea)
    {
        return aldea.TransformPoint(new Vector3(0f, SueloLocalY, 0f)).y;
    }
}
