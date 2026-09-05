using Equinor.SubSurfAppManagementMonitoringNuGet.Config;

namespace Equinor.SubSurfAppManagementMonitoringNuGet.UnitTests;

using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;

// AuditActivityProcessor is obsolete (superseded by AuditActivityEnricher via AddSamMonitoring), but this file
// intentionally keeps testing it directly to preserve backward-compatible coverage for existing consumers
// still on the legacy Startup.Configure()/AddSubSurfAppManagementMonitoring() path.
#pragma warning disable CS0618 // Type or member is obsolete
public class AuditActivityProcessorShould
{
    private Mock<IHttpContextAccessor> _mockContextAccessor;
    private AuditActivityProcessor _processor;
    private Activity _activity;

    [SetUp]
    public void Setup()
    {
        _mockContextAccessor = new Mock<IHttpContextAccessor>();
        _mockContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        _processor = new AuditActivityProcessor(_mockContextAccessor.Object);
        _activity = new Activity("TestOperation").Start();
    }

    [TearDown]
    public void TearDown()
    {
        _activity.Dispose();
        _processor.Dispose();
    }

    [Test]
    public void NotSetAnyTags_WhenHttpContextIsNull()
    {
        // Arrange
        _mockContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        _activity.Stop();
        _processor.OnEnd(_activity);

        // Assert
        Assert.That(
            _activity.TagObjects,
            Is.Empty,
            "No tags should be set when HttpContext is null"
        );
    }

    [Test]
    public void SetClientAddressTag_WhenRemoteIpAddressIsPresent()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = IPAddress.Parse("192.168.1.1") },
        };
        _mockContextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        _activity.Stop();
        _processor.OnEnd(_activity);

        // Assert
        Assert.That(
            _activity.GetTagItem(AuditActivityProcessor.RemoteIpTag),
            Is.EqualTo("192.168.1.1")
        );
    }

    [Test]
    public void SetClientAddressTag_FromLastXForwardedForHop_WhenHeaderIsPresent()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = IPAddress.Parse("10.10.10.10") },
            Request = { Headers = { ["X-Forwarded-For"] = "203.0.113.10, 70.41.3.18" } },
        };
        _mockContextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        _activity.Stop();
        _processor.OnEnd(_activity);

        // Assert
        Assert.That(
            _activity.GetTagItem(AuditActivityProcessor.RemoteIpTag),
            Is.EqualTo("70.41.3.18")
        );
    }

    [Test]
    public void SetClientAddressTag_FromRemoteIpAddress_WhenXForwardedForIsEmpty()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = IPAddress.Parse("192.168.1.1") },
            Request = { Headers = { ["X-Forwarded-For"] = "   " } },
        };
        _mockContextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        _activity.Stop();
        _processor.OnEnd(_activity);

        // Assert
        Assert.That(
            _activity.GetTagItem(AuditActivityProcessor.RemoteIpTag),
            Is.EqualTo("192.168.1.1")
        );
    }

    [Test]
    public void NotSetClientAddressTag_WhenRemoteIpAddressIsNull()
    {
        // RemoteIpAddress is null by default
        // Act
        _activity.Stop();
        _processor.OnEnd(_activity);

        // Assert
        Assert.That(_activity.GetTagItem(AuditActivityProcessor.RemoteIpTag), Is.Null);
    }

    [Test]
    public void SetUserAgentTag_WhenUserAgentHeaderIsPresent()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers.UserAgent = "Mozilla/5.0";
        _mockContextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        _activity.Stop();
        _processor.OnEnd(_activity);

        // Assert
        Assert.That(
            _activity.GetTagItem(AuditActivityProcessor.UserAgentTag),
            Is.EqualTo("Mozilla/5.0")
        );
    }

    [Test]
    public void NotSetUserAgentTag_WhenUserAgentHeaderIsAbsent()
    {
        // Act
        _activity.Stop();
        _processor.OnEnd(_activity);

        // Assert
        Assert.That(_activity.GetTagItem(AuditActivityProcessor.UserAgentTag), Is.Null);
    }

    [Test]
    public void SetAuthIdTag_FromNameClaim_WhenUserIsAuthenticated()
    {
        // Arrange
        var context = CreateAuthenticatedHttpContext(
            new Claim(ClaimTypes.Name, "John Doe"),
            new Claim(ClaimTypes.Email, "john@example.com")
        );
        _mockContextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        _activity.Stop();
        _processor.OnEnd(_activity);

        // Assert
        Assert.That(
            _activity.GetTagItem(AuditActivityProcessor.AuthenticatedUserTag),
            Is.EqualTo("John Doe"),
            "Name claim should take priority over Email claim"
        );
    }

    [Test]
    public void SetAuthIdTag_FromEmailClaim_WhenNameClaimIsAbsent()
    {
        // Arrange
        var context = CreateAuthenticatedHttpContext(
            new Claim(ClaimTypes.Email, "john@example.com")
        );
        _mockContextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        _activity.Stop();
        _processor.OnEnd(_activity);

        // Assert
        Assert.That(
            _activity.GetTagItem(AuditActivityProcessor.AuthenticatedUserTag),
            Is.EqualTo("john@example.com"),
            "Email claim should be used as fallback when Name claim is absent"
        );
    }

    [Test]
    public void SetUserApplicationRolesTag_WhenUserHasRoles()
    {
        // Arrange
        var context = CreateAuthenticatedHttpContext(
            new Claim(ClaimTypes.Name, "John Doe"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "Editor")
        );
        _mockContextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        _activity.Stop();
        _processor.OnEnd(_activity);

        // Assert
        Assert.That(
            _activity.GetTagItem(AuditActivityProcessor.ApplicationRolesTag),
            Is.EqualTo("Admin, Editor")
        );
    }

    [Test]
    public void SetUserApplicationRolesTag_FromRawRolesClaimType_WhenMapInboundClaimsIsDisabled()
    {
        // Apps that set JwtBearerOptions.MapInboundClaims = false keep the raw Azure AD "roles" claim instead of
        // the mapped ClaimTypes.Role claim. Role capture must work regardless of that consumer-side choice, and
        // must stay consistent with AuditActivityEnricher, which shares this resolution logic via AuditClaims.
        var context = CreateAuthenticatedHttpContext(
            new Claim(ClaimTypes.Name, "John Doe"),
            new Claim("roles", "Admin"),
            new Claim("roles", "Editor")
        );
        _mockContextAccessor.Setup(x => x.HttpContext).Returns(context);

        _activity.Stop();
        _processor.OnEnd(_activity);

        Assert.That(
            _activity.GetTagItem(AuditActivityProcessor.ApplicationRolesTag),
            Is.EqualTo("Admin, Editor")
        );
    }

    [Test]
    public void DedupeUserApplicationRolesTag_WhenSameRoleClaimAppearsOnMultipleIdentities()
    {
        // Arrange: cookie + JWT identities both asserting "Admin" is a common real-world duplication source.
        var cookieIdentity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "John Doe"), new Claim(ClaimTypes.Role, "Admin")],
            "Cookies"
        );
        var jwtIdentity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Admin"), new Claim(ClaimTypes.Role, "Editor")],
            "Bearer"
        );
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal([cookieIdentity, jwtIdentity]),
        };
        _mockContextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        _activity.Stop();
        _processor.OnEnd(_activity);

        // Assert
        Assert.That(
            _activity.GetTagItem(AuditActivityProcessor.ApplicationRolesTag),
            Is.EqualTo("Admin, Editor"),
            "Duplicate role claims across identities should only be counted once"
        );
    }

    [Test]
    public void SetUserApplicationRolesTag_AsEmpty_WhenUserHasNoRoles()
    {
        // Arrange
        var context = CreateAuthenticatedHttpContext(new Claim(ClaimTypes.Name, "John Doe"));
        _mockContextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        _activity.Stop();
        _processor.OnEnd(_activity);

        // Assert
        Assert.That(
            _activity.GetTagItem(AuditActivityProcessor.ApplicationRolesTag),
            Is.EqualTo("")
        );
    }

    [Test]
    public void NotSetAuthTags_WhenUserIsNotAuthenticated()
    {
        // User is unauthenticated by default
        // Act
        _activity.Stop();
        _processor.OnEnd(_activity);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(_activity.GetTagItem(AuditActivityProcessor.AuthenticatedUserTag), Is.Null);
            Assert.That(_activity.GetTagItem(AuditActivityProcessor.ApplicationRolesTag), Is.Null);
        }
    }

    [Test]
    public void NotSetAuthIdTag_WhenHttpContextResolutionThrows()
    {
        // A failure resolving HttpContext itself happens before we can know whether the request is even
        // authenticated. Previously this mislabeled such requests as an unresolved authenticated user
        // ("<unknown>"), corrupting SAM's per-user KQL analytics. It must now leave the tag unset instead.
        _mockContextAccessor
            .Setup(x => x.HttpContext)
            .Throws(new Exception("Simulated exception"));

        _activity.Stop();
        _processor.OnEnd(_activity);

        Assert.That(_activity.GetTagItem(AuditActivityProcessor.AuthenticatedUserTag), Is.Null);
    }

    [Test]
    public void SetAuthIdToUnknown_WhenClaimResolutionThrowsOnAnAuthenticatedRequest()
    {
        // Only once we know the request IS authenticated, and claim resolution itself fails, should the
        // "<unknown>" sentinel be set - never for unrelated failures.
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ThrowingClaimsIdentity()) };
        _mockContextAccessor.Setup(x => x.HttpContext).Returns(context);

        _activity.Stop();
        _processor.OnEnd(_activity);

        Assert.That(
            _activity.GetTagItem(AuditActivityProcessor.AuthenticatedUserTag),
            Is.EqualTo("<unknown>")
        );
    }

    private static DefaultHttpContext CreateAuthenticatedHttpContext(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        return new DefaultHttpContext { User = principal };
    }

    [Test]
    public void KeepTagKeysStable_BecauseSamsOwnAppInsightsKqlQueriesDependOnThem()
    {
        // AuditActivityProcessor.AuthenticatedUserTag ("enduser.id") is the OpenTelemetry semantic convention
        // that the Azure Monitor exporter maps to the classic "user_AuthenticatedId" field, and
        // ApplicationRolesTag ("UserApplicationRoles") is surfaced under Application Insights' customDimensions.
        // Sam.Api.Services.ApplicationInsightService.GetRoleCountPerDay's KQL query reads both of these by exact
        // name (customDimensions["UserApplicationRoles"], user_AuthenticatedId) to compute per-role usage
        // analytics, so renaming either tag here would silently break that feature for every consuming app.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(AuditActivityProcessor.AuthenticatedUserTag, Is.EqualTo("enduser.id"));
            Assert.That(AuditActivityProcessor.ApplicationRolesTag, Is.EqualTo("UserApplicationRoles"));
        }
    }

    /// <summary>
    /// A <see cref="ClaimsIdentity"/> that reports as authenticated but throws when its claims are enumerated,
    /// used to simulate a failure during claim resolution specifically (as opposed to a failure resolving
    /// <see cref="HttpContext"/> itself).
    /// </summary>
    private sealed class ThrowingClaimsIdentity() : ClaimsIdentity(authenticationType: "Test")
    {
        public override IEnumerable<Claim> Claims => throw new InvalidOperationException("Simulated claims failure");
    }
}
#pragma warning restore CS0618
