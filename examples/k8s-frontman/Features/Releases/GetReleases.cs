using k8s.Operator.Informer;
using Microsoft.AspNetCore.Http.HttpResults;

namespace k8s.Frontman.Features.Releases;

public static class GetReleases
{
    public static Results<Ok<ReleaseResponse[]>, BadRequest> Handle(IInformer<Release> releases)
    {
        var releasesList = releases.List()
            .Select(x => new ReleaseResponse(x.Metadata.Name, x.Spec.Url, x.Status?.CurrentVersion ?? "unknown", x.Status?.PreviousVersion ?? "unknown"))
            .ToArray();

        return TypedResults.Ok(releasesList);
    }

    public record ReleaseResponse(string Name, string Url, string CurrentVersion, string PreviousVersion);
}
