using Ngnix_Proxy;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Map("/auth", HandleMap);

app.Run();

static void HandleMap(IApplicationBuilder app)
{
    app.Run(context =>
    {
        Console.WriteLine("Request come\n");
        var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
        var config = configuration.GetSection("Proxy").Get<List<HostAddress>>();
        if(config != null && config.Any())
        {
            var recipient = context.Request.Headers["Auth-SMTP-To"].ToString();
            if(!string.IsNullOrEmpty(recipient))
            {
                Console.WriteLine("Recipient : {0}\n", recipient);
                var to = new System.Net.Mail.MailAddress(recipient);
                var server = config.FirstOrDefault(l => l.Host == to.Host);
                if(server != null)
                {
                    context.Response.Headers.Add("Auth-Status", "OK");
                    context.Response.Headers.Add("Auth-Server", server.Address);
                    context.Response.Headers.Add("Auth-Port", server.Port.ToString());
                    context.Response.Headers.Add("Auth-Pass", "plain-text-pass");
                    context.Response.StatusCode = 200;
                    return Task.CompletedTask;
                }
            }
            Console.WriteLine("Recipient not found\n");
        }
        context.Response.Headers.Add("Auth-Status", "Invalid configuration");
        context.Response.Headers.Add("Auth-Wait", "3");
        context.Response.StatusCode = 200;
        return Task.CompletedTask;
    });
}
