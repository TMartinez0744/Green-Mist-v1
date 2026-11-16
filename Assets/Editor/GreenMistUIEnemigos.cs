using UnityEditor;
using UnityEngine;

// Menu "Green Mist > 10. Barras de vida de enemigos".
//
// LAS BARRAS NUEVAS NO NECESITAN ESTE MENU PARA VERSE: el script HealthBarUI
// arma su propio dibujo al jugar (pista oscura, marco hueso, relleno carmesi,
// estela clara), asi que los enemigos que ya tenian barra la muestran con el
// estilo nuevo apenas Unity compila.
//
// Este menu hace lo que el script solo no puede:
//   1) Le pone barra a los enemigos que NO tenian ninguna (hoy: "final boss"
//      y "DummyEnemy"). Crea un hijo "GM_UI_VidaEnemigo" con HealthBarUI
//      apuntando a su vida. Al jefe (nombre con "boss" o "jefe") le pone una
//      barra mas larga.
//   2) Apaga en la escena los Slider verdes viejos (no los borra), asi no se
//      ven mas en la ventana Scene. Jugando ya se apagaban solos.
//   3) Si algun dia cambia el estilo de fabrica (GreenMistUIEstilo.VersionEstilo
//      sube), le escribe a cada barra los colores y tamaños nuevos UNA sola vez
//      (campo "Style Version" de HealthBarUI) y despues vuelve a respetar lo
//      que ajustes a mano. Hoy las barras ya nacen con el estilo actual, asi
//      que este menu NO te pisa nada de lo que hayas tocado en el Inspector.
//      Para forzar los valores de fabrica: pone Style Version en 0 y corre el menu.
//
// Un enemigo, para este menu, es un objeto con tag "Enemy" y componente Health.
//
// SE PUEDE CORRER LAS VECES QUE QUIERAS: no duplica barras.
// Para volver a los Slider verdes: Green Mist > 10b. Volver a las barras viejas.
public static class GreenMistUIEnemigos
{
    const string NombreBarra = "GM_UI_VidaEnemigo";
    const string TagEnemigo = "Enemy";
    const string TituloUndo = "Green Mist: barras de enemigos";

    [MenuItem("Green Mist/10. Barras de vida de enemigos", false, 112)]
    public static void Aplicar()
    {
        if (!GreenMistEditorUtil.PuedeEditarEscena()) return;

        Undo.SetCurrentGroupName(TituloUndo);

        Health[] vidas = Object.FindObjectsByType<Health>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        HealthBarUI[] barras = Object.FindObjectsByType<HealthBarUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        int listas = 0;          // barras que quedaron con el estilo nuevo
        int restiladas = 0;      // barras que ya existian y recibieron los valores de fabrica
        int slidersApagados = 0;
        int sinEnemigo = 0;      // barras que no saben de quien son
        string creadas = "";     // nombres de los enemigos a los que se les creo una barra

        // ---------- 1) Las barras que ya estan en la escena ----------
        foreach (HealthBarUI barra in barras)
        {
            GreenMistUIEstilo.Registrar(barra);

            // Si nadie le dijo de quien es, es del enemigo del que cuelga.
            if (barra.target == null) barra.target = barra.GetComponentInParent<Health>(true);
            if (barra.target == null)
            {
                sinEnemigo++;
                Debug.LogWarning("[Green Mist] La barra de vida '" + Ruta(barra.transform) + "' no tiene asignado ningún enemigo (campo Target de HealthBarUI) "
                                 + "y no cuelga de ninguno: no va a mostrar nada. Arrastrale el enemigo al campo Target.", barra);
            }

            barra.newStyle = true;

            if (GreenMistUIEstilo.HayQueRestilar(barra.styleVersion))
            {
                EstiloDeFabrica(barra, barra.target != null && EsJefe(barra.target));
                restiladas++;
            }

            // El Slider verde viejo se apaga (no se borra).
            if (barra.slider != null && barra.slider.gameObject.activeSelf)
            {
                Undo.RecordObject(barra.slider.gameObject, TituloUndo);
                barra.slider.gameObject.SetActive(false);
                slidersApagados++;
            }

            listas++;
        }

        // ---------- 2) Enemigos sin barra: se les crea una ----------
        int enemigos = 0;
        int sinCollider = 0;
        foreach (Health vida in vidas)
        {
            if (vida.gameObject.tag != TagEnemigo) continue;
            enemigos++;

            // Sin collider en el mismo objeto la barra no puede medir la cabeza y usa World Offset.
            if (vida.GetComponent<Collider>() == null) sinCollider++;

            if (TieneBarra(vida, barras)) continue;

            // Un hijo del enemigo: si despues duplicas al enemigo, la barra se duplica con el.
            // (Buscar o crear por nombre: correr el menu de nuevo no hace otra.)
            GameObject objeto = GreenMistEditorUtil.BuscarOCrearHijo(vida.transform, NombreBarra);
            objeto.layer = GreenMistUIEstilo.CapaUI();

            HealthBarUI nueva = GreenMistUIEstilo.Componente<HealthBarUI>(objeto);
            GreenMistUIEstilo.Registrar(nueva);
            nueva.target = vida;
            nueva.newStyle = true;
            EstiloDeFabrica(nueva, EsJefe(vida));

            creadas += (creadas.Length > 0 ? ", " : "") + "'" + vida.name + "'" + (EsJefe(vida) ? " (jefe: barra larga)" : "");
            listas++;
        }

        // ---------- 3) Avisos ----------
        if (enemigos == 0)
            Debug.LogWarning("[Green Mist] No encontré ningún enemigo (objetos con tag '" + TagEnemigo + "' y componente Health). No había a quién ponerle barra.");
        if (sinCollider > 0)
            Debug.LogWarning("[Green Mist] " + sinCollider + " enemigo(s) no tienen un Collider en el mismo objeto que Health: su barra no puede medir "
                             + "dónde está la cabeza y usa el campo World Offset de HealthBarUI. Ajustalo a mano si queda muy alta o muy baja.");

        GreenMistEditorUtil.MarcarEscenaSucia();

        Debug.Log("[Green Mist] Barras de vida de enemigos: " + listas + " con el estilo nuevo (pista oscura, marco hueso, relleno carmesí y estela clara)."
                  + (creadas.Length > 0 ? " Barras NUEVAS para los que no tenían: " + creadas + "." : "")
                  + (restiladas > 0 ? " A " + restiladas + " que ya existían les puse colores y tamaños de fábrica (versión de estilo " + GreenMistUIEstilo.VersionEstilo + "); desde ahora se respeta lo que ajustes a mano." : "")
                  + (slidersApagados > 0 ? " Apagué " + slidersApagados + " Slider verdes viejos (no se borró nada)." : "")
                  + (sinEnemigo > 0 ? " OJO: " + sinEnemigo + " barra(s) sin enemigo asignado (ver avisos)." : "")
                  + " El dibujo de la barra se arma solo al jugar: en la ventana Scene no se ve. Para volver atrás: Green Mist > 10b. Guardá la escena con Ctrl+S.");
    }

    // Vuelve a los Slider verdes de antes. No borra nada: las barras creadas por
    // el menu 10 (jefe, muñeco) quedan en la escena pero, como no tienen Slider
    // viejo, no muestran nada hasta que vuelvas a correr el menu 10.
    [MenuItem("Green Mist/10b. Volver a las barras viejas", false, 113)]
    public static void Restaurar()
    {
        if (!GreenMistEditorUtil.PuedeEditarEscena()) return;

        Undo.SetCurrentGroupName(TituloUndo + " (volver a las viejas)");

        HealthBarUI[] barras = Object.FindObjectsByType<HealthBarUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        int conSlider = 0;
        int sinSlider = 0;
        foreach (HealthBarUI barra in barras)
        {
            GreenMistUIEstilo.Registrar(barra);
            barra.newStyle = false;

            if (barra.slider == null)
            {
                sinSlider++;
                continue;
            }

            conSlider++;
            if (!barra.slider.gameObject.activeSelf)
            {
                Undo.RecordObject(barra.slider.gameObject, TituloUndo);
                barra.slider.gameObject.SetActive(true);
            }
        }

        GreenMistEditorUtil.MarcarEscenaSucia();

        Debug.Log("[Green Mist] Volví a las barras viejas: " + conSlider + " enemigo(s) usan otra vez el Slider verde."
                  + (sinSlider > 0 ? " " + sinSlider + " barra(s) no tienen Slider viejo (las creó el menú 10): quedan en la escena pero no muestran nada." : "")
                  + " No se borró nada. Para volver a las nuevas: Green Mist > 10. Guardá la escena con Ctrl+S.");
    }

    // ------------------------------------------------------------------

    // Colores de la paleta compartida y tamaños de fabrica. No toca tiempos ni
    // el lugar de la barra: eso es tuyo.
    static void EstiloDeFabrica(HealthBarUI barra, bool jefe)
    {
        barra.fillColor = GreenMistUIEstilo.Carmesi;
        barra.trailColor = GreenMistUIEstilo.EstelaHueso;
        barra.trackColor = GreenMistUIEstilo.Pista;
        barra.borderColor = GreenMistUIEstilo.BordeBarra;

        barra.width = jefe ? HealthBarUI.AnchoJefe : HealthBarUI.AnchoNormal;
        barra.height = jefe ? HealthBarUI.AltoJefe : HealthBarUI.AltoNormal;
        barra.borderThickness = HealthBarUI.BordeDeFabrica;

        barra.styleVersion = GreenMistUIEstilo.VersionEstilo;
    }

    // El codigo del juego no distingue al jefe de los demas: aca se lo reconoce por el nombre.
    static bool EsJefe(Health vida)
    {
        string nombre = vida.name.ToLowerInvariant();
        return nombre.Contains("boss") || nombre.Contains("jefe");
    }

    // true si alguna de las barras que ya estaban en la escena es de este enemigo.
    static bool TieneBarra(Health vida, HealthBarUI[] barras)
    {
        foreach (HealthBarUI barra in barras)
            if (barra.target == vida) return true;
        return false;
    }

    // "enemy-1-patrol/Canvas": para que el aviso diga donde esta el objeto.
    static string Ruta(Transform t)
    {
        string ruta = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            ruta = t.name + "/" + ruta;
        }
        return ruta;
    }
}
