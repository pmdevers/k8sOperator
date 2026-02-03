using k8s.Operator;
using k8s.Frontman.Features.Releases;

namespace k8s.Frontman.Features.Releases;

public static class ReleaseFeature
{
    extension<T>(T app)
        where T : IHost, IApplicationBuilder
    {
        public void MapRelease()
        {
            app.UseMiddleware<ReleaseMiddleware>();
            app.ReconcilerFor<Release>(ReleaseReconciler.ReconcileAsync);
        }
    }
}
