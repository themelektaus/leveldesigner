using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace LevelDesigner.Editor
{
    public class EditorJob
    {
        const int PROCESS_FINALIZE_MAX_WAIT_COUNT = 50;
        const int PROCESS_FINALIZE_WAIT_SLEEP_DURATION = 20;

        public string name = null;
        public IEnumerator process;
        public int timeout = 300;

        public delegate void OnDoneHandler(object result);
        public event OnDoneHandler OnDone;

        public delegate void OnFailHandler();
        public event OnFailHandler OnFail;

        public delegate void OnCancelHandler();
        public event OnCancelHandler OnCancel;

        public delegate void OnTimeoutHandler();
        public event OnTimeoutHandler OnTimeout;

        public delegate void OnAlwaysHandler();
        public event OnAlwaysHandler OnAlways;

        public int progressID { get; private set; }

        object result;
        int cancel;
        IEnumerator runningProcess;
        System.DateTime? startTime;
        System.DateTime? endTime;
        float duration;

        public bool IsRunning => runningProcess is not null;

        string LogNameString
        {
            get
            {
                var result = "Job";

                if (!string.IsNullOrEmpty(name))
                    result += $" \"{name}\"";

                return result;
            }
        }

        string DurationString => duration.ToString().Replace(",", ".") + " seconds";

        public void Run()
        {
            startTime = null;
            endTime = null;
            EditorApplication.update += Update;
        }

        public void Cancel()
        {
            cancel = PROCESS_FINALIZE_MAX_WAIT_COUNT;
        }

        public void CancelImmediate()
        {
            cancel = 1;

        }

        void Update()
        {
            if (!startTime.HasValue)
            {
                runningProcess = process;
                startTime = System.DateTime.Now;
                Debug.Log($"{LogNameString} startet at {startTime}");
                progressID = Progress.Start("Editor Job");
            }

            if (cancel > 0)
            {
                FinalizeUpdate();
                Debug.LogWarning($"{LogNameString} canceled after {DurationString}");
                OnCancel?.Invoke();
                OnFail?.Invoke();
                OnAlways?.Invoke();
                Progress.Remove(progressID);
                return;
            }

            if (startTime.Value.AddSeconds(timeout) < System.DateTime.Now)
            {
                FinalizeUpdate();
                Debug.LogError($"{LogNameString} timed out after {DurationString}");
                OnTimeout?.Invoke();
                OnFail?.Invoke();
                OnAlways?.Invoke();
                Progress.Remove(progressID);
                return;
            }

            if (runningProcess.MoveNext())
            {
                result = runningProcess.Current;
                return;
            }

            FinalizeUpdate();
            Debug.Log($"{LogNameString} done after {DurationString}");
            OnDone?.Invoke(result);
            OnAlways?.Invoke();
            Progress.Remove(progressID);
        }

        void FinalizeUpdate()
        {
            endTime = System.DateTime.Now;
            duration = Mathf.Round((float) (endTime.Value - startTime.Value).TotalSeconds * 100) / 100;
            EditorApplication.update -= Update;
            var pendingProcess = runningProcess;
            runningProcess = null;
            int maxWaitCount = cancel > 0 ? cancel : PROCESS_FINALIZE_MAX_WAIT_COUNT;
            while (maxWaitCount > 0 && pendingProcess.MoveNext())
            {
                System.Threading.Thread.Sleep(PROCESS_FINALIZE_WAIT_SLEEP_DURATION);
                maxWaitCount--;
            }
        }

        public struct WebResponse
        {
            public long code;
            public byte[] data;
            public string text;

            public override string ToString()
            {
                return code.ToString();
            }

            public string ToJson()
            {
                return JsonUtility.ToJson(this);
            }

            public T FromJson<T>()
            {
                return JsonUtility.FromJson<T>(text);
            }
        }

        public static EditorJob Download(string url, string path, bool refreshAssetDatabase)
        {
            var job = new EditorJob();
            job.process = _WebRequest(job, url, null);
            job.OnDone += result =>
            {
                var response = (WebResponse) result;
                System.IO.File.WriteAllBytes(path, response.data);
                if (refreshAssetDatabase)
                    AssetDatabase.Refresh();
            };
            return job;
        }

        public static EditorJob WebRequest(string url, WWWForm postForm = null)
        {
            var job = new EditorJob();
            job.process = _WebRequest(job, url, postForm);
            return job;
        }

        static IEnumerator _WebRequest(EditorJob job, string url, WWWForm postForm)
        {
            using (var request = postForm == null ? UnityWebRequest.Get(url) : UnityWebRequest.Post(url, postForm))
            {
                request.timeout = 300;
                request.SendWebRequest();

                while (!request.isDone)
                {
                    if (!job.IsRunning)
                        request.Abort();

                    yield return null;
                }

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("Request Error: " + request.error);
                }

                yield return new WebResponse
                {
                    code = request.responseCode,
                    data = request.downloadHandler.data,
                    text = request.downloadHandler.text
                };
            }
        }

        public static bool WaitFor(IEnumerator command)
        {
            return command.MoveNext();
        }

        public static IEnumerator WaitCommand(float seconds)
        {
            var startTime = System.DateTime.Now;
            while ((System.DateTime.Now - startTime).TotalSeconds <= seconds)
                yield return null;
        }

        public static void RunSync(IEnumerator process)
        {
            for (; process.MoveNext();) ;
        }
    }
}