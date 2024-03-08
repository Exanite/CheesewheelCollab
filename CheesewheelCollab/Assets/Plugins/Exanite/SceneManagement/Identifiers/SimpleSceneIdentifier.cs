using System;
using Cysharp.Threading.Tasks;
using UniDi;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Exanite.SceneManagement.Identifiers
{
    public class SimpleSceneIdentifier : SceneIdentifier
    {
#if ODIN_INSPECTOR
        [Required]
#endif
        [SerializeField] private string sceneName;
        [SerializeField] private InspectorLocalPhysicsMode localPhysicsMode;

        public LocalPhysicsMode LocalPhysicsMode
        {
            get => (LocalPhysicsMode)localPhysicsMode;
            set => localPhysicsMode = (InspectorLocalPhysicsMode)value;
        }

        public override async UniTask<Scene> Load(bool isAdditive = true)
        {
            var sceneLoadManager = ProjectContext.Instance.Container.Resolve<SceneLoadManager>();

            var newScene = await sceneLoadManager.LoadScene(sceneName, isAdditive, default, LocalPhysicsMode);
            var newSceneInitializer = SceneInitializerRegistry.SceneInitializers[newScene];
            Assert.AreEqual(this, newSceneInitializer.Identifier, "Loaded scene does not have expected scene identifier");

            return newScene;
        }

        [Serializable] [Flags]
        public enum InspectorLocalPhysicsMode
        {
            None = 0,
            Physics2D = 1,
            Physics3D = 2,
        }
    }
}
