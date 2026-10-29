using System;
using UnityEngine;

namespace AstraSyndicate.Core
{
    public static class AwaitableExtensions
    {
        /// <summary>Starts an awaitable without awaiting it. Exceptions are logged instead of being lost.</summary>
        public static async void Forget(this Awaitable awaitable)
        {
            try
            {
                await awaitable;
            }
            catch (OperationCanceledException)
            {
                // Cancellation (e.g. the owning object was destroyed) is an expected way to stop.
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <inheritdoc cref="Forget(Awaitable)"/>
        public static async void Forget<T>(this Awaitable<T> awaitable)
        {
            try
            {
                await awaitable;
            }
            catch (OperationCanceledException)
            {
                // Cancellation (e.g. the owning object was destroyed) is an expected way to stop.
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
