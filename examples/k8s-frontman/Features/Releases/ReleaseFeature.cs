using k8s.Frontman.Features.Releases;
using k8s.Operator;

namespace k8s.Frontman.Features.Releases;

public static class ReleaseFeature
{
    extension<T>(T app)
        where T : IHost, IApplicationBuilder, IEndpointRouteBuilder
    {
        public void MapRelease()
        {
            app.UseMiddleware<ReleaseMiddleware>();

            app.MapGet("/releases", GetReleases.Handle);

            app.ReconcilerFor<Release>(ReleaseReconciler.ReconcileAsync);
        }
    }
}
