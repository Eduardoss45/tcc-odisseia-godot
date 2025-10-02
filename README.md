# 📜 Alterações e Implementações

## 1. **Sistema de Carregamento Dinâmico de NPCs**

Agora, os NPCs do jogo não são mais criados com dados fixos no código.
Foi implementado um interpretador que lê um arquivo JSON (`npcs.json`) e instancia cada NPC com suas configurações específicas.

### **Vantagens**

- Fácil adição de novos NPCs sem modificar o código-fonte.
- Centralização das configurações em um único arquivo.
- Possibilidade de criar NPCs com sprites, posições e diálogos diferentes.
- Suporte a múltiplos NPCs na mesma cena.
- A sprite sheet é 0, 0 por padrão

---

## 2. **Uso do `Godot.FileAccess`**

- Removido o uso de `System.IO` para evitar conflito de nomes com `Godot.FileAccess`.
- `Godot.FileAccess` garante que os arquivos funcionem tanto no editor quanto no jogo exportado.
- Arquivos de configuração devem estar com **"Keep File"** ativado no Import Dock.

---

## 3. **Estrutura do Arquivo JSON**

Criado um formato padronizado para definir NPCs.
Exemplo de `npcs.json`:

```json
[
  {
    "SpriteSheetRows": 1,
    "SpriteSheetCols": 8,
    "SpriteSheetWidth": 512,
    "SpriteSheetHeight": 64,
    "DialogTimelineName": "npc_dialogo_1",
    "CharacterResourcePath": "res://Chars/Npc.dch",
    "PlayerResourcePath": "res://Chars/Player.dch",
    "SpriteSheetPath": "res://Sprites/player_spritesheet.png",
    "Position": [200, 300]
  },
  {
    // Ex: Copia
    "SpriteSheetRows": 1, // numero de linhas
    "SpriteSheetCols": 8, // numero de colunas
    "SpriteSheetWidth": 512, // Tamanho da sprite sheet
    "SpriteSheetHeight": 64, // Tamanho da sprite sheet
    "DialogTimelineName": "npc_dialogo_2", // Ex: "quest_tutorial"
    "CharacterResourcePath": "res://Chars/Npc.dch", // Ex: Guarda.dch
    "PlayerResourcePath": "res://Chars/Player.dch", // Ex: Odisseu.dch
    "SpriteSheetPath": "res://Sprites/player_spritesheet.png", // sprite sheet
    "Position": [400, 300] // posição no mundo em px
  }
]
```

---

## 4. **Classe `NpcData`**

- Criada uma classe interna no `Game.cs` para mapear diretamente os dados do JSON.
- Garante tipagem forte e facilita a manutenção do código.

---

## 5. **Função `LoadNpcFromData`**

- Responsável por:

  - Instanciar a cena base do NPC (`Npc.tscn`).
  - Preencher todas as propriedades do NPC.
  - Posicionar o NPC no mundo.
  - Adicionar o NPC à árvore de cena.

---

## 6. **Como Adicionar Novos NPCs**

1. Abrir o arquivo `npcs.json`.
2. Copiar e colar um bloco de NPC existente.
3. Alterar:

   - `DialogTimelineName` (nome da timeline de diálogo).
   - `Position` (posição no mapa).
   - Outras propriedades, se necessário.

4. Salvar e rodar o jogo — o NPC aparecerá automaticamente.

---

## 7. **Compatibilidade**

- **Editor Godot:** Totalmente funcional.
- **Jogo exportado:** Funciona desde que `npcs.json` esteja configurado como **Keep File**.
