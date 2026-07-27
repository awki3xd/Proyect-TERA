using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class Pausa : MonoBehaviour
{
    [SerializeField] private UIDocument UiDocument;
    [SerializeField] private AudioClip AudioClik;
    [SerializeField] private AudioSource _AudioSource;

    private VisualElement _Contenedor;
    private VisualElement _PanelAjustes;
    private VisualElement _Volumen;
    private VisualElement _Tablero;

    private Button _Pausa;
    private Button _Continuar;
    private Button _Ajustes;
    private Button _Menu;
    private Button _Volver;

    private bool procesandoAccion = false;

    void Start()
    {
        if (UiDocument == null)
            UiDocument = GetComponent<UIDocument>();

        if (UiDocument == null) return;

        var root = UiDocument.rootVisualElement;

        _Contenedor = root.Q<VisualElement>("Contenedor");
        _PanelAjustes = root.Q<VisualElement>("PanelAjustes");
        _Volumen = root.Q<VisualElement>("Volumen");
        _Tablero = root.Q<VisualElement>("Tablero");

        _Pausa = root.Q<Button>("Pausa");
        _Continuar = root.Q<Button>("Continuar");
        _Ajustes = root.Q<Button>("Ajustes");
        _Menu = root.Q<Button>("Menu");
        _Volver = root.Q<Button>("Volver");

        // Ocultar menú de pausa visual al arrancar el nivel
        EstablecerEstadoPausaVisual(false);

        if (_Pausa != null)
        {
            _Pausa.clicked += OnPausaClicked;
            _Pausa.RegisterCallback<ClickEvent>(evt => OnPausaClicked());
        }

        if (_Continuar != null)
        {
            _Continuar.clicked += OnContinuarClicked;
            _Continuar.RegisterCallback<ClickEvent>(evt => OnContinuarClicked());
        }

        if (_Ajustes != null)
        {
            _Ajustes.clicked += OnAjustesClicked;
            _Ajustes.RegisterCallback<ClickEvent>(evt => OnAjustesClicked());
        }

        if (_Menu != null)
        {
            _Menu.clicked += OnMenuClicked;
            _Menu.RegisterCallback<ClickEvent>(evt => OnMenuClicked());
        }

        if (_Volver != null)
        {
            _Volver.clicked += OnVolverClicked;
            _Volver.RegisterCallback<ClickEvent>(evt => OnVolverClicked());
        }
    }

    private void OnPausaClicked()
    {
        PlaySound();
        if (LevelManager.Instance != null) LevelManager.Instance.PausarJuego();
    }

    private void OnContinuarClicked()
    {
        PlaySound();
        if (LevelManager.Instance != null) LevelManager.Instance.ReanudarJuego();
    }

    private void OnAjustesClicked()
    {
        PlaySound();
        if (_Volumen != null) _Volumen.style.display = DisplayStyle.Flex;
        if (_PanelAjustes != null) _PanelAjustes.style.display = DisplayStyle.None;
    }

    private void OnVolverClicked()
    {
        PlaySound();
        if (_PanelAjustes != null) _PanelAjustes.style.display = DisplayStyle.Flex;
        if (_Volumen != null) _Volumen.style.display = DisplayStyle.None;
    }

    private void OnMenuClicked()
    {
        if (procesandoAccion) return;
        procesandoAccion = true;

        PlaySound();
        Time.timeScale = 1f;

        if (Unity.Netcode.NetworkManager.Singleton != null)
        {
            Unity.Netcode.NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("Menu");
    }

    private void PlaySound()
    {
        if (_AudioSource != null && AudioClik != null)
        {
            _AudioSource.PlayOneShot(AudioClik);
        }
    }

    public void EstablecerEstadoPausaVisual(bool pausado)
    {
        if (_Contenedor != null)
        {
            _Contenedor.style.display = pausado ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (pausado)
        {
            if (_PanelAjustes != null) _PanelAjustes.style.display = DisplayStyle.Flex;
            if (_Volumen != null) _Volumen.style.display = DisplayStyle.None;
            if (_Tablero != null) _Tablero.style.display = DisplayStyle.None;
        }
        else
        {
            if (_PanelAjustes != null) _PanelAjustes.style.display = DisplayStyle.None;
            if (_Volumen != null) _Volumen.style.display = DisplayStyle.None;
        }
    }
}
