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

    /// <summary>
    /// Inicializa los parámetros de daño y origen dinámicamente al instanciar el objeto.
    /// </summary>
    public void Inicializar(float cantidadDaño, OrigenDaño origenDaño)
    {
        daño = cantidadDaño;
        origen = origenDaño;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {

        // Por ahora, solo procesamos el daño cuando proviene de un Enemigo
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
                        
                        // Después de aplicar el daño, este objeto se destruye
                        Destroy(gameObject);
                    }
                }
            }
        }
        else if (origen == OrigenDaño.Jugador)
        {
            // TODO: Implementar daño a enemigos (horda) cuando estemos programando el jugador
        }
    }
}
