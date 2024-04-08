using Cysharp.Threading.Tasks;
using Exanite.Networking.Transports.LiteNetLib;
using Exanite.SceneManagement;
using Source.Players;
using TMPro;
using UniDi;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Source.UserInterface
{
    public class MainMenuDisplay : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private TMP_InputField nameField;
        [SerializeField] private TMP_InputField addressField;
        [SerializeField] private TMP_InputField portField;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button connectButton;

        [Header("Settings")]
        [SerializeField] private SceneIdentifier clientScene;
        [SerializeField] private SceneIdentifier serverScene;

        [Inject] private LnlTransportSettings transportSettings;
        [Inject] private LocalPlayerSettings playerSettings;

        [Inject] private SceneLoader sceneLoader;

        private void Start()
        {
            nameField.text = playerSettings.PlayerName;
            addressField.text = transportSettings.RemoteAddress;
            portField.text = transportSettings.Port.ToString();

            hostButton.onClick.AddListener(() =>
            {
                ParseFields();

                clientScene.Load(LoadSceneMode.Additive);
                serverScene.Load(LoadSceneMode.Additive);
                sceneLoader.UnloadScene(gameObject.scene).Forget();
            });

            connectButton.onClick.AddListener(() =>
            {
                ParseFields();

                clientScene.Load(LoadSceneMode.Additive);
                sceneLoader.UnloadScene(gameObject.scene).Forget();
            });
        }

        private void ParseFields()
        {
            playerSettings.PlayerName = nameField.text;
            transportSettings.RemoteAddress = addressField.text;
            transportSettings.Port = ushort.Parse(portField.text);
        }
    }
}
