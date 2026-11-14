using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Funciones compartidas por todos los menus "Green Mist".
// No tiene menu propio: solo ayuda a los demas scripts de esta carpeta.
public static class GreenMistEditorUtil
{
    public const string RutaEscena = "Assets/Scenes/SampleScene.unity";

    // Carpeta donde se guarda todo lo que generan los menus (materiales, texturas, profiles).
    public const string CarpetaGenerados = "Assets/GreenMist";

    // El contenedor del pueblo. Sus hijos usan coordenadas LOCALES a el.
    public const string NombreAldea = "Assets";

    // Todo lo nuevo del pueblo cuelga de este hijo de "Assets" (posicion local 0,0,0).
    public const string NombreAmbiente = "GreenMist_Ambiente";

    // Devuelve true si se puede tocar la escena. Si no, avisa por Console por que no.
    public static bool PuedeEditarEscena()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Green Mist] Salí de Play mode antes de correr este menú.");
            return false;
        }
        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            Debug.LogWarning("[Green Mist] Cerrá el Prefab Mode (la flecha '<' arriba de la Hierarchy) antes de correr este menú.");
            return false;
        }
        if (SceneManager.GetActiveScene().path != RutaEscena)
        {
            Debug.LogWarning("[Green Mist] Abrí la escena " + RutaEscena + " antes de correr este menú.");
            return false;
        }
        return true;
    }

    // Busca un objeto en la RAIZ de la escena por nombre (tambien si esta desactivado).
    public static GameObject BuscarRaiz(string nombre)
    {
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
            if (go.name == nombre) return go;
        return null;
    }

    // El contenedor "Assets" del pueblo. Devuelve null (y avisa) si no esta.
    public static Transform Aldea()
    {
        GameObject go = BuscarRaiz(NombreAldea);
        if (go == null)
        {
            Debug.LogError("[Green Mist] No encontré el objeto raíz '" + NombreAldea + "' (el contenedor del pueblo).");
            return null;
        }
        return go.transform;
    }

    // El hijo "GreenMist_Ambiente" de "Assets". Lo crea si no existe.
    // Como queda en posicion local (0,0,0), sus hijos usan las mismas coordenadas locales que el pueblo.
    public static Transform ContenedorAmbiente()
    {
        Transform aldea = Aldea();
        if (aldea == null) return null;
        return BuscarOCrearHijo(aldea, NombreAmbiente).transform;
    }

    // Busca un hijo DIRECTO por nombre; si no existe lo crea (con Undo) en posicion local cero.
    public static GameObject BuscarOCrearHijo(Transform padre, string nombre)
    {
        Transform t = padre.Find(nombre);
        if (t != null) return t.gameObject;

        GameObject go = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(go, "Green Mist: crear " + nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go;
    }

    // Busca o crea un objeto en la RAIZ de la escena (con Undo).
    public static GameObject BuscarOCrearRaiz(string nombre)
    {
        GameObject go = BuscarRaiz(nombre);
        if (go != null) return go;

        go = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(go, "Green Mist: crear " + nombre);
        return go;
    }

    // Devuelve el componente T del objeto; si no lo tiene, lo agrega (con Undo).
    public static T ObtenerOAgregar<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = Undo.AddComponent<T>(go);
        return c;
    }

    // El TimeOfDay de la escena (hay uno solo, en el objeto raiz "TimeOfDay").
    public static TimeOfDay BuscarTimeOfDay()
    {
        TimeOfDay t = Object.FindFirstObjectByType<TimeOfDay>(FindObjectsInactive.Include);
        if (t == null) Debug.LogWarning("[Green Mist] No encontré ningún TimeOfDay en la escena.");
        return t;
    }

    // Altura del suelo (Y de mundo) debajo de un punto de mundo, segun el Terrain.
    public static float AlturaSueloMundo(Vector3 puntoMundo)
    {
        Terrain terreno = Terrain.activeTerrain;
        if (terreno == null) return 0f;
        return terreno.SampleHeight(puntoMundo) + terreno.transform.position.y;
    }

    // Crea la carpeta (y las de arriba) si no existe. Ej: "Assets/GreenMist/Materials".
    public static void AsegurarCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;

        string padre = System.IO.Path.GetDirectoryName(ruta).Replace('\\', '/');
        string nombre = System.IO.Path.GetFileName(ruta);
        AsegurarCarpeta(padre);
        AssetDatabase.CreateFolder(padre, nombre);
    }

    // Marca la escena como modificada, para que Ctrl+S la guarde.
    public static void MarcarEscenaSucia()
    {
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
}
