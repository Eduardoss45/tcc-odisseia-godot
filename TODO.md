# Atualizar README.md

# Detectar paredes

```cs
Vector2I coords = tileMap.LocalToMap(player.Position);
TileData tileData = tileMap.GetCellTileData(coords);

if (tileData != null)
{
    // Pega um dado personalizado configurado no TileSet (ex: "parede")
    var isWall = (bool)tileData.GetCustomData("parede");

    if (isWall)
    {
        GD.Print("O player está em uma parede!");
    }
}
```

# Criar padronização da malha de sprites (8x9, 8x1, etc..)