using System.Net;
using System.Text.Encodings.Web;
using System.Web;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseHttpsRedirection();

var serviceName = app.Configuration.GetValue<string>("ServiceName");

app.MapGet("/login", async (HttpRequest request) =>
{
    var code = Guid.NewGuid().ToString("N");
    var callbackUrl = $"https://{serviceName}/callback?code={code}";
    var redirectUrl = $"http://zhlgd.whut.edu.cn/tpass/login?service={HttpUtility.UrlEncode(callbackUrl)}";
    return Results.Redirect(redirectUrl);
});

app.MapGet("/callback", async (string code, string ticket) =>
{

    ticket = ticket.Trim();
    var url = $"http://zhlgd.whut.edu.cn/tpass/serviceValidate?ticket={ticket}&service={serviceName}";
    var res = await new HttpClient().GetAsync(url);
    var doc = new XmlDocument();
    doc.Load(res.Content.ReadAsStream());
    var node = doc.SelectSingleNode("/cas:authenticationSuccess/cas:attributes");
    return node?.Value ?? "";
});

app.Run();
