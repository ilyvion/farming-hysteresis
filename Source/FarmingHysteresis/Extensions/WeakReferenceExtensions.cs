internal static class WeakReferenceExtensions
{
    public static T GetTargetOrThrow<T>(this System.WeakReference<T> weakReference)
        where T : class =>
        weakReference.TryGetTarget(out var target)
            ? target
            : throw new InvalidOperationException("WeakReference was dead when expected not to be");
}
