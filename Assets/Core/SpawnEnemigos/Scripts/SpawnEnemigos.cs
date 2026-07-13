using System.Collections;
using UnityEngine;

public class SpawnEnemigos : MonoBehaviour
{
    [Header("Referencias de ScriptableObjects (Base de Datos)")]
    [Tooltip("Referencia al asset de DatosPersonaje en el proyecto para leer las estadísticas del jugador.")]
    public DatosPersonaje datosPersonaje;
    [Tooltip("Referencia al asset de DatosNivel en el proyecto para conocer el nivel actual y escalar cantidades.")]
    public DatosNivel datosNivel;
    [Tooltip("Referencia al asset global de DatosGlobalesEnemigos en el proyecto como plantilla base.")]
    public DatosGlobalesEnemigos datosGlobalesEnemigos;

    [Header("Referencias de Prefabs de Enemigos")]
    [Tooltip("Prefab del enemigo Escorpión (cuerpo a cuerpo).")]
    public GameObject prefabEscorpion;
    [Tooltip("Prefab del enemigo Mosca (a distancia).")]
    public GameObject prefabMosca;

    [Header("Configuración de Spawneo")]
    [Tooltip("Radio de la circunferencia alrededor del centro (0,0) desde la cual nacerán los enemigos fuera de pantalla.")]
    public float radioSpawneo = 15f;

    [Header("Estadísticas de Enemigos para esta Partida (Clonadas)")]
    [Tooltip("Instancia local en memoria de los datos de enemigos. Las modificaciones hechas aquí no afectarán al archivo en disco y desaparecerán al terminar la partida.")]
    public DatosGlobalesEnemigos datosEnemigosLocales;

    private void Start()
    {
        // 1. Inicializar y aplicar modificadores de estadísticas globales
        CalcularEstadisticasEnemigosPartida();

        // 2. Iniciar rutinas procedurales e infinitas de oleadas
        StartCoroutine(SpawneoIndividualCo());
        StartCoroutine(SpawneoGrupalCo());
    }

    /// <summary>
    /// Calcula las estadísticas de los enemigos para la partida actual en base al nivel de poder del jugador,
    /// clona los datos para que sean temporales, y aplica la probabilidad de potenciar una estadística aleatoria.
    /// </summary>
    private void CalcularEstadisticasEnemigosPartida()
    {
        if (datosPersonaje == null || datosGlobalesEnemigos == null)
        {
            Debug.LogWarning("DatosPersonaje o DatosGlobalesEnemigos no asignados en el Spawner.");
            return;
        }

        // 1. Calcular la media (promedio) de las estadísticas de Génesis (Base 100)
        float sumaStatsPlayer = datosPersonaje.armadura +
                               datosPersonaje.curacion +
                               datosPersonaje.velocidadMovimiento +
                               datosPersonaje.daño +
                               datosPersonaje.velocidadAtaque +
                               datosPersonaje.rangoAtaque;

        float promedioPlayer = sumaStatsPlayer / 6f;
        float factorEscala = promedioPlayer / 100f; // Ej: Si el promedio es 150, el factor es 1.5x (50% más fuerte)

        Debug.Log($"[Spawner] Promedio de estadísticas del Jugador: {promedioPlayer}%. Factor de escalado de enemigos: {factorEscala}x");

        // 2. Modificar directamente el ScriptableObject global original en disco/proyecto (persistente)
        datosGlobalesEnemigos.vida *= factorEscala;
        datosGlobalesEnemigos.velocidadMovimiento *= factorEscala;
        datosGlobalesEnemigos.daño *= factorEscala;
        datosGlobalesEnemigos.velocidadAtaque *= factorEscala;
        datosGlobalesEnemigos.rangoAtaque *= factorEscala;

        // 3. Clonar el ScriptableObject global ya modificado en memoria para el spawner local
        // Las modificaciones aleatorias temporales se aplicarán sobre este clon y no alterarán el asset.
        datosEnemigosLocales = Instantiate(datosGlobalesEnemigos);

        // 4. Aplicar un 60% de probabilidad de potenciar una estadística de forma aleatoria (entre 20% y 30%)
        float probabilidadBoost = Random.Range(0f, 1f);
        if (probabilidadBoost <= 0.60f)
        {
            int indiceStat = Random.Range(1, 6); // 1: Vida, 2: VelocidadMov, 3: Daño, 4: VelAtaque, 5: Rango
            float porcentajeBoost = Random.Range(1.20f, 1.30f); // 20% a 30% de incremento

            switch (indiceStat)
            {
                case 1:
                    datosEnemigosLocales.vida *= porcentajeBoost;
                    Debug.Log($"[Spawner] ¡Estadística potenciada en esta partida!: VIDA (+{Mathf.Round((porcentajeBoost - 1f) * 100f)}%)");
                    break;
                case 2:
                    datosEnemigosLocales.velocidadMovimiento *= porcentajeBoost;
                    Debug.Log($"[Spawner] ¡Estadística potenciada en esta partida!: VELOCIDAD DE MOVIMIENTO (+{Mathf.Round((porcentajeBoost - 1f) * 100f)}%)");
                    break;
                case 3:
                    datosEnemigosLocales.daño *= porcentajeBoost;
                    Debug.Log($"[Spawner] ¡Estadística potenciada en esta partida!: DAÑO (+{Mathf.Round((porcentajeBoost - 1f) * 100f)}%)");
                    break;
                case 4:
                    datosEnemigosLocales.velocidadAtaque *= porcentajeBoost;
                    Debug.Log($"[Spawner] ¡Estadística potenciada en esta partida!: VELOCIDAD DE ATAQUE (+{Mathf.Round((porcentajeBoost - 1f) * 100f)}%)");
                    break;
                case 5:
                    datosEnemigosLocales.rangoAtaque *= porcentajeBoost;
                    Debug.Log($"[Spawner] ¡Estadística potenciada en esta partida!: RANGO DE ATAQUE (+{Mathf.Round((porcentajeBoost - 1f) * 100f)}%)");
                    break;
            }
        }
        else
        {
            Debug.Log("[Spawner] No se potenció ninguna estadística para los enemigos en esta partida.");
        }
    }

    /// <summary>
    /// Corrutina que spawnea de forma continua e individual enemigos a lo largo de la circunferencia exterior.
    /// La cantidad de enemigos que nacen en cada intervalo se escala linealmente según el nivel actual de la partida.
    /// </summary>
    private IEnumerator SpawneoIndividualCo()
    {
        while (true)
        {
            int nivel = datosNivel != null ? datosNivel.numeroNivel : 1;
            float factorNivel = Mathf.InverseLerp(1f, 20f, nivel); // 0 en nivel 1, 1 en nivel 20

            // Escalar intervalo de tiempo según nivel (Nivel 1 es más lento, Nivel 20 es el más rápido)
            float minIntervalo = Mathf.Lerp(2.0f, 0.1f, factorNivel);
            float maxIntervalo = Mathf.Lerp(5.0f, 1.0f, factorNivel);
            float intervalo = Random.Range(minIntervalo, maxIntervalo);
            yield return new WaitForSeconds(intervalo);

            // La cantidad de enemigos individuales spawneados en este intervalo es igual al nivel (capado a un máximo de 20)
            int cantidad = Mathf.Clamp(nivel, 1, 20);

            for (int i = 0; i < cantidad; i++)
            {
                Vector2 puntoSpawneo = ObtenerPuntoSpawneoAleatorio();
                InstanciarYInicializarEnemigo(puntoSpawneo);
            }
        }
    }

    /// <summary>
    /// Corrutina que spawnea oleadas grupales (un grupo de enemigos concentrados en un mismo punto de la circunferencia).
    /// El tamaño del grupo escala dinámicamente según el nivel. Ocurre en intervalos largos de tiempo.
    /// </summary>
    private IEnumerator SpawneoGrupalCo()
    {
        while (true)
        {
            int nivel = datosNivel != null ? datosNivel.numeroNivel : 1;
            float factorNivel = Mathf.InverseLerp(1f, 20f, nivel);

            // Escalar intervalo de grupo según nivel (Nivel 1: cada 15-30s; Nivel 20: cada 5-10s)
            float minGrupoIntervalo = Mathf.Lerp(15f, 5f, factorNivel);
            float maxGrupoIntervalo = Mathf.Lerp(30f, 10f, factorNivel);
            float intervalo = Random.Range(minGrupoIntervalo, maxGrupoIntervalo);
            yield return new WaitForSeconds(intervalo);

            // Escalar tamaño de grupos dinámicamente:
            // Nivel 1: min 2, max 4.
            // A partir de ahí sube linealmente. Nivel 20: min 15, max 20.
            int minGrupo = Mathf.Clamp(2 + (nivel - 1), 2, 15);
            int maxGrupo = Mathf.Clamp(4 + (nivel - 1) * 2, 4, 20);
            int cantidadEnGrupo = Random.Range(minGrupo, maxGrupo + 1);

            // Generar un único punto de spawneo para todo el grupo (ellos se esparcirán solos al apuntar)
            Vector2 puntoGrupo = ObtenerPuntoSpawneoAleatorio();

            for (int i = 0; i < cantidadEnGrupo; i++)
            {
                InstanciarYInicializarEnemigo(puntoGrupo);
            }

            Debug.Log($"[Spawner] Spawned group of size {cantidadEnGrupo} at {puntoGrupo} (Level {nivel})");
        }
    }

    /// <summary>
    /// Calcula un punto aleatorio a lo largo de la circunferencia exterior de spawneo.
    /// </summary>
    private Vector2 ObtenerPuntoSpawneoAleatorio()
    {
        float angulo = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * radioSpawneo;
    }

    /// <summary>
    /// Instancia aleatoriamente un Escorpión o una Mosca y lo inicializa con las estadísticas locales correspondientes de la partida.
    /// </summary>
    private void InstanciarYInicializarEnemigo(Vector2 posicion)
    {
        GameObject prefabElegido = null;

        if (prefabEscorpion != null && prefabMosca != null)
        {
            prefabElegido = Random.value < 0.5f ? prefabEscorpion : prefabMosca;
        }
        else if (prefabEscorpion != null)
        {
            prefabElegido = prefabEscorpion;
        }
        else if (prefabMosca != null)
        {
            prefabElegido = prefabMosca;
        }

        if (prefabElegido == null) return;

        GameObject objEnemigo = Instantiate(prefabElegido, posicion, Quaternion.identity);

        // Inicializar componentes correspondientes del enemigo
        EscorpionController escorpion = objEnemigo.GetComponent<EscorpionController>();
        if (escorpion != null)
        {
            escorpion.Inicializar(datosEnemigosLocales, posicion);
        }
        else
        {
            MoscaController mosca = objEnemigo.GetComponent<MoscaController>();
            if (mosca != null)
            {
                mosca.Inicializar(datosEnemigosLocales, posicion);
            }
        }
    }
}
