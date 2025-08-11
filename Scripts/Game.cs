using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class Game : Node2D
{
    [Export]
    public string NpcScenePath { get; set; } = "res://Cenas/Npc.tscn";

    [Export]
    public string NpcConfigPath { get; set; } = "res://Config/npcs.json";

    public override void _Ready()
    {
        // Abre o arquivo JSON com Godot.FileAccess
        using var file = Godot.FileAccess.Open(NpcConfigPath, Godot.FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"Não foi possível abrir o arquivo JSON: {NpcConfigPath}");
            return;
        }

        string jsonText = file.GetAsText();

        try
        {
            var npcList = JsonSerializer.Deserialize<List<NpcData>>(jsonText);

            foreach (var npcData in npcList)
                LoadNpcFromData(npcData);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Erro ao ler JSON: {ex.Message}");
        }
    }

    private void LoadNpcFromData(NpcData data)
    {
        var npcScene = GD.Load<PackedScene>(NpcScenePath);
        if (npcScene == null)
        {
            GD.PrintErr($"Falha ao carregar a cena NPC: {NpcScenePath}");
            return;
        }

        var npcInstance = npcScene.Instantiate<Npc>();

        npcInstance.SpriteSheetRows = data.SpriteSheetRows;
        npcInstance.SpriteSheetCols = data.SpriteSheetCols;
        npcInstance.SpriteSheetWidth = data.SpriteSheetWidth;
        npcInstance.SpriteSheetHeight = data.SpriteSheetHeight;
        npcInstance.DialogTimelineName = data.DialogTimelineName;
        npcInstance.CharacterResourcePath = data.CharacterResourcePath;
        npcInstance.PlayerResourcePath = data.PlayerResourcePath;
        npcInstance.SpriteSheetPath = data.SpriteSheetPath;
        npcInstance.Position = new Vector2(data.Position[0], data.Position[1]);

        AddChild(npcInstance);
    }

    private class NpcData
    {
        public int SpriteSheetRows { get; set; }
        public int SpriteSheetCols { get; set; }
        public int SpriteSheetWidth { get; set; }
        public int SpriteSheetHeight { get; set; }
        public string DialogTimelineName { get; set; }
        public string CharacterResourcePath { get; set; }
        public string PlayerResourcePath { get; set; }
        public string SpriteSheetPath { get; set; }
        public float[] Position { get; set; }
    }
}
