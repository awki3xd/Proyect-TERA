using UnityEngine;

[CreateAssetMenu(fileName = "NuevosDatosPersonaje", menuName = "Datos/Personaje")]
public class DatosPersonaje : ScriptableObject
{
    [Header("Supervivencia (Base 100 = 100%)")]
    [Tooltip("La vida es inmutable. La armadura actúa como mitigación de daño porcentual.")]
    public float armadura = 100f;
    public float curacion = 100f;

    [Header("Movilidad")]
    public float velocidadMovimiento = 100f;

    [Header("Estadísticas de Combate")]
    public float daño = 100f;
    public float velocidadAtaque = 100f;
    public float rangoAtaque = 100f;

    [ContextMenu("Resetear a Valores por Defecto")]
    public void ResetearAValoresPorDefecto()
    {
        armadura = 100f;
        curacion = 100f;
        velocidadMovimiento = 100f;
        daño = 100f;
        velocidadAtaque = 100f;
        rangoAtaque = 100f;
    }
}