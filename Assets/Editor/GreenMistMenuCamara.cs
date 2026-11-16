using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Menu "Green Mist > 11. Cámara del menú de inicio".
//
// Arma el RECORRIDO DE CAMARA del menu de inicio: en vez de quedarse quieta
// detras de Onji, la camara pasea despacio por cinco tomas del pueblo.
//
//   GM_MenuCamara                         (objeto raiz)            -> MenuCameraTour
//     Toma 1 - El pueblo desde arriba
//       T1 Inicio                         (donde empieza la camara)
//       T1 Fin                            (donde termina)
//     Toma 2 - El pasillo de casas
//     Toma 3 - El estanque y el puente
//     Toma 4 - El camino al santuario
//     Toma 5 - El templo
//
// Cada punto es un objeto vacio: su posicion es donde esta la camara y su
// rotacion es hacia donde mira. Las posiciones "de fabrica" se calculan en el
// momento, midiendo los objetos de verdad (las casas, el estanque, los faroles
// de piedra, el torii, el templo) y la altura del terreno.
//
// SE PUEDE CORRER LAS VECES QUE QUIERAS. Los puntos se escriben SOLO AL CREARLOS:
// si ya moviste uno, correr el menu de nuevo NO lo toca. Las conexiones (el
// panel del menu, la camara) si se rehacen siempre. Lo que falte se vuelve a
// crear. No le cambies el "Toma N" del principio a los nombres: asi se reconocen.
//
// COMO MOVER UNA TOMA: elegi el punto (por ejemplo "T3 Inicio"), lleva la Scene
// view a donde te guste y usa GameObject > Align With View (Ctrl+Shift+F).
//
// PARA VOLVER A LAS POSICIONES DE FABRICA: el boton "Rehacer las tomas de
// fábrica" que aparece en el Inspector de GM_MenuCamara (pregunta antes).
//
// Ademas este menu pone en tiempo real las particulas del ambiente (niebla,
// luciernagas, petalos) que ya estan en la escena, para que se muevan con el
// juego frenado en el menu. No hace falta volver a correr los menus 3, 4 y 5.
//
// Para volver a la camara quieta: Green Mist > 11b.
public static class GreenMistMenuCamara
{
    public const string NombreRaiz = "GM_MenuCamara";
    const string TituloUndo = "Green Mist: cámara del menú";

    const string NombrePanelMenu = "MainMenuPanel";
    const string NombreCamara = "Main Camera";

    // Las cinco tomas de fabrica, en el orden en que se recorren.
    static readonly string[] NombresDeToma =
    {
        "Toma 1 - El pueblo desde arriba",
        "Toma 2 - El pasillo de casas",
        "Toma 3 - El estanque y el puente",
        "Toma 4 - El camino al santuario",
        "Toma 5 - El templo",
    };

    // Segundos que dura cada una (despues se cambian en el Inspector, campo Duration).
    static readonly float[] DuracionDeToma = { 22f, 20f, 20f, 22f, 24f };

    // El texto del menu va en el tercio IZQUIERDO de la pantalla. Por eso la
    // camara no mira derecho a lo que muestra: mira unos grados a la izquierda,
    // y asi lo que muestra queda corrido hacia la derecha.
    const float CorridoPueblo = 10f;
    const float CorridoCalle = 10f;
    const float CorridoEstanque = 9f;
    const float CorridoSando = 8f;
    const float CorridoTemplo = 12f;

    // ---------- Medidas de cada toma (en metros) ----------
    const float PuebloAltura = 27f;        // altura de la camara sobre la calle
    const float PuebloAlCostado = 6f;      // cuanto se corre al costado de la calle
    const float PuebloAvance = 15f;        // cuanto avanza en toda la toma
    const float PuebloMiraAdelante = 62f;  // a que distancia adelante apunta (con la altura da unos 23 grados hacia abajo)

    const float CalleAlturaOjos = 1.65f;   // altura de la cabeza de una persona
    const float CalleAvance = 19f;         // con 20 segundos da paso lento (algo menos de 1 m/s)

    const float EstanqueDesdeLaOrilla = 5.5f;  // a cuanto de la orilla se para la camara
    const float EstanqueAltura = 1.3f;         // sobre el agua
    const float EstanqueAlCostado = 4.5f;      // se mueve esto para cada lado

    const float SandoAltura = 2.1f;
    const float SandoAvance = 26f;

    const float TemploDistanciaMinima = 45f;

    // Lo que se calcula para cada toma de fabrica.
    class TomaDeFabrica
    {
        public bool lista;               // se pudo calcular
        public string falta = "";        // si no: que fue lo que no se encontro
        public Vector3 inicioPos, finPos;
        public Quaternion inicioRot, finRot;
    }

    // ==================================================================
    //  LOS MENUS
    // ==================================================================

    [MenuItem("Green Mist/11. Cámara del menú de inicio", false, 114)]
    public static void Aplicar()
    {
        Armar(false);
    }

    // Lo llama el boton "Rehacer las tomas de fábrica" del Inspector de GM_MenuCamara.
    // Hace lo mismo que el menu 11, pero ademas vuelve a poner TODOS los puntos
    // en su posicion de fabrica (pisa lo que hayas movido; se deshace con Ctrl+Z).
    public static void RehacerTomasDeFabrica()
    {
        Armar(true);
    }

    // Apaga el recorrido: la camara del menu vuelve a quedar quieta detras de
    // Onji, como antes. No borra nada (los puntos que acomodaste quedan guardados).
    [MenuItem("Green Mist/11b. Volver a la cámara fija del menú", false, 115)]
    public static void Restaurar()
    {
        if (!GreenMistEditorUtil.PuedeEditarEscena()) return;

        GameObject raiz = GreenMistEditorUtil.BuscarRaiz(NombreRaiz);
        if (raiz == null)
        {
            Debug.Log("[Green Mist] No hay ningún '" + NombreRaiz + "' en la escena: la cámara del menú ya es la fija. No cambié nada.");
            return;
        }

        if (raiz.activeSelf)
        {
            Undo.RecordObject(raiz, TituloUndo + " (volver a la fija)");
            raiz.SetActive(false);
            GreenMistEditorUtil.MarcarEscenaSucia();
        }

        Debug.Log("[Green Mist] Cámara fija otra vez: apagué '" + NombreRaiz + "' (no se borró nada, tus tomas quedan guardadas). "
                  + "La niebla, las luciérnagas, los pétalos y los faroles se siguen moviendo en el menú: eso no cambia. "
                  + "Para volver al recorrido: Green Mist > 11. Guardá la escena con Ctrl+S.");
    }

    static void Armar(bool rehacer)
    {
        if (!GreenMistEditorUtil.PuedeEditarEscena()) return;

        Transform aldea = GreenMistEditorUtil.Aldea();
        if (aldea == null) return;

        Undo.SetCurrentGroupName(TituloUndo);

        // ---------- 1) Lo que ya hay en la escena ----------
        Camera camara = BuscarCamaraPrincipal();
        GameObject panelMenu = BuscarPanelMenu();

        // ---------- 2) El objeto raiz y el script del recorrido ----------
        GameObject raiz = GreenMistEditorUtil.BuscarOCrearRaiz(NombreRaiz);
        if (!raiz.activeSelf)
        {
            // "11b" lo deja apagado.
            Undo.RecordObject(raiz, TituloUndo);
            raiz.SetActive(true);
        }

        MenuCameraTour recorrido = GreenMistEditorUtil.ObtenerOAgregar<MenuCameraTour>(raiz);
        Undo.RecordObject(recorrido, TituloUndo);
        if (!recorrido.enabled) recorrido.enabled = true;
        if (panelMenu != null) recorrido.mainMenuPanel = panelMenu;
        if (camara != null) recorrido.targetCamera = camara;

        // ---------- 3) Las posiciones de fabrica, medidas sobre la escena ----------
        TomaDeFabrica[] fabrica = CalcularTomas(aldea);

        // ---------- 4) Los objetos de cada toma (los puntos, solo al crearlos) ----------
        Transform[] inicios = new Transform[NombresDeToma.Length];
        Transform[] fines = new Transform[NombresDeToma.Length];
        int creadas = 0, recolocadas = 0, respetadas = 0;

        for (int i = 0; i < NombresDeToma.Length; i++)
        {
            int numero = i + 1;
            Transform objetoToma = BuscarToma(raiz.transform, numero);

            if (!fabrica[i].lista)
            {
                Debug.LogWarning("[Green Mist] Toma " + numero + ": " + fabrica[i].falta
                                 + (objetoToma == null ? " No armé esa toma (el recorrido sigue con las demás)."
                                                       : " La toma ya existía y la dejé como estaba."));
            }

            // Sin posicion de fabrica y sin objeto: no hay con que armarla.
            if (objetoToma == null && !fabrica[i].lista) continue;

            if (objetoToma == null)
                objetoToma = GreenMistEditorUtil.BuscarOCrearHijo(raiz.transform, NombresDeToma[i]).transform;

            bool inicioNuevo, finNuevo;
            inicios[i] = Punto(objetoToma, "T" + numero + " Inicio", true, fabrica[i].lista, out inicioNuevo);
            fines[i] = Punto(objetoToma, "T" + numero + " Fin", false, fabrica[i].lista, out finNuevo);

            if (fabrica[i].lista)
            {
                bool escribirInicio = inicioNuevo || rehacer;
                bool escribirFin = finNuevo || rehacer;
                if (escribirInicio) Colocar(inicios[i], fabrica[i].inicioPos, fabrica[i].inicioRot);
                if (escribirFin) Colocar(fines[i], fabrica[i].finPos, fabrica[i].finRot);

                if (inicioNuevo && finNuevo) creadas++;
                else if (escribirInicio || escribirFin) recolocadas++;
                else respetadas++;

                // Solo se revisan los puntos que escribio este menu: los que acomodaste vos, no.
                if (escribirInicio || escribirFin)
                    AvisarSiQuedaAdentro(numero, inicios[i].position, fines[i].position);
            }
            else
            {
                respetadas++;
            }
        }

        // ---------- 5) La lista de tomas del script (solo se agrega lo que falta) ----------
        AnotarTomas(recorrido, inicios, fines);
        EditorUtility.SetDirty(recorrido);

        // ---------- 6) El ambiente en tiempo real ----------
        // Solo lo que armaron los menus 3, 4 y 5 (cuelga de Assets/GreenMist_Ambiente).
        Transform ambiente = aldea.Find(GreenMistEditorUtil.NombreAmbiente);
        int particulas = GreenMistParticulas.PonerTiempoReal(ambiente);

        // ---------- 7) Chequeos ----------
        if (panelMenu == null && recorrido.mainMenuPanel == null)
            Debug.LogWarning("[Green Mist] No encontré '" + NombrePanelMenu + "' (el panel que prende MainMenuUI). Sin él el recorrido no sabe "
                             + "cuándo se está en el menú y no hace nada. Arrastralo al campo Main Menu Panel de '" + NombreRaiz + "'.");

        if (camara == null && recorrido.targetCamera == null)
            Debug.LogWarning("[Green Mist] No encontré la cámara principal ('" + NombreCamara + "' o una con el tag MainCamera). "
                             + "Jugando se usa la que tenga ese tag; si no hay ninguna, el recorrido no hace nada.");

        if (Terrain.activeTerrain == null)
            Debug.LogWarning("[Green Mist] No hay un Terrain activo: no pude medir la altura del suelo. "
                             + "Revisá que ninguna toma quede bajo tierra (sobre todo la del templo).");

        if (recorrido.onlyShot != 0)
            Debug.LogWarning("[Green Mist] '" + NombreRaiz + "' está en modo ensayo (Only Shot = " + recorrido.onlyShot
                             + "): jugando se repite solo esa toma. Ponelo en 0 para ver el recorrido entero.");

        GreenMistEditorUtil.MarcarEscenaSucia();

        Debug.Log("[Green Mist] Cámara del menú lista en '" + NombreRaiz + "': "
                  + creadas + " tomas nuevas, "
                  + (rehacer ? recolocadas + " devueltas a su posición de fábrica" : recolocadas + " con algún punto repuesto")
                  + ", " + respetadas + " que ya estaban y no toqué."
                  + (particulas > 0 ? " Puse en tiempo real " + particulas + " sistemas de partículas del ambiente (niebla, luciérnagas, pétalos): ahora se mueven en el menú."
                                    : "")
                  + " NADIE VIO todavía estas tomas: miralas en Play y acomodá las que no te gusten"
                  + " (elegí un punto 'T# Inicio' o 'T# Fin', mové la Scene view y usá GameObject > Align With View, Ctrl+Shift+F)."
                  + " En el Inspector de '" + NombreRaiz + "' hay botones para probarlas sin Play."
                  + " Para volver a la cámara quieta: Green Mist > 11b. Guardá la escena con Ctrl+S.");
    }

    // ==================================================================
    //  LAS CINCO TOMAS DE FABRICA
    // ==================================================================
    // Todas las cuentas se hacen en las coordenadas LOCALES del pueblo (las de
    // los hijos de "Assets") y recien al final se pasan a coordenadas de mundo.

    static TomaDeFabrica[] CalcularTomas(Transform aldea)
    {
        TomaDeFabrica[] tomas = new TomaDeFabrica[NombresDeToma.Length];
        for (int i = 0; i < tomas.Length; i++) tomas[i] = new TomaDeFabrica();

        float piso = GreenMistParticulas.SueloLocalY;   // el suelo plano del pueblo

        // ---- La calle: se mide con las casas ("Casa_A ...") ----
        float calleZ, calleDesde, calleHasta, ambasDesde, ambasHasta;
        bool hayCalle = MedirCalle(aldea, out calleZ, out calleDesde, out calleHasta, out ambasDesde, out ambasHasta);

        // Hacia que lado se camina por la calle: alejandose de Onji, asi no se lo
        // atraviesa ni queda parado en medio de la toma. (+1 = hacia +X, -1 = hacia -X.)
        float sentido = 1f;
        GameObject jugador = GreenMistEditorUtil.BuscarRaiz("PlayerRoot");
        if (hayCalle && jugador != null)
        {
            float xJugador = aldea.InverseTransformPoint(jugador.transform.position).x;
            if (xJugador > (calleDesde + calleHasta) * 0.5f) sentido = -1f;
        }

        // ---- El sando: se mide con los faroles de piedra ----
        Vector3 sandoInicio, sandoDireccion;
        float sandoLargo;
        Vector3 centroCalle = hayCalle ? new Vector3((calleDesde + calleHasta) * 0.5f, piso, calleZ) : new Vector3(60f, piso, 608f);
        bool haySando = MedirSando(aldea, centroCalle, out sandoInicio, out sandoDireccion, out sandoLargo);

        // ================= TOMA 1: el pueblo desde arriba =================
        if (hayCalle)
        {
            // Arranca un poco antes de la primera casa, alta, corrida hacia la DERECHA
            // de la calle (segun hacia donde avanza) y mirando en diagonal hacia abajo:
            // asi la calle se aleja hacia la derecha de la pantalla.
            float xInicio = (sentido > 0f ? calleDesde : calleHasta) - sentido * 4f;
            float zCamara = calleZ - sentido * PuebloAlCostado;

            Vector3 a = new Vector3(xInicio, piso + PuebloAltura, zCamara);
            Vector3 b = a + new Vector3(sentido * PuebloAvance, -2f, 0f);
            Vector3 miraA = new Vector3(a.x + sentido * PuebloMiraAdelante, piso + 2f, calleZ);
            Vector3 miraB = miraA + (b - a);   // la camara avanza sin girar

            Cerrar(tomas[0], aldea, a, miraA, b, miraB, CorridoPueblo, 6f);
        }
        else
        {
            tomas[0].falta = "no encontré las dos filas de casas de la calle (objetos 'Casa_A ...' hijos de 'Assets').";
        }

        // ================= TOMA 2: el pasillo de casas =================
        if (hayCalle)
        {
            // Por el medio de la calle, a la altura de los ojos, en el tramo donde
            // hay casas de los DOS lados. Deja 12 metros de casas por delante al terminar.
            float tramo = ambasHasta - ambasDesde;
            float avance = Mathf.Min(CalleAvance, tramo - 3f - 12f);
            if (avance >= 6f)
            {
                float xInicio = sentido > 0f ? ambasDesde + 3f : ambasHasta - 3f;

                Vector3 a = new Vector3(xInicio, piso + CalleAlturaOjos, calleZ);
                Vector3 b = a + new Vector3(sentido * avance, 0f, 0f);
                Vector3 miraA = a + new Vector3(sentido * 30f, 0.8f, 0f);   // apenas hacia arriba: se ven los faroles de los aleros
                Vector3 miraB = miraA + (b - a);

                Cerrar(tomas[1], aldea, a, miraA, b, miraB, CorridoCalle, 1f);
            }
            else
            {
                tomas[1].falta = "el tramo de calle con casas de los dos lados es muy corto para caminarlo.";
            }
        }
        else
        {
            tomas[1].falta = "no encontré las dos filas de casas de la calle (objetos 'Casa_A ...' hijos de 'Assets').";
        }

        // ================= TOMA 3: el estanque y el puente =================
        Transform agua = aldea.Find("Agua pond");
        if (agua != null)
        {
            Vector3 centro = aldea.InverseTransformPoint(agua.position);
            float radio = 6.5f;
            WaterWaves olas = agua.GetComponent<WaterWaves>();
            if (olas != null) radio = olas.radio * agua.lossyScale.x;

            // El puente: se mira DE COSTADO, que es como se le ve el arco. Su caja
            // dice para que lado es largo; la camara se para del otro lado.
            Vector3 largoDelPuente = new Vector3(1f, 0f, 0f);
            Vector3 mira = centro + new Vector3(0f, 0.6f, 0f);
            Bounds cajaPuente;
            if (BuscarPuente(aldea, centro, radio, out cajaPuente))
            {
                if (cajaPuente.size.z > cajaPuente.size.x) largoDelPuente = new Vector3(0f, 0f, 1f);
                mira = new Vector3(cajaPuente.center.x, Mathf.Max(cajaPuente.center.y, centro.y + 0.6f), cajaPuente.center.z);
            }
            else
            {
                Debug.LogWarning("[Green Mist] Toma 3: no encontré el puente del estanque (un hijo de 'Assets' con 'Bridge' en el nombre). "
                                 + "Armé la toma igual, mirando al centro del agua.");
            }

            // De los dos costados posibles, el que deja el camino al santuario (los
            // faroles y los sakuras) DETRAS del puente, de fondo.
            Vector3 costado = new Vector3(largoDelPuente.z, 0f, largoDelPuente.x);   // perpendicular al puente
            Vector3 fondo = haySando ? sandoInicio + sandoDireccion * (sandoLargo * 0.5f) : centroCalle;
            if (Vector3.Dot(costado, centro - fondo) < 0f) costado = -costado;

            // La camara se desplaza hacia SU derecha, paralela al puente.
            Vector3 desplazamiento = Vector3.Cross(costado, Vector3.up);

            Vector3 parada = centro + costado * (radio + EstanqueDesdeLaOrilla);
            parada.y = centro.y + EstanqueAltura;

            Vector3 a = parada - desplazamiento * EstanqueAlCostado;
            Vector3 b = parada + desplazamiento * EstanqueAlCostado;

            Cerrar(tomas[2], aldea, a, mira, b, mira, CorridoEstanque, 0.8f);
        }
        else
        {
            tomas[2].falta = "no encontré el estanque ('Agua pond', hijo de 'Assets').";
        }

        // ================= TOMA 4: el camino al santuario =================
        if (haySando)
        {
            // Por el medio del camino, entre los faroles de piedra, yendo hacia el torii.
            // Arranca 5 metros antes del primer farol y no recorre mas que una parte del camino.
            float avance = Mathf.Min(SandoAvance, sandoLargo * 0.5f);

            Vector3 a = sandoInicio - sandoDireccion * 5f;
            a.y = piso + SandoAltura;
            Vector3 b = a + sandoDireccion * avance;
            Vector3 miraA = a + sandoDireccion * 40f + new Vector3(0f, 0.3f, 0f);
            Vector3 miraB = miraA + (b - a);

            Cerrar(tomas[3], aldea, a, miraA, b, miraB, CorridoSando, 1f);
        }
        else
        {
            tomas[3].falta = "no encontré los faroles de piedra del camino al santuario (objetos 'SM_Prop_Stone_Lantern...' hijos de 'Assets').";
        }

        // ================= TOMA 5: el templo =================
        Transform templo = aldea.Find("TEMPLO");
        Bounds cajaTemploMundo = new Bounds();
        if (templo != null && GreenMistParticulas.LimitesDe(templo, out cajaTemploMundo))
        {
            CalcularTemplo(tomas[4], aldea, cajaTemploMundo, aldea.TransformPoint(centroCalle));
        }
        else
        {
            tomas[4].falta = "no encontré el templo ('TEMPLO', hijo de 'Assets') o no tiene nada que se dibuje.";
        }

        return tomas;
    }

    // La toma del templo se calcula en coordenadas de MUNDO, porque depende de la
    // altura del terreno: la camara sube despacio y gira un poco alrededor.
    static void CalcularTemplo(TomaDeFabrica toma, Transform aldea, Bounds caja, Vector3 puebloMundo)
    {
        Vector3 centro = caja.center;

        // La base: donde el templo toca el suelo (su caja puede estar algo enterrada).
        float baseY = caja.min.y;
        if (Terrain.activeTerrain != null) baseY = Mathf.Max(baseY, GreenMistEditorUtil.AlturaSueloMundo(centro));
        float alto = Mathf.Max(caja.max.y - baseY, 5f);

        // Se lo mira desde el lado del pueblo (por donde llega el camino), un
        // poco en diagonal: asi se le ven dos caras y no queda chato.
        Vector3 haciaElPueblo = puebloMundo - centro;
        haciaElPueblo.y = 0f;
        if (haciaElPueblo.sqrMagnitude < 1f) haciaElPueblo = Vector3.forward;
        haciaElPueblo.Normalize();

        // Lejos como para que entre entero en la pantalla, con aire arriba.
        float distancia = Mathf.Max(TemploDistanciaMinima, alto * 1.55f);

        // Inicio: bajo, casi a la altura de la base, mirandolo de abajo hacia arriba.
        Vector3 ladoA = Quaternion.AngleAxis(28f, Vector3.up) * haciaElPueblo;
        Vector3 a = new Vector3(centro.x, baseY + alto * 0.08f, centro.z) + ladoA * distancia;
        Vector3 miraA = new Vector3(centro.x, baseY + alto * 0.60f, centro.z);

        // Fin: mas alto, un poco mas cerca y girado unos grados (un arco suave).
        Vector3 ladoB = Quaternion.AngleAxis(12f, Vector3.up) * haciaElPueblo;
        Vector3 b = new Vector3(centro.x, baseY + alto * 0.42f, centro.z) + ladoB * (distancia * 0.9f);
        Vector3 miraB = new Vector3(centro.x, baseY + alto * 0.50f, centro.z);

        // Que el terreno no quede por encima del camino de la camara...
        LevantarSobreElTerreno(ref a, ref b, 3f);

        // ...ni entre la camara y el templo. Al inicio alcanza con verle la parte
        // de arriba (despues la camara sube y lo descubre); al final, casi entero.
        a = SubirHastaVer(a, new Vector3(centro.x, baseY + alto * 0.75f, centro.z));
        b = SubirHastaVer(b, new Vector3(centro.x, baseY + alto * 0.25f, centro.z));

        toma.inicioPos = a;
        toma.finPos = b;
        toma.inicioRot = Mirar(a, miraA, CorridoTemplo);
        toma.finRot = Mirar(b, miraB, CorridoTemplo);
        toma.lista = true;
    }

    // Termina una toma calculada en coordenadas LOCALES del pueblo: la pasa a
    // mundo, la sube si el terreno le queda encima y calcula hacia donde mira.
    //   a, b          donde empieza y donde termina la camara
    //   miraA, miraB  a que punto mira en cada punta
    //   corrido       grados que se corre lo que mira hacia la derecha de la pantalla
    //   despeje       metros minimos que tiene que haber entre la camara y el terreno
    static void Cerrar(TomaDeFabrica toma, Transform aldea, Vector3 a, Vector3 miraA, Vector3 b, Vector3 miraB,
                       float corrido, float despeje)
    {
        Vector3 aMundo = aldea.TransformPoint(a);
        Vector3 bMundo = aldea.TransformPoint(b);
        Vector3 miraAMundo = aldea.TransformPoint(miraA);
        Vector3 miraBMundo = aldea.TransformPoint(miraB);

        LevantarSobreElTerreno(ref aMundo, ref bMundo, despeje);

        toma.inicioPos = aMundo;
        toma.finPos = bMundo;
        toma.inicioRot = Mirar(aMundo, miraAMundo, corrido);
        toma.finRot = Mirar(bMundo, miraBMundo, corrido);
        toma.lista = true;
    }

    // La rotacion de una camara parada en "desde" que mira hacia "hacia", girada
    // "corrido" grados a la IZQUIERDA. Al girar la camara a la izquierda, lo que
    // miraba queda corrido a la derecha de la pantalla (lejos del texto del menu).
    static Quaternion Mirar(Vector3 desde, Vector3 hacia, float corrido)
    {
        Vector3 direccion = hacia - desde;
        if (direccion.sqrMagnitude < 0.0001f) direccion = Vector3.forward;

        Quaternion derecho = Quaternion.LookRotation(direccion.normalized, Vector3.up);
        return Quaternion.AngleAxis(-corrido, Vector3.up) * derecho;
    }

    // Recorre el camino de la camara y, si en algun punto el terreno le queda a
    // menos de "despeje" metros, sube las dos puntas lo que haga falta.
    static void LevantarSobreElTerreno(ref Vector3 a, ref Vector3 b, float despeje)
    {
        if (Terrain.activeTerrain == null) return;

        float falta = 0f;
        for (int i = 0; i <= 10; i++)
        {
            Vector3 p = Vector3.Lerp(a, b, i / 10f);
            float minimo = GreenMistEditorUtil.AlturaSueloMundo(p) + despeje;
            if (minimo - p.y > falta) falta = minimo - p.y;
        }

        a.y += falta;
        b.y += falta;
    }

    // Si el terreno tapa la linea entre la camara y "objetivo", sube la camara de
    // a 3 metros hasta que lo vea (o hasta 45 metros, para no irse al cielo).
    // Solo mira el TERRENO: fuera de Play las casas y las rocas no tienen collider.
    static Vector3 SubirHastaVer(Vector3 camara, Vector3 objetivo)
    {
        Terrain terreno = Terrain.activeTerrain;
        TerrainCollider piel = terreno != null ? terreno.GetComponent<TerrainCollider>() : null;
        if (piel == null) return camara;

        for (int i = 0; i < 15; i++)
        {
            Vector3 hacia = objetivo - camara;
            float largo = hacia.magnitude;
            if (largo < 0.5f) break;

            RaycastHit golpe;
            if (!piel.Raycast(new Ray(camara, hacia / largo), out golpe, largo)) break;   // nada en el medio
            camara.y += 3f;
        }
        return camara;
    }

    // ------------------------------------------------------------------
    // Medir la escena
    // ------------------------------------------------------------------

    // La caja de un objeto en coordenadas LOCALES del pueblo (la que da Unity,
    // Renderer.bounds, esta en coordenadas de mundo). Se pasan las 8 esquinas.
    static Bounds CajaLocal(Transform aldea, Bounds mundo)
    {
        Bounds local = new Bounds(aldea.InverseTransformPoint(mundo.center), Vector3.zero);
        for (int i = 0; i < 8; i++)
        {
            Vector3 esquina = mundo.center + Vector3.Scale(mundo.extents,
                new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
            local.Encapsulate(aldea.InverseTransformPoint(esquina));
        }
        return local;
    }

    // Mide la calle con las casas "Casa_A ...". Las casas forman dos filas
    // enfrentadas a lo largo de X; la calle es el hueco entre las dos.
    //   centroZ                   la Z del medio de la calle
    //   desde, hasta              de que X a que X hay casas (en cualquiera de las filas)
    //   ambasDesde, ambasHasta    de que X a que X hay casas de los DOS lados
    // Devuelve false si no encuentra dos filas.
    static bool MedirCalle(Transform aldea, out float centroZ, out float desde, out float hasta,
                           out float ambasDesde, out float ambasHasta)
    {
        centroZ = desde = hasta = ambasDesde = ambasHasta = 0f;

        List<Bounds> casas = new List<Bounds>();
        for (int i = 0; i < aldea.childCount; i++)
        {
            Transform hijo = aldea.GetChild(i);
            if (!hijo.name.StartsWith("Casa_A") || !hijo.gameObject.activeInHierarchy) continue;

            Bounds caja;
            if (GreenMistParticulas.LimitesDe(hijo, out caja)) casas.Add(CajaLocal(aldea, caja));
        }
        if (casas.Count < 4) return false;

        // Fila 1: la Z donde coinciden mas casas. Fila 2: lo mismo, entre las que
        // estan a mas de 6 metros de la fila 1. (Hay casas sueltas fuera de la
        // calle: por eso no alcanza con tomar la mas al norte y la mas al sur.)
        float z1, z2;
        if (FilaMasPoblada(casas, float.NaN, out z1) < 2) return false;
        if (FilaMasPoblada(casas, z1, out z2) < 2) return false;

        float zSur = Mathf.Min(z1, z2);
        float zNorte = Mathf.Max(z1, z2);

        // El medio de la calle: a mitad de camino entre los centros de las dos filas.
        // (No entre los bordes: una sola casa mas grande que las demas correria el medio.)
        float medio = (zSur + zNorte) * 0.5f;

        // El borde de cada fila que da a la calle, y de donde a donde llega cada una.
        float bordeSur = float.MinValue, bordeNorte = float.MaxValue;
        float surDesde = float.MaxValue, surHasta = float.MinValue;
        float norteDesde = float.MaxValue, norteHasta = float.MinValue;

        foreach (Bounds casa in casas)
        {
            // Una casa cuya caja CRUZA el medio de la calle no es parte de una fila:
            // es un portal. (En el pueblo hay una, "Casa_A (8)", con el techo corrido
            // encima de la calle.) Si se la contara, su caja taparia la calle entera
            // y pareceria que entre las dos filas no hay lugar para pasar.
            if (casa.min.z < medio && casa.max.z > medio) continue;

            if (Mathf.Abs(casa.center.z - zSur) < AnchoDeFila)
            {
                bordeSur = Mathf.Max(bordeSur, casa.max.z);
                surDesde = Mathf.Min(surDesde, casa.min.x);
                surHasta = Mathf.Max(surHasta, casa.max.x);
            }
            else if (Mathf.Abs(casa.center.z - zNorte) < AnchoDeFila)
            {
                bordeNorte = Mathf.Min(bordeNorte, casa.min.z);
                norteDesde = Mathf.Min(norteDesde, casa.min.x);
                norteHasta = Mathf.Max(norteHasta, casa.max.x);
            }
        }

        // Alguna de las dos filas quedo sin ninguna casa que sirva.
        if (surDesde > surHasta || norteDesde > norteHasta) return false;

        // Tiene que quedar lugar para pasar entre las dos filas. Se mide en la parte
        // mas angosta, de alero a alero: en este pueblo da unos 2,6 metros, por eso
        // el minimo es 1,5 (con margen, asi no se pierden las tomas por mover una casa).
        if (bordeNorte - bordeSur < 1.5f) return false;

        centroZ = medio;
        desde = Mathf.Min(surDesde, norteDesde);
        hasta = Mathf.Max(surHasta, norteHasta);
        ambasDesde = Mathf.Max(surDesde, norteDesde);
        ambasHasta = Mathf.Min(surHasta, norteHasta);
        return true;
    }

    // Dos casas son de la misma fila si sus centros estan a menos de esto en Z.
    const float AnchoDeFila = 2.5f;

    // Busca la fila con mas casas. Si "lejosDe" es un numero, solo cuenta las casas
    // que estan a mas de 6 metros de esa Z. Devuelve cuantas casas tiene la fila
    // y, en "z", el promedio de sus centros.
    static int FilaMasPoblada(List<Bounds> casas, float lejosDe, out float z)
    {
        z = 0f;
        int mejorCantidad = 0;

        for (int i = 0; i < casas.Count; i++)
        {
            float zi = casas[i].center.z;
            if (!float.IsNaN(lejosDe) && Mathf.Abs(zi - lejosDe) < 6f) continue;

            int cantidad = 0;
            float suma = 0f;
            for (int j = 0; j < casas.Count; j++)
            {
                float zj = casas[j].center.z;
                if (!float.IsNaN(lejosDe) && Mathf.Abs(zj - lejosDe) < 6f) continue;
                if (Mathf.Abs(zj - zi) >= AnchoDeFila) continue;
                cantidad++;
                suma += zj;
            }

            if (cantidad > mejorCantidad)
            {
                mejorCantidad = cantidad;
                z = suma / cantidad;
            }
        }
        return mejorCantidad;
    }

    // Mide el camino al santuario con los faroles de piedra, que estan en dos
    // filas a los costados del camino.
    //   inicio      el punto del MEDIO del camino a la altura del farol mas lejano al torii
    //   direccion   hacia donde se camina (hacia el torii), de largo 1
    //   largo       metros desde "inicio" hasta el ultimo farol
    // Devuelve false si no hay faroles suficientes.
    static bool MedirSando(Transform aldea, Vector3 centroCalle, out Vector3 inicio, out Vector3 direccion, out float largo)
    {
        inicio = Vector3.zero;
        direccion = Vector3.back;
        largo = 0f;

        int cantidad = 0;
        float xMin = float.MaxValue, xMax = float.MinValue, zMin = float.MaxValue, zMax = float.MinValue;
        for (int i = 0; i < aldea.childCount; i++)
        {
            Transform hijo = aldea.GetChild(i);
            if (!hijo.name.StartsWith("SM_Prop_Stone_Lantern") || !hijo.gameObject.activeInHierarchy) continue;

            Vector3 p = hijo.localPosition;
            xMin = Mathf.Min(xMin, p.x); xMax = Mathf.Max(xMax, p.x);
            zMin = Mathf.Min(zMin, p.z); zMax = Mathf.Max(zMax, p.z);
            cantidad++;
        }
        if (cantidad < 4) return false;

        float piso = GreenMistParticulas.SueloLocalY;
        bool aLoLargoDeZ = (zMax - zMin) >= (xMax - xMin);

        // Las dos puntas del camino, por el medio.
        Vector3 puntaA, puntaB;
        if (aLoLargoDeZ)
        {
            float xMedio = (xMin + xMax) * 0.5f;
            puntaA = new Vector3(xMedio, piso, zMin);
            puntaB = new Vector3(xMedio, piso, zMax);
        }
        else
        {
            float zMedio = (zMin + zMax) * 0.5f;
            puntaA = new Vector3(xMin, piso, zMedio);
            puntaB = new Vector3(xMax, piso, zMedio);
        }

        largo = Vector3.Distance(puntaA, puntaB);
        if (largo < 12f) return false;

        // Se camina HACIA el torii. Si no esta, se camina alejandose de la calle.
        bool empiezaEnA;
        Transform torii = BuscarHijoQueContenga(aldea, "Torii");
        if (torii != null)
        {
            Vector3 t = aldea.InverseTransformPoint(torii.position);
            empiezaEnA = (puntaA - t).sqrMagnitude > (puntaB - t).sqrMagnitude;
        }
        else
        {
            Debug.LogWarning("[Green Mist] Toma 4: no encontré el torii (un hijo de 'Assets' llamado 'Torii'). "
                             + "Armé la toma igual, caminando desde la calle hacia el fondo del camino.");
            empiezaEnA = (puntaA - centroCalle).sqrMagnitude < (puntaB - centroCalle).sqrMagnitude;
        }

        inicio = empiezaEnA ? puntaA : puntaB;
        direccion = ((empiezaEnA ? puntaB : puntaA) - inicio).normalized;
        return true;
    }

    // El puente del estanque: un hijo de "Assets" con "Bridge" en el nombre que
    // este encima del agua. Devuelve su caja en coordenadas locales del pueblo.
    static bool BuscarPuente(Transform aldea, Vector3 centroAgua, float radio, out Bounds cajaLocal)
    {
        cajaLocal = new Bounds();
        float mejor = radio + 4f;   // mas lejos que esto no es el puente del estanque
        bool hay = false;

        for (int i = 0; i < aldea.childCount; i++)
        {
            Transform hijo = aldea.GetChild(i);
            if (hijo.name.IndexOf("Bridge", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (!hijo.gameObject.activeInHierarchy) continue;

            Bounds caja;
            if (!GreenMistParticulas.LimitesDe(hijo, out caja)) continue;

            Bounds local = CajaLocal(aldea, caja);
            Vector3 separacion = local.center - centroAgua;
            separacion.y = 0f;
            if (separacion.magnitude >= mejor) continue;

            mejor = separacion.magnitude;
            cajaLocal = local;
            hay = true;
        }
        return hay;
    }

    // Un hijo DIRECTO de "padre": primero con ese nombre exacto; si no, el
    // primero que lo tenga dentro del nombre (sin mirar mayusculas).
    static Transform BuscarHijoQueContenga(Transform padre, string texto)
    {
        Transform exacto = padre.Find(texto);
        if (exacto != null) return exacto;

        for (int i = 0; i < padre.childCount; i++)
        {
            Transform hijo = padre.GetChild(i);
            if (hijo.name.IndexOf(texto, System.StringComparison.OrdinalIgnoreCase) >= 0) return hijo;
        }
        return null;
    }

    // Avisa si alguna punta (o el medio) de una toma recien calculada quedo dentro
    // de la caja de algun objeto. No es una prueba exacta (la caja de un arbol
    // incluye el aire debajo de la copa), por eso es solo un aviso para ir a mirar.
    static void AvisarSiQuedaAdentro(int numero, Vector3 inicio, Vector3 fin)
    {
        Renderer[] dibujos = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        for (int paso = 0; paso <= 4; paso++)
        {
            Vector3 p = Vector3.Lerp(inicio, fin, paso / 4f);

            foreach (Renderer dibujo in dibujos)
            {
                if (dibujo is ParticleSystemRenderer) continue;        // la niebla y demas no cuentan
                if (!dibujo.enabled) continue;

                Bounds caja = dibujo.bounds;
                // Los objetos gigantes (montañas de fondo, acantilados) tienen cajas enormes y huecas.
                if (Mathf.Max(caja.size.x, caja.size.y, caja.size.z) > 80f) continue;
                if (!caja.Contains(p)) continue;

                string donde = paso == 0 ? "el inicio" : (paso == 4 ? "el fin" : "el recorrido");
                Debug.LogWarning("[Green Mist] Toma " + numero + ": " + donde + " pasa por dentro de la caja de '" + dibujo.name
                                 + "'. Puede estar bien (por ejemplo, debajo de la copa de un árbol) o puede estar atravesando algo: "
                                 + "mirala y, si hace falta, mové ese punto.", dibujo);
                return;   // un aviso por toma alcanza
            }
        }
    }

    // ------------------------------------------------------------------
    // Los objetos de las tomas
    // ------------------------------------------------------------------

    // El objeto de la toma numero N: un hijo de la raiz cuyo nombre empieza con
    // "Toma N" (asi se lo reconoce aunque le cambies el resto del nombre).
    static Transform BuscarToma(Transform raiz, int numero)
    {
        string prefijo = "Toma " + numero;
        for (int i = 0; i < raiz.childCount; i++)
        {
            Transform hijo = raiz.GetChild(i);
            if (!hijo.name.StartsWith(prefijo)) continue;

            // "Toma 1" no es "Toma 10": despues del numero no puede venir otro numero.
            if (hijo.name.Length > prefijo.Length && char.IsDigit(hijo.name[prefijo.Length])) continue;
            return hijo;
        }
        return null;
    }

    // Busca un punto por nombre dentro de la toma. Si no existe y "crear" es
    // true, lo crea (y le pone una etiqueta de color para verlo en la Scene view).
    static Transform Punto(Transform toma, string nombre, bool esInicio, bool crear, out bool nuevo)
    {
        Transform punto = toma.Find(nombre);
        nuevo = false;
        if (punto != null || !crear) return punto;

        nuevo = true;
        GameObject go = GreenMistEditorUtil.BuscarOCrearHijo(toma, nombre);

        // Las etiquetas de colores de Unity (las mismas del boton de icono del
        // Inspector): muestran el nombre en la Scene view y se les puede hacer clic.
        // Verde = inicio, naranja = fin, igual que las camaritas dibujadas.
        GUIContent icono = EditorGUIUtility.IconContent(esInicio ? "sv_label_3" : "sv_label_5");
        Texture2D imagen = icono != null ? icono.image as Texture2D : null;
        if (imagen != null) EditorGUIUtility.SetIconForObject(go, imagen);

        return go.transform;
    }

    static void Colocar(Transform punto, Vector3 posicion, Quaternion rotacion)
    {
        Undo.RecordObject(punto, TituloUndo);
        punto.SetPositionAndRotation(posicion, rotacion);
    }

    // Deja en la lista del script una entrada por cada toma de fabrica que exista.
    // Lo que ya esta anotado NO se toca (ni su duracion ni el orden que le diste);
    // solo se agregan las que falten, cada una en su lugar segun el numero de toma.
    static void AnotarTomas(MenuCameraTour recorrido, Transform[] inicios, Transform[] fines)
    {
        List<MenuCameraTour.Shot> lista = new List<MenuCameraTour.Shot>();
        if (recorrido.shots != null) lista.AddRange(recorrido.shots);

        for (int i = 0; i < NombresDeToma.Length; i++)
        {
            if (inicios[i] == null || fines[i] == null) continue;

            // Ya esta anotada? Alcanza con que alguna entrada use uno de sus dos puntos
            // (por si diste vuelta una toma cruzando el inicio con el fin).
            bool anotada = false;
            foreach (MenuCameraTour.Shot toma in lista)
            {
                if (!UsaAlgunPunto(toma, inicios[i], fines[i])) continue;

                // Si a la entrada le quedo un punto vacio (lo borraste y este menu lo
                // acaba de crear de nuevo), se lo volvemos a conectar. Sin esto la
                // toma quedaria incompleta y el recorrido la saltearia sin avisar.
                // (Se le pone el punto que la otra punta NO esta usando.)
                if (toma.start == null) toma.start = (toma.end == inicios[i]) ? fines[i] : inicios[i];
                if (toma.end == null) toma.end = (toma.start == fines[i]) ? inicios[i] : fines[i];

                anotada = true;
                break;
            }
            if (anotada) continue;

            // Una entrada con el mismo nombre que quedo vacia (borraste la toma y
            // el menu la volvio a crear): se reusa, asi conserva su lugar y su duracion.
            MenuCameraTour.Shot libre = null;
            foreach (MenuCameraTour.Shot toma in lista)
            {
                if (toma != null && toma.start == null && toma.end == null && toma.name == NombresDeToma[i]) { libre = toma; break; }
            }

            if (libre == null)
            {
                libre = new MenuCameraTour.Shot();
                libre.name = NombresDeToma[i];
                libre.duration = DuracionDeToma[i];

                // Una toma nueva va ANTES de la primera entrada que sea de una toma de
                // fabrica con numero mas alto. Asi, si la toma 1 se arma recien en una
                // corrida posterior, igual queda primera y no al final del recorrido.
                // Si no hay ninguna posterior, va al final.
                int lugar = lista.Count;
                for (int k = 0; k < lista.Count && lugar == lista.Count; k++)
                {
                    for (int j = i + 1; j < NombresDeToma.Length; j++)
                    {
                        if (UsaAlgunPunto(lista[k], inicios[j], fines[j])) { lugar = k; break; }
                    }
                }
                lista.Insert(lugar, libre);
            }
            libre.start = inicios[i];
            libre.end = fines[i];
        }

        recorrido.shots = lista.ToArray();
    }

    // True si esa entrada de la lista usa alguno de esos dos puntos (en cualquiera
    // de sus dos puntas). Un punto que no existe (null) nunca cuenta: si no, una
    // entrada con una punta vacia pareceria "usar" un punto que no esta.
    static bool UsaAlgunPunto(MenuCameraTour.Shot toma, Transform a, Transform b)
    {
        if (toma == null) return false;
        if (a != null && (toma.start == a || toma.end == a)) return true;
        if (b != null && (toma.start == b || toma.end == b)) return true;
        return false;
    }

    // ------------------------------------------------------------------
    // Buscar cosas de la escena
    // ------------------------------------------------------------------

    // La camara del juego: el objeto raiz "Main Camera" o, si no, cualquier camara con el tag MainCamera.
    static Camera BuscarCamaraPrincipal()
    {
        GameObject porNombre = GreenMistEditorUtil.BuscarRaiz(NombreCamara);
        if (porNombre != null)
        {
            Camera c = porNombre.GetComponent<Camera>();
            if (c != null) return c;
        }

        foreach (Camera c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c.CompareTag("MainCamera")) return c;
        return null;
    }

    // El panel que MainMenuUI prende y apaga: el interruptor de "estamos en el menu".
    // (Se busca igual que en el menu 9.)
    static GameObject BuscarPanelMenu()
    {
        MainMenuUI menu = Object.FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
        if (menu != null)
        {
            // Su campo es privado, asi que lo leemos como lo lee el Inspector. Solo se LEE.
            SerializedProperty campo = new SerializedObject(menu).FindProperty("menuPanel");
            if (campo != null && campo.objectReferenceValue != null)
                return campo.objectReferenceValue as GameObject;
        }

        PlayerDeath muerte = Object.FindFirstObjectByType<PlayerDeath>(FindObjectsInactive.Include);
        if (muerte != null && muerte.mainMenuPanel != null) return muerte.mainMenuPanel;

        // Por nombre, en toda la escena (tambien apagados).
        foreach (GameObject raiz in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                if (t.name == NombrePanelMenu) return t.gameObject;
        }
        return null;
    }
}

// El Inspector de MenuCameraTour (lo que se ve al elegir "GM_MenuCamara").
// Muestra los campos de siempre y, debajo, botones para PROBAR las tomas sin
// entrar a Play.
//
// La prueba mueve la camara de la SCENE VIEW (la ventana donde armas la escena),
// no la camara del juego ni ningun objeto: por eso no ensucia la escena y no
// hay nada que deshacer. Es una aproximacion: la Scene view no muestra el menu
// ni los efectos de noche. Como queda de verdad se ve en Play.
[CustomEditor(typeof(MenuCameraTour))]
public class GreenMistMenuCamaraInspector : Editor
{
    // La prueba en curso (una sola a la vez).
    static MenuCameraTour probando;
    static int tomaProbada = -1;
    static double empezoEn;
    static float velocidad = 1f;

    // El tamaño que usa Unity para la Scene view en "Align View to Selected".
    const float TamanoDeVista = 10f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MenuCameraTour recorrido = (MenuCameraTour)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Probar las tomas en la Scene view", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "No cambia nada de la escena: solo mueve la vista de la ventana Scene.\n"
            + "• Ir al inicio / Ir al fin: te lleva a ese punto y lo deja elegido. Para corregirlo, acomodá la vista "
            + "y usá GameObject > Align With View (Ctrl+Shift+F).\n"
            + "• Reproducir: hace el movimiento de la toma. Es aproximado (sin menú ni efectos de noche): "
            + "como queda de verdad se ve en Play.",
            MessageType.Info);

        velocidad = EditorGUILayout.Slider(
            new GUIContent("Velocidad de la prueba", "1 = a la velocidad real. 4 = cuatro veces más rápido, para no esperar."),
            velocidad, 1f, 8f);

        int cantidad = recorrido.shots != null ? recorrido.shots.Length : 0;
        if (cantidad == 0)
            EditorGUILayout.LabelField("(No hay tomas. Corré Green Mist > 11 para armarlas.)");

        for (int i = 0; i < cantidad; i++)
        {
            MenuCameraTour.Shot toma = recorrido.shots[i];
            bool sirve = MenuCameraTour.IsUsable(toma);
            string titulo = (i + 1) + ". " + (toma != null && !string.IsNullOrEmpty(toma.name) ? toma.name : "(sin nombre)")
                            + (sirve ? "" : "  (apagada o incompleta)");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(titulo, GUILayout.MinWidth(80f));

            EditorGUI.BeginDisabledGroup(!sirve);
            bool irInicio = GUILayout.Button("Ir al inicio", GUILayout.Width(84f));
            bool irFin = GUILayout.Button("Ir al fin", GUILayout.Width(68f));
            bool reproducir = GUILayout.Button("Reproducir", GUILayout.Width(84f));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // Las acciones se hacen un instante DESPUES de dibujar (delayCall):
            // cambiar lo que esta elegido en medio del dibujo del Inspector da errores.
            if (irInicio) { Transform punto = toma.start; EditorApplication.delayCall += () => IrA(punto); }
            if (irFin) { Transform punto = toma.end; EditorApplication.delayCall += () => IrA(punto); }
            if (reproducir) EmpezarPrueba(recorrido, i);
        }

        if (probando != null && GUILayout.Button("Detener la prueba")) TerminarPrueba();

        EditorGUILayout.Space();
        EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
        if (GUILayout.Button(new GUIContent("Rehacer las tomas de fábrica...",
                "Vuelve a poner TODOS los puntos donde los calcula el menú Green Mist > 11. Pisa lo que hayas movido (pregunta antes).")))
        {
            EditorApplication.delayCall += PreguntarYRehacer;
        }
        EditorGUI.EndDisabledGroup();
    }

    static void PreguntarYRehacer()
    {
        bool si = EditorUtility.DisplayDialog(
            "Rehacer las tomas de fábrica",
            "Todos los puntos de las cinco tomas vuelven a la posición que calcula el menú Green Mist > 11.\n\n"
            + "Se pierden los puntos que hayas acomodado a mano. Las duraciones y el orden no cambian.\n"
            + "(Se puede deshacer con Ctrl+Z.)",
            "Rehacer", "Cancelar");
        if (si) GreenMistMenuCamara.RehacerTomasDeFabrica();
    }

    // Lleva la Scene view a un punto (lo mismo que GameObject > Align View to
    // Selected) y lo deja elegido, listo para corregirlo con Align With View.
    static void IrA(Transform punto)
    {
        if (punto == null) return;
        TerminarPrueba();

        SceneView vista = SceneView.lastActiveSceneView;
        if (vista == null)
        {
            Debug.LogWarning("[Green Mist] Abrí una ventana Scene para poder probar las tomas.");
            return;
        }

        if (vista.in2DMode) vista.in2DMode = false;
        if (vista.orthographic) vista.orthographic = false;
        vista.AlignViewToObject(punto);
        Selection.activeTransform = punto;
    }

    static void EmpezarPrueba(MenuCameraTour recorrido, int indice)
    {
        if (SceneView.lastActiveSceneView == null)
        {
            Debug.LogWarning("[Green Mist] Abrí una ventana Scene para poder probar las tomas.");
            return;
        }

        probando = recorrido;
        tomaProbada = indice;
        empezoEn = EditorApplication.timeSinceStartup;

        // "update" es una lista de funciones que el editor llama todo el tiempo.
        // Se quita antes de agregar para no quedar anotado dos veces.
        EditorApplication.update -= AvanzarPrueba;
        EditorApplication.update += AvanzarPrueba;
    }

    static void TerminarPrueba()
    {
        EditorApplication.update -= AvanzarPrueba;
        probando = null;
        tomaProbada = -1;
    }

    static void AvanzarPrueba()
    {
        SceneView vista = SceneView.lastActiveSceneView;

        // Se corta sola si se cerro la Scene view, si se borro el objeto o si
        // elegiste otra cosa (ahi ya no esta a la vista el boton de detener).
        if (vista == null || probando == null || Selection.activeGameObject != probando.gameObject
            || probando.shots == null || tomaProbada < 0 || tomaProbada >= probando.shots.Length)
        {
            TerminarPrueba();
            return;
        }

        MenuCameraTour.Shot toma = probando.shots[tomaProbada];
        float duracion = toma != null ? Mathf.Max(toma.duration, 1f) : 1f;
        float avance = (float)(EditorApplication.timeSinceStartup - empezoEn) * velocidad / duracion;

        Vector3 posicion;
        Quaternion rotacion;
        if (!probando.GetPose(tomaProbada, avance, out posicion, out rotacion))
        {
            TerminarPrueba();
            return;
        }

        // La Scene view no se mueve como una camara comun: gira alrededor de un
        // "pivote" que queda ADELANTE de ella. Para que la camara quede justo en
        // "posicion", el pivote se pone adelante, a la distancia que usa la vista.
        if (vista.in2DMode) vista.in2DMode = false;
        if (vista.orthographic) vista.orthographic = false;
        vista.size = TamanoDeVista;
        vista.rotation = rotacion;
        vista.pivot = posicion + rotacion * Vector3.forward * vista.cameraDistance;
        vista.Repaint();

        if (avance >= 1f) TerminarPrueba();
    }
}
