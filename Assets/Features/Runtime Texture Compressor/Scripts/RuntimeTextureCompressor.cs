using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace CodeZash.Compressor {
    public class RuntimeTextureCompressor : MonoBehaviour {

        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private string url = "https://i.imgur.com/2YjvS6p.png";
        [SerializeField] private Renderer targetRenderer;

        private void Start() {
            if (targetRenderer == null) {
                Debug.LogError("Target renderer is null");
                return;
            }
            if (loadOnStart) {
                DownloadAndCompressTexture();
            }
        }

        // Download the texture from the given url and compress it
        public IEnumerator DownloadAndCompressTextureCoroutine(string url, System.Action<Texture2D> callback) {
            Debug.Log($"Downloading texture from [{url}]");
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogError(www.error);
            } else {
                Debug.Log($"Texture downloaded from [{url}]");
                Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                callback(texture);
            }
        }

        public void DownloadAndCompressTexture() {
            if (string.IsNullOrEmpty(url)) {
                Debug.LogError("Url is null or empty");
                return;
            }
            StartCoroutine(DownloadAndCompressTextureCoroutine(url, (texture) => CompressTexture(texture, targetRenderer)));
        }

        // Download the texture from the given url and compress it
        public void DownloadAndCompressTexture(string url, Renderer renderer) {
            if (string.IsNullOrEmpty(url)) {
                Debug.LogError("Url is null or empty");
                return;
            }
            StartCoroutine(DownloadAndCompressTextureCoroutine(url, (texture) => CompressTexture(texture, renderer)));
        }

        // Compress the texture and set it to the renderer
        private void CompressTexture(Texture2D texture, Renderer renderer) {
            texture.Compress(true);
            Debug.Log($"Texture [{texture.name}] compressed to [{texture.format}]");
            renderer.material.mainTexture = texture;
        }
    }
}