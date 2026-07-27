using UnityEngine;

[CreateAssetMenu(fileName = "NuevosDatosNivel", menuName = "Datos/Nivel")]
public class DatosNivel : ScriptableObject
{
    [Header("Configuracion Base")]
    public int numeroNivel = 1;
    [SerializeField, Range(1, 3)] public int cantidadNodos = 3;

    [Header("Modificadores de Partida")]
    [Tooltip("Si se activa, perder todos los nodos hara que el pasto retroceda en lugar de estancarse.")]
    public bool modoExtremo = false;

    [ContextMenu("Resetear a Valores por Defecto")]
    public void ResetearAValoresPorDefecto()
    {
        numeroNivel = 1;
        cantidadNodos = 3;
        modoExtremo = false;
    }
}