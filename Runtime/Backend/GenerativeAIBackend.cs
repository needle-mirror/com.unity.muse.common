using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Muse.Common.Account;
using Unity.Muse.Common.Api;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.Scripting;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Muse.Common
{
    class GenerativeAIBackend
    {
        internal static event Func<long, string, bool> OnServerError;

        internal static class StatusEnum
        {
            public const string waiting = "waiting";
            public const string working = "working";
            public const string done = "done";
            public const string failed = "failed";
        }

        internal enum GeneratorModel
        {
            StableDiffusionV_1_4 = 14,
            //StableDiffusionV_1_5 = 15,
            StableDiffusionV_2_1 = 21
        }

        public delegate void ArtifactProgressCallback(string guid,
                                                        string statusEnum,
                                                        float progress,
                                                        string errorMsg);

        protected static string AccessToken =>
            //TODO: Fix this up when we can get access tokens from cloudlab canvas outside of the editor
#if UNITY_EDITOR
                                    CloudProjectSettings.accessToken;
#else
                                    GameObject.Find("App").GetComponent<RuntimeCloudContext>().accessToken;
#endif

        internal static TexturesUrl TexturesUrl => new() {orgId = AccountInfo.Instance.Organization?.Id};

        internal static ICloudContext s_Context;

        [Preserve]
        static GenerativeAIBackend()
        {
            s_Context = CloudContextFactory.GetCloudContext();
        }

        [Serializable]
        internal class DownloadURLResponse
        {
            public bool success;
            public string url;
        }

        /// <summary>
        /// Initiate Image variation generation on Cloud. It only allocates texture ids and actual generation occurs in background.
        /// Use `RequestStatus` to query progress and `DownloadImage` to download intermediate or final result.
        /// </summary>
        public static UnityWebRequestAsyncOperation VariateImage(
            string sourceGuid,
            string imageB64,
            string prompt,
            ImageVariationSettingsRequest settings,
            Action<TextToImageResponse, string> onDone)
        {
            object request;

            if (string.IsNullOrEmpty(sourceGuid))
                request = new ImageVariationBase64Request(imageB64, prompt, settings);
            else
                request = new ImageVariationRequest(sourceGuid, prompt, settings);

            return SendJsonRequest(TexturesUrl.variate, request,
                RequestHandler<TextToImageResponse>((data, error) =>
                {
                    if (data != null)
                        data.seed = settings.seed;
                    onDone?.Invoke(data, error);
                }));
        }

        public static UnityWebRequestAsyncOperation ControlNetGenerate(
            string sourceGuid,
            string sourceBase64,
            string prompt,
            ImageVariationSettingsRequest settings,
            Action<TextToImageResponse, string> onDone)
        {
            return SendJsonRequest(TexturesUrl.generate, new ControlNetGenerateRequest(sourceGuid, sourceBase64, prompt, settings),
                RequestHandler<TextToImageResponse>((data, error) =>
                {
                    if (data != null)
                        data.seed = settings.seed;
                    onDone?.Invoke(data, error);
                }));
        }

        public static UnityWebRequestAsyncOperation GenerateInpainting(string prompt,
                                        string sourceGuid,
                                        Texture2D mask,
                                        MaskType maskType,
                                        TextToImageRequest settings,
                                        Action<TextToImageResponse, string> onDone)
        {
            return SendJsonRequest(TexturesUrl.inpaint, new InpaintingItemRequest(prompt, sourceGuid, mask, maskType, settings),
                RequestHandler<TextToImageResponse>((data, error) =>
                {
                    if (data != null)
                        data.seed = settings.seed;
                    onDone?.Invoke(data, error);
                }));
        }


        /// <summary>
        /// Download texture image from the Cloud
        /// </summary>
        /// <param name="artifact">The typed artifact identifier to request</param>
        /// <param name="onDone">Callback called when results are received. Callback parameters (Texture2D, byte[], string)
        ///                     represent received Texture2D object, it's original byte stream as PNG file and error string. In case error occured
        ///                     error string is non-null and other parameters are null</param>
        /// <returns>The the reference to the async operation this generates so that it may be cancelled</returns>
        public static UnityWebRequestAsyncOperation DownloadArtifact<TArtifactType>(Artifact<TArtifactType> artifact,  Action<object, string> onDone)
        {
            void HandleRequest(object data, string error)
            {
                var jsonData = JsonUtility.FromJson<DownloadURLResponse>(Encoding.UTF8.GetString((byte[])data));
                DownloadImageRequest(jsonData.url, onDone);
            }

            return SendGetRequest(TexturesUrl.textureAssets(artifact.Guid), null, HandleRequest);
        }

        /// <summary>
        /// Starts polling for the status of artifact generation. Will return the status through the supplied callback.
        /// This is not cancellable as many chained web requests can be generated by polling until completed.
        /// </summary>
        /// <param name="artifact">The artifact you wish to query the status of</param>
        /// <param name="onStatusReceived">The callback to receive the update from. This is guaranteed to run on the Unity main thread. Cannot be <b>null</b></param>
        /// <param name="pollUntilCompletedOrFailed"></param>
        /// <typeparam name="TArtifactType">The concrete artifact Unity type you are polling for</typeparam>
        public static void GetArtifactStatus<TArtifactType>(Artifact<TArtifactType> artifact,
                                                            ArtifactProgressCallback onStatusReceived,
                                                            bool pollUntilCompletedOrFailed = true)
        {
            void CheckStatus()
            {
                RequestStatus(artifact.Guid, (response, s) =>
                {
                    if (response is not {success: true})
                    {
                        onStatusReceived?.Invoke(artifact.Guid, StatusEnum.failed, -1f, s);
                        return;
                    }
                    var statusString = response.results[0].status;
                    switch (statusString)
                    {
                        case StatusEnum.done:
                            onStatusReceived?.Invoke(response.results[0].guid,
                                response.results[0].status,
                                response.results[0].progress,
                                string.Empty);
                            break;
                        case StatusEnum.failed:
                            onStatusReceived?.Invoke(response.results[0].guid,
                                response.results[0].status,
                                -1f,
                                s);
                            break;
                        case StatusEnum.waiting:
                        case StatusEnum.working:
                            onStatusReceived?.Invoke(response.results[0].guid,
                                response.results[0].status,
                                response.results[0].progress,
                                string.Empty);
                            if (pollUntilCompletedOrFailed)
                            {
                                CheckStatus();
                            }

                            break;
                    }
                });
            }

            CheckStatus();
        }

        /// <summary>
        /// Request status update from the Cloud for generation of artifacts
        /// </summary>
        static void RequestStatus(string guid, Action<StatusResponse, string> onDone)
        {
            SendGetRequest(TexturesUrl.jobs(guid), null, RequestHandler(onDone));
        }

        static UnityWebRequestAsyncOperation SendRequest(UnityWebRequest request, Action<object, string> onDone)
        {
            void PollForRequestCompletion()
            {
                if (!request.isDone)
                {
                    s_Context.RegisterNextFrameCallback(PollForRequestCompletion);
                    return;
                }
                if (!string.IsNullOrEmpty(request.error) || request.downloadedBytes == 0)
                {
                    try
                    {
                        var errorMessage = $"Request failed: {request.url} -- Failed to download because " +
                            (request.downloadedBytes == 0
                                ? $"response was empty: {request.error}"
                                : request.error + $"\n{request.downloadHandler?.text}");

                        if (request.responseCode >= 400 && errorMessage.Contains("Invalid or expired access_token"))
                        {
#if UNITY_EDITOR
                            UnityEditor.EditorApplication.ExecuteMenuItem("Window/Unity Connect/Clear Access Token");
                            UnityEditor.CloudProjectSettings.RefreshAccessToken(result =>
                            {
                                Debug.LogWarning("Access token has been refreshed. Please try your action again.");
                            });
                            errorMessage += " -- Trying to refresh access token. Please try again after token has been refreshed.";
#endif
                        }

                        if (request.error != "Request aborted")
                        {
                            var handled = OnServerError?.Invoke(request.responseCode, request.error) ?? false;
                            if (!handled)
                                Debug.LogError(errorMessage);
                        }

                        if (onDone != null && onDone.Target != null)
                            onDone(request.downloadHandler?.data, errorMessage);
                    }
                    finally
                    {
                        request.Dispose();
                    }
                }
                else
                {
                    try
                    {
                        byte[] data = request.downloadHandler.data;

                        if (onDone != null && onDone.Target != null)
                            onDone(data, null);
                    }
                    finally
                    {
                        request.Dispose();
                    }
                }
            }

            // Register the update event
            s_Context.RegisterNextFrameCallback(PollForRequestCompletion);

            // Kick off the webrequest
            return request.SendWebRequest();
        }

        protected static UnityWebRequestAsyncOperation SendJsonRequest(string serviceURL, object requestBody, Action<object, string> onDone)
        {
            var requestJson = JsonUtility.ToJson(requestBody);

            var request = new UnityWebRequest(serviceURL, "POST");
            request.SetRequestHeader("content-type", "application/json; charset=UTF-8");
            request.SetRequestHeader("authorization", $"Bearer {AccessToken}");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestJson));
            request.uploadHandler.contentType = "application/json";
            request.downloadHandler = new DownloadHandlerBuffer();

           return SendRequest(request, onDone);
        }

        static UnityWebRequestAsyncOperation SendGetRequest(string serviceURL, ItemRequest data, Action<object, string> onDone)
        {
            var url = serviceURL;
            if (!string.IsNullOrEmpty(data?.parameters))
                url = serviceURL + "?" + data.parameters;

            var request = new UnityWebRequest(url, "GET");
            request.SetRequestHeader("content-type", "application/json; charset=UTF-8");
            request.SetRequestHeader("Authorization", $"Bearer {AccessToken}");
            request.downloadHandler = new DownloadHandlerBuffer();

            return SendRequest(request, onDone);
        }

        static UnityWebRequestAsyncOperation DownloadImageRequest(string imageURL, Action<object, string> onDone)
        {
            var request = UnityWebRequestTexture.GetTexture(imageURL);
            return SendRequest(request, onDone);
        }

        /// <summary>
        /// Generic request handler
        /// </summary>
        protected static Action<object, string> RequestHandler<T>(Action<T, string> callabck) where T : class
        {
            return (data, error) =>
            {
                if (data != null && string.IsNullOrEmpty(error))
                {
                    var content = Encoding.UTF8.GetString((byte[]) data);
                    T result = null;
                    try
                    {
                        result = JsonUtility.FromJson<T>(content);
                    }
                    catch (Exception exception)
                    {
                        error = $"Error handling request: {content}\nException: {exception.Message}";
                    }

                    callabck(result, error);
                    return;
                }

                callabck(null, error);
            };
        }

        public static UnityWebRequestAsyncOperation GetEntitlements(Action<SubscriptionResponse, string> onDone)
        {
            return SendGetRequest(TexturesUrl.entitlements, null, RequestHandler(onDone));
        }

        public static UnityWebRequestAsyncOperation GetStatus(Action<ClientStatusResponse, string> onDone)
        {
            return SendGetRequest(TexturesUrl.status, new ClientStatusRequest(), RequestHandler(onDone));
        }

        public static UnityWebRequestAsyncOperation GetUsage(Action<UsageResponse, string> onDone)
        {
            return SendGetRequest(TexturesUrl.usage, null, RequestHandler(onDone));
        }
    }
}
