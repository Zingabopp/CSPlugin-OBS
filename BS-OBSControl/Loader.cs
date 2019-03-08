using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using UnityEngine;
using CommandPluginLib;

namespace BS_OBSControl
{
    class Loader : MonoBehaviour
    {
        bool loaded;

        public void Start()
        {
            Logger.Debug("Loader started");
            loaded = false;
            StartCoroutine(LoadToServer());
        }

        public IEnumerator<WaitForSeconds> LoadToServer()
        {
            yield return new WaitForSeconds(.5f);
            GameObject server = null;
            while(!loaded)
            {

                if ((SceneManager.GetActiveScene().name != "GameCore"))
                {
                    Logger.Trace("Attempting to find server GameObject");
                    server = GameObject.FindObjectsOfType<GameObject>().Where(c => c.name.Contains("CIHTTPServer")).FirstOrDefault();
                    if (server != null)
                    {
                        Logger.Debug($"Found server GameObject: {server.name}");
                        var obsComp = server.AddComponent<OBSControl>();
                        loaded = true;
                        LoadSuccess(this.gameObject, 
                            server.GetComponents<ICommandPlugin>().Where(c => c.PluginName == "Command-Interface").FirstOrDefault(),
                            obsComp
                            );

                    }
                    else
                    {
                        Logger.Trace("Server not found, waiting 3 sec");
                        yield return new WaitForSeconds(3);
                    }
                }
                else
                    yield return new WaitForSeconds(3);


            }
        }

        public event Action<GameObject, ICommandPlugin, ICommandPlugin> LoadSuccess;
    }
}
