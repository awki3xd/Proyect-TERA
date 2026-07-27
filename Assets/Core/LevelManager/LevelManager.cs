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
        if (!partidaFinalizada)
        {
            ProcesarDerrota("¡Génesis ha sido destruido!");
        }
    }

    private void ProcesarVictoria()
    {
        partidaFinalizada = true;
        victoriaProcesada = true;

        Debug.Log("[LevelManager] ¡Victoria Alcanzada!");

        if (spawnEnemigos != null)
            spawnEnemigos.enabled = false;

        int nivelActual = datosNivel != null ? datosNivel.numeroNivel : 1;
        if (HudController.Instance != null)
        {
            HudController.Instance.MostrarVictoria(nivelActual);
        }

        StartCoroutine(RutinaVictoriaCo());
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
            spawnEnemigos.enabled = false;

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
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject != null)
                {
                    client.PlayerObject.Despawn();
                }
            }
            NetworkManager.Singleton.SceneManager.LoadScene("Derrota", LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene("Derrota");
        }
    }
}
