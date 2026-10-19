using System.Security.Claims;
using OrderFlow.Application.Common;

namespace OrderFlow.Api.Services;

public class CurrentUser : ICurrentUser
{
    private readonly ClaimsPrincipal? _user;

    public CurrentUser(IHttpContextAccessor accessor) => _user = accessor.HttpContext?.User;

    public int? UserId => ReadInt("sub");
    public int? CustomerId => ReadInt("customer_id");
    public bool IsAdmin => _user?.IsInRole("Admin") ?? false;

    private int? ReadInt(string claimType) =>
        int.TryParse(_user?.FindFirst(claimType)?.Value, out var value) ? value : null;
}