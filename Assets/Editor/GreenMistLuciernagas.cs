using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Menu "Green Mist > 4. Luciérnagas".
//
// Crea (o actualiza) puntitos de luz que flotan despacio sobre el estanque y a lo largo
// de la avenida de sakuras del sando. Son SOLO DE NOCHE: el componente NightParticleFade
// del grupo los apaga hacia el amanecer.
//
// Todo cuelga de "Assets/GreenMist_Ambiente/GreenMist_Luciernagas".
//
// Si lo corres de nuevo: los emisores se rearman con estos valores, pero los valores de
// NightParticleFade (a que hora se apagan) se respetan.
public static class GreenMistLuciernagas
{
    const string NombreGrupo = "GreenMist_Luciernagas";
    const string Prefijo = "Luciernagas_";

    // Particulas VIVAS por zona. Total 110 (tope acordado: 120).
    const int CantEstanque = 50;
    const int CantAvenida = 60;

    [MenuItem("Green Mist/4. Luciérnagas", false, 104)]
    public static void Aplicar()
    {
        if (!GreenMistEditorUtil.PuedeEditarEscena()) return;

        Transform aldea = GreenMistEditorUtil.Aldea();
        if (aldea == null) return;
        Transform ambiente = GreenMistEditorUtil.ContenedorAmbiente();
        if (ambiente == null) return;

        // ---------- 1) El grupo y el componente que las apaga de dia ----------
        GameObject grupo = GreenMistEditorUtil.BuscarOCrearHijo(ambiente, NombreGrupo);
        Undo.RecordObject(grupo.transform, "Green Mist: luciérnagas");
        grupo.transform.localPosition = Vector3.zero;      // asi sus hijos usan las coordenadas del pueblo
        grupo.transform.localRotation = Quaternion.identity;
        grupo.transform.localScale = Vector3.one;

        TimeOfDay hora = GreenMistEditorUtil.BuscarTimeOfDay();
        NightParticleFade noche = GreenMistEditorUtil.ObtenerOAgregar<NightParticleFade>(grupo);
        Undo.RecordObject(noche, "Green Mist: luciérnagas");
        noche.timeOfDay = hora;
        EditorUtility.SetDirty(noche);

        // ---------- 2) Textura y material ----------
        Texture2D textura = GreenMistParticulas.TexturaPunto();
        if (textura == null) return;

        // Color con valores MAYORES que 1 a proposito: el color de cada particula va de 0 a 1,
        // asi que para pasar el umbral del bloom (0.75) y tener halo, el brillo extra
        // tiene que venir del material. Amarillo verdoso, como una luciernaga de verdad.
        Color brillo = new Color(1.65f, 1.8f, 0.75f, 1f);

        Material mat = GreenMistParticulas.MaterialParticulas(
            "GM_Luciernagas",
            false,          // sin luz (Particles/Unlit): ellas son la luz
            textura, brillo,
            true,           // ADITIVO: suma luz en vez de tapar lo de atras
            false,
            0f,             // sin soft particles: son tan chicas que no hace falta
            0.3f, 1.2f,     // se apagan si pasan a menos de 30 cm de la camara (si no, harian un fogonazo)
            GreenMistParticulas.ColaLuciernagas);
        if (mat == null) return;

        // ---------- 3) Los emisores ----------
        List<string> usados = new List<string>();
        Transform padre = grupo.transform;

        // --- Estanque: una caja que cubre el agua y un poco de orilla, de 0.4 a 2.1 m sobre el agua.
        // El puente pasa por el medio: las que queden dentro del puente simplemente no se ven
        // (quedan tapadas por la madera), no hace falta esquivarlo.
        Vector3 centroAgua = new Vector3(60f, -30.03f, 545f);
        float radio = 6.5f;
        Transform agua = aldea.Find("Agua pond");
        if (agua != null)
        {
            centroAgua = padre.InverseTransformPoint(agua.position);
            WaterWaves olas = agua.GetComponent<WaterWaves>();
            if (olas != null) radio = olas.radio * agua.lossyScale.x;
        }
        else
        {
            Debug.LogWarning("[Green Mist] No encontré 'Agua pond' como hijo de 'Assets': puse las luciérnagas del estanque en su posición original (60, -30, 545).");
        }

        float lado = radio * 2f + 5f;
        ParticleSystem estanque = GreenMistParticulas.PrepararEmisor(padre, "Luciernagas_Estanque", usados);
        estanque.transform.localPosition = centroAgua + new Vector3(0f, 1.25f, 0f);
        ConfigurarLuciernagas(estanque, mat, new Vector3(lado, 1.7f, lado), CantEstanque);

        // --- Avenida de sakuras: el tramo del sando donde estan los arboles (X 19 a 43).
        // El largo en Z sale de donde estan los arboles de verdad (su caja de dibujado), no de numeros fijos.
        float zMin = 515f, zMax = 575f;
        List<Transform> sakuras = GreenMistParticulas.BuscarSakuras(aldea);
        bool hayArboles = false;
        foreach (Transform arbol in sakuras)
        {
            Bounds caja;
            if (!GreenMistParticulas.LimitesDe(arbol, out caja)) continue;

            float z0 = padre.InverseTransformPoint(caja.min).z;
            float z1 = padre.InverseTransformPoint(caja.max).z;
            if (!hayArboles) { zMin = Mathf.Min(z0, z1); zMax = Mathf.Max(z0, z1); hayArboles = true; }
            else { zMin = Mathf.Min(zMin, Mathf.Min(z0, z1)); zMax = Mathf.Max(zMax, Mathf.Max(z0, z1)); }
        }
        if (!hayArboles)
            Debug.LogWarning("[Green Mist] No encontré los árboles 'SAKURA-N': puse las luciérnagas de la avenida entre Z 515 y Z 575.");

        // Un poco de margen, sin salirse del sando (Z 490 a 590).
        zMin = Mathf.Clamp(zMin - 3f, 490f, 580f);
        zMax = Mathf.Clamp(zMax + 3f, zMin + 10f, 590f);

        ParticleSystem avenida = GreenMistParticulas.PrepararEmisor(padre, "Luciernagas_Avenida", usados);
        avenida.transform.localPosition = new Vector3(31f, GreenMistParticulas.SueloLocalY + 1.5f, (zMin + zMax) * 0.5f);
        ConfigurarLuciernagas(avenida, mat, new Vector3(24f, 2.2f, zMax - zMin), CantAvenida);

        int borrados = GreenMistParticulas.BorrarSobrantes(padre, Prefijo, usados);
        GreenMistParticulas.VistaPrevia(padre);

        AssetDatabase.SaveAssets();
        GreenMistEditorUtil.MarcarEscenaSucia();

        Debug.Log("[Green Mist] Luciérnagas listas: " + usados.Count + " emisores y " + (CantEstanque + CantAvenida)
                  + " partículas (tope 120) en '" + GreenMistEditorUtil.NombreAmbiente + "/" + NombreGrupo + "'"
                  + (borrados > 0 ? ", " + borrados + " emisores viejos borrados" : "") + ".");

        // Aviso util: con la hora guardada en la escena (0.98) estan apagadas y parece que el menu no hizo nada.
        // (Max: el componente ordena los dos umbrales, asi que la hora de apagado es el mayor de los dos.)
        float apagadasDesde = Mathf.Max(noche.fullUntil, noche.offFrom);
        if (hora != null && hora.time >= apagadasDesde)
            Debug.LogWarning("[Green Mist] Ojo: son solo de noche. Ahora TimeOfDay.time = " + hora.time.ToString("0.00")
                             + " y se apagan desde " + apagadasDesde.ToString("0.00")
                             + ", así que en Play NO se van a ver. Bajá 'Time' del objeto TimeOfDay a 0 o 0.5 para verlas.");
    }

    // Los valores de un emisor de luciernagas: pocas, diminutas, que deambulan y laten.
    static void ConfigurarLuciernagas(ParticleSystem ps, Material mat, Vector3 caja, int cantidad)
    {
        GreenMistParticulas.ConfigurarBase(ps, cantidad, 7f, 12f);

        ParticleSystem.MainModule main = ps.main;
        main.startSize3D = false;
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);   // 5 a 11 cm: el halo lo pone el bloom
        main.startRotation3D = false;
        main.startRotation = 0f;

        // Entre amarillo calido y verde claro. El brillo de verdad lo pone el material.
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.92f, 0.55f, 1f), new Color(0.72f, 1f, 0.60f, 1f));

        GreenMistParticulas.FormaCaja(ps, caja);

        // Casi quietas, apenas subiendo; el paseo errante lo da el ruido.
        GreenMistParticulas.Velocidad(ps, new Vector3(-0.05f, 0f, -0.05f), new Vector3(0.05f, 0.06f, 0.05f));
        GreenMistParticulas.Ruido(ps, new Vector3(0.30f, 0.18f, 0.30f), 0.3f, 0.15f);

        // Transparencia a lo largo de su vida: aparece, late una vez (baja y vuelve a subir) y se apaga.
        Gradient latido = new Gradient();
        latido.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.12f),
                new GradientAlphaKey(0.25f, 0.42f),
                new GradientAlphaKey(1f, 0.62f),
                new GradientAlphaKey(0f, 1f)
            });
        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(latido);

        ParticleSystem.SizeOverLifetimeModule tam = ps.sizeOverLifetime;
        tam.enabled = false;
        ParticleSystem.RotationOverLifetimeModule giro = ps.rotationOverLifetime;
        giro.enabled = false;

        GreenMistParticulas.ConfigurarRenderer(ps, mat, 0.5f);
    }
}
