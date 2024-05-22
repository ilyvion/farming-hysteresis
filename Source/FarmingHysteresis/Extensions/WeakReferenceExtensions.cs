using System;

internal static class WeakReferenceExtensions
{
    public static T GetTargetOrThrow<T>(this WeakReference<T> weakReference)
        where T : class
    {
        if (weakReference.TryGetTarget(out var target))
        {
            return target;
        }
        else
        {
            throw new Exception("WeakReference was dead when expected not to be");
        }
    }
}
