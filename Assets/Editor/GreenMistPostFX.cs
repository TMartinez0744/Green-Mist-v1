using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Menu "Green Mist > 2. Bloom y color de noche".
//
// Crea (o actualiza) un Volume global llamado "GreenMist_VolumeNoche" con su propio
// profile en Assets/GreenMist/GreenMistNoche.asset. NO toca tu Global Volume ni
// SampleSceneProfile: este se suma encima, solo de noche (ver NightVolumeWeight).
//
// Si lo corres de nuevo, vuelve a poner estos valores y pisa los que hayas
// cambiado a mano en el profile.
public static class GreenMistPostFX
{
    const string RutaProfile = GreenMistEditorUtil.CarpetaGenerados + "/GreenMistNoche.asset";
    const string NombreVolume = "GreenMist_VolumeNoche";

    [MenuItem("Green Mist/2. Bloom y color de noche", false, 102)]
    public static void Aplicar()
    {
        if (!GreenMistEditorUtil.PuedeEditarEscena()) return;

        // ---------- 1) El profile (un archivo nuevo, aparte del tuyo) ----------
        GreenMistEditorUtil.AsegurarCarpeta(GreenMistEditorUtil.CarpetaGenerados);

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaProfile);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, RutaProfile);
        }

        // BLOOM: el halo. Todo pixel mas brillante que "threshold" derrama luz alrededor.
        // Con 1 casi nada llegaba; con 0.75 entran el fuego y lo que el farol ilumina de cerca.
        Bloom bloom = Componente<Bloom>(profile);
        bloom.threshold.Override(0.75f);            // desde que brillo empieza el halo
        bloom.intensity.Override(0.9f);             // cuanta luz derrama
        bloom.scatter.Override(0.72f);              // que tan ancho es el halo
        bloom.tint.Override(new Color(1f, 0.88f, 0.72f)); // halo calido, como fuego
        bloom.highQualityFiltering.Override(true);

        // SPLIT TONING: tiñe las sombras de un color y las luces de otro.
        // Es lo que da el contraste frio/calido: sombras azul-verdosas, luces naranjas.
        // (0.5, 0.5, 0.5) seria "sin teñir".
        SplitToning tonos = Componente<SplitToning>(profile);
        tonos.shadows.Override(new Color(0.38f, 0.52f, 0.52f));
        tonos.highlights.Override(new Color(0.62f, 0.50f, 0.38f));
        tonos.balance.Override(-15f);               // negativo = el tinte de sombras ocupa mas

        // COLOR ADJUSTMENTS: retoques generales de la imagen.
        ColorAdjustments ajustes = Componente<ColorAdjustments>(profile);
        ajustes.postExposure.Override(0f);          // brillo general (0 = no tocar)
        ajustes.contrast.Override(15f);             // sombras mas profundas, luces mas marcadas
        ajustes.saturation.Override(-10f);          // paleta un poco apagada (ukiyo-e)
        ajustes.colorFilter.Override(Color.white);  // blanco = no teñir; el tinte lo hace Split Toning

        // WHITE BALANCE: corre TODA la imagen (luces incluidas) hacia frio/calido y verde/magenta.
        // Va muy suave a proposito: con valores fuertes los faroles dejaban de verse naranjas.
        WhiteBalance balance = Componente<WhiteBalance>(profile);
        balance.temperature.Override(-3f);          // negativo = mas frio (azulado)
        balance.tint.Override(-2f);                 // negativo = hacia el verde

        // VIGNETTE: oscurece los bordes para llevar la mirada al centro.
        Vignette vineta = Componente<Vignette>(profile);
        vineta.intensity.Override(0.33f);
        vineta.smoothness.Override(0.45f);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        // ---------- 2) El Volume en la escena ----------
        GameObject go = GreenMistEditorUtil.BuscarOCrearRaiz(NombreVolume);

        Volume volume = GreenMistEditorUtil.ObtenerOAgregar<Volume>(go);
        Undo.RecordObject(volume, "Green Mist: Volume de noche");
        volume.isGlobal = true;
        volume.priority = 10f;                      // mayor que el Global Volume (0): se aplica encima
        volume.weight = 1f;
        volume.sharedProfile = profile;

        NightVolumeWeight peso = GreenMistEditorUtil.ObtenerOAgregar<NightVolumeWeight>(go);
        Undo.RecordObject(peso, "Green Mist: Volume de noche");
        peso.timeOfDay = GreenMistEditorUtil.BuscarTimeOfDay();

        EditorUtility.SetDirty(volume);
        EditorUtility.SetDirty(peso);
        GreenMistEditorUtil.MarcarEscenaSucia();

        Debug.Log("[Green Mist] Bloom y color de noche listos: objeto '" + NombreVolume
                  + "' y profile " + RutaProfile + ".");

        // La escena esta guardada casi de dia: a esa hora este efecto esta apagado a proposito.
        if (peso.timeOfDay != null && peso.timeOfDay.time >= peso.offFrom)
        {
            Debug.LogWarning("[Green Mist] Ahora 'time' del TimeOfDay vale " + peso.timeOfDay.time
                             + ": a esa hora (amanecer) el efecto de noche está apagado. "
                             + "Poné 'time' en 0 o 0.5 en el objeto TimeOfDay para verlo.");
        }
    }

    // Devuelve el override de tipo T del profile; si no esta, lo crea y lo guarda dentro del .asset.
    // Tambien lo deja tildado, por si lo habias destildado para comparar.
    static T Componente<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (!profile.TryGet(out T c))
            c = VolumeProfileFactory.CreateVolumeComponent<T>(profile, false, false);
        c.active = true;
        return c;
    }
}
