using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NuevoInventario", menuName = "Datos/Inventario")]
public class DatosInventario : ScriptableObject
{
    [Header("Habilidad Especial")]
    [Tooltip("Ranura exclusiva para la habilidad tactica activa (ej. Dash, Bengala).")]
    public GameObject habilidadEspecial;

    [Header("Armas de Fuego (Automaticas)")]
    [Tooltip("Limite estricto de 4 armas que disparan en simultaneo.")]
    public GameObject[] armasEquipadas = new GameObject[4];

    [Header("Bolsa General")]
    [Tooltip("Inventario dinamico sin limite para acumular recursos o armamento inactivo.")]
    public List<GameObject> bolsa = new List<GameObject>();

    [Header("Economía")]
    [Tooltip("Cantidad de materiales (Bridgmanita) recolectados por el jugador.")]
    [SerializeField] private int materiales = 0;

    [Header("Estados de Juego")]
    [Tooltip("Indica si Génesis está actualmente reparando un nodo, lo que desactiva las ranuras 3 y 4.")]
    public bool estaReparando = false;

    // Propiedad pública encapsulada para acceder a los materiales (Solo lectura externa)
    public int Materiales => materiales;

    /// <summary>
    /// Añade materiales a la bolsa del inventario de forma segura.
    /// </summary>
    public void AñadirMateriales(int cantidad)
    {
        materiales += cantidad;
    }
}