using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeath : MonoBehaviour
{
    [Header("Refs")]
    public Health health;
    public Animator animator;

    [Header("Animator")]
    public string dieTrigger = "Die";

    [Header("Timing")]
    [Tooltip("Cuanto se ve la animacion de morir antes de que la pantalla empiece a oscurecerse.")]
    public float verAnimacion = 1.6f;

    [Tooltip("Cuanto dura el oscurecido, estilo Dark Souls. El juego sigue corriendo mientras tanto. "
           + "Con la pantalla de derrota nueva el oscurecido lo dibuja 'GM_UI_Derrota': pone el mismo numero en su 'Dim Time'.")]
    public float fundidoNegro = 1.5f;

    [Tooltip("Pausa a oscuras antes de mostrar DEFEAT. Cuando termina, el juego se frena y vuelve el mouse.")]
    public float esperaEnNegro = 0.6f;

    [Header("UI")]
    public GameObject gameOverPanel;   // DeathMenu / GameOverPanel
    public GameObject mainMenuPanel;   // MainMenuPanel (en la misma escena)

    [Tooltip("Tildado (lo tilda Green Mist > 9. Menus): la pantalla de derrota nueva hace ella sola el oscurecido lento, "
           + "asi que Game Over Panel se prende apenas termina 'Ver Animacion'. "
           + "Destildado (menu viejo): el panel aparece de golpe al final, despues de 'Fundido Negro' y 'Espera En Negro'.")]
    public bool panelFadesIn = false;

    [Header("Disable On Death")]
    public MonoBehaviour[] disableOnDeath;
    public CharacterController characterController;

    bool dead;
    bool recargando;   // ya se eligio RETRY o MAIN MENU: la escena se recarga en el proximo frame

    void Awake()
    {
        if (!health) health = GetComponent<Health>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!characterController) characterController = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        if (health) health.OnDied += HandleDied;
    }

    void OnDisable()
    {
        if (health) health.OnDied -= HandleDied;
    }

    void HandleDied()
    {
        if (dead) return;
        dead = true;

        // cortar control
        if (disableOnDeath != null)
            foreach (var b in disableOnDeath)
                if (b) b.enabled = false;

        if (characterController) characterController.enabled = false;

        // disparar anim
        if (animator)
        {
            animator.updateMode = AnimatorUpdateMode.UnscaledTime; // clave si después pausás
            animator.ResetTrigger(dieTrigger);
            animator.SetTrigger(dieTrigger);
        }

        StartCoroutine(DeathFlow());
    }

    IEnumerator DeathFlow()
    {
        // 1. Dejar que se vea caer. El juego sigue corriendo: la camara
        //    queda sobre el cuerpo, los enemigos se quedan ahi.
        yield return new WaitForSecondsRealtime(verAnimacion);

        // 2. Pantalla de derrota NUEVA: se prende ya. Ella sola oscurece la
        //    pantalla de a poco y despues muestra DEFEAT (DefeatScreenView).
        //
        //    Antes este paso le pedia un fundido a negro a ScreenFader, pero ese
        //    fundido nunca se llego a ver (el FadeCanvas queda transparente
        //    despues de PLAY) y, si se hubiera visto, habria tapado el menu de
        //    derrota: el FadeCanvas se dibuja encima de todo.
        if (panelFadesIn && gameOverPanel) gameOverPanel.SetActive(true);

        // 3. Lo que dura el oscurecido mas un respiro a oscuras.
        yield return new WaitForSecondsRealtime(fundidoNegro + esperaEnNegro);

        // 4. Menu viejo: aparece recien ahora, de golpe. (Con la pantalla nueva
        //    ya estaba prendido y esta linea no cambia nada.)
        if (gameOverPanel) gameOverPanel.SetActive(true);

        // Pausar el juego. La UI sigue andando con unscaled time.
        Time.timeScale = 0f;

        // Si justo habia un hit stop corriendo (el golpe congela el tiempo
        // 0.06 s y despues lo vuelve a poner en 1), el juego volvia a andar
        // detras del menu y el mouse quedaba escondido. Lo frenamos de nuevo.
        yield return new WaitForSecondsRealtime(0.1f);
        Time.timeScale = 0f;
    }

    // ===== BOTONES (tienen que ser public void sin parámetros) =====

    public void Retry()
    {
        RecargarEscena();
    }

    // Vuelve al menu principal RECARGANDO la escena, igual que el MAIN MENU de
    // la pausa (PauseMenu.GoToMainMenu). La escena siempre arranca en el menu
    // principal (MainMenuUI.Start), y asi Onji, los enemigos y el mundo vuelven
    // a cero. Antes solo se prendia el panel del menu sin recargar nada, y al
    // apretar PLAY Onji seguia muerto.
    public void MainMenu()
    {
        RecargarEscena();
    }

    void RecargarEscena()
    {
        if (recargando) return;   // ya se pidio: no recargar dos veces (clic + Enter a la vez)
        recargando = true;

        // Si la corrutina de la muerte seguia viva, que no vuelva a frenar el tiempo.
        StopAllCoroutines();

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Quit()
    {
        Time.timeScale = 1f;
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    // ===== PARA PROBAR =====

    // Mata a Onji al instante, para ver la pantalla de derrota sin pelear.
    // Como se usa: jugando (despues de apretar PLAY), apreta Esc. Se abre la
    // pausa y vuelve el mouse (mientras se juega el mouse esta encerrado en la
    // Game view y no se puede llegar al Inspector). Elegi PlayerRoot y en el
    // Inspector hace clic derecho sobre el titulo "Player Death" > "Probar derrota".
    // La pausa se cierra sola y Onji cae.
    [ContextMenu("Probar derrota (solo jugando)")]
    void ProbarDerrota()
    {
        if (!Application.isPlaying || health == null) return;
        if (health.Current <= 0) return;   // ya esta muerto: no hay nada que probar

        // En el menu principal no: quedarian dos pantallas una encima de la otra.
        if (mainMenuPanel != null && mainMenuPanel.activeInHierarchy)
        {
            Debug.Log("[Green Mist] Probar derrota: solo funciona JUGANDO. Apretá PLAY y probá de nuevo.");
            return;
        }

        // Si el juego esta en pausa (Esc), primero la cerramos: asi la pantalla de
        // derrota no aparece encima del menu de pausa y el juego vuelve a correr.
        if (Time.timeScale == 0f)
        {
            PauseMenu pausa = FindFirstObjectByType<PauseMenu>();
            if (pausa != null) pausa.Resume();
        }

        // Sigue frenado y no era la pausa: mejor no hacer nada.
        if (Time.timeScale == 0f)
        {
            Debug.Log("[Green Mist] Probar derrota: el juego está frenado y no es por la pausa. Probá de nuevo en un momento.");
            return;
        }

        health.Take(health.Current);
    }
}
