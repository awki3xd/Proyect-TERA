using UnityEngine;

public class GeneradorNodos : MonoBehaviour
{
    [Header("Configuracion Principal")]
    [SerializeField] private GameObject prefabNodo;
    [SerializeField] private DatosNivel datosNivel; 

    [Header("Parametros de Formacion")]
    [SerializeField] private float distanciaDosNodos = 5f;
    [SerializeField] private float radioTresNodos = 4f;

    // Propiedad de solo lectura para que el pasto pueda ver las maquinas creadas
    public NodoEstandar[] NodosCreados { get; private set; }

    private void Start()
    {
        GenerarFormacion();
    }

    private void GenerarFormacion()
    {
        if (prefabNodo == null || datosNivel == null) return;

        int cantidad = datosNivel.cantidadNodos;
        
        // Limitamos el tamaño del arreglo a 3 para coincidir con tu logica geometrica actual
        int totalAInstanciar = cantidad >= 3 ? 3 : cantidad;
        NodosCreados = new NodoEstandar[totalAInstanciar];

        if (cantidad == 1)
        {
            NodosCreados[0] = InstanciarNodo(Vector2.zero);
        }
        else if (cantidad == 2)
        {
            Vector2 posicionIzquierda = new Vector2(-distanciaDosNodos / 2f, 0f);
            Vector2 posicionDerecha = new Vector2(distanciaDosNodos / 2f, 0f);

            NodosCreados[0] = InstanciarNodo(posicionIzquierda);
            NodosCreados[1] = InstanciarNodo(posicionDerecha);
        }
        else if (cantidad >= 3) 
        {
            float anguloBase = Random.Range(0f, 360f);

            for (int i = 0; i < 3; i++)
            {
                float anguloActual = anguloBase + (i * 120f);
                float anguloRadianes = anguloActual * Mathf.Deg2Rad;

                float posX = Mathf.Cos(anguloRadianes) * radioTresNodos;
                float posY = Mathf.Sin(anguloRadianes) * radioTresNodos;
                
                NodosCreados[i] = InstanciarNodo(new Vector2(posX, posY));
            }
        }
    }

    private NodoEstandar InstanciarNodo(Vector2 posicion)
    {
        GameObject obj = Instantiate(prefabNodo, posicion, Quaternion.identity, transform);
        
        // Atrapamos el script de la maquina y lo devolvemos para guardarlo en el arreglo
        return obj.GetComponent<NodoEstandar>();
    }
}