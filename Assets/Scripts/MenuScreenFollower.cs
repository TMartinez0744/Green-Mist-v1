using UnityEngine;

// Prende y apaga las pantallas NUEVAS (menu de inicio y derrota) copiando lo
// que hacen los paneles VIEJOS. Va en el objeto "GM_UI_Menus_Canvas".
//
// POR QUE EXISTE: medio juego usa "MainMenuPanel esta prendido" como "estamos
// en el menu" y "DeathMenu esta prendido" como "game over" (MainMenuUI,
// PlayerDeath, PauseMenu, la barra de vida, los petalos del menu, los grillos,
// los pasos...). Esos dos objetos siguen siendo los INTERRUPTORES: nadie dejo
// de mirarlos. Lo unico que cambio es que ahora estan vacios (lo viejo que
// tenian adentro quedo apagado) y las pantallas nuevas viven en otro Canvas,
// uno que escala con la pantalla.
//
// Este script es el cable entre las dos cosas: mira cada interruptor y deja su
// pantalla nueva igual (prendida o apagada). Nada mas. No pausa, no lee teclas
// y no decide nada.
[DisallowMultipleComponent]
public class MenuScreenFollower : MonoBehaviour
{
    [Header("Menu de inicio")]
    [Tooltip("El interruptor: 'MainMenuPanel', el panel que prende y apaga MainMenuUI.")]
    public GameObject mainMenuPanel;

    [Tooltip("La pantalla nueva ('GM_UI_Inicio'). Se prende y se apaga junto con el interruptor.")]
    public GameObject startScreen;

    [Header("Derrota")]
    [Tooltip("El interruptor: 'DeathMenu', el panel que prende PlayerDeath cuando Onji muere.")]
    public GameObject gameOverPanel;

    [Tooltip("La pantalla nueva ('GM_UI_Derrota'). Se prende y se apaga junto con el interruptor.")]
    public GameObject defeatScreen;

    [Header("Interno (lo anota Green Mist > 9, lo usa 9b para volver atras)")]
    [Tooltip("Los objetos viejos que apago el menu Green Mist > 9. No hace falta tocarlo.")]
    public GameObject[] hiddenOldObjects;

    [Tooltip("Las imagenes viejas (el fondo de los paneles) que desactivo el menu Green Mist > 9. No hace falta tocarlo.")]
    public Behaviour[] hiddenOldGraphics;

    void Start()
    {
        Sincronizar();
    }

    // LateUpdate y no Update: MainMenuUI y PlayerDeath cambian los paneles desde
    // corrutinas, que corren DESPUES de todos los Update. Asi la pantalla nueva
    // cambia en el mismo frame que su interruptor, antes de que se dibuje nada.
    void LateUpdate()
    {
        Sincronizar();
    }

    void Sincronizar()
    {
        Copiar(mainMenuPanel, startScreen);
        Copiar(gameOverPanel, defeatScreen);
    }

    static void Copiar(GameObject interruptor, GameObject pantalla)
    {
        // Si falta alguna de las dos referencias no tocamos nada.
        if (interruptor == null || pantalla == null) return;

        bool prendido = interruptor.activeInHierarchy;
        if (pantalla.activeSelf != prendido) pantalla.SetActive(prendido);
    }
}
