using UnityEngine;

public class NodoEstandar : MonoBehaviour
{
    [Header("Estado Actual")]
    [Tooltip("Apaga esta casilla en pleno juego para simular que un enemigo lo destruyo")]
    public bool estaActivo = true;

    // Esta funcion es la que lee el gestor de terraformacion
    public bool EstaFuncionando()
    {
        return estaActivo;
    }
}