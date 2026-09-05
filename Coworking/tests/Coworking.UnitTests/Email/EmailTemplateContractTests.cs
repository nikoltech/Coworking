using Coworking.Infrastructure.Services.Email.Services;
using Coworking.Infrastructure.Services.Email.Templates.Models;
using HandlebarsDotNet;
using LazyCache;

namespace Coworking.UnitTests.Email;

public class EmailTemplateContractTests
{
    /// A row per letter, so a new letter costs a row rather than a test.
    public static TheoryData<string, object> Letters => new()
    {
        {
            "booking-created.hbs",
            new BookingCreatedTemplateModel(
                To: "guest@example.com",
                UserName: "Vitalii",
                DeskName: "A1",
                CoworkingName: "Main",
                FormattedStart: "05.09.2026 10:00",
                FormattedEnd: "05.09.2026 11:00",
                TimeZoneId: "Europe/Kyiv")
        },
        {
            "booking-cancelled.hbs",
            new BookingCancelledTemplateModel(
                To: "guest@example.com",
                UserName: "Vitalii",
                DeskName: "A1",
                CoworkingName: "Main",
                FormattedStart: "05.09.2026 10:00",
                FormattedEnd: "05.09.2026 11:00",
                TimeZoneId: "Europe/Kyiv",
                CancellationReason: "by user")
        }
    };

    /// <summary>
    /// Handlebars renders an unknown field as an empty string, so a template drifting from its
    /// model fails silently. Strict binding turns that into a failure here rather than a blank
    /// line in someone's inbox; the running service stays lenient, where a blank line still
    /// beats no email at all. Reading through the service also proves the files were deployed.
    /// </summary>
    [Theory]
    [MemberData(nameof(Letters))]
    public async Task EveryFieldATemplateAsksFor_ExistsOnItsModel(string file, object model)
    {
        var source = await new EmailTemplateService(new CachingService())
            .GetTemplateFromHbsFileAsync(file);

        var strict = Handlebars.Create(new HandlebarsConfiguration
        {
            ThrowOnUnresolvedBindingExpression = true
        });

        strict.Compile(source)(model);
    }

    [Fact]
    public void EveryTemplateShipped_IsCoveredByARow()
    {
        var shipped = Directory.GetFiles(EmailTemplateService.TemplatesDirectory, "*.hbs")
            .Select(Path.GetFileName)
            .Order();

        var covered = Letters
            .Select(row => (string)row[0]!)
            .Order();

        Assert.Equal(shipped, covered);
    }
}
