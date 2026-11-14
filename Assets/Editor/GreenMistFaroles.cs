using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu "Green Mist > 1. Faroles que titilan".
//
// Agrega LanternFlicker a la RAIZ de los 3 prefabs de farol/brasero y le pone
// los valores de cada tipo. Se puede correr las veces que quieras: si el
// componente ya esta, solo actualiza los valores (no lo duplica).
//
// Modifica los PREFABS, no la escena. No se deshace con Ctrl+Z: para volver
// atras se usa git sobre los 3 archivos .prefab.
public static class GreenMistFaroles
{
    const string Papel = "Assets/Prefabs/SM_Prop_Paper_Lantern_01 Variant.prefab";
    const string Piedra = "Assets/Prefabs/SM_Prop_Stone_Lantern_02.prefab";
    const string Brasero = "Assets/Prefabs/Brasero.prefab";

    [MenuItem("Green Mist/1. Faroles que titilan", false, 101)]
    static void Aplicar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Green Mist] Salí de Play mode antes de correr este menú.");
            return;
        }

        // Si hay un prefab abierto para editar, guardar por debajo puede pisarse con lo abierto.
        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            Debug.LogWarning("[Green Mist] Cerrá el Prefab Mode (la flecha '<' arriba de la Hierarchy) antes de correr este menú.");
            return;
        }

        int nuevos = 0, actualizados = 0;

        //                  amount  speed  crackle  crackleSpeed
        Configurar(Papel,   0.30f,  2.2f,  0.25f,   8f,  ref nuevos, ref actualizados); // medio
        Configurar(Piedra,  0.16f,  1.3f,  0.15f,   5f,  ref nuevos, ref actualizados); // el más calmo
        Configurar(Brasero, 0.50f,  3.5f,  0.45f,  12f,  ref nuevos, ref actualizados); // el más vivo

        Debug.Log("[Green Mist] Faroles listos: " + nuevos + " prefabs con componente nuevo, "
                  + actualizados + " actualizados.");
    }

    static void Configurar(string ruta, float amount, float speed, float crackle, float crackleSpeed,
                           ref int nuevos, ref int actualizados)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) == null)
        {
            Debug.LogError("[Green Mist] No encontré el prefab: " + ruta);
            return;
        }

        // Abre el contenido del prefab, deja editarlo y al salir del "using" lo guarda solo.
        using (var edicion = new PrefabUtility.EditPrefabContentsScope(ruta))
        {
            GameObject raiz = edicion.prefabContentsRoot;

            LanternFlicker titileo = raiz.GetComponent<LanternFlicker>();
            if (titileo == null)
            {
                titileo = raiz.AddComponent<LanternFlicker>();
                nuevos++;
            }
            else
            {
                actualizados++;
            }

            titileo.amount = amount;
            titileo.speed = speed;
            titileo.crackle = crackle;
            titileo.crackleSpeed = crackleSpeed;
        }
    }
}
