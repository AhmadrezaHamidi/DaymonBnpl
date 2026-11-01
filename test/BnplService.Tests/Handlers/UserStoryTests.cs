using Microsoft.EntityFrameworkCore;
using BnplService.Application.Handler.Admin.Messages.Commands.SavePageMessage;
using BnplService.Application.Handler.Admin.Messages.Queries.SearchPageMessages;
using BnplService.Application.Handler.Admin.Settings.Commands.UpdateSettings;
using BnplService.Application.Handler.Admin.Settings.Queries.GetSettings;
using BnplService.Application.Handler.Customer.Config.Queries.GetConfig;
using BnplService.Application.Handler.Customer.Messages.Queries.GetPageMessage;
using BnplService.Application.Handler.Customer.Profile.Commands.UpsertProfileAddress;
using BnplService.Application.Handler.Customer.Profile.Commands.UpsertProfileInfo;
using BnplService.Application.Handler.Customer.Profile.Queries.GetProfile;
using BnplService.Application.Handler.Customer.Profile.Queries.GetProvinces;

namespace BnplService.Tests.Handlers;

/// <summary>معیارهای پذیرش یوزر استوری الماس امید (بخش ۳، ۴ و US-5) که در فیگما نبود.</summary>
public class UserStoryTests
{
    private static UpsertProfileAddressCommand Address(string province = "تهران", string phone = "02188888888") => new()
    {
        Province = province, City = "تهران", Street = "خیابان ولیعصر، پلاک ۱", PostalCode = "1234567890", TelephoneNumber = phone,
    };

    [Fact]
    public async Task Suggested_amounts_come_from_admin_settings()
    {
        await using var host = await BnplTestHost.CreateAsync();
        Assert.Equal([50_000_000, 100_000_000, 200_000_000, 500_000_000], (await host.Ok(new GetConfigQuery())).SuggestedAmounts);

        var s = (await host.Ok(new GetSettingsQuery())).Settings;
        UpdateSettingsCommand Update(IReadOnlyList<long> amounts) => new()
        {
            IsEnabled = true, MinAmount = s.MinAmount, MaxAmount = s.MaxAmount, AmountStep = s.AmountStep, CompletionHours = s.CompletionHours,
            ActivationMinutes = s.ActivationMinutes, GatewayPaymentMinutes = s.GatewayPaymentMinutes,
            MaxActiveContractsPerCustomer = s.MaxActiveContractsPerCustomer, RowVersion = s.RowVersion, SuggestedAmounts = amounts,
        };
        Assert.Equal(400, (await host.Send(Update([55_000_000]))).StatusCode);   // مضرب گام نیست
        var saved = await host.Ok(Update([300_000_000, 60_000_000, 60_000_000]));
        Assert.Equal([60_000_000, 300_000_000], saved.SuggestedAmounts);
        Assert.Equal([60_000_000, 300_000_000], (await host.Ok(new GetConfigQuery())).SuggestedAmounts);
    }

    [Fact]
    public async Task Registered_profile_and_address_can_be_viewed_for_editing()
    {
        await using var host = await BnplTestHost.CreateAsync();
        host.Profiles.ProfileCompleted = host.Profiles.AddressCompleted = false;
        var empty = await host.Ok(new GetProfileQuery());
        Assert.Equal("09121111111", empty.MobileNumber);
        Assert.False(empty.ProfileCompleted);
        Assert.Null(empty.NationalCode);

        await host.Ok(new UpsertProfileInfoCommand
        {
            FirstName = " علی ", LastName = "محمدی", FatherName = "حسین", NationalCode = "0013542419",
            PersianBirthDate = "1360/11/12", Gender = "Male", Sheba = "820540102680020817909002",
        });
        await host.Ok(Address());

        var profile = await host.Ok(new GetProfileQuery());
        Assert.True(profile.ProfileCompleted && profile.AddressCompleted);
        Assert.Equal("علی", profile.FirstName);
        Assert.Equal("0013542419", profile.NationalCode);
        Assert.Equal("820540102680020817909002", profile.Sheba);
        Assert.Equal("تهران", profile.Province);
        Assert.Equal("02188888888", profile.TelephoneNumber);
        Assert.Equal(BnplTestHost.Start.UtcDateTime, profile.AddressUpdatedAt);

        // ویرایش همان ردیف را به‌روز می‌کند
        await host.Ok(Address("اصفهان", "03133333333"));
        Assert.Equal("اصفهان", (await host.Ok(new GetProfileQuery())).Province);
        Assert.Equal(1, await host.Db(db => db.CustomerProfiles.CountAsync()));
    }

    [Fact]
    public async Task Address_province_comes_from_the_list_and_phone_matches_its_area_code()
    {
        await using var host = await BnplTestHost.CreateAsync();
        var provinces = await host.Ok(new GetProvincesQuery());
        Assert.Equal(31, provinces.Count);
        Assert.Contains(provinces, p => p.Name == "فارس" && p.AreaCode == "071");

        Assert.Equal("استان را از فهرست انتخاب کنید.", (await host.Send(Address("تهرانپارس"))).Message);
        Assert.Equal("پیش‌شمارهٔ تلفن ثابت استان تهران ۰۲۱ است.", (await host.Send(Address("تهران", "03133333333"))).Message);
        Assert.Null(host.Profiles.LastAddress);

        await host.Ok(Address("كهگيلويه و بويراحمد", "07433333333"));   // ی و ک عربی
        Assert.Equal("کهگیلویه و بویراحمد", host.Profiles.LastAddress!.Province);
    }

    [Fact]
    public async Task Page_message_box_shows_the_active_message_or_nothing()
    {
        await using var host = await BnplTestHost.CreateAsync();
        var query = new GetPageMessageQuery { PageKey = "bnpl.new-request" };
        Assert.Null(await host.Ok(query));

        Assert.Equal(400, (await host.Send(new SavePageMessageCommand { PageKey = "صفحه", Text = "متن" })).StatusCode);
        Assert.Equal(400, (await host.Send(new SavePageMessageCommand { PageKey = "bnpl.x", Text = "متن پیام", Color = "red" })).StatusCode);

        var saved = await host.Ok(new SavePageMessageCommand
        {
            PageKey = "BNPL.New-Request", Text = "تا پایان آبان کارمزد قسط اول نصف است.", Kind = PageMessageKind.Notice, Color = "#1a7f37",
        });
        Assert.Equal("bnpl.new-request", saved.PageKey);
        var shown = await host.Ok(query);
        Assert.Equal(PageMessageKind.Notice, shown!.Kind);
        Assert.Equal("#1A7F37", shown.Color);

        // زمان‌بندی و غیرفعال‌سازی
        await host.Ok(new SavePageMessageCommand
        {
            Id = saved.Id, Text = saved.Text, Kind = saved.Kind, ValidTo = BnplTestHost.Start.UtcDateTime.AddDays(1), RowVersion = saved.RowVersion,
        });
        host.Time.Advance(TimeSpan.FromDays(2));
        Assert.Null(await host.Ok(query));
        Assert.Single((await host.Ok(new SearchPageMessagesQuery { PageKey = "new-request" })).Items);
    }
}
