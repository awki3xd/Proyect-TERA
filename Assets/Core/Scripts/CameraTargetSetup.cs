using System.Collections;
using UnityEngine;
using Unity.Cinemachine; // Requerido para acceder a CinemachineTargetGroup en Cinemachine 3.x

public class CameraTargetSetup : MonoBehaviour
{
    [Header("Referencias de Cinemachine")]
    [Tooltip("Referencia al componente CinemachineTargetGroup en la escena. Si se deja vacío, se buscará automáticamente.")]
    public CinemachineTargetGroup targetGroup;

    [Header("Ajustes de Encuadre")]
    [Tooltip("Peso de prioridad del jugador para el encuadre (a mayor peso, más centrado estará).")]
    public float pesoJugador = 1.2f;
    [Tooltip("Radio de seguridad alrededor del jugador.")]
    public float radioJugador = 1.5f;
    
    [Space(5)]
    [Tooltip("Peso de prioridad de los nodos.")]
    public float pesoNodo = 1.0f;
    [Tooltip("Radio de seguridad alrededor de los nodos para evitar que toquen el borde de la pantalla.")]
    public float radioNodo = 1.0f;

    private IEnumerator Start()
    {
        // Esperar un frame (yield return null) para garantizar que GeneradorNodos 
        // ya haya instanciado todos los nodos en su propio Start()
        yield return null;

        // 1. Buscar el TargetGroup si no fue arrastrado al inspector
        if (targetGroup == null)
        {
            targetGroup = FindAnyObjectByType<CinemachineTargetGroup>();
        }

        if (targetGroup == null)
        {
            Debug.LogWarning("No se encontró ningún CinemachineTargetGroup en la escena. Asegúrate de tener uno creado.");
            yield break;
        }

        // Limpiar el grupo de objetivos previos para evitar duplicados
        if (targetGroup.Targets != null)
        {
            targetGroup.Targets.Clear();
        }

        // 2. Buscar al jugador (Génesis) por Tag e incorporarlo al grupo
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            targetGroup.AddMember(player.transform, pesoJugador, radioJugador);
        }
        else
        {
            Debug.LogWarning("No se encontró ningún GameObject con el tag 'Player' para añadir al Target Group.");
        }

        // 3. Buscar todos los Nodos generados en la escena por su Tag e incorporarlos
        GameObject[] nodos = GameObject.FindGameObjectsWithTag("Nodo");
        foreach (var nodo in nodos)
        {
            if (nodo != null)
            {
                targetGroup.AddMember(nodo.transform, pesoNodo, radioNodo);
            }
        }

        Debug.Log("Cinemachine Target Group inicializado dinámicamente. Total objetivos: " + (targetGroup.Targets != null ? targetGroup.Targets.Count : 0));
    }
}
