using Equinor.SubSurfAppManagementMonitoringNuGet.Config;

namespace Equinor.SubSurfAppManagementMonitoringNuGet.UnitTests;

using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

public class AuditActivityEnricherShould
{
    private Activity _activity;

    [SetUp]
    public void Setup()
    {
        _activity = new Activity("TestOperation").Start();
    }

    [TearDown]
    public void TearDown()
    {
        _activity.Dispose();
    }

    [Test]
    public void SetClientAddressTag_WhenRemoteIpAddressIsPresent()
    {
        var httpRequest = CreateHttpRequest(context =>
            context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.1"));

        AuditActivityEnricher.Enrich(_activity, httpRequest);

        Assert.That(_activity.GetTagItem(AuditTagNames.RemoteIpTag), Is.EqualTo("192.168.1.1"));
    }

    [Test]
    public void SetClientAddressTag_FromLastXForwardedForHop_WhenHeaderIsPresent()
    {
        var httpRequest = CreateHttpRequest(context =>
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse("10.10.10.10");
            context.Request.Headers["X-Forwarded-For"] = "203.0.113.10, 70.41.3.18";
        });

        AuditActivityEnricher.Enrich(_activity, httpRequest);

        Assert.That(_activity.GetTagItem(AuditTagNames.RemoteIpTag), Is.EqualTo("70.41.3.18"));
    }

    [Test]
    public void SetUserAgentTag_WhenUserAgentHeaderIsPresent()
    {
        var httpRequest = CreateHttpRequest(context =>
            context.Request.Headers.UserAgent = "Mozilla/5.0");

        AuditActivityEnricher.Enrich(_activity, httpRequest);

        Assert.That(_activity.GetTagItem(AuditTagNames.UserAgentTag), Is.EqualTo("Mozilla/5.0"));
    }

    [Test]
    public void NotSetUserAgentTag_WhenUserAgentHeaderIsAbsent()
    {
        var httpRequest = CreateHttpRequest();

        AuditActivityEnricher.Enrich(_activity, httpRequest);

        Assert.That(_activity.GetTagItem(AuditTagNames.UserAgentTag), Is.Null);
    }

    [Test]
    public void SetAuthIdTag_FromNameClaim_WhenUserIsAuthenticated()
    {
        var httpRequest = CreateAuthenticatedHttpRequest(
            new Claim(ClaimTypes.Name, "John Doe"),
            new Claim(ClaimTypes.Email, "john@example.com")
        );

        AuditActivityEnricher.Enrich(_activity, httpRequest);

        Assert.That(
            _activity.GetTagItem(AuditTagNames.AuthenticatedUserTag),
            Is.EqualTo("John Doe"),
            "Name claim should take priority over Email claim"
        );
    }

    [Test]
    public void SetUserApplicationRolesTag_WhenUserHasRoles()
    {
        var httpRequest = CreateAuthenticatedHttpRequest(
            new Claim(ClaimTypes.Name, "John Doe"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "Editor")
        );

        AuditActivityEnricher.Enrich(_activity, httpRequest);

        Assert.That(_activity.GetTagItem(AuditTagNames.ApplicationRolesTag), Is.EqualTo("Admin, Editor"));
    }

    [Test]
    public void SetUserApplicationRolesTag_FromRawRolesClaimType_WhenMapInboundClaimsIsDisabled()
    {
        // Apps that set JwtBearerOptions.MapInboundClaims = false keep the raw Azure AD "roles" claim instead of
        // the mapped ClaimTypes.Role claim. Role capture must work regardless of that consumer-side choice.
        var httpRequest = CreateAuthenticatedHttpRequest(
            new Claim(ClaimTypes.Name, "John Doe"),
            new Claim("roles", "Admin"),
            new Claim("roles", "Editor")
        );

        AuditActivityEnricher.Enrich(_activity, httpRequest);

        Assert.That(_activity.GetTagItem(AuditTagNames.ApplicationRolesTag), Is.EqualTo("Admin, Editor"));
    }

    [Test]
    public void DedupeUserApplicationRolesTag_WhenSameRoleClaimAppearsOnMultipleIdentities()
    {
        var cookieIdentity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "John Doe"), new Claim(ClaimTypes.Role, "Admin")],
            "Cookies"
        );
        var jwtIdentity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Admin"), new Claim(ClaimTypes.Role, "Editor")],
            "Bearer"
        );
        var httpRequest = CreateHttpRequest(context =>
            context.User = new ClaimsPrincipal([cookieIdentity, jwtIdentity]));

        AuditActivityEnricher.Enrich(_activity, httpRequest);

        Assert.That(
            _activity.GetTagItem(AuditTagNames.ApplicationRolesTag),
            Is.EqualTo("Admin, Editor"),
            "Duplicate role claims across identities should only be counted once"
        );
    }

    [Test]
    public void NotSetAuthTags_WhenUserIsNotAuthenticated()
    {
        var httpRequest = CreateHttpRequest();

        AuditActivityEnricher.Enrich(_activity, httpRequest);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_activity.GetTagItem(AuditTagNames.AuthenticatedUserTag), Is.Null);
            Assert.That(_activity.GetTagItem(AuditTagNames.ApplicationRolesTag), Is.Null);
        }
    }

    [Test]
    public void NotSetClientAddressTag_WhenCaptureClientIpAddressIsDisabled()
    {
        var httpRequest = CreateHttpRequest(context =>
            context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.1"));
        var options = new SamMonitoringOptions { CaptureClientIpAddress = false };

        AuditActivityEnricher.Enrich(_activity, httpRequest, options);

        Assert.That(_activity.GetTagItem(AuditTagNames.RemoteIpTag), Is.Null);
    }

    [Test]
    public void StillSetUserAndRoleTags_WhenCaptureClientIpAddressIsDisabled()
    {
        // Disabling IP capture should not affect the rest of the audit trail SAM's analytics rely on.
        var httpRequest = CreateAuthenticatedHttpRequest(
            new Claim(ClaimTypes.Name, "John Doe"),
            new Claim(ClaimTypes.Role, "Admin")
        );
        var options = new SamMonitoringOptions { CaptureClientIpAddress = false };

        AuditActivityEnricher.Enrich(_activity, httpRequest, options);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_activity.GetTagItem(AuditTagNames.AuthenticatedUserTag), Is.EqualTo("John Doe"));
            Assert.That(_activity.GetTagItem(AuditTagNames.ApplicationRolesTag), Is.EqualTo("Admin"));
        }
    }

    [Test]
    public void SetClientAddressTag_WhenOptionsOverloadIsUsedWithDefaults()
    {
        // The default SamMonitoringOptions (as constructed by the parameterless Enrich overload) must keep
        // capturing IP addresses, preserving backward-compatible, "correct by default" behavior.
        var httpRequest = CreateHttpRequest(context =>
            context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.1"));

        AuditActivityEnricher.Enrich(_activity, httpRequest, new SamMonitoringOptions());

        Assert.That(_activity.GetTagItem(AuditTagNames.RemoteIpTag), Is.EqualTo("192.168.1.1"));
    }

    [Test]
    public void SetAuthIdToUnknown_WhenClaimResolutionThrowsOnAnAuthenticatedRequest()
    {
        // Only once we know the request IS authenticated, and claim resolution itself fails, should the
        // "<unknown>" sentinel be set - never for unrelated failures (see AuditActivityProcessorShould for the
        // matching regression test on the legacy path).
        var httpRequest = CreateHttpRequest(context =>
            context.User = new ClaimsPrincipal(new ThrowingClaimsIdentity()));

        AuditActivityEnricher.Enrich(_activity, httpRequest);

        Assert.That(_activity.GetTagItem(AuditTagNames.AuthenticatedUserTag), Is.EqualTo("<unknown>"));
    }

    private static HttpRequest CreateHttpRequest(Action<DefaultHttpContext>? configure = null)
    {
        var context = new DefaultHttpContext();
        configure?.Invoke(context);
        return context.Request;
    }

    private static HttpRequest CreateAuthenticatedHttpRequest(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        return CreateHttpRequest(context => context.User = principal);
    }

    /// <summary>
    /// A <see cref="ClaimsIdentity"/> that reports as authenticated but throws when its claims are enumerated,
    /// used to simulate a failure during claim resolution specifically (as opposed to any other failure inside
    /// <see cref="AuditActivityEnricher.Enrich(Activity, HttpRequest)"/>).
    /// </summary>
    private sealed class ThrowingClaimsIdentity() : ClaimsIdentity(authenticationType: "Test")
    {
        public override IEnumerable<Claim> Claims => throw new InvalidOperationException("Simulated claims failure");
    }
}
