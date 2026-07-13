using UnityEngine;

public class EntidadDaño : MonoBehaviour
{
    public enum OrigenDaño
    {
        Enemigo,
        Jugador
    }

    [Header("Configuración de Daño")]
    [Tooltip("Cantidad de daño que infligirá este objeto.")]
    public float daño = 10f;

    [Header("Configuración de Origen")]
    [Tooltip("Define quién generó este daño para segmentar los objetivos válidos.")]
    public OrigenDaño origen = OrigenDaño.Enemigo;

    [Header("Comportamiento de Impacto")]
    [Tooltip("Define si el objeto de daño se autodestruye al impactar un objetivo válido (ej: Balas).")]
    public bool destruirAlImpactar = true;

    /// <summary>
    /// Inicializa los parámetros de daño y origen dinámicamente al instanciar el objeto.
    /// </summary>
    public void Inicializar(float cantidadDaño, OrigenDaño origenDaño, bool  destruir)
    {
        daño = cantidadDaño;
        origen = origenDaño;
        destruirAlImpactar = destruir;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Daño generado por los Enemigos (hacia el Jugador o los Nodos)
        if (origen == OrigenDaño.Enemigo)
        {
            if (other.CompareTag("Player"))
            {
                // TODO: Implementar el daño recibido por el jugador en el futuro
                // Dejado en blanco temporalmente a petición del usuario
            }
            else if (other.CompareTag("Nodo"))
            {
                NodoEstandar nodo = other.GetComponent<NodoEstandar>();
                if (nodo != null)
                {
                    // Consultamos el estado del nodo antes de dañarlo
                    if (!nodo.EstaRoto())
                    {
                        nodo.RecibirDaño(daño);
                        
                        // Después de aplicar el daño, este objeto se destruye si está configurado para ello
                        if (destruirAlImpactar)
                        {
                            Destroy(gameObject);
                        }
                    }
                }
            }
        }
        // 2. Daño generado por el Jugador (balas, espadazos, etc.) hacia los Enemigos
        else if (origen == OrigenDaño.Jugador)
        {
            if (other.CompareTag("Enemigo"))
            {
                // Por ahora el escorpión es el único enemigo en la horda, buscamos su script directamente.
                // En el futuro, crearemos una interfaz común para modularizar a todos los enemigos.
                EscorpionController escorpion = other.GetComponent<EscorpionController>();
                if (escorpion != null)
                {
                    escorpion.RecibirDaño(daño);
                    
                    // Si el proyectil debe autodestruirse al impactar (ej: balas)
                    if (destruirAlImpactar)
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}
