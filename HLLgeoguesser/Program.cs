using System.Text.Json;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// Lubame staatilised failid (wwwroot)
var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

/* 
 * Varem lisatud /api/scenes:
 * Loeb failinimed wwwroot/Scenes/SME kaustast.
 */
string sceneFolder = Path.Combine(builder.Environment.WebRootPath, "Scenes", "SME");
app.MapGet("/api/scenes", () =>
{
    if (!Directory.Exists(sceneFolder))
    {
        return Results.NotFound("Scenes/SME folder not found!");
    }

    var files = Directory.EnumerateFiles(sceneFolder);
    var list = files.Select(path =>
    {
        var filename = Path.GetFileName(path);
        return Path.Combine("Scenes", "SME", filename).Replace("\\", "/");
    }).ToList();

    return Results.Ok(list);
});

/*
 * UUENDUS: POST /api/saveCoords
 * Saadab massiivi { filename, x, y }.
 * Salvestame need "wwwroot/scenesCoords.json".
 */
app.MapPost("/api/saveCoords", async (HttpContext http) =>
{
    using var reader = new StreamReader(http.Request.Body);
    var body = await reader.ReadToEndAsync();

    Console.WriteLine("Received JSON: " + body);

    try
    {
        // 🟢 MUUDATUS: JSON case-insensitive
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var scenes = JsonSerializer.Deserialize<List<SceneData>>(body, options);

        if (scenes == null)
        {
            Console.WriteLine("❌ Deserialization failed: scenes == null");
            return Results.BadRequest("Invalid JSON data.");
        }

        // ✅ Logime õigesti loetud väärtused
        foreach (var scene in scenes)
        {
            Console.WriteLine($"Parsed scene: Filename={scene.Filename}, X={scene.X}, Y={scene.Y}");
        }

        // ✅ Salvestame andmed
        string coordsFile = Path.Combine(builder.Environment.WebRootPath, "scenesCoords.json");
        var jsonToSave = JsonSerializer.Serialize(scenes, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(coordsFile, jsonToSave);

        Console.WriteLine("✅ Saved to scenesCoords.json!");
        return Results.Ok(new { message = "Coords saved to scenesCoords.json", count = scenes.Count });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ JSON parsing error: {ex.Message}");
        return Results.BadRequest("JSON parsing error.");
    }
});



app.Run();

/*
 * Abiklass massiivi deserialiseerimiseks
 */
public class SceneData
{
    public string? Filename { get; set; }
    public float? X { get; set; }
    public float? Y { get; set; }
}
