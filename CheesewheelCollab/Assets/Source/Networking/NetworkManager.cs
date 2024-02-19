using Cysharp.Threading.Tasks;
using UniDi;
using UnityEngine;
using Network = Exanite.Networking.Network;

namespace Source.Networking
{
    public class NetworkManager : MonoBehaviour
    {
        [Inject] private Network network;

        private void Start()
        {
            network.StartConnection().Forget();
        }
    }
}
