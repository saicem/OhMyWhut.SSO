using System.Web;
using System.Xml;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseHttpsRedirection();

var Scheme = app.Configuration.GetValue<string>("Scheme");
var ServiceHost = app.Configuration.GetValue<string>("ServiceHost");
var ServicePort = app.Configuration.GetValue<string>("ServicePort");

var serviceUrl = $"{Scheme}://{ServiceHost}:{ServicePort}";

//https://localhost/login?extra=default

app.MapGet("/login", (IMemoryCache cache, string extra) =>
{
    var code = Guid.NewGuid().ToString("N");
    cache.Set(code, extra);
    var callbackUrl = $"{serviceUrl}/callback?code={code}";
    var redirectUrl = $"http://zhlgd.whut.edu.cn/tpass/login?service={HttpUtility.UrlEncode(callbackUrl)}";
    return Results.Redirect(redirectUrl);
});

app.MapGet("/callback", async (IMemoryCache cache, string code, string ticket) =>
{
    var has = cache.TryGetValue(code, out string? extra);
    ticket = ticket.Trim();
    var url = $"http://zhlgd.whut.edu.cn/tpass/serviceValidate?ticket={ticket}&service={HttpUtility.UrlEncode($"{serviceUrl}/callback")}";
    var res = await new HttpClient().GetAsync(url);
    var doc = new XmlDocument();
    doc.Load(res.Content.ReadAsStream());
    var authNode = doc.DocumentElement?.FirstChild;
    if (authNode is null || authNode.Name == "cas:authenticationFailure")
    {
        return Results.BadRequest("Authentication Failed");
    }
    var attributesNode = authNode.LastChild;
    if (attributesNode is null || attributesNode.Name != "cas:attributes")
    {
        return Results.BadRequest("Unexpect xml");
    }
    var school = attributesNode.SelectSingleNode("descendant::*[local-name()='UNIT_NAME']")!.InnerText;
    var speciality = attributesNode.SelectSingleNode("descendant::*[local-name()='SPECIALTY_NAME']")!.InnerText;
    var studentId = attributesNode.SelectSingleNode("descendant::*[local-name()='ID_NUMBER']")!.InnerText;
    var cardId = attributesNode.SelectSingleNode("descendant::*[local-name()='CARDNO']")!.InnerText;
    var studentType = attributesNode.SelectSingleNode("descendant::*[local-name()='ID_TYPE_NAME']")!.InnerText;
    var grade = attributesNode.SelectSingleNode("descendant::*[local-name()='GRADE']")!.InnerText;
    var name = attributesNode.SelectSingleNode("descendant::*[local-name()='USER_NAME']")!.InnerText;
    var sex = attributesNode.SelectSingleNode("descendant::*[local-name()='USER_SEX_NAME']")!.InnerText;
    return Results.Ok(new
    {
        姓名 = name,
        性别 = sex,
        年级 = grade,
        学位 = studentType,
        学院 = school,
        专业 = speciality,
        学号 = studentId,
        校园卡号 = cardId,
    });
});

app.Run();
