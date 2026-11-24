using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Chatan.LLM
{
    /// <summary>Lets code <c>await</c> a <see cref="UnityWebRequest"/>.</summary>
    /// <remarks>
    /// Unity raises <c>completed</c> on the main thread, so after the <c>await</c> the
    /// caller can touch the UI. Never block on the task with <c>.Result</c> or
    /// <c>.Wait()</c>: the main thread would wait for itself and Unity would freeze.
    /// </remarks>
    public static class UnityWebRequestAwait
    {
        /// <summary>Sends the request and finishes when it completes, whatever the result.</summary>
        public static Task<UnityWebRequest> SendAsync(this UnityWebRequest request)
        {
            var tcs = new TaskCompletionSource<UnityWebRequest>();
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            if (operation.isDone)
            {
                tcs.TrySetResult(request);
            }
            else
            {
                operation.completed += _ => tcs.TrySetResult(request);
            }

            return tcs.Task;
        }
    }
}
