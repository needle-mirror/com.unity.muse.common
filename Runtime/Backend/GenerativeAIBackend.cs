#define UNITY_GENERATIVE_AI_STAGING

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.Muse.Common
{
    public class GenerativeAIBackend
    {
        internal static event Action OnForbiddenAccess;

        public static class StatusEnum
        {
            public const string WAITING = "waiting";
            public const string WORKING = "working";
            public const string DONE = "done";
            public const string FAILED = "failed";
        }

        public enum GeneratorModel
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
                                    UnityConnectProxy.instance.GetAccessToken();
#else
                                    GameObject.Find("App").GetComponent<RuntimeCloudContext>().accessToken;
#endif

#if UNITY_GENERATIVE_AI_STAGING
        protected const string k_ServiceBaseURL = "https://musetools-stg-hbasf8cec2dxb0dh.z01.azurefd.net/api/v1";
#else
        protected const string k_ServiceBaseURL = "https://musetools-test-auamdbc0faeda7f4.z01.azurefd.net/api/v1";
#endif
        protected static readonly string k_TextToImageServiceBaseURL = $"{k_ServiceBaseURL}/text-to-image";
        static readonly string k_InpaintingURL = $"{k_ServiceBaseURL}/inpaint/generate";
        static readonly string k_ImageVariateURL = $"{k_ServiceBaseURL}/image_variate/generate";
        static readonly string k_ControlNetGenerateURL = $"{k_ServiceBaseURL}/controlnet/generate";
        static readonly string k_RequestGenerateStatusURL = $"{k_TextToImageServiceBaseURL}/request_status";
        static readonly string k_DownloadGeneratedImageURL = $"{k_TextToImageServiceBaseURL}/download_image";
        static readonly string k_DownloadURL = $"{k_TextToImageServiceBaseURL}/download_url";

        static readonly string k_StyleServiceBaseURL = $"{k_ServiceBaseURL}/style";
        static readonly string k_StyleTrainURL = $"{k_StyleServiceBaseURL}/train";
        static readonly string k_StyleTrainStatusURL = $"{k_StyleServiceBaseURL}/status";

        static ICloudContext s_Context;

        static GenerativeAIBackend()
        {
            s_Context = CloudContextFactory.GetCloudContext();
        }


        [System.Serializable]
        public class DownloadURLResponse
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
            void HandleRequest(object data, string error)
            {
                if (onDone != null)
                {
                    if (data != null)
                    {
                        var res = JsonUtility.FromJson<TextToImageResponse>(Encoding.UTF8.GetString((byte[])data));
                        res.seed = settings.seed;
                        onDone(res, error);
                        return;
                    }
                    onDone(null, error);
                }
            }

            object request;

            if (string.IsNullOrEmpty(sourceGuid))
            {
                request = new ImageVariationBase64Request(imageB64, prompt, settings, AccessToken);
            }
            else
            {
                request = new ImageVariationRequest(sourceGuid, prompt, settings, AccessToken);
            }

            return SendJSONRequest(k_ImageVariateURL, request, HandleRequest);
        }

        public static UnityWebRequestAsyncOperation ControlNetGenerate(
            string sourceGuid,
            string sourceBase64,
            string prompt,
            ImageVariationSettingsRequest settings,
            Action<TextToImageResponse, string> onDone)
        {
            void HandleRequest(object data, string error)
            {
                if (onDone != null)
                {
                    if (data != null)
                    {
                        var res = JsonUtility.FromJson<TextToImageResponse>(Encoding.UTF8.GetString((byte[])data));
                        res.seed = settings.seed;
                        onDone(res, error);
                        return;
                    }
                    onDone(null, error);
                }
            }

            return SendJSONRequest(k_ControlNetGenerateURL, new ControlNetGenerateRequest(
                    sourceGuid,
                    sourceBase64,
                    prompt,
                    settings, AccessToken),
                HandleRequest);
        }

        public static UnityWebRequestAsyncOperation GenerateInpainting(string prompt,
                                        string sourceGuid,
                                        Texture2D mask,
                                        MaskType maskType,
                                        TextToImageRequest settings,
                                        Action<TextToImageResponse, string> onDone)
        {
            void HandleRequest(object data, string error)
            {
                if (onDone != null)
                {
                    if (data != null)
                    {
                        var res = JsonUtility.FromJson<TextToImageResponse>(Encoding.UTF8.GetString((byte[])data));
                        res.seed = settings.seed;
                        onDone(res, error);
                        return;
                    }
                    onDone(null, error);
                }
            }

            return SendJSONRequest(k_InpaintingURL, new InpaintingItemRequest(prompt, sourceGuid, mask, maskType, settings, AccessToken),
                HandleRequest);
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

            return SendJSONRequest(k_DownloadURL, new DownloadImageRequest(artifact.Guid, AccessToken), HandleRequest);
        }

        /// <summary>
        /// Starts polling for the status of artifact generation. Will return the status through the supplied callback.
        /// This is not cancellable as many chained web requests can be generated by polling until copleted.
        /// TODO: Don't do that.
        /// </summary>
        /// <param name="artifact">The artifact you wish to query the status of</param>
        /// <param name="onStatusReceived">The callback to receive the update from. This is guaranteed to run on the Unity main thread. Cannot be <b>null</b></param>
        /// <param name="pollUntilCompletedOrFailed"></param>
        /// <typeparam name="TArtifactType">The concrete artifact Unity type you are polling for</typeparam>
        public static void GetArtifactStatus<TArtifactType>(Artifact<TArtifactType> artifact,
                                                            ArtifactProgressCallback onStatusReceived,
                                                            bool pollUntilCompletedOrFailed = true)
        {
            CheckStatus();

            void CheckStatus()
            {
                RequestStatus(new List<string> { artifact.Guid }, AccessToken, (response, s) =>
                {
                    if (response is not {success: true})
                    {
                        onStatusReceived?.Invoke(artifact.Guid, StatusEnum.FAILED, -1f, s);
                        return;
                    }
                    var statusString = response.results[0].status;
                    switch (statusString)
                    {
                        case StatusEnum.DONE:
                            onStatusReceived?.Invoke(response.results[0].guid,
                                response.results[0].status,
                                response.results[0].progress,
                                string.Empty);
                            break;
                        case StatusEnum.FAILED:
                            onStatusReceived?.Invoke(response.results[0].guid,
                                response.results[0].status,
                                -1f,
                                s);
                            break;
                        case StatusEnum.WAITING:
                        case StatusEnum.WORKING:
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
            };
        }

        /// <summary>
        /// Request status update from the Cloud for generation of artifacts
        /// </summary>
        /// <param name="guids">List of texture ids to query for progress</param>
        /// <param name="accessToken">Unity Connect Session token</param>
        /// <param name="onDone">Callback called when results are received. Callback parameters (StatusResponse, string).
        /// In case error occured error string is non-null and other parameters are null</param>
        /// <returns>The the reference to the async operation this generates so that it may be cancelled</returns>
        static UnityWebRequestAsyncOperation RequestStatus(List<string> guids, string accessToken, Action<StatusResponse, string> onDone)
        {
            void HandleRequest(object data, string error)
            {
                if (onDone != null && onDone.Target != null)
                {
                    StatusResponse res = null;
                    if (data != null && string.IsNullOrEmpty(error))
                    {
                        var json = Encoding.UTF8.GetString((byte[]) data);
                        res = JsonUtility.FromJson<StatusResponse>(json);

                        if (error != null && !string.IsNullOrEmpty(res.error))
                            error += res.error;

                        if (error == null)
                            error = res.error;
                    }
                    onDone(res, error);
                }
            }

            return SendJSONRequest(k_RequestGenerateStatusURL, new StatusRequest(guids, accessToken), HandleRequest);
        }


        /// <summary>
        /// Train a style using a set of textures
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="name"></param>
        /// <param name="texturesData"></param>
        /// <param name="onDone"></param>
        public static void RequestStyleTrain(string guid, string name, string[] texturesData, Action<StyleTrainResponse, string> onDone)
        {
            void HandleResponse(object data, string error)
            {
                if (onDone != null && onDone.Target != null)
                {
                    if (data != null && String.IsNullOrEmpty(error))
                    {
                        var res = JsonUtility.FromJson<StyleTrainResponse>(Encoding.UTF8.GetString((byte[])data));
                        onDone(res, error);
                        return;
                    }

                    onDone(null, error);
                }
            }

            SendJSONRequest(k_StyleTrainURL, new StyleTrainRequest(AccessToken, guid, name, texturesData), HandleResponse);
        }

        public static void RequestStyleTrainStatus(string guid, Action<StyleTrainStatusResponse, string> onDone)
        {
            void HandleResponse(object data, string error)
            {
                if (onDone != null && onDone.Target != null)
                {
                    StyleTrainStatusResponse res = null;
                    if (data != null && string.IsNullOrEmpty(error))
                    {
                        res = JsonUtility.FromJson<StyleTrainStatusResponse>(Encoding.UTF8.GetString((byte[])data));

                        if (error != null && !string.IsNullOrEmpty(res.error))
                            error += res.error;

                        error ??= res.error;
                    }

                    onDone(res, error);
                }
            }

            SendJSONRequest(k_StyleTrainStatusURL, new StyleTrainStatusRequest(AccessToken, guid), HandleResponse);
        }

        protected static UnityWebRequestAsyncOperation SendRequest(UnityWebRequest request, Action<object, string> onDone)
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
                        var errorMessage = "Failed to download because " +
                            (request.downloadedBytes == 0 ? $"response was empty: {request?.error}" : request.error + $"\n{request?.downloadHandler?.text}");

                        if (request.error != "Request aborted")
                        {
                            Debug.LogError(errorMessage);

                            if (request.error.Contains("HTTP/1.1 403 Forbidden"))
                            {
                                OnForbiddenAccess?.Invoke();
                            }
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

        protected static UnityWebRequestAsyncOperation SendJSONRequest(string serviceURL, object requestBody, Action<object, string> onDone)
        {
            var requestJSON = JsonUtility.ToJson(requestBody);

            var request = new UnityWebRequest(serviceURL, "POST");
            request.SetRequestHeader("content-type", "application/json; charset=UTF-8");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestJSON));
            request.uploadHandler.contentType = "application/json";
            request.downloadHandler = new DownloadHandlerBuffer();

           return SendRequest(request, onDone);
        }

        static UnityWebRequestAsyncOperation DownloadImageRequest(string imageURL, Action<object, string> onDone)
        {
            var request = UnityWebRequestTexture.GetTexture(imageURL);

            return SendRequest(request, onDone);
        }
    }
}