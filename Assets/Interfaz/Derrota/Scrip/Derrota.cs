using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class Derrota : MonoBehaviour
{
    [SerializeField] private UIDocument _UIDocument;
    [SerializeField] private AudioClip _AudioClip;

    private Button _Continuar;
    private Button _VolverMenu;
    [SerializeField]private AudioSource _AudioSource;
   
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       var root = _UIDocument.rootVisualElement; 

        _Continuar=root.Q<Button>("Continuar");
        _VolverMenu = root.Q<Button>("VolverMenu");

        _Continuar.clicked += () =>
        { 
            _AudioSource.PlayOneShot(_AudioClip);
            
        };
        _VolverMenu.clicked += () => 
        {
            _AudioSource.PlayOneShot(_AudioClip);
            
            // Apagar el servidor/cliente para limpiar la conexión antes de volver al menú
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Unity.Netcode.NetworkManager.Singleton.Shutdown();
            }
            
            // Cargar la escena del Menú por su nombre para evitar errores de índice
            SceneManager.LoadSceneAsync("Menu");
        };

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
