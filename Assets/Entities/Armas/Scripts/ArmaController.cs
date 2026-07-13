using UnityEngine;

public struct EstadisticasArma
{
    [Tooltip("Distancia de ataque del arma.")]
    public float rango;
    [Tooltip("Daño base infligido por disparo.")]
    public float daño;
    [Tooltip("Cadencia de disparo en balas por segundo.")]
    public float cadencia;
    [Tooltip("Velocidad de rotación en grados por segundo para apuntar al objetivo.")]
    public float velocidadRotacion;
}

public class ArmaController : MonoBehaviour
{
    [Header("Referencias a Datos")]
    public DatosPersonaje datosPersonaje;
    [Tooltip("Configuración estática y de balance de esta arma.")]
    public DatosArma datosArma;

    [Header("Referencias de Escena e Instanciación")]
    [Tooltip("Punto de salida (boca del cañón) desde donde se disparan las balas.")]
    public Transform cañonPistola;

    [Header("Propiedades")]
    [Tooltip("Indica si el arma puede disparar.")]
    public bool puedeDisparar = true;

    // Estadísticas reales calculadas en el Start (multiplicadas por estadísticas del jugador)
    private EstadisticasArma estadisticasCalculadas;
    private float cooldownDisparo;
    private bool inicializado = false;

    private void Start()
    {
        if (!inicializado)
        {
            InicializarEstadisticas();
        }
    }

    /// <summary>
    /// Calcula las estadísticas finales del arma para la escena actual,
    /// aplicando los multiplicadores directamente sobre las estadísticas de la estructura base.
    /// </summary>
    private void InicializarEstadisticas()
    {
        inicializado = true;

        if (datosArma != null)
        {
            // Copiar los datos base del ScriptableObject de base de datos de armas
            estadisticasCalculadas.rango = datosArma.rango;
            estadisticasCalculadas.daño = datosArma.daño;
            estadisticasCalculadas.cadencia = datosArma.cadencia;
            estadisticasCalculadas.velocidadRotacion = datosArma.velocidadRotacion;

            if (datosPersonaje != null)
            {
                // Las estadísticas del personaje actúan como modificadores porcentuales (Base 100 = 100%)
                float multRango = datosPersonaje.rangoAtaque / 100f;
                float multDaño = datosPersonaje.daño / 100f;
                float multVelocidadAtaque = datosPersonaje.velocidadAtaque / 100f;

                estadisticasCalculadas.rango *= multRango;
                estadisticasCalculadas.daño *= multDaño;
                estadisticasCalculadas.cadencia *= multVelocidadAtaque;
            }
        }
        else
        {
            Debug.LogWarning("DatosArma no asignado en " + gameObject.name);
        }

        cooldownDisparo = 0f;
    }

    private void Update()
    {
        // 1. Buscar el enemigo más cercano (usando la búsqueda genérica por Tag)
        GameObject enemigoCercano = BuscarEnemigoMasCercano();

        if (enemigoCercano != null)
        {
            // 2. Apuntar matemáticamente al enemigo usando producto punto y producto cruz 2D
            Vector2 posicionPistola = transform.position;
            Vector2 posicionEnemigo = enemigoCercano.transform.position;
            Vector2 vectorHaciaEnemigo = (posicionEnemigo - posicionPistola).normalized;

            // Dirección actual en la que apunta la pistola (asumiendo que el sprite mira a la derecha por defecto)
            Vector2 direccionActual = transform.right;

            // A. Calcular ángulo entre el cañón y el enemigo con el PRODUCTO PUNTO (Dot Product)
            float dot = Vector2.Dot(direccionActual, vectorHaciaEnemigo);
            dot = Mathf.Clamp(dot, -1f, 1f); // Evitar imprecisiones de flotantes para Acos
            float anguloDiferencia = Mathf.Acos(dot) * Mathf.Rad2Deg;

            // B. Calcular dirección del giro (horario / antihorario) con el PRODUCTO CRUZ 2D (Cross Product)
            // En 2D: (u.x * v.y - u.y * v.x)
            float cross = (direccionActual.x * vectorHaciaEnemigo.y) - (direccionActual.y * vectorHaciaEnemigo.x);
            float sentidoGiro = cross >= 0f ? 1f : -1f;

            // C. Rotar gradualmente hacia el objetivo
            if (anguloDiferencia > 0.5f) // Umbral mínimo de tolerancia para evitar parpadeos
            {
                float pasoRotacion = estadisticasCalculadas.velocidadRotacion * Time.deltaTime;
                float rotacionPaso = Mathf.Min(pasoRotacion, anguloDiferencia);
                transform.Rotate(0f, 0f, rotacionPaso * sentidoGiro);
            }

            // 3. Control de disparo con cadencia
            cooldownDisparo -= Time.deltaTime;
            
            // Solo dispara si el cooldown ha expirado y la pistola está alineada con el enemigo (tolerancia de 5 grados)
            if (cooldownDisparo <= 0f && anguloDiferencia <= 10f && puedeDisparar)
            {
                Disparar();
                cooldownDisparo = estadisticasCalculadas.cadencia;
            }
        }
    }

    private void Disparar()
    {
        // El prefab de la bala/ataque se obtiene dinámicamente del ScriptableObject de base de datos
        GameObject prefab = datosArma != null ? datosArma.prefabBala : null;
        if (prefab == null)
        {
            Debug.LogWarning("Prefab de Bala no asignado en DatosArma de " + gameObject.name);
            return;
        }

        // Reproducir sonido específico de disparo del arma
        if (SoundManager.Instance != null && datosArma != null)
        {
            string nombre = datosArma.nombreArma.ToLower();
            if (nombre == "pistola")
            {
                SoundManager.Instance.PlaySFX(SoundID.DisparoPistola);
            }
            else if (nombre == "rifle")
            {
                SoundManager.Instance.PlaySFX(SoundID.DisparoRifle);
            }
            else if (nombre == "metralleta")
            {
                SoundManager.Instance.PlaySFX(SoundID.DisparoMetralleta);
            }
        }

        // Definir punto de origen (boca de cañón o la posición propia si no está asignado)
        Vector2 origen = cañonPistola != null ? (Vector2)cañonPistola.position : (Vector2)transform.position;
        
        // La bala hereda la rotación exacta actual de la pistola
        Quaternion rotacionBala = transform.rotation;

        GameObject balaObj = Instantiate(prefab, origen, rotacionBala);
        
        // Enviar la estructura DatosProyectil al controlador a través de la interfaz IProyectil
        IProyectil proyectil = balaObj.GetComponent<IProyectil>();
        if (proyectil != null)
        {
            DatosProyectil datos = new DatosProyectil
            {
                daño = estadisticasCalculadas.daño,
                rango = estadisticasCalculadas.rango,
                origen = EntidadDaño.OrigenDaño.Jugador, // Génesis dispara
                arma = datosArma != null ? datosArma.nombreArma : "" // Identificador de arma
            };
            proyectil.Inicializar(datos);
        }
        else
        {
            Debug.LogWarning("La bala instanciada no contiene un componente que implemente IProyectil.");
        }

        Debug.Log("Arma disparó. Daño: " + estadisticasCalculadas.daño + ", Rango: " + estadisticasCalculadas.rango);
    }

    private GameObject BuscarEnemigoMasCercano()
    {
        GameObject[] enemigos = GameObject.FindGameObjectsWithTag("Enemigo");
        GameObject closest = null;
        float minDist = estadisticasCalculadas.rango; // Solo detectamos enemigos dentro del rango calculado

        foreach (var enemigo in enemigos)
        {
            if (enemigo != null)
            {
                float dist = Vector2.Distance(transform.position, enemigo.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = enemigo;
                }
            }
        }

        return closest;
    }
}
