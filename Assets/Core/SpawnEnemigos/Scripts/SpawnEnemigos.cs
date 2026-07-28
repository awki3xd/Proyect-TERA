using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

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
    [Tooltip("Prefab del enemigo Avispa (francotirador).")]
    public GameObject prefabAvispa;
    [Tooltip("Prefab del enemigo Escarabajo (escudo).")]
    public GameObject prefabEscarabajo;

    [Header("Configuración de Spawneo")]
    [Tooltip("Radio de la circunferencia alrededor del centro (0,0) desde la cual nacerán los enemigos fuera de pantalla.")]
    public float radioSpawneo = 15f;

    [Header("Estadísticas de Enemigos para esta Partida (Clonadas)")]
    [Tooltip("Instancia local en memoria de los datos de enemigos. Las modificaciones hechas aquí no afectarán al archivo en disco.")]
    public DatosGlobalesEnemigos datosEnemigosLocales;

    private bool alternarGrupo = false;

    private void Start()
    {
        // Solo el Servidor/Host debe ejecutar las rutinas de spawneo de enemigos si Netcode esta activo
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        // Usar los datos globales calculados previamente en la base
        if (datosGlobalesEnemigos != null)
        {
            datosEnemigosLocales = Instantiate(datosGlobalesEnemigos);
        }

        // Iniciar rutinas procedurales de oleadas
        StartCoroutine(SpawneoIndividualCo());
        StartCoroutine(SpawneoGrupalCo());
    }

    /// <summary>
    /// Corrutina de spawneo individual secundario.
    /// Spawnea únicamente unidades básicas (Escorpiones o Moscas) en baja cantidad.
    /// </summary>
    private IEnumerator SpawneoIndividualCo()
    {
        while (true)
        {
            int nivel = datosNivel != null ? datosNivel.numeroNivel : 1;
            float factorNivel = Mathf.InverseLerp(1f, 20f, nivel); // 0 en nivel 1, 1 en nivel 20

            // Escalar intervalo de tiempo secundario
            float minIntervalo = Mathf.Lerp(8.0f, 3.0f, factorNivel);
            float maxIntervalo = Mathf.Lerp(12.0f, 6.0f, factorNivel);
            float intervalo = Random.Range(minIntervalo, maxIntervalo);
            yield return new WaitForSeconds(intervalo);

            // Cantidad baja (1 a 4 enemigos sueltos por intervalo)
            int cantidad = Mathf.Clamp(Mathf.CeilToInt(nivel * 0.25f), 1, 4);

            for (int i = 0; i < cantidad; i++)
            {
                Vector2 puntoSpawneo = ObtenerPuntoSpawneoAleatorio();
                // Seleccionar solo entre Escorpión o Mosca
                GameObject prefabBasico = (Random.value < 0.5f && prefabEscorpion != null) ? prefabEscorpion : prefabMosca;
                if (prefabBasico == null) prefabBasico = prefabEscorpion ?? prefabMosca;

                if (prefabBasico != null)
                {
                    InstanciarPrefabEspecifico(prefabBasico, puntoSpawneo);
                }
            }
        }
    }

    /// <summary>
    /// Corrutina principal de spawneo en oleadas grupales.
    /// Spawnea grupos especializados de Escorpiones (Melee) o Moscas (Rango).
    /// El tiempo entre hordas escala según el nivel (10s en Nivel 1 -> 3s en Nivel 20).
    /// La probabilidad de incluir una unidad especial (Escarabajo en cuerpo a cuerpo / Avispa en rango)
    /// escala de 5% (Nivel 1) a 100% (Nivel 20).
    /// </summary>
    private IEnumerator SpawneoGrupalCo()
    {
        while (true)
        {
            int nivel = datosNivel != null ? datosNivel.numeroNivel : 1;
            float factorNivel = Mathf.InverseLerp(1f, 20f, nivel);

            // Tiempo entre grupos: 10s en Nivel 1 -> 3s en Nivel 20
            float intervalo = Mathf.Lerp(10.0f, 3.0f, factorNivel);
            yield return new WaitForSeconds(intervalo);

            // Tamaño del grupo escala dinámicamente según nivel
            int minGrupo = Mathf.Clamp(2 + Mathf.FloorToInt((nivel - 1) * 0.3f), 2, 6);
            int maxGrupo = Mathf.Clamp(4 + Mathf.FloorToInt((nivel - 1) * 0.5f), 4, 12);
            int cantidadEnGrupo = Random.Range(minGrupo, maxGrupo + 1);

            // Probabilidad de spawnear una unidad especial en el grupo (5% en Nivel 1 -> 100% en Nivel 20)
            float probabilidadEspecial = Mathf.Lerp(0.05f, 1.0f, factorNivel);
            bool incluirEspecial = Random.value <= probabilidadEspecial;

            // Punto de origen de la horda en el perímetro
            Vector2 puntoGrupo = ObtenerPuntoSpawneoAleatorio();

            // Alternar o seleccionar tipo de grupo: Melee (Escorpiones) o Rango (Moscas)
            alternarGrupo = !alternarGrupo;
            bool esGrupoMelee = alternarGrupo;

            if (esGrupoMelee)
            {
                // Horda Cuerpo a Cuerpo: Escorpiones + (Opcional 1 Escarabajo)
                if (incluirEspecial && prefabEscarabajo != null)
                {
                    InstanciarPrefabEspecifico(prefabEscarabajo, puntoGrupo + Random.insideUnitCircle * 0.5f);
                }

                if (prefabEscorpion != null)
                {
                    for (int i = 0; i < cantidadEnGrupo; i++)
                    {
                        Vector2 offsetPos = puntoGrupo + Random.insideUnitCircle * 0.5f;
                        InstanciarPrefabEspecifico(prefabEscorpion, offsetPos);
                    }
                }
            }
            else
            {
                // Horda a Distancia: Moscas + (Opcional 1 Avispa)
                if (incluirEspecial && prefabAvispa != null)
                {
                    InstanciarPrefabEspecifico(prefabAvispa, puntoGrupo + Random.insideUnitCircle * 0.5f);
                }

                if (prefabMosca != null)
                {
                    for (int i = 0; i < cantidadEnGrupo; i++)
                    {
                        Vector2 offsetPos = puntoGrupo + Random.insideUnitCircle * 0.5f;
                        InstanciarPrefabEspecifico(prefabMosca, offsetPos);
                    }
                }
            }

            Debug.Log($"[Spawner] Spawneada horda {(esGrupoMelee ? "Melee" : "Rango")} (Tamaño: {cantidadEnGrupo}, Especial: {incluirEspecial}) en Nivel {nivel}. Siguiente en {intervalo:F1}s");
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
    /// Instancia un prefab específico y llama a su método de inicialización correspondiente.
    /// </summary>
    private void InstanciarPrefabEspecifico(GameObject prefab, Vector2 posicion)
    {
        if (prefab == null) return;

        GameObject objEnemigo = Instantiate(prefab, posicion, Quaternion.identity);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
        {
            NetworkObject netObj = objEnemigo.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
        }

        EscorpionController escorpion = objEnemigo.GetComponent<EscorpionController>();
        if (escorpion != null)
        {
            escorpion.Inicializar(datosEnemigosLocales, posicion);
            return;
        }

        MoscaController mosca = objEnemigo.GetComponent<MoscaController>();
        if (mosca != null)
        {
            mosca.Inicializar(datosEnemigosLocales, posicion);
            return;
        }

        AvispaController avispa = objEnemigo.GetComponent<AvispaController>();
        if (avispa != null)
        {
            avispa.Inicializar(datosEnemigosLocales, posicion);
            return;
        }

        EscarabajoController escarabajo = objEnemigo.GetComponent<EscarabajoController>();
        if (escarabajo != null)
        {
            escarabajo.Inicializar(datosEnemigosLocales, posicion);
            return;
        }
    }
}
