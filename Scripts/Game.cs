using Godot;
using System;
using System.Collections.Generic;

public partial class Game : Node2D
{
    // Caminho para a cena do NPC (PackedScene)
    [Export]
    public string NpcScenePath { get; set; } = "res://Cenas/Npc.tscn";

    public override void _Ready()
    {
        // Config (poderia vir de JSON)
        var npcData = new Dictionary<string, object>()
        {
            {"SpriteSheetRows", 8},
            {"SpriteSheetCols", 9},
            {"SpriteSheetWidth", 512},
            {"SpriteSheetHeight", 561}, // ! altura da textura - 14 pixels 
            {"DialogTimelineName", "npc_dialogo_1"},
            {"CharacterResourcePath", "res://Chars/Npc.dch"},
            {"PlayerResourcePath", "res://Chars/Player.dch"},
            {"SpriteSheetPath", "res://Sprites/npc_spritesheet.png"},
            {"Position", new Vector2(200, 300)} // posição do NPC no mundo
        };

        LoadNpcFromData(npcData);
    }

    private void LoadNpcFromData(Dictionary<string, object> data)
    {
        PackedScene npcScene = GD.Load<PackedScene>(NpcScenePath);
        if (npcScene == null)
        {
            GD.PrintErr($"Falha ao carregar a cena NPC: {NpcScenePath}");
            return;
        }
        Npc npcInstance = npcScene.Instantiate<Npc>();

        // Configura propriedades do NPC via dados dinâmicos
        npcInstance.SpriteSheetRows = Convert.ToInt32(data["SpriteSheetRows"]);
        npcInstance.SpriteSheetCols = Convert.ToInt32(data["SpriteSheetCols"]);
        npcInstance.SpriteSheetWidth = Convert.ToInt32(data["SpriteSheetWidth"]);
        npcInstance.SpriteSheetHeight = Convert.ToInt32(data["SpriteSheetHeight"]);
        npcInstance.DialogTimelineName = data["DialogTimelineName"].ToString();
        npcInstance.CharacterResourcePath = data["CharacterResourcePath"].ToString();
        npcInstance.PlayerResourcePath = data["PlayerResourcePath"].ToString();
        npcInstance.SpriteSheetPath = data["SpriteSheetPath"].ToString();

        // Ajusta a posição no mundo, se fornecida
        if (data.ContainsKey("Position") && data["Position"] is Vector2 pos)
        {
            npcInstance.Position = pos;
        }
        AddChild(npcInstance);
    }
}
