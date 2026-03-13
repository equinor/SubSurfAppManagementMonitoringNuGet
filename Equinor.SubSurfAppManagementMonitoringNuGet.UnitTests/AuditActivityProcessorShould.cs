using Equinor.SubSurfAppManagementMonitoringNuGet.Config;

namespace Equinor.SubSurfAppManagementMonitoringNuGet.UnitTests;

using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;

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
        Assert.That(_activity.TagObjects, Is.Empty, "No tags should be set when HttpContext is null");
    }

    [Test]
    public void SetClientAddressTag_WhenRemoteIpAddressIsPresent()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            Connection =
            {
                RemoteIpAddress = IPAddress.Parse("192.168.1.1")
            }
        };
        _mockContextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        _activity.Stop();
        _processor.OnEnd(_activity);

        // Assert
        Assert.That(_activity.GetTagItem(AuditActivityProcessor.RemoteIpTag), Is.EqualTo("192.168.1.1"));
    }

    [Test]
    public void SetClientAddressTag_FromFirstXForwardedForHop_WhenHeaderIsPresent()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            Connection =
            {
                RemoteIpAddress = IPAddress.Parse("10.10.10.10")
            },
            Request = { Headers = { ["X-Forwarded-For"] = "203.0.113.10, 70.41.3.18" } }
        };
        _mockContextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        _activity.Stop();
        _processor.OnEnd(_activity);

        // Assert
        Assert.That(_activity.GetTagItem(AuditActivityProcessor.RemoteIpTag), Is.EqualTo("203.0.113.10"));
    }

    [Test]
    public void SetClientAddressTag_FromRemoteIpAddress_WhenXForwardedForIsEmpty()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            Connection =
            {
                RemoteIpAddress = IPAddress.Parse("192.168.1.1")

            },
            Request = { Headers = { ["X-Forwarded-For"] = "   " } }
        };
        _mockContextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        _activity.Stop();
        _processor.OnEnd(_activity);

        // Assert
        Assert.That(_activity.GetTagItem(AuditActivityProcessor.RemoteIpTag), Is.EqualTo("192.168.1.1"));
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
        Assert.That(_activity.GetTagItem(AuditActivityProcessor.UserAgentTag), Is.EqualTo("Mozilla/5.0"));
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
            "Name claim should take priority over Email claim");
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
            "Email claim should be used as fallback when Name claim is absent");
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
        Assert.That(_activity.GetTagItem(AuditActivityProcessor.ApplicationRolesTag), Is.EqualTo("Admin, Editor"));
    }

    [Test]
    public void SetUserApplicationRolesTag_AsEmpty_WhenUserHasNoRoles()
    {
        // Arrange
        var context = CreateAuthenticatedHttpContext(
            new Claim(ClaimTypes.Name, "John Doe")
        );
        _mockContextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        _activity.Stop();
        _processor.OnEnd(_activity);

        // Assert
        Assert.That(_activity.GetTagItem(AuditActivityProcessor.ApplicationRolesTag), Is.EqualTo(""));
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
    public void SetAuthIdToUnknown_WhenExceptionIsThrown()
    {
        // Arrange
        _mockContextAccessor.Setup(x => x.HttpContext).Throws(new Exception("Simulated exception"));

        // Act
        _activity.Stop();
        _processor.OnEnd(_activity);

        // Assert
        Assert.That(_activity.GetTagItem(AuditActivityProcessor.AuthenticatedUserTag), Is.EqualTo("<unknown>"));
    }

    private static DefaultHttpContext CreateAuthenticatedHttpContext(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        return new DefaultHttpContext { User = principal };
    }
}
