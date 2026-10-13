using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Infrastructure.Authorization;

internal sealed class PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    //Policy caching is not strictly necessary, but it can improve performance by avoiding the need to create a new policy for every request.
    private readonly ConcurrentDictionary<string, AuthorizationPolicy> _permissionPolicies =
        new(StringComparer.Ordinal);

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        AuthorizationPolicy? policy = await base.GetPolicyAsync(policyName);

        return policy ?? _permissionPolicies.GetOrAdd(
            policyName,
            static name => new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(name))
                .Build());
    }
}
