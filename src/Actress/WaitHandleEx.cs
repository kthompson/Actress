using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Actress
{
    static class WaitHandleEx
    {
        public static Task<bool> ToTask(this WaitHandle waitHandle, TimeSpan? maxValue = null)
        {
            var tcs = new TaskCompletionSource<bool>();

            // Registering callback to wait till WaitHandle changes its state
            WaitOrTimerCallback callBack = (o, timeout) =>
            {
                tcs.SetResult(!timeout);
            };

            ThreadPool.RegisterWaitForSingleObject(waitHandle, callBack, null,
                maxValue ?? TimeSpan.MaxValue, true);

            return tcs.Task;
        }
    }
}
