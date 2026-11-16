using UnityEditor;
using UnityEngine;

// Menu "Green Mist > 0. Aplicar todo (2 a 10)".
//
// Corre de una los menus 2, 3, 4, 5, 7, 8, 9 y 10 (el 6, giro asistido, es solo
// codigo y no necesita menu; el 1, faroles, ya lo corriste y toca prefabs, no la escena).
// Se puede correr las veces que quieras: no duplica nada.
public static class GreenMistTodo
{
    [MenuItem("Green Mist/0. Aplicar todo (2 a 10)", false, 50)]
    public static void Aplicar()
    {
        if (!GreenMistEditorUtil.PuedeEditarEscena()) return;

        // Agrupa todo en un solo paso de Undo: un Ctrl+Z deshace todo junto.
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Green Mist: aplicar todo");
        int grupo = Undo.GetCurrentGroup();

        GreenMistPostFX.Aplicar();      // 2. Bloom y color de noche
        GreenMistNiebla.Aplicar();      // 3. Niebla verde
        GreenMistLuciernagas.Aplicar(); // 4. Luciernagas
        GreenMistPetalos.Aplicar();     // 5. Petalos de sakura
        GreenMistSonido.Aplicar();      // 7. Sonido
        GreenMistUI.Aplicar();          // 8. Interfaz (pausa y vida)
        GreenMistUIMenus.Aplicar();     // 9. Menus (inicio y derrota)
        GreenMistUIEnemigos.Aplicar();  // 10. Barras de vida de enemigos

        Undo.CollapseUndoOperations(grupo);

        Debug.Log("[Green Mist] Listo todo (2 a 10). Guardá la escena con Ctrl+S.");
    }
}
