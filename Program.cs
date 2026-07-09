using AlbansLodgingHouse.Data;
using AlbansLodgingHouse.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddScoped<BookingRepository>();
builder.Services.AddScoped<RoomTypeRepository>();
builder.Services.AddScoped<RoomTypeImageRepository>();
builder.Services.AddScoped<ManagementAccessService>();
builder.Services.AddScoped<ManagementEmailService>();
builder.Services.AddSingleton<QrCodeService>();
builder.Services.AddSingleton<BookingCardImageService>();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddScoped<BookingNotificationService>();
builder.Services.AddScoped<RoomImageStorageService>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 40 * 1024 * 1024; // up to 5 photos x 5 MB + form overhead
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapGet("/Booking/Card/{newId:guid}", async (
    Guid newId,
    BookingRepository bookingRepository,
    QrCodeService qrCodeService,
    BookingCardImageService cardImageService) =>
{
    var booking = await bookingRepository.GetByNewIdAsync(newId);
    if (booking is null) return Results.NotFound();

    var qrText = QrCodeService.BuildBookingQrText(booking);
    var qrBytes = qrCodeService.GeneratePngBytes(qrText);
    var jpegBytes = cardImageService.GenerateJpeg(booking, qrBytes);

    return Results.File(jpegBytes, "image/jpeg", $"{booking.BookingReferenceNo}.jpg");
});

app.Run();
