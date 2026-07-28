using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    // Propiedad estática accesible globalmente para consultar si el juego acepta inputs de los jugadores
    public static bool EstaPausado { get; private set; } = false;
    public static bool PuedeAceptarInputs => !EstaPausado && Instance != null && !Instance.partidaFinalizada;

    [Header("Referencias Principales")]
    [SerializeField] private GestorTerraformacion gestorTerraformacion;
    [SerializeField] private GeneradorNodos generadorNodos;
    [SerializeField] private DatosNivel datosNivel;
    [SerializeField] private SpawnEnemigos spawnEnemigos;
    [SerializeField] private Pausa pausaUI;

    [Header("Estado de Nivel")]
    private bool partidaFinalizada = false;
    private bool victoriaProcesada = false;
    private bool derrotaProcesada = false;

    public GameObject olaTerraformacion;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Asegurarse de arrancar sin pausa y con Time.timeScale normal
        EstaPausado = false;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        BuscarReferenciasAutomaticas();
    }

    private void OnDestroy()
    {
        // Restaurar timeScale al destruir el manager o cambiar de escena
        Time.timeScale = 1f;
        EstaPausado = false;
    }

    private void BuscarReferenciasAutomaticas()
    {
        if (gestorTerraformacion == null)
            gestorTerraformacion = FindAnyObjectByType<GestorTerraformacion>();

        if (generadorNodos == null)
            generadorNodos = FindAnyObjectByType<GeneradorNodos>();

        if (spawnEnemigos == null)
            spawnEnemigos = FindAnyObjectByType<SpawnEnemigos>();

        if (pausaUI == null)
            pausaUI = FindAnyObjectByType<Pausa>();
    }

    private void Update()
    {
        // Detectar tecla de pausa (ESC)
        if (Input.GetKeyDown(KeyCode.Escape) && !partidaFinalizada)
        {
            AlternarPausa();
        }

        if (partidaFinalizada) return;

        VerificarVictoria();
        VerificarDerrotaNodos();
    }

    // --- SISTEMA DE PAUSA GLOBAL ---

    public void AlternarPausa()
    {
        if (partidaFinalizada) return;

        if (EstaPausado)
        {
            ReanudarJuego();
        }
        else
        {
            PausarJuego();
        }
    }

    public void PausarJuego()
    {
        if (partidaFinalizada) return;

        EstaPausado = true;
        Time.timeScale = 0f; // Congelar la actualización de frames y físicas del juego

        if (pausaUI == null)
            pausaUI = FindAnyObjectByType<Pausa>();

        if (pausaUI != null)
        {
            pausaUI.EstablecerEstadoPausaVisual(true);
        }

        Debug.Log("[LevelManager] Juego Pausado (Time.timeScale = 0f).");
    }

    public void ReanudarJuego()
    {
        EstaPausado = false;
        Time.timeScale = 1f;

        if (pausaUI == null)
            pausaUI = FindAnyObjectByType<Pausa>();

        if (pausaUI != null)
        {
            pausaUI.EstablecerEstadoPausaVisual(false);
        }

        Debug.Log("[LevelManager] Juego Reanudado (Time.timeScale = 1).");
    }

    // --- VERIFICACIONES DE CONDICIONES DE NIVEL ---

    private void VerificarVictoria()
    {
        if (gestorTerraformacion == null) return;

        if (gestorTerraformacion.porcentajeActual.Value >= 1.0f && !partidaFinalizada)
        {
            ProcesarVictoria();
        }
    }

    private void VerificarDerrotaNodos()
    {
        if (generadorNodos == null || generadorNodos.NodosCreados == null || generadorNodos.NodosCreados.Length == 0) return;

        int nodosVivosNoRotos = 0;
        for (int i = 0; i < generadorNodos.NodosCreados.Length; i++)
        {
            if (generadorNodos.NodosCreados[i] != null && !generadorNodos.NodosCreados[i].EstaRoto())
            {
                nodosVivosNoRotos++;
            }
        }

        // Se activa la derrota únicamente cuando TODOS los nodos están ROTOS (vida <= 0)
        if (nodosVivosNoRotos == 0 && !partidaFinalizada)
        {
            ProcesarDerrota("¡Todas las Antenas han sido destruidas!");
        }
    }

    public void NotificarMuerteJugador()
    {
        if (partidaFinalizada) return;

        // Buscar todos los controladores de jugador en la escena (incluyendo objetos deshabilitados/muertos)
        PlayerController[] jugadores = FindObjectsOfType<PlayerController>(true);

        int jugadoresVivos = 0;
        foreach (var p in jugadores)
        {
            if (p != null && p.gameObject.activeSelf && p.vida > 0f)
            {
                jugadoresVivos++;
            }
        }

        Debug.Log($"[LevelManager] Notificación de muerte recibida. Jugadores con vida activa: {jugadoresVivos}");

        // Se declara la derrota únicamente cuando TODOS los jugadores están muertos (0 vivos)
        if (jugadoresVivos == 0)
        {
            ProcesarDerrota("¡Todos los exploradores han sido destruidos!");
        }
    }

    private void ProcesarVictoria()
    {
        partidaFinalizada = true;
        victoriaProcesada = true;

        Debug.Log("[LevelManager] ¡Victoria Alcanzada! Disparando onda de terraformación verde.");

        if (spawnEnemigos != null)
        {
            spawnEnemigos.DetenerSpawneo();
        }

        // Bloquear inputs del jugador solo deteniendo velocidad, sin deshabilitar el script
        // (deshabilitar PlayerController rompe la gestion de escenas de Netcode)
        PlayerController[] jugadores = FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in jugadores)
        {
            if (p == null) continue;
            Rigidbody2D rb = p.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        int nivelActual = datosNivel != null ? datosNivel.numeroNivel : 1;
        if (HudController.Instance != null)
        {
            HudController.Instance.MostrarVictoria(nivelActual);
        }

        StartCoroutine(OlaTerraformacionCo());
        StartCoroutine(RutinaVictoriaCo());
    }

    private IEnumerator OlaTerraformacionCo()
    {
        if (olaTerraformacion != null)
        {
            // 1. Activar el GameObject de la ola de terraformación
            olaTerraformacion.SetActive(true);

            // 2. Disparar el trigger 'ola' en el Animator
            Animator anim = olaTerraformacion.GetComponent<Animator>();
            if (anim == null)
            {
                anim = olaTerraformacion.GetComponentInChildren<Animator>();
            }

            if (anim != null)
            {
                anim.SetTrigger("ola");
            }

            // 3. Esperar 2 segundos mientras la animación se expande e impacta a los enemigos
            yield return new WaitForSeconds(2.0f);

            // 4. Apagar el GameObject
            olaTerraformacion.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[LevelManager] Referencia 'olaTerraformacion' no asignada en el Inspector.");
        }
    }

    private IEnumerator RutinaVictoriaCo()
    {
        yield return new WaitForSecondsRealtime(3.5f);

        if (datosNivel != null)
        {
            datosNivel.numeroNivel += 1;
        }

        Time.timeScale = 1f;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null && NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("BaseMenu", LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene("BaseMenu");
        }
    }

    private void ProcesarDerrota(string motivo)
    {
        partidaFinalizada = true;
        derrotaProcesada = true;

        Debug.Log($"[LevelManager] Derrota: {motivo}");

        if (spawnEnemigos != null)
        {
            spawnEnemigos.DetenerSpawneo();
        }

        // Bloquear inputs de movimiento y detener física de todos los jugadores al perder
        PlayerController[] jugadores = FindObjectsOfType<PlayerController>(true);
        foreach (var p in jugadores)
        {
            if (p != null)
            {
                p.enabled = false;
                Rigidbody2D rb = p.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
        }

        if (HudController.Instance != null)
        {
            HudController.Instance.MostrarDerrota(motivo);
        }

        StartCoroutine(RutinaDerrotaCo());
    }

    private IEnumerator RutinaDerrotaCo()
    {
        yield return new WaitForSecondsRealtime(3.5f);

        Time.timeScale = 1f;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null && NetworkManager.Singleton.IsServer)
        {
            // NO hacer Despawn de jugadores antes del LoadScene; Netcode los migra automaticamente
            NetworkManager.Singleton.SceneManager.LoadScene("Derrota", LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene("Derrota");
        }
    }
}
